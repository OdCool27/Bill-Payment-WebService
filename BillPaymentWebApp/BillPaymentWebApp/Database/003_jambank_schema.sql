IF DB_NAME() <> N'BillPaymentPartners'
BEGIN
    RAISERROR('Run this JamBank migration in the BillPaymentPartners database.', 16, 1);
    RETURN;
END;
GO

-- Add JamBank simulation tables alongside the partner tables. This script does not
-- alter, delete, or reset existing database objects or data.
IF OBJECT_ID(N'dbo.JamBankAccounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.JamBankAccounts
    (
        JamBankAccountId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_JamBankAccounts PRIMARY KEY,
        UserId INT NOT NULL,
        AccountNumber NVARCHAR(50) NOT NULL,
        AccountType NVARCHAR(30) NOT NULL,
        Nickname NVARCHAR(50) NOT NULL,
        Balance DECIMAL(18,2) NOT NULL,
        CONSTRAINT UQ_JamBankAccounts_User_Account UNIQUE (UserId, AccountNumber),
        CONSTRAINT CK_JamBankAccounts_Balance CHECK (Balance >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.JamBankLinkedAccounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.JamBankLinkedAccounts
    (
        JamBankLinkedAccountId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_JamBankLinkedAccounts PRIMARY KEY,
        UserId INT NOT NULL,
        Biller NVARCHAR(20) NOT NULL,
        AccountNumber NVARCHAR(50) NOT NULL,
        Nickname NVARCHAR(50) NOT NULL,
        IsVerified BIT NOT NULL CONSTRAINT DF_JamBankLinkedAccounts_IsVerified DEFAULT (0),
        CONSTRAINT UQ_JamBankLinkedAccounts_User_Biller_Account
            UNIQUE (UserId, Biller, AccountNumber)
    );
END;
GO

IF OBJECT_ID(N'dbo.JamBankTransactions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.JamBankTransactions
    (
        JamBankTransactionId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_JamBankTransactions PRIMARY KEY,
        UserId INT NOT NULL,
        TxnDate DATETIME NOT NULL,
        Narrative NVARCHAR(500) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        Biller NVARCHAR(50) NULL,
        BillerAccountNumber NVARCHAR(50) NULL,
        ExternalReference NVARCHAR(100) NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.JamBankAccounts WHERE UserId = 1 AND AccountNumber = N'123456789')
BEGIN
    INSERT INTO dbo.JamBankAccounts (UserId, AccountNumber, AccountType, Nickname, Balance)
    VALUES (1, N'123456789', N'Savings', N'Main Savings', 25000.00);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.JamBankAccounts WHERE UserId = 1 AND AccountNumber = N'987654321')
BEGIN
    INSERT INTO dbo.JamBankAccounts (UserId, AccountNumber, AccountType, Nickname, Balance)
    VALUES (1, N'987654321', N'Chequing', N'Daily Chequing', 4300.50);
END;
GO
