using System;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityStocks
{
    public static class Downloader
    {
        static Downloader()
        {
            ServicePointManager.ServerCertificateValidationCallback = (p1,p2,p3,p4) => true;
        }

        /** async file download */
        public static void Download(UnityWebRequest client, Action<byte[]> callback, Action<string> errorCallback)
        {
            Debug.LogFormat("downloading url={0}", client.url);

            var handler = new Handler(data =>
            {
                if (client.isNetworkError || client.responseCode != 200 || data == null)
                    errorCallback(string.Format("download failed statusCode={0} error={1} url={2}", client.responseCode, client.error, client.url));
                else
                    callback(data);
            });
            
            client.downloadHandler = handler;
            client.SendWebRequest();
        }

        private class Handler : DownloadHandlerScript
        {
            private readonly Action<byte[]> _callback;
            private byte[] _data;

            public Handler(Action<byte[]> callback)
            {
                _callback = callback;
            }

            protected override void CompleteContent()
            {
                _callback(_data);
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (_data == null)
                {
                    _data = data;
                }
                else
                {
                    var oldLength = _data.Length;
                    Array.Resize(ref _data, _data.Length + dataLength);
                    Array.Copy(data, 0, _data, oldLength, dataLength);
                }
                return true;
            }
        }
    }
}
