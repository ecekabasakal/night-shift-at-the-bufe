using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NightShift.Tests
{
    // Loads the real game data (Assets/Game/Resources/Data/*.json) for every test, always fresh from disk.
    public static class TestData
    {
        public static GameData Load()
        {
            return GameData.LoadFromResources();
        }

        // strings.json as written, so tests can see duplicates and empty values (GameData only exposes lookups).
        public static StringEntry[] LoadStringEntries()
        {
            TextAsset asset = Resources.Load<TextAsset>("Data/strings");
            Assert.That(asset, Is.Not.Null, "Resources/Data/strings.json is missing");
            StringTable table = JsonUtility.FromJson<StringTable>(asset.text);
            return table != null && table.entries != null ? table.entries : new StringEntry[0];
        }

        public static IEnumerable<string> CustomerIds()
        {
            foreach (CustomerData customer in Load().Customers) yield return customer.id;
        }

        // First customer matching the predicate; fails the test when the data has none.
        public static CustomerData First(GameData data, Predicate<CustomerData> match, string description)
        {
            CustomerData found = Array.Find(data.Customers, match);
            Assert.That(found, Is.Not.Null, "The data has no customer that is " + description);
            return found;
        }
    }

    // Small helpers that drive an IGameSession the way GameBootstrap does: fixed-step ticks, Decide, Advance.
    public static class SessionDriver
    {
        public const float Step = 0.1f; // GameBootstrap's largest frame step

        // Ticks for the given time with Shift held or released. Stops early if the night ends.
        public static void Hold(IGameSession session, float seconds, bool shiftHeld)
        {
            int frames = (int)Math.Round(seconds / Step);
            for (int i = 0; i < frames && !session.IsFinished; i++) session.Tick(Step, shiftHeld);
        }

        // Looks with Shift for the given total time in short bursts, letting the meter drain in between,
        // so the meter never gets near 1.
        public static void LookInBursts(IGameSession session, float seconds)
        {
            int frames = (int)Math.Round(seconds / Step);
            while (frames > 0 && !session.IsFinished)
            {
                if (session.Noticed > 0.5f)
                {
                    while (session.Noticed > 0f) session.Tick(Step, false);
                }
                else
                {
                    session.Tick(Step, true);
                    frames--;
                }
            }
        }

        // Correct decision for the current customer, then Continue.
        public static void ServeCorrectly(IGameSession session)
        {
            session.Decide(session.CurrentCustomer.GetCorrectAction());
            session.Advance();
        }

        // Starts a night and serves everyone correctly (no Shift) until the given customer is at the counter.
        public static CustomerData StartNightAt(IGameSession session, string customerId)
        {
            session.StartNight();
            while (!session.IsFinished && session.CurrentCustomer.id != customerId) ServeCorrectly(session);
            Assert.That(session.IsFinished, Is.False, "Customer '" + customerId + "' never arrived");
            return session.CurrentCustomer;
        }

        // Serves every remaining customer correctly without Shift.
        public static void FinishNightCorrectly(IGameSession session)
        {
            while (!session.IsFinished) ServeCorrectly(session);
        }

        // The buttons this customer shows: Sell, Refuse and, if they have a specialActionKey, Special.
        public static List<PlayerAction> AvailableActions(CustomerData customer)
        {
            List<PlayerAction> actions = new List<PlayerAction> { PlayerAction.Sell, PlayerAction.Refuse };
            if (customer.HasSpecialAction()) actions.Add(PlayerAction.Special);
            return actions;
        }

        public static List<PlayerAction> WrongActions(CustomerData customer)
        {
            List<PlayerAction> actions = AvailableActions(customer);
            actions.Remove(customer.GetCorrectAction());
            return actions;
        }
    }

    // Base class for tests that need a fresh session over the real data.
    public abstract class SessionTestBase
    {
        protected GameData data;
        protected IGameSession session;

        [SetUp]
        public void CreateSession()
        {
            data = TestData.Load();
            session = new GameSession(data);
        }
    }
}
