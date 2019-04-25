using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moneybox.App;
using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using Moneybox.App.Features;
using Moq;

namespace Moneybox.Tests
{
    [TestClass]
    public class WithdrawMoneyTests
    {
        private readonly List<Account> accounts = new List<Account>();
        private readonly List<string> fundsLowNotifications = new List<string>();
        private WithdrawMoney withdrawMoneyService;

        private Account AddAccount(Guid id, decimal balance, string userEmail)
        {
            var account = new Account
            {
                Id = id,
                Balance = balance,
                PaidIn = balance,
                User = new User
                {
                    Email = userEmail,
                },
            };

            this.accounts.Add(account);

            return account;
        }

        [TestInitialize]
        public void Initialise()
        {
            var accountRepository = new Mock<IAccountRepository>();
            accountRepository.Setup(r => r.GetAccountById(It.IsAny<Guid>())).Returns((Guid id) => this.accounts.FirstOrDefault(a => a.Id == id));
            accountRepository.Setup(r => r.Update(It.IsAny<Account>()));

            var notificationService = new Mock<INotificationService>();
            notificationService.Setup(r => r.NotifyApproachingPayInLimit(It.IsAny<string>()));
            notificationService.Setup(r => r.NotifyFundsLow(It.IsAny<string>())).Callback((string e) => this.fundsLowNotifications.Add(e));

            this.withdrawMoneyService = new WithdrawMoney(accountRepository.Object, notificationService.Object);
        }
        
        [TestMethod]
        public void Transfer_Deducts_From_Source_Account()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 400m, "test.1@example.com");

            this.withdrawMoneyService.Execute(fromAccount.Id, 400m);

            Assert.AreEqual(0m, fromAccount.Balance);
        }
        
        [TestMethod]
        public void Transfer_Deducts_From_Source_Account_Withdrawn()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 400m, "test.1@example.com");

            this.withdrawMoneyService.Execute(fromAccount.Id, 400m);

            Assert.AreEqual(-400m, fromAccount.Withdrawn);
        }

        [TestMethod]
        public void Transfer_Throws_Exception_For_Insufficient_Funds()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 399m, "test.1@example.com");

            Assert.ThrowsException<InvalidOperationException>(() => this.withdrawMoneyService.Execute(fromAccount.Id, 400m), "Insufficient funds to make transfer");
        }

        [TestMethod]
        public void Transfer_Notified_Source_For_Under_Low_Funds_Limit()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 600m, "test.1@example.com");
            
            this.withdrawMoneyService.Execute(fromAccount.Id, 101m);

            Assert.AreEqual(1, this.fundsLowNotifications.Count);
            Assert.AreEqual(fromAccount.User.Email, this.fundsLowNotifications[0]);
        }

        [TestMethod]
        public void Transfer_Didnt_Notify_Source_For_At_Low_Funds_Limit()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 600m, "test.1@example.com");
            
            this.withdrawMoneyService.Execute(fromAccount.Id, 100m);

            Assert.AreEqual(0, this.fundsLowNotifications.Count);
        }
    }
}
