using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UnityStocks
{
    public class SettingsController : MonoBehaviour
    {
        public InputField ApiKey;
        public InputField Password;
        public InputField PrivateKey;

        void Start()
        {
            ApiKey.text = GDAX.Settings.ApiKey;
            Password.text = GDAX.Settings.Password;
            PrivateKey.text = GDAX.Settings.PrivateKey;
        }

        public void Close()
        {
            SceneManager.LoadScene(0);
        }

        public void Save()
        {
            GDAX.Settings.ApiKey = ApiKey.text;
            GDAX.Settings.Password = Password.text;
            GDAX.Settings.PrivateKey = PrivateKey.text;

            File.WriteAllText(GDAX.SettingsFile, JsonConvert.SerializeObject(GDAX.Settings));

            SceneManager.LoadScene(0);
        }
    }
}