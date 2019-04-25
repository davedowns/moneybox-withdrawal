using System;

namespace Moneybox.App
{
    public class Account
    {
        public const decimal PayInLimit = 4000m;

        public Guid Id { get; set; }

        public User User { get; set; }

        public decimal Balance { get; set; }

        public decimal Withdrawn { get; set; }

        public decimal PaidIn { get; set; }

        public bool TryAddToBalance(decimal amount)
        {
            var paidIn = this.PaidIn + amount;
            if (paidIn > Account.PayInLimit)
            {
                return false;
            }

            this.Balance = this.Balance + amount;
            this.PaidIn = this.PaidIn + amount;
            return true;
        }

        public bool TryDeductFromBalance(decimal amount)
        {
            var fromBalance = this.Balance - amount;
            if (fromBalance < 0m)
            {
                return false;
            }

            this.Balance = this.Balance - amount;
            this.Withdrawn = this.Withdrawn - amount;
            return true;
        }
    }
}
