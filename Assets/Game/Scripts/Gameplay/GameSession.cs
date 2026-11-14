namespace NightShift
{
    // The rules of one night: customer queue, decisions, mistakes, Shift vision, clock and endings.
    // Pure C#: GameBootstrap (or a test) drives it by calling the methods of IGameSession.
    public class GameSession : IGameSession
    {
        private readonly CustomerData[] customers;
        private readonly BalanceData balance;
        private readonly NoticedMeter meter;

        // The decision made for the current customer (returned again if Decide is called twice).
        private DecisionResult lastDecision;

        // Set by a decision that ends the night; applied by Advance().
        private bool hasPendingEnding;
        private EndingId pendingEnding;

        public ServingStep Step { get; private set; }
        public int CustomerIndex { get; private set; }
        public int Mistakes { get; private set; }
        public int ClockMinute { get; private set; }
        public EndingId Ending { get; private set; }
        public bool IsShiftActive { get; private set; }
        public float TotalShiftSeconds { get; private set; }

        public GameSession(GameData data)
        {
            customers = data.Customers;
            balance = data.Balance;
            meter = new NoticedMeter(balance);
            Reset();
        }

        public int CustomerCount
        {
            get { return customers.Length; }
        }

        public CustomerData CurrentCustomer
        {
            get { return IsCustomerAtCounter ? customers[CustomerIndex] : null; }
        }

        public int MaxMistakes
        {
            get { return balance.maxMistakes; }
        }

        public bool IsFinished
        {
            get { return Step == ServingStep.Finished; }
        }

        public float Noticed
        {
            get { return meter.Value; }
        }

        public bool IsStaredAt
        {
            get { return IsShiftActive && CurrentCustomer.staresBack; }
        }

        private bool IsCustomerAtCounter
        {
            get { return Step == ServingStep.AwaitingDecision || Step == ServingStep.ShowingFeedback; }
        }

        public void Reset()
        {
            Step = ServingStep.NotStarted;
            CustomerIndex = -1;
            Mistakes = 0;
            ClockMinute = 0;
            Ending = EndingId.Morning;
            IsShiftActive = false;
            TotalShiftSeconds = 0f;
            meter.Reset();
            lastDecision = new DecisionResult(PlayerAction.Sell, false, "", false);
            hasPendingEnding = false;
        }

        public void StartNight()
        {
            Reset();
            NextCustomerOrEndOfNight();
        }

        public void Tick(float deltaSeconds, bool shiftHeld)
        {
            if (!IsCustomerAtCounter) return;
            if (deltaSeconds < 0f) deltaSeconds = 0f;

            IsShiftActive = shiftHeld;

            if (shiftHeld)
            {
                meter.Fill(CurrentCustomer, deltaSeconds);
                TotalShiftSeconds += deltaSeconds;
            }
            else
            {
                meter.Drain(deltaSeconds);
            }

            if (meter.IsFull)
            {
                Finish(EndingId.Seen);
            }
        }

        public bool CanDecide(PlayerAction action)
        {
            if (Step != ServingStep.AwaitingDecision) return false;
            if (action == PlayerAction.Special) return CurrentCustomer.HasSpecialAction();
            return true;
        }

        public DecisionResult Decide(PlayerAction action)
        {
            // A call that does not fit (already decided, night over, no such button) changes nothing.
            if (!CanDecide(action)) return lastDecision;

            CustomerData customer = CurrentCustomer;
            bool correct = action == customer.GetCorrectAction();

            if (correct)
            {
                lastDecision = new DecisionResult(action, true, customer.correctResultKey, false);
            }
            else
            {
                ApplyWrongDecision(customer);
                lastDecision = new DecisionResult(action, false, customer.wrongResultKey, hasPendingEnding);
            }

            Step = ServingStep.ShowingFeedback;
            return lastDecision;
        }

        public void Advance()
        {
            if (Step != ServingStep.ShowingFeedback) return;

            if (hasPendingEnding)
            {
                Finish(pendingEnding);
            }
            else
            {
                NextCustomerOrEndOfNight();
            }
        }

        private void ApplyWrongDecision(CustomerData customer)
        {
            if (customer.GetWrongOutcome() == WrongOutcome.InstantEnd)
            {
                SetPendingEnding(customer.GetInstantEnding());
                return;
            }

            Mistakes += 1;
            if (Mistakes >= MaxMistakes)
            {
                SetPendingEnding(EndingId.Fired);
            }
        }

        private void SetPendingEnding(EndingId ending)
        {
            hasPendingEnding = true;
            pendingEnding = ending;
        }

        private void NextCustomerOrEndOfNight()
        {
            int next = CustomerIndex + 1;

            if (next < customers.Length)
            {
                CustomerIndex = next;
                ClockMinute = customers[next].arrivalMinute;
                Step = ServingStep.AwaitingDecision;
                return;
            }

            ClockMinute = balance.shiftLengthMinutes;
            bool changed = TotalShiftSeconds > balance.changedShiftSecondsThreshold;
            Finish(changed ? EndingId.Changed : EndingId.Morning);
        }

        // The night is over: no customer, Shift off. Noticed, Mistakes and the clock keep their values.
        private void Finish(EndingId ending)
        {
            Ending = ending;
            Step = ServingStep.Finished;
            CustomerIndex = -1;
            IsShiftActive = false;
            hasPendingEnding = false;
        }
    }
}
