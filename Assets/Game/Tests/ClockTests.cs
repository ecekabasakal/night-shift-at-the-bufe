using NUnit.Framework;

namespace NightShift.Tests
{
    // The night clock: follows each customer's arrivalMinute, ends at 06:00, and is formatted as HH:MM.
    public class ClockTests : SessionTestBase
    {
        [Test]
        public void Clock_ShowsEachCustomersArrivalMinute_WhileTheyAreAtTheCounter()
        {
            session.StartNight();
            int served = 0;

            while (!session.IsFinished)
            {
                CustomerData customer = session.CurrentCustomer;
                Assert.That(customer, Is.SameAs(data.Customers[served]), "Customers come in file order");
                Assert.That(session.ClockMinute, Is.EqualTo(customer.arrivalMinute), customer.id + " on arrival");

                SessionDriver.Hold(session, 5f, false);
                session.Decide(customer.GetCorrectAction());
                Assert.That(session.ClockMinute, Is.EqualTo(customer.arrivalMinute), customer.id + " during feedback");

                session.Advance();
                served++;
            }

            Assert.That(served, Is.EqualTo(data.Customers.Length));
        }

        [Test]
        public void Clock_EndsAtShiftLength_AfterTheLastCustomer()
        {
            session.StartNight();
            SessionDriver.FinishNightCorrectly(session);

            Assert.That(session.ClockMinute, Is.EqualTo(data.Balance.shiftLengthMinutes));
            Assert.That(GameData.FormatClock(session.ClockMinute), Is.EqualTo("06:00"), "The shift is 23:00 to 06:00");
        }

        [Test]
        public void Clock_IsZero_BeforeTheNightStarts()
        {
            Assert.That(session.ClockMinute, Is.EqualTo(0));
        }

        [TestCase(0, "23:00")]
        [TestCase(5, "23:05")]
        [TestCase(59, "23:59")]
        [TestCase(60, "00:00")]
        [TestCase(61, "00:01")]
        [TestCase(240, "03:00")]
        [TestCase(419, "05:59")]
        [TestCase(420, "06:00")]
        public void FormatClock_ShowsMinutesAfter2300_As24HourTime(int minutesAfter23, string expected)
        {
            Assert.That(GameData.FormatClock(minutesAfter23), Is.EqualTo(expected));
        }
    }
}
