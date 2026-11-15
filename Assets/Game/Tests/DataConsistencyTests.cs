using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace NightShift.Tests
{
    // Checks the four JSON files against Docs/DATA_SCHEMA.md and the design brief; protects later data edits and translations.
    public class DataConsistencyTests
    {
        private static readonly string[] Actions = { "sell", "refuse", "special" };
        private static readonly string[] WrongOutcomes = { "mistake", "instantEnd" };
        private static readonly string[] InstantEndings = { "seen", "fired" };
        private static readonly string[] Kinds = { "human", "cat" };
        private static readonly string[] Accessories = { "none", "whistle", "glasses", "bag", "scarf" };
        private static readonly string[] Items = { "tost", "ayran", "simit", "gevrek", "boyoz", "kumru", "cigarettes", "water", "matches", "cekirdek", "catfood", "none" };
        private static readonly string[] AnomalyVocabulary =
        {
            Anomalies.NoShadow, Anomalies.ReversedShadow, Anomalies.NoEyes, Anomalies.WrongEyes, Anomalies.ExtraFingers,
            Anomalies.TooManyTeeth, Anomalies.NoWhistle, Anomalies.Hollow, Anomalies.Staring, Anomalies.TallCat
        };
        private static readonly string[] RequiredKeys =
        {
            "ui.title", "ui.tagline", "ui.shiftHint", "ui.start", "ui.sell", "ui.refuse", "ui.notebookTitle", "ui.notebookHint",
            "ui.noticed", "ui.mistakes", "ui.playAgain", "ui.continue", "ui.controls", "intro.note",
            "ui.keyEnter", "ui.notebookClose", "ui.soundOn", "ui.soundOff", // used by the UI code
            // v2
            "item.gevrek", "item.boyoz", "item.kumru", "rule.r8_gevrek",
            "handover.option1", "handover.option2", "handover.option3",
            HandoverNotes.PromptKey, HandoverNotes.DoneKey, HandoverNotes.HeadingKey
        };
        private static readonly Regex HexColor = new Regex("^#[0-9A-Fa-f]{6}$");
        private const int AyranCutoffMinute = 240; // 03:00
        private const int CatMistakeFromMinute = 240; // 03:00
        private const string GevrekRuleId = "r8_gevrek";
        private const int MaxHandoverNoteLength = 110;

        private GameData data;
        private CustomerData[] customers;
        private List<string> problems;

        [SetUp]
        public void LoadData()
        {
            data = TestData.Load();
            customers = data.Customers;
            problems = new List<string>();
        }

        // Every test collects all its findings and reports them together.
        private void AssertNoProblems()
        {
            Assert.That(problems, Is.Empty, string.Join("\n", problems.ToArray()));
        }

        private void Check(bool ok, string problem)
        {
            if (!ok) problems.Add(problem);
        }

        private static bool OneOf(string value, string[] allowed)
        {
            return Array.IndexOf(allowed, value) >= 0;
        }

        // ---- customers.json ----

        [Test]
        public void Customers_AreBetween10And14()
        {
            Assert.That(customers.Length, Is.InRange(10, 14));
        }

        [Test]
        public void Customers_HaveUniqueNonEmptyIds()
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (CustomerData c in customers)
            {
                Check(!string.IsNullOrEmpty(c.id), "A customer has an empty id");
                Check(seen.Add(c.id ?? ""), "Duplicate customer id: " + c.id);
            }
            AssertNoProblems();
        }

        [Test]
        public void Customers_ArriveInStrictlyIncreasingOrder_Within0To419()
        {
            int previous = -1;
            foreach (CustomerData c in customers)
            {
                Check(c.arrivalMinute >= 0 && c.arrivalMinute <= 419, c.id + ": arrivalMinute " + c.arrivalMinute + " is outside 0-419");
                Check(c.arrivalMinute > previous, c.id + ": arrivalMinute " + c.arrivalMinute + " is not after the previous customer (" + previous + ")");
                previous = c.arrivalMinute;
            }
            AssertNoProblems();
        }

        [Test]
        public void Customers_EnumLikeStringsAreInTheSchemaVocabulary()
        {
            foreach (CustomerData c in customers)
            {
                Check(OneOf(c.correctAction, Actions), c.id + ": correctAction '" + c.correctAction + "'");
                Check(OneOf(c.wrongOutcome, WrongOutcomes), c.id + ": wrongOutcome '" + c.wrongOutcome + "'");
                Check(OneOf(c.requestItem, Items), c.id + ": requestItem '" + c.requestItem + "'");
                Check(c.appearance != null, c.id + ": appearance is missing");
                if (c.appearance == null) continue;
                Check(OneOf(c.appearance.kind, Kinds), c.id + ": appearance.kind '" + c.appearance.kind + "'");
                Check(OneOf(c.appearance.accessory, Accessories), c.id + ": appearance.accessory '" + c.appearance.accessory + "'");
                Check(c.appearance.hairStyle >= 0 && c.appearance.hairStyle <= 4, c.id + ": appearance.hairStyle " + c.appearance.hairStyle);
                Check(c.appearance.outfit >= 0 && c.appearance.outfit <= 3, c.id + ": appearance.outfit " + c.appearance.outfit);
            }
            AssertNoProblems();
        }

        [Test]
        public void Customers_InstantEnd_IsUsedAtMostTwice_AndAlwaysNamesSeenOrFired()
        {
            int instantEnds = 0;
            foreach (CustomerData c in customers)
            {
                if (c.wrongOutcome == "instantEnd")
                {
                    instantEnds++;
                    Check(OneOf(c.instantEndingId, InstantEndings), c.id + ": instantEnd with instantEndingId '" + c.instantEndingId + "'");
                }
                else
                {
                    Check(string.IsNullOrEmpty(c.instantEndingId), c.id + ": instantEndingId '" + c.instantEndingId + "' set without wrongOutcome instantEnd");
                }
            }
            Check(instantEnds <= 2, instantEnds + " customers use instantEnd (max 2)");
            AssertNoProblems();
        }

        [Test]
        public void Customers_ShiftAnomaliesAreInTheSchemaVocabulary()
        {
            foreach (CustomerData c in customers)
            {
                Check(c.shiftAnomalies != null, c.id + ": shiftAnomalies is missing (use [])");
                if (c.shiftAnomalies == null) continue;
                foreach (string anomaly in c.shiftAnomalies)
                {
                    Check(OneOf(anomaly, AnomalyVocabulary), c.id + ": unknown anomaly '" + anomaly + "'");
                }
            }
            AssertNoProblems();
        }

        [Test]
        public void Customers_StaresBack_GoesTogetherWithTheStaringAnomaly()
        {
            foreach (CustomerData c in customers)
            {
                Check(c.staresBack == c.HasAnomaly(Anomalies.Staring),
                    c.id + ": staresBack=" + c.staresBack + " but 'staring' anomaly present=" + c.HasAnomaly(Anomalies.Staring));
            }
            AssertNoProblems();
        }

        [Test]
        public void Customers_ColoursAreValidHex()
        {
            foreach (CustomerData c in customers)
            {
                if (c.appearance == null) { problems.Add(c.id + ": appearance is missing"); continue; }
                CheckColor(c.id, "skinColor", c.appearance.skinColor);
                CheckColor(c.id, "hairColor", c.appearance.hairColor);
                CheckColor(c.id, "outfitColor", c.appearance.outfitColor);
            }
            AssertNoProblems();
        }

        private void CheckColor(string id, string field, string value)
        {
            Check(value != null && HexColor.IsMatch(value), id + ": " + field + " '" + value + "' is not #RRGGBB");
        }

        [Test]
        public void Customers_RepeatOf_PointsToAnEarlierCustomerWithIdenticalAppearance()
        {
            for (int i = 0; i < customers.Length; i++)
            {
                CustomerData c = customers[i];
                if (string.IsNullOrEmpty(c.repeatOf)) continue;

                int target = Array.FindIndex(customers, other => other.id == c.repeatOf);
                Check(target >= 0, c.id + ": repeatOf '" + c.repeatOf + "' does not exist");
                if (target < 0) continue;
                Check(target < i, c.id + ": repeatOf '" + c.repeatOf + "' does not come earlier in the night");

                Appearance a = c.appearance, b = customers[target].appearance;
                bool same = a != null && b != null && a.kind == b.kind && a.skinColor == b.skinColor && a.hairColor == b.hairColor
                    && a.hairStyle == b.hairStyle && a.outfitColor == b.outfitColor && a.outfit == b.outfit && a.accessory == b.accessory;
                Check(same, c.id + ": appearance differs from '" + c.repeatOf + "'");
            }
            AssertNoProblems();
        }

        [Test]
        public void Customers_WhoseCorrectActionIsSpecial_HaveASpecialButton()
        {
            foreach (CustomerData c in customers)
            {
                if (c.correctAction == "special") Check(c.HasSpecialAction(), c.id + ": correctAction is special but specialActionKey is empty");
            }
            AssertNoProblems();
        }

        // ---- strings.json ----

        [Test]
        public void Customers_EveryTextKeyExistsInStrings()
        {
            foreach (CustomerData c in customers)
            {
                CheckKey(c.id, "nameKey", c.nameKey, true);
                CheckKey(c.id, "greetingKey", c.greetingKey, true);
                CheckKey(c.id, "requestKey", c.requestKey, true);
                CheckKey(c.id, "shiftHintKey", c.shiftHintKey, true);
                CheckKey(c.id, "correctResultKey", c.correctResultKey, true);
                CheckKey(c.id, "wrongResultKey", c.wrongResultKey, true);
                CheckKey(c.id, "specialActionKey", c.specialActionKey, false);
                Check(data.Has("item." + c.requestItem), c.id + ": strings.json has no 'item." + c.requestItem + "'");
            }
            AssertNoProblems();
        }

        private void CheckKey(string id, string field, string key, bool required)
        {
            if (string.IsNullOrEmpty(key))
            {
                Check(!required, id + ": " + field + " is empty");
                return;
            }
            Check(data.Has(key), id + ": " + field + " '" + key + "' is not in strings.json");
        }

        [Test]
        public void Strings_RequiredUiKeysAndIntroNoteExist()
        {
            foreach (string key in RequiredKeys) Check(data.Has(key), "Missing required key: " + key);
            AssertNoProblems();
        }

        [Test]
        public void Strings_EveryEndingHasTitleAndBody()
        {
            foreach (EndingId ending in Enum.GetValues(typeof(EndingId)))
            {
                Check(data.Has(DataParsing.EndingTitleKey(ending)), "Missing " + DataParsing.EndingTitleKey(ending));
                Check(data.Has(DataParsing.EndingBodyKey(ending)), "Missing " + DataParsing.EndingBodyKey(ending));
            }
            AssertNoProblems();
        }

        [Test]
        public void Strings_HaveNoEmptyValuesAndNoDuplicateKeys()
        {
            StringEntry[] entries = TestData.LoadStringEntries();
            Assert.That(entries, Is.Not.Empty, "strings.json has no entries");

            HashSet<string> seen = new HashSet<string>();
            foreach (StringEntry entry in entries)
            {
                Check(!string.IsNullOrEmpty(entry.key), "An entry has an empty key (value: '" + entry.value + "')");
                Check(!string.IsNullOrEmpty(entry.value) && entry.value.Trim().Length > 0, "Empty value for key: " + entry.key);
                Check(seen.Add(entry.key ?? ""), "Duplicate key: " + entry.key);
            }
            AssertNoProblems();
        }

        // ---- rules.json ----

        [Test]
        public void Rules_AreBetween6And8_WithUniqueIds()
        {
            Assert.That(data.Rules.Length, Is.InRange(6, 8));
            HashSet<string> seen = new HashSet<string>();
            foreach (RuleData rule in data.Rules)
            {
                Check(!string.IsNullOrEmpty(rule.id), "A rule has an empty id");
                Check(seen.Add(rule.id ?? ""), "Duplicate rule id: " + rule.id);
            }
            AssertNoProblems();
        }

        [Test]
        public void Rules_EveryTextKeyExistsInStrings()
        {
            foreach (RuleData rule in data.Rules) Check(data.Has(rule.textKey), rule.id + ": textKey '" + rule.textKey + "' is not in strings.json");
            AssertNoProblems();
        }

        [Test]
        public void Rules_EveryRuleIsUsedByACustomer_AndEveryRuleIdOnACustomerExists()
        {
            foreach (RuleData rule in data.Rules)
            {
                Check(Array.Exists(customers, c => c.ruleId == rule.id), "Rule '" + rule.id + "' is not used by any customer");
            }
            foreach (CustomerData c in customers)
            {
                if (string.IsNullOrEmpty(c.ruleId)) continue;
                Check(Array.Exists(data.Rules, rule => rule.id == c.ruleId), c.id + ": ruleId '" + c.ruleId + "' does not exist");
            }
            AssertNoProblems();
        }

        // ---- balance.json ----

        [Test]
        public void Balance_ValuesArePositive()
        {
            BalanceData b = data.Balance;
            Check(b.noticedFillPerSecond > 0f, "noticedFillPerSecond must be > 0");
            Check(b.noticedFillPerSecondOther > 0f, "noticedFillPerSecondOther must be > 0");
            Check(b.noticedFillPerSecondStare > 0f, "noticedFillPerSecondStare must be > 0");
            Check(b.noticedDrainPerSecond > 0f, "noticedDrainPerSecond must be > 0");
            Check(b.changedShiftSecondsThreshold > 0f, "changedShiftSecondsThreshold must be > 0");
            Check(b.shiftLengthMinutes > 0, "shiftLengthMinutes must be > 0");
            Check(b.maxMistakes >= 1, "maxMistakes must be >= 1");
            AssertNoProblems();
        }

        [Test]
        public void Balance_FillRatesAreOrdered_Base_Other_Stare()
        {
            BalanceData b = data.Balance;
            Assert.That(b.noticedFillPerSecond, Is.LessThan(b.noticedFillPerSecondOther));
            Assert.That(b.noticedFillPerSecondOther, Is.LessThan(b.noticedFillPerSecondStare));
        }

        [Test]
        public void Balance_ShiftEndsAfterTheLastCustomerArrives()
        {
            Assert.That(customers, Is.Not.Empty);
            Assert.That(data.Balance.shiftLengthMinutes, Is.GreaterThan(customers[customers.Length - 1].arrivalMinute));
        }

        // ---- design invariants from the brief ----

        [Test]
        public void Design_FirstThreeCustomersAreOrdinaryHumansToSellTo()
        {
            Assert.That(customers.Length, Is.GreaterThanOrEqualTo(3));
            for (int i = 0; i < 3; i++)
            {
                CustomerData c = customers[i];
                bool ordinary = !c.isOther && !c.staresBack && c.appearance != null && c.appearance.kind == "human"
                    && (c.shiftAnomalies == null || c.shiftAnomalies.Length == 0);
                Check(ordinary, c.id + ": customer " + (i + 1) + " is not an ordinary human");
                Check(c.correctAction == "sell", c.id + ": customer " + (i + 1) + " should be a plain sale, is '" + c.correctAction + "'");
            }
            AssertNoProblems();
        }

        [Test]
        public void Design_AyranIsSoldBefore0300_AndRefusedFrom0300()
        {
            Check(Array.Exists(customers, c => c.requestItem == "ayran" && c.arrivalMinute < AyranCutoffMinute && c.correctAction == "sell"),
                "No ayran request before 03:00 with correctAction sell");
            Check(Array.Exists(customers, c => c.requestItem == "ayran" && c.arrivalMinute >= AyranCutoffMinute && c.correctAction == "refuse"),
                "No ayran request at/after 03:00 with correctAction refuse");
            foreach (CustomerData c in customers)
            {
                if (c.requestItem == "ayran" && c.arrivalMinute >= AyranCutoffMinute)
                    Check(c.correctAction != "sell", c.id + ": sells ayran after 03:00, against the notebook rule");
            }
            AssertNoProblems();
        }

        [Test]
        public void Design_SpecialButtonIsCorrectAtLeastOnce_AndADecoyAtLeastOnce()
        {
            Check(Array.Exists(customers, c => c.HasSpecialAction() && c.correctAction == "special"), "No customer for whom the special button is correct");
            Check(Array.Exists(customers, c => c.HasSpecialAction() && c.correctAction != "special"), "No decoy special button");
            AssertNoProblems();
        }

        // ---- v2: street cat, gevrek rule, shift handover (DATA_SCHEMA.md "Addendum v2") ----

        [Test]
        public void Design_TheCatReactsToEveryOtherExceptOneAfter0300_AndToNoHuman()
        {
            List<CustomerData> missedOthers = new List<CustomerData>();
            foreach (CustomerData c in customers)
            {
                if (!c.isOther) Check(!c.catReacts, c.id + ": the cat reacts to an ordinary human");
                else if (!c.catReacts) missedOthers.Add(c);
            }

            Check(missedOthers.Count == 1, missedOthers.Count + " 'other' customers have catReacts false (the cat makes exactly 1 mistake)");
            foreach (CustomerData c in missedOthers)
            {
                Check(c.arrivalMinute >= CatMistakeFromMinute, c.id + ": the cat's mistake is at minute " + c.arrivalMinute + ", before 03:00");
            }
            AssertNoProblems();
        }

        [Test]
        public void Design_SimitIsRequestedByExactlyTwoOthers_RefusedUnderTheGevrekRule()
        {
            int simitRequests = 0;
            foreach (CustomerData c in customers)
            {
                if (c.requestItem != "simit") continue;
                simitRequests++;
                Check(c.isOther, c.id + ": asks for simit but is an ordinary human");
                Check(c.ruleId == GevrekRuleId, c.id + ": asks for simit with ruleId '" + c.ruleId + "'");
                Check(c.correctAction == "refuse", c.id + ": asks for simit with correctAction '" + c.correctAction + "'");
            }
            Check(simitRequests == 2, simitRequests + " customers ask for simit (must be exactly 2)");
            AssertNoProblems();
        }

        [Test]
        public void Design_AnOrdinaryHumanAsksForGevrek_AndIsSoldIt()
        {
            Check(Array.Exists(customers, c => !c.isOther && c.requestItem == "gevrek" && c.ruleId == GevrekRuleId && c.correctAction == "sell"),
                "No ordinary human requests gevrek with ruleId " + GevrekRuleId + " and correctAction sell");
            foreach (CustomerData c in customers)
            {
                if (c.isOther || c.requestItem != "gevrek") continue;
                Check(data.Get(c.requestKey).IndexOf("gevrek", StringComparison.OrdinalIgnoreCase) >= 0,
                    c.id + ": requests gevrek but the request line does not say 'gevrek'");
            }
            AssertNoProblems();
        }

        [Test]
        public void Design_NoOrdinaryHumanSaysSimit()
        {
            foreach (CustomerData c in customers)
            {
                if (c.isOther) continue;
                Check(!SaysSimit(c.greetingKey), c.id + ": greeting line says 'simit'");
                Check(!SaysSimit(c.requestKey), c.id + ": request line says 'simit'");
            }
            AssertNoProblems();
        }

        private bool SaysSimit(string key)
        {
            return data.Get(key).IndexOf("simit", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        [Test]
        public void Rules_TheGevrekRuleExists_AndNeedsNoShift()
        {
            RuleData rule = Array.Find(data.Rules, r => r.id == GevrekRuleId);
            Assert.That(rule, Is.Not.Null, "rules.json has no " + GevrekRuleId);
            Assert.That(rule.needsShift, Is.False, GevrekRuleId + " is decided by what the customer says, not by Shift");
            Assert.That(rule.textKey, Is.EqualTo("rule." + GevrekRuleId));
        }

        [Test]
        public void Strings_HandoverNotesAreAtMost110Characters()
        {
            for (int i = 0; i < HandoverNotes.OptionCount; i++)
            {
                string key = HandoverNotes.OptionKey(i);
                Check(data.Has(key), "Missing required key: " + key);
                Check(data.Get(key).Length <= MaxHandoverNoteLength, key + " is " + data.Get(key).Length + " characters (max " + MaxHandoverNoteLength + ")");
            }
            AssertNoProblems();
        }

        [Test]
        public void Strings_PreviousNoteHeadingIsExact()
        {
            Assert.That(data.Get("ui.previousNote"), Is.EqualTo("Previous shift's note"));
        }
    }
}
