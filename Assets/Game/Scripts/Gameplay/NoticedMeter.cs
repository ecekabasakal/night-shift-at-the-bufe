namespace NightShift
{
    // The Noticed meter (0..1): fills while Shift is held, drains while it is released.
    public class NoticedMeter
    {
        private readonly BalanceData balance;

        public float Value { get; private set; }

        public bool IsFull
        {
            get { return Value >= 1f; }
        }

        public NoticedMeter(BalanceData balance)
        {
            this.balance = balance;
        }

        public void Reset()
        {
            Value = 0f;
        }

        // Looking at a customer for deltaSeconds. The starer fills fastest, then any other, then a human.
        public void Fill(CustomerData customer, float deltaSeconds)
        {
            SetClamped(Value + FillRate(customer) * deltaSeconds);
        }

        public void Drain(float deltaSeconds)
        {
            SetClamped(Value - balance.noticedDrainPerSecond * deltaSeconds);
        }

        private float FillRate(CustomerData customer)
        {
            if (customer.staresBack) return balance.noticedFillPerSecondStare;
            if (customer.isOther) return balance.noticedFillPerSecondOther;
            return balance.noticedFillPerSecond;
        }

        private void SetClamped(float value)
        {
            if (value < 0f) value = 0f;
            if (value > 1f) value = 1f;
            Value = value;
        }
    }
}
