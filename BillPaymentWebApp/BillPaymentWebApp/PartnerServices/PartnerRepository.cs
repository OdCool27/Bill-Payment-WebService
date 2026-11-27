using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace BillPaymentWebApp.PartnerServices
{
    public sealed class PartnerRepository
    {
        private readonly string connectionString;

        public PartnerRepository()
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["PartnerDatabase"];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
                throw new InvalidOperationException("PartnerDatabase connection string is not configured.");

            connectionString = settings.ConnectionString;
        }

        public AccountRecord FindAccount(string provider, string accountNumber)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT AccountId, AccountNumber, AccountHolder, CurrentBalance, AccountStatus
FROM dbo.PartnerAccounts
WHERE Provider = @Provider AND AccountNumber = @AccountNumber;";
                command.Parameters.AddWithValue("@Provider", provider);
                command.Parameters.AddWithValue("@AccountNumber", accountNumber);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return ReadAccount(reader);
                }
            }
        }

        public PaymentResult ProcessPayment(string provider, PaymentRequest request)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        AccountRecord account = LockAccount(connection, transaction, provider, request.AccountNumber);
                        if (account == null)
                            throw new PartnerAccountNotFoundException();
                        if (!string.Equals(account.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                            throw new InactivePartnerAccountException();

                        if (PaymentReferenceExists(connection, transaction, provider, request.PaymentReference))
                            throw new DuplicatePaymentException();

                        decimal newBalance = account.Balance + request.Amount;
                        UpdateBalance(connection, transaction, account.AccountId, newBalance);
                        DateTime timestamp = DateTime.UtcNow;
                        InsertTransaction(connection, transaction, account, provider, request, newBalance, timestamp);
                        transaction.Commit();

                        return new PaymentResult
                        {
                            TransactionReference = request.PaymentReference,
                            AccountNumber = account.AccountNumber,
                            Amount = request.Amount,
                            PreviousBalance = account.Balance,
                            NewBalance = newBalance,
                            Narrative = request.Narrative,
                            TransactionTimestamp = timestamp
                        };
                    }
                    catch
                    {
                        try { transaction.Rollback(); } catch (InvalidOperationException) { }
                        throw;
                    }
                }
            }
        }

        private static AccountRecord LockAccount(SqlConnection connection, SqlTransaction transaction, string provider, string accountNumber)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"SELECT AccountId, AccountNumber, AccountHolder, CurrentBalance, AccountStatus
FROM dbo.PartnerAccounts WITH (UPDLOCK, HOLDLOCK)
WHERE Provider = @Provider AND AccountNumber = @AccountNumber;";
                command.Parameters.AddWithValue("@Provider", provider);
                command.Parameters.AddWithValue("@AccountNumber", accountNumber);
                using (SqlDataReader reader = command.ExecuteReader())
                    return reader.Read() ? ReadAccount(reader) : null;
            }
        }

        private static bool PaymentReferenceExists(SqlConnection connection, SqlTransaction transaction, string provider, string reference)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT COUNT(1) FROM dbo.PartnerTransactions WITH (UPDLOCK, HOLDLOCK) WHERE Provider = @Provider AND TransactionReference = @Reference;";
                command.Parameters.AddWithValue("@Provider", provider);
                command.Parameters.AddWithValue("@Reference", reference);
                return (int)command.ExecuteScalar() > 0;
            }
        }

        private static void UpdateBalance(SqlConnection connection, SqlTransaction transaction, long accountId, decimal newBalance)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "UPDATE dbo.PartnerAccounts SET CurrentBalance = @Balance WHERE AccountId = @AccountId;";
                command.Parameters.AddWithValue("@Balance", newBalance);
                command.Parameters.AddWithValue("@AccountId", accountId);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertTransaction(SqlConnection connection, SqlTransaction transaction, AccountRecord account, string provider, PaymentRequest request, decimal newBalance, DateTime timestamp)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"INSERT INTO dbo.PartnerTransactions
(AccountId, Provider, TransactionReference, TransactionType, Amount, PreviousBalance, NewBalance, Narrative, TransactionTimestamp, PaymentSource)
VALUES (@AccountId, @Provider, @Reference, 'BILL_PAYMENT', @Amount, @PreviousBalance, @NewBalance, @Narrative, @Timestamp, @Source);";
                command.Parameters.AddWithValue("@AccountId", account.AccountId);
                command.Parameters.AddWithValue("@Provider", provider);
                command.Parameters.AddWithValue("@Reference", request.PaymentReference);
                command.Parameters.AddWithValue("@Amount", request.Amount);
                command.Parameters.AddWithValue("@PreviousBalance", account.Balance);
                command.Parameters.AddWithValue("@NewBalance", newBalance);
                command.Parameters.AddWithValue("@Narrative", request.Narrative);
                command.Parameters.AddWithValue("@Timestamp", timestamp);
                command.Parameters.AddWithValue("@Source", (object)request.Source ?? DBNull.Value);
                command.ExecuteNonQuery();
            }
        }

        private static AccountRecord ReadAccount(SqlDataReader reader)
        {
            return new AccountRecord
            {
                AccountId = reader.GetInt64(0),
                AccountNumber = reader.GetString(1),
                AccountHolder = reader.GetString(2),
                Balance = reader.GetDecimal(3),
                Status = reader.GetString(4)
            };
        }
    }
}
