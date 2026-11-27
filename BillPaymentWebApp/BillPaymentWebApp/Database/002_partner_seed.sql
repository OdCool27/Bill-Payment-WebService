USE BillPaymentPartners;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

MERGE dbo.PartnerAccounts AS target
USING (VALUES
	('FLOW', N'FLOW100001', N'John Brown', CAST(5000.00 AS DECIMAL(18,2)), 'ACTIVE'),
	('FLOW', N'FLOW100002', N'Jane Williams', CAST(8500.00 AS DECIMAL(18,2)), 'ACTIVE'),
	('FLOW', N'FLOW100003', N'Mark Johnson', CAST(1200.00 AS DECIMAL(18,2)), 'INACTIVE'),
	('SAGICOR', N'SAG100001', N'Peter Campbell', CAST(15000.00 AS DECIMAL(18,2)), 'ACTIVE'),
	('SAGICOR', N'SAG100002', N'Sarah Thompson', CAST(7500.00 AS DECIMAL(18,2)), 'ACTIVE'),
	('SAGICOR', N'SAG100003', N'David Clarke', CAST(3000.00 AS DECIMAL(18,2)), 'INACTIVE')
) AS source (Provider, AccountNumber, AccountHolder, CurrentBalance, AccountStatus)
ON target.Provider = source.Provider AND target.AccountNumber = source.AccountNumber
WHEN MATCHED THEN UPDATE SET
	AccountHolder = source.AccountHolder,
	CurrentBalance = source.CurrentBalance,
	AccountStatus = source.AccountStatus
WHEN NOT MATCHED THEN INSERT (Provider, AccountNumber, AccountHolder, CurrentBalance, AccountStatus)
VALUES (source.Provider, source.AccountNumber, source.AccountHolder, source.CurrentBalance, source.AccountStatus);

COMMIT TRANSACTION;
GO
