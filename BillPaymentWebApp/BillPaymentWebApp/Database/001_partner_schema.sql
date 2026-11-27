IF DB_ID(N'BillPaymentPartners') IS NULL
BEGIN
	CREATE DATABASE BillPaymentPartners;
END
GO

USE BillPaymentPartners;
GO

IF OBJECT_ID(N'dbo.PartnerTransactions', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.PartnerTransactions
	(
		TransactionId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartnerTransactions PRIMARY KEY,
		AccountId BIGINT NOT NULL,
		Provider VARCHAR(20) NOT NULL,
		TransactionReference NVARCHAR(100) NOT NULL,
		TransactionType VARCHAR(30) NOT NULL CONSTRAINT DF_PartnerTransactions_Type DEFAULT ('BILL_PAYMENT'),
		Amount DECIMAL(18,2) NOT NULL,
		PreviousBalance DECIMAL(18,2) NOT NULL,
		NewBalance DECIMAL(18,2) NOT NULL,
		Narrative NVARCHAR(500) NOT NULL,
		TransactionTimestamp DATETIME2(3) NOT NULL CONSTRAINT DF_PartnerTransactions_Timestamp DEFAULT (SYSUTCDATETIME()),
		PaymentSource NVARCHAR(100) NULL,
		CONSTRAINT CK_PartnerTransactions_Provider CHECK (Provider IN ('FLOW', 'SAGICOR')),
		CONSTRAINT CK_PartnerTransactions_Amount CHECK (Amount > 0),
		CONSTRAINT UQ_PartnerTransactions_ProviderReference UNIQUE (Provider, TransactionReference)
	);
END
GO

IF OBJECT_ID(N'dbo.PartnerAccounts', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.PartnerAccounts
	(
		AccountId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartnerAccounts PRIMARY KEY,
		Provider VARCHAR(20) NOT NULL,
		AccountNumber NVARCHAR(50) NOT NULL,
		AccountHolder NVARCHAR(150) NOT NULL,
		CurrentBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_PartnerAccounts_Balance DEFAULT (0),
		AccountStatus VARCHAR(20) NOT NULL CONSTRAINT DF_PartnerAccounts_Status DEFAULT ('ACTIVE'),
		DateCreated DATETIME2(3) NOT NULL CONSTRAINT DF_PartnerAccounts_DateCreated DEFAULT (SYSUTCDATETIME()),
		CONSTRAINT CK_PartnerAccounts_Provider CHECK (Provider IN ('FLOW', 'SAGICOR')),
		CONSTRAINT CK_PartnerAccounts_Balance CHECK (CurrentBalance >= 0),
		CONSTRAINT CK_PartnerAccounts_Status CHECK (AccountStatus IN ('ACTIVE', 'INACTIVE')),
		CONSTRAINT UQ_PartnerAccounts_ProviderNumber UNIQUE (Provider, AccountNumber)
	);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PartnerTransactions_Accounts')
BEGIN
	ALTER TABLE dbo.PartnerTransactions
		ADD CONSTRAINT FK_PartnerTransactions_Accounts
		FOREIGN KEY (AccountId) REFERENCES dbo.PartnerAccounts(AccountId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PartnerAccounts_ProviderStatus')
	CREATE INDEX IX_PartnerAccounts_ProviderStatus ON dbo.PartnerAccounts (Provider, AccountStatus);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PartnerTransactions_AccountTimestamp')
	CREATE INDEX IX_PartnerTransactions_AccountTimestamp ON dbo.PartnerTransactions (AccountId, TransactionTimestamp DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PartnerTransactions_Reference')
	CREATE INDEX IX_PartnerTransactions_Reference ON dbo.PartnerTransactions (TransactionReference);
GO
