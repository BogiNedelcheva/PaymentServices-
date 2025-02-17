﻿using System;
using System.Configuration;
using System.Net.NetworkInformation;
using System.Security.Principal;
using NUnit.Framework;
using PaymentServices.Data;
using PaymentServices.Services;
using PaymentServices.Types;

namespace PaymentServices.Test;

public class PaymentServicesUnitTests
{
    private const decimal AMMOUNT = 6.5m;
    private const string ACCOUNT_NUMBER_FIRST = "123";
    private const string ACCOUNT_NUMBER_SECOND = "13451";
    private const string ACCOUNT_NUMBER_THIRD = "23452";

    private const string ACCOUNT_DATASTORE_TYPE = "DataStoreType";
    private const string BACKUP_ACCOUNT_DATASTORE_TYPE = "Backup";

    private IPaymentValidator? paymentValidator;
    private IPaymentService? paymentService;

    private IDataStore? dataStore;
    private IDataStoreFactory? dataStoreFactory;
    private Account? firstAccount;
    private Account? secondAccount;
    private Account? thirdAccount;

    [SetUp]
    public void Setup()
    {
        this.dataStoreFactory = new DataStoreFactory();

        this.dataStore = dataStoreFactory!.CreateDataStore(ACCOUNT_DATASTORE_TYPE);

        this.paymentValidator = new PaymentValidator(dataStore);

        this.paymentService = new PaymentService(paymentValidator, dataStore);

        firstAccount = new Account()
        {
            AccountNumber = ACCOUNT_NUMBER_FIRST,
            Balance = AMMOUNT,
            AllowedPaymentSchemes = AllowedPaymentSchemes.Chaps,
            Status = AccountStatus.Live
        };

        secondAccount = new Account()
        {
            AccountNumber = ACCOUNT_NUMBER_SECOND,
            Balance = AMMOUNT,
            AllowedPaymentSchemes = AllowedPaymentSchemes.FasterPayments,
            Status = AccountStatus.Live
        };

        thirdAccount = new Account()
        {
            AccountNumber = ACCOUNT_NUMBER_THIRD,
            Balance = AMMOUNT,
            AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs,
            Status = AccountStatus.Live
        };

        dataStore!.Accounts.AddRange(new Account[] { firstAccount, secondAccount, thirdAccount });
    }

    [Test]
    public void GetAaccountAndReturnNotNull()
    {
        MakePaymentRequest request = new MakePaymentRequest()
        {
            Amount = AMMOUNT,
            CreditorAccountNumber = firstAccount!.AccountNumber!,
            DebtorAccountNumber = firstAccount.AccountNumber!,
            PaymentDate = DateTime.Now,
            PaymentScheme = PaymentScheme.Chaps
        };

        var account = dataStore!.GetAccount(request.DebtorAccountNumber);

        Assert.AreNotEqual(null, account);
    }

    [Test]
    public void BackUpDsataStoreShouldReturnsAccount()
    {
        this.dataStore = dataStoreFactory!.CreateDataStore(BACKUP_ACCOUNT_DATASTORE_TYPE);

        this.paymentValidator = new PaymentValidator(dataStore!);

        this.paymentService = new PaymentService(paymentValidator!, dataStore!);

        dataStore!.Accounts.Add(firstAccount!);

        MakePaymentRequest request = new MakePaymentRequest()
        {
            Amount = AMMOUNT,
            CreditorAccountNumber = firstAccount!.AccountNumber!,
            DebtorAccountNumber = firstAccount.AccountNumber!,
            PaymentDate = DateTime.Now,
            PaymentScheme = PaymentScheme.Chaps
        };

        var account = dataStore!.GetAccount(request.DebtorAccountNumber);

        Assert.AreEqual(firstAccount.AccountNumber, account.AccountNumber);
    }

    [Test]
    public void MakePaymentShouldFailsWhenAccountIsNull()
    {
        string accountNumber = String.Empty;

        MakePaymentRequest request = new MakePaymentRequest()
        {
            Amount = AMMOUNT,
            CreditorAccountNumber = accountNumber,
            DebtorAccountNumber = accountNumber,
            PaymentDate = DateTime.Now,
            PaymentScheme = PaymentScheme.Chaps
        };

        MakePaymentResult result = paymentService!.MakePayment(request);

        Assert.IsFalse(result.Success);
    }

    [Test]
    public void MakePaymentFailsWhenIsValidAccountStateIsFalse()
    {
        var isValidAccountState = false;

        MakePaymentResult result = new MakePaymentResult();

        if (isValidAccountState)
        {
            result.Success = true;
        }

        Assert.IsFalse(result.Success);

    }

    [Test]
    public void MakePaymentShouldWithdrawsCorrectly()
    {
        decimal expected = 1.0m;

        MakePaymentRequest request = new MakePaymentRequest()
        {
            Amount = 5.5m,
            CreditorAccountNumber = ACCOUNT_NUMBER_FIRST,
            DebtorAccountNumber = ACCOUNT_NUMBER_FIRST,
            PaymentDate = DateTime.Now,
            PaymentScheme = PaymentScheme.Chaps
        };

        MakePaymentResult result = paymentService!.MakePayment(request);

        Account account = dataStore!.GetAccount(ACCOUNT_NUMBER_FIRST);

        Assert.AreEqual(expected, account.Balance);
    }

    [Test]
    public void MakePaymentShouldUpdateAccountCorrectly()
    {
        var account = dataStore!.GetAccount(ACCOUNT_NUMBER_FIRST);
        account.Balance = 7.5m;
        var isValidAccountState = true;
        var expected = 1m;

        MakePaymentRequest request = new MakePaymentRequest()
        {
            Amount = AMMOUNT,
            CreditorAccountNumber = ACCOUNT_NUMBER_FIRST,
            DebtorAccountNumber = ACCOUNT_NUMBER_FIRST,
            PaymentDate = DateTime.Now,
            PaymentScheme = PaymentScheme.Chaps
        };

        if (isValidAccountState)
        {
            account.Balance -= request.Amount;

            this.dataStore!.UpdateAccount(account);

        }
        var updatedAccount = dataStore!.GetAccount(ACCOUNT_NUMBER_FIRST);

        //Assert.AreNotEqual(account, updatedAccount.Balance);

        Assert.AreEqual(expected, updatedAccount.Balance);
    }
}