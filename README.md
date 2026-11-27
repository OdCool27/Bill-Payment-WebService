# Bill Payment Web Service

Academic CIT4009 Enterprise Computing I simulation of an NCB online banking application with FLOW and SAGICOR LIFE partner payment services. The existing application is ASP.NET Web Forms on .NET Framework 4.7.2. This partner implementation uses SQL Server and ADO.NET; it does not connect to real FLOW, SAGICOR, or banking systems.

## Demo credentials

Existing application demo login:

- Username: `testuser`
- Password: `password123`

Partner test accounts are fictional:

| Provider | Account number | Holder | Balance | Status |
|---|---|---|---:|---|
| FLOW | FLOW100001 | John Brown | 5000.00 | ACTIVE |
| FLOW | FLOW100002 | Jane Williams | 8500.00 | ACTIVE |
| FLOW | FLOW100003 | Mark Johnson | 1200.00 | INACTIVE |
| SAGICOR LIFE | SAG100001 | Peter Campbell | 15000.00 | ACTIVE |
| SAGICOR LIFE | SAG100002 | Sarah Thompson | 7500.00 | ACTIVE |
| SAGICOR LIFE | SAG100003 | David Clarke | 3000.00 | INACTIVE |

## Setup

1. Open SQL Server Management Studio or Azure Data Studio.
2. Run `BillPaymentWebApp/BillPaymentWebApp/Database/001_partner_schema.sql`, followed by `BillPaymentWebApp/BillPaymentWebApp/Database/002_partner_seed.sql` to create and seed the partner database.
3. Set the `PartnerDatabase` connection string in `BillPaymentWebApp/BillPaymentWebApp/Web.config`. The checked-in value uses LocalDB and Windows authentication; use an environment/deployment transform for other credentials and do not commit secrets. On application startup, the app automatically creates its separate JamBank tables in this same database if they are missing.
4. Start the existing Web Forms project in Visual Studio. The partner endpoints are hosted by the same application and can be called independently.

The database uses shared `PartnerAccounts` and `PartnerTransactions` tables with a `Provider` discriminator. Account numbers are unique per provider. Payment references are unique per provider, and the payment repository performs the balance update and transaction insert in one serializable SQL transaction.

## API contract

Base URL during IIS Express development is normally `https://localhost:44374/` (use the URL shown by Visual Studio).

### Verify an account

- `POST /api/flow/accounts/verify`
- `POST /api/sagicor/accounts/verify`

Request:

```json
{"accountNumber":"FLOW100001"}
```

Success for an active account:

```json
{"success":true,"exists":true,"provider":"FLOW","accountNumber":"FLOW100001","accountHolder":"John Brown","accountStatus":"ACTIVE","message":"Account verified successfully."}
```

A missing account returns HTTP 200 with `exists: false`. An existing inactive account returns `exists: true`, `accountStatus: "INACTIVE"`, and an inactive message; callers must not treat it as eligible for payment. Missing or invalid account numbers return HTTP 400.

### Submit a payment

- `POST /api/flow/payments`
- `POST /api/sagicor/payments`

Request:

```json
{"accountNumber":"FLOW100001","amount":2500.00,"paymentReference":"NCB-TXN-10001","source":"NCB","narrative":"Bill payment received from NCB online banking."}
```

Successful response (HTTP 200):

```json
{"success":true,"provider":"FLOW","transactionReference":"NCB-TXN-10001","accountNumber":"FLOW100001","amount":2500.00,"previousBalance":5000.00,"newBalance":7500.00,"narrative":"Bill payment received from NCB online banking.","message":"Payment processed successfully."}
```

The response also includes `transactionTimestamp`. Payment errors use these statuses:

- `400`: malformed JSON, missing fields, non-positive amount, or invalid field length.
- `404`: destination account does not exist.
- `409`: account is inactive or payment reference was already processed.
- `500`: database/service failure; the SQL transaction is rolled back.

The service never deducts NCB funds. It only credits the selected partner account after validating it.

## Validation and tests

No test project or test framework existed in the original repository, so no additional dependency was introduced. For integration validation after configuring SQL Server, run the following cases with Postman, curl, or the Visual Studio HTTP client:

1. Verify FLOW100001 and SAG100001.
2. Verify a nonexistent number and inactive FLOW100003/SAG100003.
3. Submit a positive payment to each active provider account and confirm the returned balance.
4. Query `PartnerTransactions` and confirm exactly one row for each reference.
5. Repeat the same payment reference and confirm HTTP 409 and no second credit.
6. Submit zero, negative, missing-reference, missing-account, and inactive-account payments.
7. Force a database failure or disconnect the database and confirm no partial balance/transaction update.

Useful SQL checks:

```sql
SELECT Provider, AccountNumber, CurrentBalance, AccountStatus
FROM dbo.PartnerAccounts;

SELECT Provider, TransactionReference, Amount, PreviousBalance, NewBalance, Narrative
FROM dbo.PartnerTransactions
ORDER BY TransactionTimestamp DESC;
```

## Project boundaries

The partner services do not implement NCB authentication, NCB account management, the banking UI, or NCB balance deductions. Person 2 can call the stable JSON routes above from the banking workflow and use the `newBalance`/transaction fields for the partner-credit result.
