using System.Collections.Generic;
using UnityEngine;

namespace NightShift
{
    // Everything loaded from the four JSON files: customers, rules, balance numbers and texts.
    // Created once at startup and never changed afterwards.
    public class GameData
    {
        public CustomerData[] Customers;
        public RuleData[] Rules;
        public BalanceData Balance;

        private readonly Dictionary<string, string> strings = new Dictionary<string, string>();

        // Runtime path: reads Assets/Game/Resources/Data/*.json.
        public static GameData LoadFromResources()
        {
            return FromJson(
                LoadText("Data/customers"),
                LoadText("Data/rules"),
                LoadText("Data/strings"),
                LoadText("Data/balance"));
        }

        // Test path: build the data from JSON text directly.
        public static GameData FromJson(string customersJson, string rulesJson, string stringsJson, string balanceJson)
        {
            GameData data = new GameData();

            CustomerList customerList = JsonUtility.FromJson<CustomerList>(customersJson);
            RuleList ruleList = JsonUtility.FromJson<RuleList>(rulesJson);
            StringTable stringTable = JsonUtility.FromJson<StringTable>(stringsJson);
            BalanceData balance = JsonUtility.FromJson<BalanceData>(balanceJson);

            data.Customers = (customerList != null && customerList.customers != null) ? customerList.customers : new CustomerData[0];
            data.Rules = (ruleList != null && ruleList.rules != null) ? ruleList.rules : new RuleData[0];
            data.Balance = balance != null ? balance : new BalanceData();

            if (stringTable != null && stringTable.entries != null)
            {
                for (int i = 0; i < stringTable.entries.Length; i++)
                {
                    StringEntry entry = stringTable.entries[i];
                    if (entry != null && !string.IsNullOrEmpty(entry.key))
                    {
                        data.strings[entry.key] = entry.value;
                    }
                }
            }

            return data;
        }

        // Text for a strings.json key. "" for an empty key (the schema uses "" for "none"),
        // "#key#" when the key is missing so the gap is visible on screen.
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            string value;
            if (strings.TryGetValue(key, out value)) return value;
            return "#" + key + "#";
        }

        // True when strings.json contains the key.
        public bool Has(string key)
        {
            return !string.IsNullOrEmpty(key) && strings.ContainsKey(key);
        }

        // Minutes after 23:00 -> "HH:MM" on a 24h clock. 0 -> "23:00", 60 -> "00:00", 420 -> "06:00".
        public static string FormatClock(int minutesAfter23)
        {
            int total = (23 * 60 + minutesAfter23) % (24 * 60);
            if (total < 0) total += 24 * 60;
            int hours = total / 60;
            int minutes = total % 60;
            return hours.ToString("00") + ":" + minutes.ToString("00");
        }

        private static string LoadText(string resourcePath)
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogError("Missing data file: Resources/" + resourcePath + ".json");
                return "{}";
            }
            return asset.text;
        }
    }
}
