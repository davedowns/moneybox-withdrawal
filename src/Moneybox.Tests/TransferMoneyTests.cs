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
    public class TransferMoneyTests
    {
        private readonly List<Account> accounts = new List<Account>();
        private readonly List<string> payInLimitNotifications = new List<string>();
        private readonly List<string> fundsLowNotifications = new List<string>();
        private TransferMoney transferMoneyService;

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
            notificationService.Setup(r => r.NotifyApproachingPayInLimit(It.IsAny<string>())).Callback((string e) => this.payInLimitNotifications.Add(e));
            notificationService.Setup(r => r.NotifyFundsLow(It.IsAny<string>())).Callback((string e) => this.fundsLowNotifications.Add(e));

            this.transferMoneyService = new TransferMoney(accountRepository.Object, notificationService.Object);
        }
        
        [TestMethod]
        public void Transfer_Deducts_From_Source_Account()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 400m, "test.1@example.com");
            var toAccount = this.AddAccount(Guid.NewGuid(), 0m, "test.2@example.com");

            this.transferMoneyService.Execute(fromAccount.Id, toAccount.Id, 400m);

            Assert.AreEqual(0m, fromAccount.Balance);
        }
        
        [TestMethod]
        public void Transfer_Deducts_From_Source_Account_Withdrawn()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 400m, "test.1@example.com");
            var toAccount = this.AddAccount(Guid.NewGuid(), 0m, "test.2@example.com");

            this.transferMoneyService.Execute(fromAccount.Id, toAccount.Id, 400m);

            Assert.AreEqual(-400m, fromAccount.Withdrawn);
        }

        [TestMethod]
        public void Transfer_Adds_To_Destination_Account()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 400m, "test.1@example.com");
            var toAccount = this.AddAccount(Guid.NewGuid(), 0m, "test.2@example.com");

            this.transferMoneyService.Execute(fromAccount.Id, toAccount.Id, 400m);

            Assert.AreEqual(400m, toAccount.Balance);
        }

        [TestMethod]
        public void Transfer_Adds_To_Destination_Account_Paid_In()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 400m, "test.1@example.com");
            var toAccount = this.AddAccount(Guid.NewGuid(), 0m, "test.2@example.com");

            this.transferMoneyService.Execute(fromAccount.Id, toAccount.Id, 400m);

            Assert.AreEqual(400m, toAccount.PaidIn);
        }

        [TestMethod]
        public void Transfer_Throws_Exception_For_Insufficient_Funds()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 399m, "test.1@example.com");
            var toAccount = this.AddAccount(Guid.NewGuid(), 0m, "test.2@example.com");

            Assert.ThrowsException<InvalidOperationException>(() => this.transferMoneyService.Execute(fromAccount.Id, toAccount.Id, 400m), "Insufficient funds to make transfer");
        }

        [TestMethod]
        public void Transfer_Allows_Pay_In_Limit_Reached_Fresh()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 4000m, "test.1@example.com");
            var toAccount = this.AddAccount(Guid.NewGuid(), 0m, "test.2@example.com");

            this.transferMoneyService.Execute(fromAccount.Id, toAccount.Id, 4000m);

            Assert.AreEqual(4000m, toAccount.Balance);
        }

        [TestMethod]
        public void Transfer_Allows_Pay_In_Limit_Reached_Over_Time()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 3990m, "test.1@example.com");
            var toAccount = this.AddAccount(Guid.NewGuid(), 10m, "test.2@example.com");
            
            this.transferMoneyService.Execute(fromAccount.Id, toAccount.Id, 3990m);

            Assert.AreEqual(4000m, toAccount.Balance);
        }

        [TestMethod]
        public void Transfer_Throws_Exception_For_Pay_In_Limit_Exceeded_Fresh()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 4001m, "test.1@example.com");
            var toAccount = this.AddAccount(Guid.NewGuid(), 0m, "test.2@example.com");

            Assert.ThrowsException<InvalidOperationException>(() => this.transferMoneyService.Execute(fromAccount.Id, toAccount.Id, 4001m), "Account pay in limit reached");
        }

        [TestMethod]
        public void Transfer_Throws_Exception_For_Pay_In_Limit_Exceeded_Over_Time()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 4000m, "test.1@example.com");
            var toAccount = this.AddAccount(Guid.NewGuid(), 10m, "test.2@example.com");

            Assert.ThrowsException<InvalidOperationException>(() => this.transferMoneyService.Execute(fromAccount.Id, toAccount.Id, 4000m), "Account pay in limit reached");
        }

        [TestMethod]
        public void Transfer_Notified_Source_For_Under_Low_Funds_Limit()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 600m, "test.1@example.com");
            var toAccount = this.AddAccount(Guid.NewGuid(), 0m, "test.2@example.com");
            
            this.transferMoneyService.Execute(fromAccount.Id, toAccount.Id, 101m);

            Assert.AreEqual(1, this.fundsLowNotifications.Count);
            Assert.AreEqual(fromAccount.User.Email, this.fundsLowNotifications[0]);
        }

        [TestMethod]
        public void Transfer_Didnt_Notify_Source_For_At_Low_Funds_Limit()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 600m, "test.1@example.com");
            var toAccount = this.AddAccount(Guid.NewGuid(), 0m, "test.2@example.com");
            
            this.transferMoneyService.Execute(fromAccount.Id, toAccount.Id, 100m);

            Assert.AreEqual(0, this.fundsLowNotifications.Count);
        }

        [TestMethod]
        public void Transfer_Notifies_Destination_For_Pay_In_Limit_Neared()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 100m, "test.1@example.com");
            var toAccount = this.AddAccount(Guid.NewGuid(), 3401m, "test.2@example.com");
            
            this.transferMoneyService.Execute(fromAccount.Id, toAccount.Id, 100m);

            Assert.AreEqual(1, this.payInLimitNotifications.Count);
            Assert.AreEqual(toAccount.User.Email, this.payInLimitNotifications[0]);
        }

        [TestMethod]
        public void Transfer_Didnt_Notify_Destination_For_Pay_In_Limit_Almost_Neared()
        {
            var fromAccount = this.AddAccount(Guid.NewGuid(), 100m, "test.1@example.com");
            var toAccount = this.AddAccount(Guid.NewGuid(), 3400m, "test.2@example.com");
            
            this.transferMoneyService.Execute(fromAccount.Id, toAccount.Id, 100m);

            Assert.AreEqual(0, this.payInLimitNotifications.Count);
        }
    }
}
