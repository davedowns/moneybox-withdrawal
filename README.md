# Moneybox Money Withdrawal

The solution contains a .NET core library (Moneybox.App) which is structured into the following 3 folders:

* Domain - this contains the domain models for a user and an account, and a notification service.
* Features - this contains two operations, one which is implemented (transfer money) and another which isn't (withdraw money)
* DataAccess - this contains a repository for retrieving and saving an account (and the nested user it belongs to)

## The task

The task is to implement a money withdrawal in the WithdrawMoney.Execute(...) method in the features folder. For consistency, the logic should be the same as the TransferMoney.Execute(...) method i.e. notifications for low funds and exceptions where the operation is not possible. 

As part of this process however, you should look to refactor some of the code in the TransferMoney.Execute(...) method into the domain models, and make these models less susceptible to misuse. We're looking to make our domain models rich in behaviour and much more than just plain old objects, however we don't want any data persistance operations (i.e. data access repositories) to bleed into our domain. This should simplify the task of implementing WithdrawMoney.Execute(...).

## Guidelines

* You should spend no more than 1 hour on this task, although there is no time limit
* You should fork or copy this repository into your own public repository (Github, BitBucket etc.) before you do your work
* Your solution must compile and run first time
* You should not alter the notification service or the the account repository interfaces
* You may add unit/integration tests using a test framework (and/or mocking framework) of your choice
* You may edit this README.md if you want to give more details around your work (e.g. why you have done something a particular way, or anything else you would look to do but didn't have time)

Once you have completed your work, send us a link to your public repository.

Good luck!

# Changes & reasoning

The commit history may be self-explanatory, but I thought I'd try and add a bit more detail here just in case...

## "Bug" fixes

I wasn't sure whether the actual logic within the `TransferMoney` service was meant to be changed beyond a simple refactor but given that the notifications could be sent even if the balances didn't change I decided to go with it and re-order the logic so that notifications would be sent after we were sure everything was successful and the balance changes had been persisted. 

I assumed that the repository saving would already handle situations were the `Update` call on `from` worked, but the `Update` call on `to` didn't (e.g. rolling back the changes manually, or them otherwise being in some transaction) and that one of the calls would throw an exception if there was a problem thereby preventing the notification services from being called.

## Refactor adding/deducting balances

From there I did a pretty straightforward encapsulation of the add/deduct balance logic and the validation that goes with it. I left the exceptions being thrown in the service classes so that the messages could be customised in other services while the validation logic would remain the same.

I toyed with the idea of having an `enum` return value rather than true/false so that it's easier to add extra validation failures later but decided that was probably overkill at this point given that there's been no indication there will be other validation. Another option would be to have those new methods throw exceptions, catch them in the service layer then rethrow them wrapped in another exception. That way the outer exception could indicate the process that was failing (e.g. transfer failed) with the inner exception representing the exact reason why. Again, that seemed probably a bit overkill for this example, but worth considering for future expansion.

## Refactor account notification logic

Next I again did a pretty straightforward encapsulation of the logic that determines whether notifications should be generated. As the `PayInLimit` is only used in the `Account` class I also made that private.

## Add withdraw logic

Now that everything's been refactored I was able to take just the withdraw and low funds notification logic from the transfer service, changing the exception's message, and use that for the withdraw service.
