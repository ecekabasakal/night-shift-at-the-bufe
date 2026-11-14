using NUnit.Framework;

namespace NightShift.Tests
{
    // The mistake counter and which of the four endings a night gets.
    public class MistakesAndEndingsTests : SessionTestBase
    {
        // Makes wrong decisions on ordinary ("mistake") customers until the given number of mistakes, then plays correctly.
        // Returns the customer of the last mistake.
        private CustomerData PlayNightWithMistakes(int mistakes)
        {
            CustomerData last = null;
            session.StartNight();
            while (!session.IsFinished)
            {
                CustomerData customer = session.CurrentCustomer;
                if (session.Mistakes < mistakes && customer.wrongOutcome == "mistake")
                {
                    last = customer;
                    session.Decide(SessionDriver.WrongActions(customer)[0]);
                    session.Advance();
                }
                else
                {
                    SessionDriver.ServeCorrectly(session);
                }
            }
            return last;
        }

        [Test]
        public void MaxMistakes_ComesFromBalance()
        {
            Assert.That(session.MaxMistakes, Is.EqualTo(data.Balance.maxMistakes));
        }

        [Test]
        public void TheLastAllowedMistake_EndsTheNightAsFired_OnlyAfterContinue()
        {
            int max = session.MaxMistakes;
            session.StartNight();

            for (int i = 1; i <= max; i++)
            {
                CustomerData customer = session.CurrentCustomer;
                Assert.That(customer.wrongOutcome, Is.EqualTo("mistake"), "This test expects the first customers to be ordinary mistakes");

                DecisionResult result = session.Decide(SessionDriver.WrongActions(customer)[0]);

                Assert.That(session.Mistakes, Is.EqualTo(i));
                Assert.That(result.EndsNight, Is.EqualTo(i == max));
                Assert.That(session.IsFinished, Is.False, "The feedback line comes before the ending");
                session.Advance();
                Assert.That(session.IsFinished, Is.EqualTo(i == max));
            }

            Assert.That(session.Ending, Is.EqualTo(EndingId.Fired));
            Assert.That(session.Mistakes, Is.EqualTo(max));
            Assert.That(session.ClockMinute, Is.EqualTo(data.Customers[max - 1].arrivalMinute), "The clock stays where the night ended");
        }

        [Test]
        public void OneMistakeFewerThanTheLimit_ThenACleanFinish_IsMorning()
        {
            int allowed = session.MaxMistakes - 1;

            PlayNightWithMistakes(allowed);

            Assert.That(session.Mistakes, Is.EqualTo(allowed));
            Assert.That(session.Ending, Is.EqualTo(EndingId.Morning));
        }

        [Test]
        public void ReachingTheLimit_AnywhereInTheNight_IsFired()
        {
            CustomerData last = PlayNightWithMistakes(session.MaxMistakes);

            Assert.That(session.Ending, Is.EqualTo(EndingId.Fired));
            Assert.That(session.ClockMinute, Is.EqualTo(last.arrivalMinute));
        }

        [Test]
        public void CleanNight_WithoutShift_IsMorning()
        {
            session.StartNight();
            SessionDriver.FinishNightCorrectly(session);

            Assert.That(session.Ending, Is.EqualTo(EndingId.Morning));
            Assert.That(session.Mistakes, Is.EqualTo(0));
            Assert.That(session.TotalShiftSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void CleanNight_WithShiftTimeJustUnderTheThreshold_IsMorning()
        {
            float threshold = data.Balance.changedShiftSecondsThreshold;
            session.StartNight();
            SessionDriver.LookInBursts(session, threshold - 0.5f);
            SessionDriver.FinishNightCorrectly(session);

            Assert.That(session.TotalShiftSeconds, Is.EqualTo(threshold - 0.5f).Within(0.05f));
            Assert.That(session.Ending, Is.EqualTo(EndingId.Morning));
        }

        [Test]
        public void CleanNight_WithShiftTimeJustOverTheThreshold_IsChanged()
        {
            float threshold = data.Balance.changedShiftSecondsThreshold;
            session.StartNight();
            SessionDriver.LookInBursts(session, threshold + 0.5f);
            SessionDriver.FinishNightCorrectly(session);

            Assert.That(session.TotalShiftSeconds, Is.EqualTo(threshold + 0.5f).Within(0.05f));
            Assert.That(session.Ending, Is.EqualTo(EndingId.Changed));
            Assert.That(session.Mistakes, Is.EqualTo(0));
        }

        [Test]
        public void MistakesBelowTheLimit_WithHeavyShiftUse_IsStillChanged()
        {
            session.StartNight();
            SessionDriver.LookInBursts(session, data.Balance.changedShiftSecondsThreshold + 0.5f);
            if (session.MaxMistakes > 1)
            {
                session.Decide(SessionDriver.WrongActions(session.CurrentCustomer)[0]);
                session.Advance();
            }
            SessionDriver.FinishNightCorrectly(session);

            Assert.That(session.Ending, Is.EqualTo(EndingId.Changed));
        }

        [Test]
        public void FiredAfterHeavyShiftUse_IsFired_NotChanged()
        {
            session.StartNight();
            SessionDriver.LookInBursts(session, data.Balance.changedShiftSecondsThreshold + 0.5f);
            while (!session.IsFinished)
            {
                session.Decide(SessionDriver.WrongActions(session.CurrentCustomer)[0]);
                session.Advance();
            }

            Assert.That(session.Ending, Is.EqualTo(EndingId.Fired));
        }

        [Test]
        public void FullMeter_IsSeen()
        {
            session.StartNight();
            SessionDriver.Hold(session, 60f, true);

            Assert.That(session.IsFinished, Is.True);
            Assert.That(session.Ending, Is.EqualTo(EndingId.Seen));
        }

        [Test]
        public void InstantEndCustomers_EndTheNightWithTheirOwnEnding_WithoutAMistake()
        {
            foreach (CustomerData customer in data.Customers)
            {
                if (customer.wrongOutcome != "instantEnd") continue;

                SessionDriver.StartNightAt(session, customer.id);
                session.Decide(SessionDriver.WrongActions(customer)[0]);
                session.Advance();

                Assert.That(session.IsFinished, Is.True, customer.id);
                Assert.That(DataParsing.ToId(session.Ending), Is.EqualTo(customer.instantEndingId), customer.id);
                Assert.That(session.Mistakes, Is.EqualTo(0), customer.id);
            }
        }
    }
}
