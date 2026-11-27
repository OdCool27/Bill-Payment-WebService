using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Data.SqlClient;

namespace BillPaymentWebApp.PartnerServices
{
    public abstract class PartnerHttpHandler : IHttpHandler
    {
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();
        protected abstract string Provider { get; }
        public bool IsReusable { get { return false; } }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.Headers["Cache-Control"] = "no-store";

            if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                Write(context, 405, new { success = false, message = "Only POST requests are supported." });
                return;
            }

            try
            {
                string body;
                using (var reader = new StreamReader(context.Request.InputStream))
                    body = reader.ReadToEnd();

                if (string.IsNullOrWhiteSpace(body))
                    throw new PartnerValidationException("Request body is required.");

                string path = context.Request.AppRelativeCurrentExecutionFilePath;
                try
                {
                    if (path.IndexOf("/verify", StringComparison.OrdinalIgnoreCase) >= 0)
                        Verify(context, serializer.Deserialize<VerifyRequest>(body));
                    else
                        Pay(context, serializer.Deserialize<PaymentRequest>(body));
                }
                catch (InvalidOperationException)
                {
                    throw new PartnerValidationException("Malformed JSON request.");
                }
            }
            catch (ArgumentException)
            {
                Write(context, 400, new { success = false, message = "Malformed JSON request." });
            }
            catch (PartnerValidationException ex)
            {
                Write(context, 400, new { success = false, message = ex.Message });
            }
            catch (PartnerAccountNotFoundException)
            {
                Write(context, 404, new { success = false, provider = Provider, message = "Account not found." });
            }
            catch (InactivePartnerAccountException)
            {
                Write(context, 409, new { success = false, provider = Provider, message = "Account is inactive and cannot receive payments." });
            }
            catch (DuplicatePaymentException)
            {
                Write(context, 409, new { success = false, provider = Provider, message = "Payment reference has already been processed." });
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                Write(context, 409, new { success = false, provider = Provider, message = "Payment reference has already been processed." });
            }
            catch (SqlException)
            {
                Write(context, 500, new { success = false, provider = Provider, message = "The partner service could not complete the database operation." });
            }
            catch (Exception)
            {
                Write(context, 500, new { success = false, provider = Provider, message = "The partner service is unavailable." });
            }
        }

        private void Verify(HttpContext context, VerifyRequest request)
        {
            ValidateAccountNumber(request == null ? null : request.AccountNumber);
            AccountRecord account = new PartnerRepository().FindAccount(Provider, request.AccountNumber.Trim());
            if (account == null)
            {
                Write(context, 200, new { success = true, exists = false, provider = Provider, message = "Account not found." });
                return;
            }

            Write(context, 200, new
            {
                success = true,
                exists = true,
                provider = Provider,
                accountNumber = account.AccountNumber,
                accountHolder = account.AccountHolder,
                accountStatus = account.Status,
                message = string.Equals(account.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase)
                    ? "Account verified successfully."
                    : "Account exists but is inactive."
            });
        }

        private void Pay(HttpContext context, PaymentRequest request)
        {
            ValidatePayment(request);
            PaymentResult result = new PartnerRepository().ProcessPayment(Provider, request);
            Write(context, 200, new
            {
                success = true,
                provider = Provider,
                transactionReference = result.TransactionReference,
                accountNumber = result.AccountNumber,
                amount = result.Amount,
                previousBalance = result.PreviousBalance,
                newBalance = result.NewBalance,
                narrative = result.Narrative,
                transactionTimestamp = result.TransactionTimestamp,
                message = "Payment processed successfully."
            });
        }

        private static void ValidateAccountNumber(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Trim().Length > 50)
                throw new PartnerValidationException("A valid accountNumber is required.");
        }

        private static void ValidatePayment(PaymentRequest request)
        {
            if (request == null)
                throw new PartnerValidationException("Request body is required.");
            ValidateAccountNumber(request.AccountNumber);
            if (request.Amount <= 0 || request.Amount > 1000000000m)
                throw new PartnerValidationException("Amount must be greater than zero and within the supported limit.");
            if (string.IsNullOrWhiteSpace(request.PaymentReference) || request.PaymentReference.Trim().Length > 100)
                throw new PartnerValidationException("A valid paymentReference is required.");
            if (!string.IsNullOrWhiteSpace(request.Source) && request.Source.Length > 100)
                throw new PartnerValidationException("source is too long.");
            if (string.IsNullOrWhiteSpace(request.Narrative))
                request.Narrative = "Bill payment received from NCB online banking.";
            if (request.Narrative.Length > 500)
                throw new PartnerValidationException("narrative is too long.");
            request.AccountNumber = request.AccountNumber.Trim();
            request.PaymentReference = request.PaymentReference.Trim();
        }

        private void Write(HttpContext context, int statusCode, object value)
        {
            context.Response.StatusCode = statusCode;
            context.Response.Write(serializer.Serialize(value));
        }
    }
}
