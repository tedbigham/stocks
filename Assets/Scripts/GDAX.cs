using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace UnityStocks
{
    public class GDAX : MonoBehaviour, IComparer<CurrencyRow>
    {
        public Text TotalUSD;
        public GameObject Content;
        public CurrencyRow RowPrefab;

        public long RefreshIntervalSeconds = 10;
        public string apiKey;
        public string password;
        public string privateKey;

        private DateTime Epoch = new DateTime(1970, 1, 1);
        private byte[] privateKeyBytes;
        private long nextRefresh;

        private Dictionary<string, Account> Accounts = new Dictionary<string, Account>();
        //private Dictionary<string, Ticker> Tickers = new Dictionary<string, Ticker>();
        private SortedDictionary<string, CurrencyRow> Rows = new SortedDictionary<string, CurrencyRow>();

        void Start()
        {
            TotalUSD.text = "...";
            privateKeyBytes = Convert.FromBase64String(privateKey);
        }

        private void RefreshAccounts()
        {
            Get<Account[]>("/accounts", accounts => {
                foreach(var a in accounts) {
                    Accounts[a.currency] = a;
                }
                RefreshTickers();
            });
            nextRefresh = Now() + RefreshIntervalSeconds;
        }

        private void RefreshTickers()
        {
            foreach(var a in Accounts.Values) {
                var id = a.currency;
                if(id == "USD") {
                    var row = GetRow(a.currency);
                    row.Holding.text = double.Parse(a.available).ToString("C");
                    row.PriceText.text = "";
                    RefreshTotal();
                } else {
                    var path = string.Format("/products/{0}-USD/ticker", a.currency);
                    var account = a;
                    Get<Ticker>(path, ticker => {
                        //Tickers[id] = ticker;
                        var price = double.Parse(ticker.price);
                        var holding = price * double.Parse(a.balance);

                        var row = GetRow(account.currency);
                        row.Price = double.Parse(ticker.price);
                        ;
                        row.PriceText.text = price.ToString("C");
                        row.Holding.text = holding.ToString("C");
                        RefreshTotal();
                    });
                }
            }
        }

        private CurrencyRow GetRow(string symbol)
        {
            if(Rows.ContainsKey(symbol))
                return Rows[symbol];

            var row = Instantiate(RowPrefab);
            var cr = row.GetComponent<CurrencyRow>();
            cr.transform.SetParent(Content.transform, false);
            cr.Symbol.text = symbol;
            Rows[symbol] = row;

            var i = 0;
            foreach (var r in Rows)
                r.Value.transform.SetSiblingIndex(i++);

            return row;
        }

        private void RefreshTotal()
        {
            double total = 0;
            foreach(var r in Rows) {
                total += r.Value.Price * double.Parse(Accounts[r.Key].balance);
            }
            TotalUSD.text = total.ToString("C");
        }

        private void Get<T>(string path, Action<T> callback)
        {
            var url = string.Format("https://api.gdax.com{0}", path);
            var ts = Now().ToString();
            var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("CB-ACCESS-KEY", apiKey);
            req.SetRequestHeader("CB-ACCESS-PASSPHRASE", password);
            req.SetRequestHeader("CB-ACCESS-TIMESTAMP", ts);
            req.SetRequestHeader("CB-ACCESS-SIGN", Sign(ts, "GET", path));

            Downloader.Download(req, response => {
                var json = Encoding.UTF8.GetString(response);
                Debug.LogFormat("received response url={0} json={1}", req.url, json);
                var obj = JsonConvert.DeserializeObject<T>(json);
                callback(obj);
            },
            err => {
                Debug.LogErrorFormat("network error url={0} err={1}", req.url, err);
            });
        }

        private string Sign(string timestamp, string method, string requestPath, string body = "")
        {
            var all = timestamp + method + requestPath + body;
            var hmac = new HMACSHA256(privateKeyBytes);
            var sig = hmac.ComputeHash(Encoding.ASCII.GetBytes(all));
            return Convert.ToBase64String(sig);
        }

        private long Now()
        {
            return (long)(DateTime.UtcNow - Epoch).TotalSeconds;
        }

        void Update()
        {
            if(Now() > nextRefresh) {
                RefreshAccounts();
            }
        }

        public int Compare(CurrencyRow x, CurrencyRow y)
        {
            throw new NotImplementedException();
        }
    }
}