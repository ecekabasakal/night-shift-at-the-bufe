using System;

namespace NightShift
{
    // Root object of customers.json. Array order = order of arrival.
    [Serializable]
    public class CustomerList
    {
        public CustomerData[] customers;
    }

    // One customer of the night, exactly as written in customers.json (see Docs/DATA_SCHEMA.md).
    // The fields are raw JSON values; the small helper methods below parse the string enums.
    [Serializable]
    public class CustomerData
    {
        public string id;
        public string nameKey;
        public int arrivalMinute;
        public string requestItem;
        public string greetingKey;
        public string requestKey;
        public string shiftHintKey;
        public bool isOther;
        public bool staresBack;
        public string ruleId;
        public string correctAction;
        public string specialActionKey;
        public string wrongOutcome;
        public string instantEndingId;
        public string correctResultKey;
        public string wrongResultKey;
        public string repeatOf;
        public Appearance appearance;
        public string[] shiftAnomalies;

        // True when this customer has a third (Special) button.
        public bool HasSpecialAction()
        {
            return !string.IsNullOrEmpty(specialActionKey);
        }

        public PlayerAction GetCorrectAction()
        {
            return DataParsing.ParseAction(correctAction);
        }

        public WrongOutcome GetWrongOutcome()
        {
            return DataParsing.ParseWrongOutcome(wrongOutcome);
        }

        // Only meaningful when GetWrongOutcome() is InstantEnd.
        public EndingId GetInstantEnding()
        {
            return DataParsing.ParseEnding(instantEndingId);
        }

        // True when the Shift view of this customer contains the given anomaly (see Anomalies).
        public bool HasAnomaly(string anomaly)
        {
            if (shiftAnomalies == null) return false;
            for (int i = 0; i < shiftAnomalies.Length; i++)
            {
                if (shiftAnomalies[i] == anomaly) return true;
            }
            return false;
        }
    }

    // How a customer looks in the normal view. Colours are "#RRGGBB" strings (see DataParsing.ParseColor).
    [Serializable]
    public class Appearance
    {
        public string kind;        // "human" | "cat"
        public string skinColor;
        public string hairColor;
        public int hairStyle;      // 0 bald, 1 short, 2 long, 3 bun, 4 cap/hat
        public string outfitColor;
        public int outfit;         // 0 t-shirt, 1 jacket, 2 hoodie, 3 uniform (bekci)
        public string accessory;   // "none" | "whistle" | "glasses" | "bag" | "scarf"
    }

    // Root object of rules.json. Array order = order in the notebook.
    [Serializable]
    public class RuleList
    {
        public RuleData[] rules;
    }

    // One line of the rule notebook.
    [Serializable]
    public class RuleData
    {
        public string id;
        public string textKey;
        public bool needsShift;
    }

    // Root object of strings.json: every player-visible text in the game.
    [Serializable]
    public class StringTable
    {
        public StringEntry[] entries;
    }

    // One key/value pair of strings.json.
    [Serializable]
    public class StringEntry
    {
        public string key;
        public string value;
    }

    // Root object of balance.json: all tuning numbers.
    [Serializable]
    public class BalanceData
    {
        public float noticedFillPerSecond;
        public float noticedFillPerSecondOther;
        public float noticedFillPerSecondStare;
        public float noticedDrainPerSecond;
        public int maxMistakes;
        public float changedShiftSecondsThreshold;
        public int shiftLengthMinutes;
    }

    // The allowed values of CustomerData.shiftAnomalies, as constants so nobody mistypes them.
    public static class Anomalies
    {
        public const string NoShadow = "noShadow";
        public const string ReversedShadow = "reversedShadow";
        public const string NoEyes = "noEyes";
        public const string WrongEyes = "wrongEyes";
        public const string ExtraFingers = "extraFingers";
        public const string TooManyTeeth = "tooManyTeeth";
        public const string NoWhistle = "noWhistle";
        public const string Hollow = "hollow";
        public const string Staring = "staring";
        public const string TallCat = "tallCat";
    }
}
