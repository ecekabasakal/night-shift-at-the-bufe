using UnityEngine;

namespace NightShift
{
    // What the player can do with a customer. Keyboard 1 / 2 / 3.
    public enum PlayerAction
    {
        Sell,
        Refuse,
        Special
    }

    // The four endings. Lowercase ids ("morning", ...) are used in JSON and in string keys.
    public enum EndingId
    {
        Morning,
        Changed,
        Seen,
        Fired
    }

    // The three top-level phases of the game (see GameState).
    public enum GamePhase
    {
        Intro,
        Serving,
        Ending
    }

    // What a wrong decision does for a given customer.
    public enum WrongOutcome
    {
        Mistake,
        InstantEnd
    }

    // Where the night currently is, inside IGameSession.
    public enum ServingStep
    {
        NotStarted,        // before StartNight() / after Reset()
        AwaitingDecision,  // a customer is at the counter, the player has not decided yet
        ShowingFeedback,   // the player decided; the feedback line is on screen until Advance()
        Finished           // the night is over; IGameSession.Ending is valid
    }

    // The result of one decision, returned by IGameSession.Decide and sent with EventBus.DecisionMade.
    public struct DecisionResult
    {
        public readonly PlayerAction Action;
        public readonly bool Correct;
        public readonly string FeedbackKey; // strings.json key of the feedback line
        public readonly bool EndsNight;     // true when the next Advance() will finish the night (fired / seen)

        public DecisionResult(PlayerAction action, bool correct, string feedbackKey, bool endsNight)
        {
            Action = action;
            Correct = correct;
            FeedbackKey = feedbackKey;
            EndsNight = endsNight;
        }
    }

    // Converts between the lowercase strings used in JSON and the C# enums.
    public static class DataParsing
    {
        public static PlayerAction ParseAction(string value)
        {
            switch (value)
            {
                case "sell": return PlayerAction.Sell;
                case "refuse": return PlayerAction.Refuse;
                case "special": return PlayerAction.Special;
            }
            Debug.LogWarning("Unknown action in data: '" + value + "' (using sell)");
            return PlayerAction.Sell;
        }

        public static WrongOutcome ParseWrongOutcome(string value)
        {
            switch (value)
            {
                case "mistake": return WrongOutcome.Mistake;
                case "instantEnd": return WrongOutcome.InstantEnd;
            }
            Debug.LogWarning("Unknown wrongOutcome in data: '" + value + "' (using mistake)");
            return WrongOutcome.Mistake;
        }

        public static EndingId ParseEnding(string value)
        {
            switch (value)
            {
                case "morning": return EndingId.Morning;
                case "changed": return EndingId.Changed;
                case "seen": return EndingId.Seen;
                case "fired": return EndingId.Fired;
            }
            Debug.LogWarning("Unknown ending id in data: '" + value + "' (using fired)");
            return EndingId.Fired;
        }

        // EndingId.Morning -> "morning"
        public static string ToId(EndingId ending)
        {
            switch (ending)
            {
                case EndingId.Morning: return "morning";
                case EndingId.Changed: return "changed";
                case EndingId.Seen: return "seen";
                default: return "fired";
            }
        }

        // PlayerAction.Sell -> "sell"
        public static string ToId(PlayerAction action)
        {
            switch (action)
            {
                case PlayerAction.Sell: return "sell";
                case PlayerAction.Refuse: return "refuse";
                default: return "special";
            }
        }

        // "ending.morning.title"
        public static string EndingTitleKey(EndingId ending)
        {
            return "ending." + ToId(ending) + ".title";
        }

        // "ending.morning.body"
        public static string EndingBodyKey(EndingId ending)
        {
            return "ending." + ToId(ending) + ".body";
        }

        // "#RRGGBB" -> Color. Returns magenta for a broken value so the mistake is visible.
        public static Color ParseColor(string hex)
        {
            Color color;
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out color))
            {
                return color;
            }
            return Color.magenta;
        }
    }
}
