using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using System;

namespace Moneybox.App.Features
{
    public class TransferMoney
    {
        private IAccountRepository accountRepository;
        private INotificationService notificationService;

        public TransferMoney(IAccountRepository accountRepository, INotificationService notificationService)
        {
            this.accountRepository = accountRepository;
            this.notificationService = notificationService;
        }

        public void Execute(Guid fromAccountId, Guid toAccountId, decimal amount)
        {
            var from = this.accountRepository.GetAccountById(fromAccountId);
            var to = this.accountRepository.GetAccountById(toAccountId);

            if (!from.TryDeductFromBalance(amount))
            {
                throw new InvalidOperationException("Insufficient funds to make transfer");
            }

            if (!to.TryAddToBalance(amount))
            {
                throw new InvalidOperationException("Account pay in limit reached");
            }

            this.accountRepository.Update(from);
            this.accountRepository.Update(to);

            if (from.Balance < 500m)
            {
                this.notificationService.NotifyFundsLow(from.User.Email);
            }

            if (Account.PayInLimit - to.PaidIn < 500m)
            {
                this.notificationService.NotifyApproachingPayInLimit(to.User.Email);
            }
        }
    }
}
