using System;
using System.Globalization;
using System.Text;

namespace NightShift.Tests
{
    // A scripted player: for every customer it looks with Shift, thinks, decides and presses Continue,
    // from StartNight() to the ending. It keeps a one-line trace per customer.
    public class NightBot
    {
        // How long to hold Shift on a customer, in one go.
        public Func<CustomerData, float> ShiftSeconds = customer => 0f;

        // Which button to press. If the choice is not on offer for this customer, the bot presses Sell.
        public Func<CustomerData, PlayerAction> ChooseAction = customer => customer.GetCorrectAction();

        // Time spent reading the dialogue with Shift released, before deciding.
        public float ThinkSeconds = 15f;

        // Time spent reading the feedback line before Continue.
        public float FeedbackSeconds = 1f;

        public string Name;
        public readonly StringBuilder Trace = new StringBuilder();

        public NightBot(string name)
        {
            Name = name;
        }

        public EndingId Play(IGameSession session)
        {
            Trace.Length = 0;
            Trace.AppendLine("Playthrough: " + Name);
            session.StartNight();

            while (!session.IsFinished)
            {
                CustomerData customer = session.CurrentCustomer;
                string line = GameData.FormatClock(session.ClockMinute) + "  " + customer.id.PadRight(18);

                float look = ShiftSeconds(customer);
                SessionDriver.Hold(session, look, true);
                line += "  shift " + Num(look, "0.0") + "s  noticed " + Num(session.Noticed, "0.00");
                if (session.IsFinished)
                {
                    Trace.AppendLine(line + "  -> the meter filled while looking");
                    break;
                }

                SessionDriver.Hold(session, ThinkSeconds, false);

                PlayerAction action = ChooseAction(customer);
                if (!session.CanDecide(action)) action = PlayerAction.Sell;
                DecisionResult result = session.Decide(action);
                line += "  " + action.ToString().PadRight(7) + (result.Correct ? "  correct" : "  WRONG  ")
                    + "  mistakes " + session.Mistakes + "/" + session.MaxMistakes
                    + (result.EndsNight ? "  -> ends the night" : "");
                Trace.AppendLine(line);

                SessionDriver.Hold(session, FeedbackSeconds, false);
                session.Advance();
            }

            Trace.Append(GameData.FormatClock(session.ClockMinute) + "  ENDING: " + session.Ending
                + "  (mistakes " + session.Mistakes + "/" + session.MaxMistakes
                + ", Shift held " + Num(session.TotalShiftSeconds, "0.0") + " s, noticed " + Num(session.Noticed, "0.00") + ")");
            return session.Ending;
        }

        private static string Num(float value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
