using System.Collections.Generic;
using NUnit.Framework;

namespace NightShift.Tests
{
    // One test case per customer (and per wrong button): what the correct and the wrong decisions do.
    public class CustomerDecisionTests : SessionTestBase
    {
        public static IEnumerable<string> AllCustomers()
        {
            return TestData.CustomerIds();
        }

        public static IEnumerable<TestCaseData> AllWrongActions()
        {
            foreach (CustomerData customer in TestData.Load().Customers)
            {
                foreach (PlayerAction action in SessionDriver.WrongActions(customer)) yield return new TestCaseData(customer.id, action);
            }
        }

        [TestCaseSource(nameof(AllCustomers))]
        public void CorrectAction_IsCorrect_AddsNoMistake_AndTheNightGoesOn(string customerId)
        {
            CustomerData customer = SessionDriver.StartNightAt(session, customerId);
            int index = session.CustomerIndex;
            PlayerAction action = customer.GetCorrectAction();
            Assert.That(session.CanDecide(action), Is.True, "The correct button is not on offer");

            DecisionResult result = session.Decide(action);

            Assert.That(result.Correct, Is.True);
            Assert.That(result.Action, Is.EqualTo(action));
            Assert.That(result.FeedbackKey, Is.EqualTo(customer.correctResultKey));
            Assert.That(result.EndsNight, Is.False);
            Assert.That(session.Mistakes, Is.EqualTo(0));
            Assert.That(session.Step, Is.EqualTo(ServingStep.ShowingFeedback));
            Assert.That(session.CurrentCustomer, Is.SameAs(customer), "The customer stays during the feedback line");

            session.Advance();

            if (index == session.CustomerCount - 1)
            {
                Assert.That(session.IsFinished, Is.True);
                Assert.That(session.Ending, Is.EqualTo(EndingId.Morning));
            }
            else
            {
                Assert.That(session.IsFinished, Is.False);
                Assert.That(session.CustomerIndex, Is.EqualTo(index + 1));
                Assert.That(session.Step, Is.EqualTo(ServingStep.AwaitingDecision));
            }
        }

        [TestCaseSource(nameof(AllWrongActions))]
        public void WrongAction_IsAMistake_OrEndsTheNightWithTheCustomersEnding(string customerId, PlayerAction action)
        {
            CustomerData customer = SessionDriver.StartNightAt(session, customerId);
            int index = session.CustomerIndex;
            Assert.That(session.CanDecide(action), Is.True);

            DecisionResult result = session.Decide(action);

            Assert.That(result.Correct, Is.False);
            Assert.That(result.FeedbackKey, Is.EqualTo(customer.wrongResultKey));
            Assert.That(session.IsFinished, Is.False, "The feedback line is shown before any ending");
            Assert.That(session.Step, Is.EqualTo(ServingStep.ShowingFeedback));

            if (customer.wrongOutcome == "instantEnd")
            {
                Assert.That(session.Mistakes, Is.EqualTo(0), "An instant end adds no mistake");
                Assert.That(result.EndsNight, Is.True);

                session.Advance();

                Assert.That(session.IsFinished, Is.True);
                Assert.That(DataParsing.ToId(session.Ending), Is.EqualTo(customer.instantEndingId));
                Assert.That(session.Mistakes, Is.EqualTo(0));
                Assert.That(session.ClockMinute, Is.EqualTo(customer.arrivalMinute), "The clock stays where the night ended");
                return;
            }

            Assert.That(session.Mistakes, Is.EqualTo(1));
            bool fired = session.MaxMistakes == 1;
            Assert.That(result.EndsNight, Is.EqualTo(fired));

            session.Advance();

            if (fired)
            {
                Assert.That(session.Ending, Is.EqualTo(EndingId.Fired));
            }
            else if (index == session.CustomerCount - 1)
            {
                Assert.That(session.IsFinished, Is.True);
                Assert.That(session.Ending, Is.EqualTo(EndingId.Morning));
            }
            else
            {
                Assert.That(session.IsFinished, Is.False);
                Assert.That(session.CustomerIndex, Is.EqualTo(index + 1));
            }
        }

        [TestCaseSource(nameof(AllCustomers))]
        public void SpecialButton_IsOfferedOnlyWhenTheCustomerHasOne(string customerId)
        {
            CustomerData customer = SessionDriver.StartNightAt(session, customerId);

            Assert.That(session.CanDecide(PlayerAction.Sell), Is.True);
            Assert.That(session.CanDecide(PlayerAction.Refuse), Is.True);
            Assert.That(session.CanDecide(PlayerAction.Special), Is.EqualTo(!string.IsNullOrEmpty(customer.specialActionKey)));
        }
    }
}
