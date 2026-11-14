using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NightShift.Tests
{
    // A scripted bot plays whole nights with different strategies: every ending must be reachable and the rules must matter.
    public class FullNightPlaythroughTests : SessionTestBase
    {
        private const float GlanceSeconds = 0.3f;  // what a player who read rule 6 does at a starer
        private const float CarefulLookSeconds = 2f;
        private const float HeavyLookSeconds = 3.5f;

        private EndingId Play(NightBot bot)
        {
            EndingId ending = bot.Play(session);
            Debug.Log(bot.Trace.ToString());
            return ending;
        }

        private static float LookFor(CustomerData customer, float seconds)
        {
            return customer.staresBack ? GlanceSeconds : seconds;
        }

        private static PlayerAction FirstWrongAction(CustomerData customer)
        {
            return SessionDriver.WrongActions(customer)[0];
        }

        public static IEnumerable<TestCaseData> InstantEndWrongActions()
        {
            foreach (CustomerData customer in TestData.Load().Customers)
            {
                if (customer.wrongOutcome != "instantEnd") continue;
                foreach (PlayerAction action in SessionDriver.WrongActions(customer)) yield return new TestCaseData(customer.id, action);
            }
        }

        [Test]
        public void CarefulPerfectPlay_ReachesMorning()
        {
            NightBot bot = new NightBot("careful perfect play (2 s of Shift per customer, a glance at starers)");
            bot.ShiftSeconds = c => LookFor(c, CarefulLookSeconds);

            Assert.That(Play(bot), Is.EqualTo(EndingId.Morning));
            Assert.That(session.Mistakes, Is.EqualTo(0));
            Assert.That(session.TotalShiftSeconds, Is.LessThanOrEqualTo(data.Balance.changedShiftSecondsThreshold));
            Assert.That(session.ClockMinute, Is.EqualTo(data.Balance.shiftLengthMinutes));
        }

        [Test]
        public void PerfectDecisions_WithHeavyShiftUse_ReachesChanged()
        {
            NightBot bot = new NightBot("perfect decisions, heavy Shift use (3.5 s per customer, a glance at starers)");
            bot.ShiftSeconds = c => LookFor(c, HeavyLookSeconds);

            Assert.That(Play(bot), Is.EqualTo(EndingId.Changed));
            Assert.That(session.Mistakes, Is.EqualTo(0));
            Assert.That(session.Noticed, Is.LessThan(1f));
            Assert.That(session.TotalShiftSeconds, Is.GreaterThan(data.Balance.changedShiftSecondsThreshold));
        }

        [Test]
        public void HoldingShiftUntilTheMeterFills_ReachesSeen()
        {
            NightBot bot = new NightBot("never lets go of Shift");
            bot.ShiftSeconds = c => 60f;

            Assert.That(Play(bot), Is.EqualTo(EndingId.Seen));
            Assert.That(session.CustomerIndex, Is.EqualTo(-1));
            Assert.That(session.ClockMinute, Is.EqualTo(data.Customers[0].arrivalMinute), "Caught at the first customer");
        }

        [Test]
        public void PerfectDecisions_ButAnOrdinaryLookAtTheStarer_ReachesSeen()
        {
            CustomerData starer = TestData.First(data, c => c.staresBack, "a starer");
            NightBot bot = new NightBot("perfect decisions, but looks at the starer as long as at anyone (2 s)");
            bot.ShiftSeconds = c => CarefulLookSeconds;

            Assert.That(Play(bot), Is.EqualTo(EndingId.Seen));
            Assert.That(session.ClockMinute, Is.EqualTo(starer.arrivalMinute));
            Assert.That(session.Mistakes, Is.EqualTo(0));
        }

        [Test]
        public void AlwaysWrongDecisions_ReachesFired()
        {
            NightBot bot = new NightBot("always presses a wrong button");
            bot.ChooseAction = FirstWrongAction;

            Assert.That(Play(bot), Is.EqualTo(EndingId.Fired));
            Assert.That(session.Mistakes, Is.EqualTo(session.MaxMistakes));
        }

        [TestCaseSource(nameof(InstantEndWrongActions))]
        public void PerfectPlay_UntilOneWrongButtonOnAnInstantEndCustomer_ReachesThatCustomersEnding(string customerId, PlayerAction wrongAction)
        {
            CustomerData target = System.Array.Find(data.Customers, c => c.id == customerId);
            NightBot bot = new NightBot("perfect play, then " + wrongAction + " on " + customerId);
            bot.ShiftSeconds = c => LookFor(c, CarefulLookSeconds);
            bot.ChooseAction = c => c.id == customerId ? wrongAction : c.GetCorrectAction();

            EndingId ending = Play(bot);

            Assert.That(DataParsing.ToId(ending), Is.EqualTo(target.instantEndingId));
            Assert.That(session.Mistakes, Is.EqualTo(0));
            Assert.That(session.ClockMinute, Is.EqualTo(target.arrivalMinute));
        }

        [Test]
        public void EveryEndingIsReachable()
        {
            HashSet<EndingId> reached = new HashSet<EndingId>();

            NightBot careful = new NightBot("careful");
            careful.ShiftSeconds = c => LookFor(c, CarefulLookSeconds);
            reached.Add(careful.Play(session));

            NightBot heavy = new NightBot("heavy Shift");
            heavy.ShiftSeconds = c => LookFor(c, HeavyLookSeconds);
            reached.Add(heavy.Play(session));

            NightBot starer = new NightBot("holds Shift");
            starer.ShiftSeconds = c => 60f;
            reached.Add(starer.Play(session));

            NightBot wrong = new NightBot("always wrong");
            wrong.ChooseAction = FirstWrongAction;
            reached.Add(wrong.Play(session));

            foreach (EndingId ending in System.Enum.GetValues(typeof(EndingId)))
            {
                Assert.That(reached.Contains(ending), Is.True, "No bot reached the ending " + ending);
            }
        }

        [TestCase(PlayerAction.Sell)]
        [TestCase(PlayerAction.Refuse)]
        public void PressingTheSameButtonAllNight_DoesNotReachMorning(PlayerAction onlyAction)
        {
            NightBot bot = new NightBot("always " + onlyAction + ", never looks");
            bot.ChooseAction = c => onlyAction;

            EndingId ending = Play(bot);

            Assert.That(ending, Is.Not.EqualTo(EndingId.Morning), "The rules must matter");
            Assert.That(ending, Is.Not.EqualTo(EndingId.Changed), "The rules must matter");
        }
    }
}
