using NUnit.Framework;

namespace NightShift.Tests
{
    // Calls that arrive at the wrong moment (stray key presses, double clicks, Play again) must be harmless.
    public class CallOrderTests : SessionTestBase
    {
        private void AssertStartUpState()
        {
            Assert.That(session.Step, Is.EqualTo(ServingStep.NotStarted));
            Assert.That(session.CurrentCustomer, Is.Null);
            Assert.That(session.CustomerIndex, Is.EqualTo(-1));
            Assert.That(session.Noticed, Is.EqualTo(0f));
            Assert.That(session.Mistakes, Is.EqualTo(0));
            Assert.That(session.TotalShiftSeconds, Is.EqualTo(0f));
            Assert.That(session.ClockMinute, Is.EqualTo(0));
            Assert.That(session.IsShiftActive, Is.False);
            Assert.That(session.IsStaredAt, Is.False);
            Assert.That(session.IsFinished, Is.False);
        }

        [Test]
        public void NewSession_IsInTheStartUpState()
        {
            AssertStartUpState();
            Assert.That(session.CustomerCount, Is.EqualTo(data.Customers.Length));
        }

        [Test]
        public void BeforeStartNight_TickDecideAndAdvanceDoNothing()
        {
            session.Tick(5f, true);
            session.Decide(PlayerAction.Sell);
            session.Advance();

            Assert.That(session.CanDecide(PlayerAction.Sell), Is.False);
            AssertStartUpState();
        }

        [Test]
        public void StartNight_BringsTheFirstCustomer()
        {
            session.StartNight();

            Assert.That(session.Step, Is.EqualTo(ServingStep.AwaitingDecision));
            Assert.That(session.CustomerIndex, Is.EqualTo(0));
            Assert.That(session.CurrentCustomer, Is.SameAs(data.Customers[0]));
        }

        [Test]
        public void SecondDecide_ForTheSameCustomer_IsIgnored()
        {
            session.StartNight();
            CustomerData customer = session.CurrentCustomer;
            DecisionResult first = session.Decide(SessionDriver.WrongActions(customer)[0]);

            Assert.That(session.CanDecide(customer.GetCorrectAction()), Is.False);
            DecisionResult second = session.Decide(customer.GetCorrectAction());
            session.Decide(SessionDriver.WrongActions(customer)[0]);

            Assert.That(session.Mistakes, Is.EqualTo(1), "Only the first decision counts");
            Assert.That(second.Correct, Is.EqualTo(first.Correct));
            Assert.That(second.FeedbackKey, Is.EqualTo(first.FeedbackKey));
            Assert.That(session.Step, Is.EqualTo(ServingStep.ShowingFeedback));
            Assert.That(session.CurrentCustomer, Is.SameAs(customer));
        }

        [Test]
        public void Advance_BeforeADecision_DoesNotSkipTheCustomer()
        {
            session.StartNight();

            session.Advance();
            session.Advance();

            Assert.That(session.CustomerIndex, Is.EqualTo(0));
            Assert.That(session.Step, Is.EqualTo(ServingStep.AwaitingDecision));
        }

        [Test]
        public void DoubleAdvance_AfterADecision_MovesOnlyOneCustomer()
        {
            session.StartNight();
            session.Decide(session.CurrentCustomer.GetCorrectAction());

            session.Advance();
            session.Advance();

            Assert.That(session.CustomerIndex, Is.EqualTo(1));
            Assert.That(session.Step, Is.EqualTo(ServingStep.AwaitingDecision));
        }

        [Test]
        public void Special_OnACustomerWithoutASpecialButton_IsIgnored()
        {
            CustomerData customer = TestData.First(data, c => !c.HasSpecialAction(), "without a special button");
            SessionDriver.StartNightAt(session, customer.id);

            Assert.That(session.CanDecide(PlayerAction.Special), Is.False);
            session.Decide(PlayerAction.Special);

            Assert.That(session.Step, Is.EqualTo(ServingStep.AwaitingDecision), "The customer is still waiting for a real decision");
            Assert.That(session.Mistakes, Is.EqualTo(0));
            Assert.That(session.CurrentCustomer, Is.SameAs(customer));
        }

        [Test]
        public void AfterTheNightEnded_NothingChangesAnymore()
        {
            session.StartNight();
            session.Decide(SessionDriver.WrongActions(session.CurrentCustomer)[0]);
            session.Advance();
            session.Tick(1000f, true); // Seen
            Assert.That(session.IsFinished, Is.True);
            int mistakes = session.Mistakes;
            int clock = session.ClockMinute;
            float shiftSeconds = session.TotalShiftSeconds;

            session.Tick(1000f, false);
            session.Tick(5f, true);
            session.Decide(PlayerAction.Sell);
            session.Decide(PlayerAction.Refuse);
            session.Advance();

            Assert.That(session.CanDecide(PlayerAction.Sell), Is.False);
            Assert.That(session.IsFinished, Is.True);
            Assert.That(session.Ending, Is.EqualTo(EndingId.Seen));
            Assert.That(session.Mistakes, Is.EqualTo(mistakes));
            Assert.That(session.ClockMinute, Is.EqualTo(clock));
            Assert.That(session.Noticed, Is.EqualTo(1f), "The meter keeps its value after the night");
            Assert.That(session.TotalShiftSeconds, Is.EqualTo(shiftSeconds));
            Assert.That(session.CurrentCustomer, Is.Null);
            Assert.That(session.CustomerIndex, Is.EqualTo(-1));
            Assert.That(session.IsShiftActive, Is.False);
        }

        [Test]
        public void Reset_MidNight_ClearsEverything_AndTheNextNightIsClean()
        {
            session.StartNight();
            SessionDriver.Hold(session, 3f, true);
            session.Decide(SessionDriver.WrongActions(session.CurrentCustomer)[0]);
            session.Advance();
            session.Decide(SessionDriver.WrongActions(session.CurrentCustomer)[0]); // left in ShowingFeedback, Shift still active

            session.Reset();
            AssertStartUpState();

            session.StartNight();
            Assert.That(session.CustomerIndex, Is.EqualTo(0));
            SessionDriver.FinishNightCorrectly(session);

            Assert.That(session.Ending, Is.EqualTo(EndingId.Morning));
            Assert.That(session.Mistakes, Is.EqualTo(0));
            Assert.That(session.TotalShiftSeconds, Is.EqualTo(0f));
            Assert.That(session.ClockMinute, Is.EqualTo(data.Balance.shiftLengthMinutes));
        }

        [Test]
        public void StartNight_AfterALostNight_StartsFromScratch()
        {
            session.StartNight();
            session.Tick(1000f, true); // Seen
            Assert.That(session.IsFinished, Is.True);

            session.StartNight();

            Assert.That(session.IsFinished, Is.False);
            Assert.That(session.CustomerIndex, Is.EqualTo(0));
            Assert.That(session.Noticed, Is.EqualTo(0f));
            Assert.That(session.Mistakes, Is.EqualTo(0));
            Assert.That(session.TotalShiftSeconds, Is.EqualTo(0f));
            Assert.That(session.ClockMinute, Is.EqualTo(data.Customers[0].arrivalMinute));
        }
    }
}
