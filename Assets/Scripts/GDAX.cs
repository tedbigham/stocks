using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace UnityStocks
{
    public class GDAX : MonoBehaviour
    {
        public Text TotalUSD;
        public GameObject Content;
        public CurrencyRow RowPrefab;

        public string apiKey;
        public string password;
        public string privateKey;

        private DateTime Epoch = new DateTime(1970, 1, 1);
        private byte[] privateKeyBytes;
        private long nextRefresh = 0;
        private const long RefreshIntervalSeconds = 10;

        private Dictionary<string, Account> Accounts = new Dictionary<string, Account>();
        private Dictionary<string, Ticker> Tickers = new Dictionary<string, Ticker>();
        private Dictionary<string, CurrencyRow> Rows = new Dictionary<string, CurrencyRow>();

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
                    row.Price.text = "";
                    RefreshTotal();
                } else {
                    var path = string.Format("/products/{0}-USD/ticker", a.currency);

                    Get<Ticker>(path, ticker => {
                        Tickers[id] = ticker;
                        var price = double.Parse(ticker.price);
                        var holding = price * double.Parse(a.balance);

                        var row = GetRow(a.currency);
                        row.Price.text = price.ToString("C");
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
            return row;
        }

        private void RefreshTotal()
        {
            double total = 0;
            foreach(var t in Tickers) {
                total += double.Parse(t.Value.price) * double.Parse(Accounts[t.Key].balance);
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
                Debug.LogFormat("received response url={0} size={1}", req.url, response.Length);
                var json = Encoding.UTF8.GetString(response);
                Debug.LogFormat("json={0}", json);
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
    }
}