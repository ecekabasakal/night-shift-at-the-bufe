using System;
using NUnit.Framework;

namespace NightShift.Tests
{
    // The Noticed meter: fill rates, drain, clamping, carry-over, the Seen ending and the Shift-seconds total.
    public class NoticedMeterTests : SessionTestBase
    {
        private const float Tolerance = 0.002f;

        private CustomerData OrdinaryHuman()
        {
            return TestData.First(data, c => !c.isOther && !c.staresBack, "an ordinary human");
        }

        private CustomerData OtherThatDoesNotStare()
        {
            return TestData.First(data, c => c.isOther && !c.staresBack, "an other that does not stare back");
        }

        private CustomerData Starer()
        {
            return TestData.First(data, c => c.staresBack, "a starer");
        }

        private void AssertFillsAt(CustomerData customer, float ratePerSecond)
        {
            SessionDriver.StartNightAt(session, customer.id);
            Assert.That(session.Noticed, Is.EqualTo(0f), "The meter should be empty on arrival when Shift was never held");

            SessionDriver.Hold(session, 0.5f, true);

            Assert.That(session.Noticed, Is.EqualTo(Math.Min(1f, ratePerSecond * 0.5f)).Within(Tolerance));
        }

        [Test]
        public void ShiftHeld_OnAnOrdinaryHuman_FillsAtTheBaseRate()
        {
            AssertFillsAt(OrdinaryHuman(), data.Balance.noticedFillPerSecond);
        }

        [Test]
        public void ShiftHeld_OnAnOther_FillsAtTheOtherRate()
        {
            AssertFillsAt(OtherThatDoesNotStare(), data.Balance.noticedFillPerSecondOther);
        }

        [Test]
        public void ShiftHeld_OnAStarer_FillsAtTheStareRate()
        {
            AssertFillsAt(Starer(), data.Balance.noticedFillPerSecondStare);
        }

        [Test]
        public void ShiftReleased_DrainsAtTheDrainRate()
        {
            session.StartNight();
            SessionDriver.Hold(session, 3f, true);
            float before = session.Noticed;

            SessionDriver.Hold(session, 1f, false);

            Assert.That(session.Noticed, Is.EqualTo(before - data.Balance.noticedDrainPerSecond).Within(Tolerance));
        }

        [Test]
        public void Meter_NeverGoesBelowZero()
        {
            session.StartNight();
            SessionDriver.Hold(session, 0.5f, true);

            session.Tick(1000f, false);

            Assert.That(session.Noticed, Is.EqualTo(0f));
            Assert.That(session.IsFinished, Is.False);
        }

        [Test]
        public void HugeDelta_ClampsAtExactlyOne_AndEndsTheNightAsSeen()
        {
            session.StartNight();

            session.Tick(1000f, true);

            Assert.That(session.Noticed, Is.EqualTo(1f));
            Assert.That(session.IsFinished, Is.True);
            Assert.That(session.Ending, Is.EqualTo(EndingId.Seen));
        }

        [Test]
        public void Meter_CarriesOverToTheNextCustomer()
        {
            session.StartNight();
            SessionDriver.Hold(session, 2f, true);
            float before = session.Noticed;
            Assert.That(before, Is.GreaterThan(0f));

            SessionDriver.ServeCorrectly(session);

            Assert.That(session.CustomerIndex, Is.EqualTo(1));
            Assert.That(session.Noticed, Is.EqualTo(before));
        }

        [Test]
        public void HoldingShift_UntilTheMeterIsFull_EndsTheNightAsSeenImmediately()
        {
            session.StartNight();
            CustomerData customer = session.CurrentCustomer;
            float expectedSeconds = 1f / data.Balance.noticedFillPerSecond;

            float held = 0f;
            while (!session.IsFinished && held < expectedSeconds * 2f)
            {
                session.Tick(SessionDriver.Step, true);
                held += SessionDriver.Step;
            }

            Assert.That(session.IsFinished, Is.True, "No decision or Continue is needed for this ending");
            Assert.That(session.Ending, Is.EqualTo(EndingId.Seen));
            Assert.That(held, Is.EqualTo(expectedSeconds).Within(SessionDriver.Step * 1.5f));
            Assert.That(session.Noticed, Is.EqualTo(1f));
            Assert.That(session.Mistakes, Is.EqualTo(0));
            Assert.That(session.ClockMinute, Is.EqualTo(customer.arrivalMinute), "The clock stays where the night ended");
            Assert.That(session.CurrentCustomer, Is.Null);
            Assert.That(session.IsShiftActive, Is.False, "Shift vision is off once the night is over");
        }

        [Test]
        public void Meter_KeepsRunningDuringFeedback_AndSeenWinsOverAPendingEnding()
        {
            CustomerData instant = TestData.First(data, c => c.wrongOutcome == "instantEnd" && c.instantEndingId == "fired", "an instant 'fired' customer");
            SessionDriver.StartNightAt(session, instant.id);
            DecisionResult result = session.Decide(SessionDriver.WrongActions(instant)[0]);
            Assert.That(result.EndsNight, Is.True);

            session.Tick(1000f, true);

            Assert.That(session.IsFinished, Is.True);
            Assert.That(session.Ending, Is.EqualTo(EndingId.Seen));
        }

        [Test]
        public void TotalShiftSeconds_CountsOnlyTheTimeShiftIsHeld_AcrossCustomers()
        {
            session.StartNight();
            SessionDriver.Hold(session, 1.5f, true);
            SessionDriver.Hold(session, 2f, false);
            SessionDriver.ServeCorrectly(session);
            SessionDriver.Hold(session, 0.5f, true);
            SessionDriver.Hold(session, 3f, false);

            Assert.That(session.TotalShiftSeconds, Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void IsShiftActive_FollowsTheShiftKey_AndIsStaredAtOnlyOnAStarer()
        {
            session.StartNight();
            Assert.That(session.CurrentCustomer.staresBack, Is.False, "The first customer should not be a starer");
            session.Tick(SessionDriver.Step, true);
            Assert.That(session.IsShiftActive, Is.True);
            Assert.That(session.IsStaredAt, Is.False);
            session.Tick(SessionDriver.Step, false);
            Assert.That(session.IsShiftActive, Is.False);

            SessionDriver.StartNightAt(session, Starer().id);
            Assert.That(session.IsStaredAt, Is.False, "Not stared at until Shift is held");
            session.Tick(SessionDriver.Step, true);
            Assert.That(session.IsStaredAt, Is.True);
            session.Tick(SessionDriver.Step, false);
            Assert.That(session.IsStaredAt, Is.False);
        }
    }
}
