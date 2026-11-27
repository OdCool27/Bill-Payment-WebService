using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

namespace BillPaymentWebApp
{
    internal static class JamBankSchemaInitializer
    {
        private const string LockResource = "BillPaymentWebService.JamBankSchema";

        public static void Initialize()
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["PartnerDatabase"];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
                throw new InvalidOperationException("PartnerDatabase must be configured before JamBank tables can be initialized.");

            string scriptPath = Path.Combine(HttpRuntime.AppDomainAppPath, "Database", "003_jambank_schema.sql");
            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("The JamBank database migration script was not deployed with the application.", scriptPath);

            string[] batches = Regex.Split(
                File.ReadAllText(scriptPath),
                @"^\s*GO\s*(?:--.*)?$",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            using (var connection = new SqlConnection(settings.ConnectionString))
            {
                connection.Open();
                AcquireInitializationLock(connection);
                try
                {
                    if (!JamBankTablesExist(connection))
                    {
                        foreach (string batch in batches)
                        {
                            if (string.IsNullOrWhiteSpace(batch))
                                continue;

                            using (var command = new SqlCommand(batch, connection))
                            {
                                command.CommandTimeout = 60;
                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    if (!JamBankTablesExist(connection))
                        throw new InvalidOperationException("JamBank table initialization completed without creating all required tables.");
                }
                finally
                {
                    ReleaseInitializationLock(connection);
                }
            }
        }

        private static bool JamBankTablesExist(SqlConnection connection)
        {
            const string tablesSql = @"
                SELECT COUNT(*)
                FROM sys.tables
                WHERE schema_id = SCHEMA_ID(N'dbo')
                  AND name IN (N'JamBankAccounts', N'JamBankLinkedAccounts', N'JamBankTransactions');";

            using (var command = new SqlCommand(tablesSql, connection))
            {
                if (Convert.ToInt32(command.ExecuteScalar()) != 3)
                    return false;
            }

            const string seedSql = @"
                SELECT CASE WHEN
                    EXISTS (SELECT 1 FROM dbo.JamBankAccounts WHERE UserId = 1 AND AccountNumber = N'123456789') AND
                    EXISTS (SELECT 1 FROM dbo.JamBankAccounts WHERE UserId = 1 AND AccountNumber = N'987654321')
                THEN 1 ELSE 0 END;";

            using (var command = new SqlCommand(seedSql, connection))
                return Convert.ToInt32(command.ExecuteScalar()) == 1;
        }

        private static void AcquireInitializationLock(SqlConnection connection)
        {
            const string sql = @"
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock
                    @Resource = @Resource,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Session',
                    @LockTimeout = 30000;
                SELECT @result;";

            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Resource", LockResource);
                int result = Convert.ToInt32(command.ExecuteScalar());
                if (result < 0)
                    throw new InvalidOperationException("Could not acquire the database lock for JamBank schema initialization.");
            }
        }

        private static void ReleaseInitializationLock(SqlConnection connection)
        {
            if (connection.State != System.Data.ConnectionState.Open)
                return;

            const string sql = "EXEC sys.sp_releaseapplock @Resource = @Resource, @LockOwner = 'Session';";
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Resource", LockResource);
                command.ExecuteNonQuery();
            }
        }
    }
}
