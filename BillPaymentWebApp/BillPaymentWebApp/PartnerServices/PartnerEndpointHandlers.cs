using System;
using System.Web.Script.Services;
using System.Web.Services;
using System.Xml.Serialization;

namespace BillPaymentWebApp.PartnerServices
{
    public sealed class FlowVerifyHandler : PartnerHttpHandler
    {
        protected override string Provider { get { return "FLOW"; } }
    }

    public sealed class FlowPaymentHandler : PartnerHttpHandler
    {
        protected override string Provider { get { return "FLOW"; } }
    }

    public sealed class SagicorVerifyHandler : PartnerHttpHandler
    {
        protected override string Provider { get { return "SAGICOR"; } }
    }

    public sealed class SagicorPaymentHandler : PartnerHttpHandler
    {
        protected override string Provider { get { return "SAGICOR"; } }
    }

    public sealed class AsmxAccountVerificationResponse
    {
        public bool Success { get; set; }
        public bool Exists { get; set; }
        public string Provider { get; set; }
        public string AccountNumber { get; set; }
        public string AccountHolder { get; set; }
        public string AccountStatus { get; set; }
        public string Message { get; set; }
    }

    public sealed class AsmxPaymentRequest
    {
        public string AccountNumber { get; set; }
        public decimal Amount { get; set; }
        public string PaymentReference { get; set; }
        public string Source { get; set; }
        public string Narrative { get; set; }
    }

    public sealed class AsmxPaymentResponse
    {
        public bool Success { get; set; }
        public string Provider { get; set; }
        public string TransactionReference { get; set; }
        public string AccountNumber { get; set; }
        public decimal Amount { get; set; }
        public decimal PreviousBalance { get; set; }
        public decimal NewBalance { get; set; }
        public string Narrative { get; set; }
        public DateTime TransactionTimestamp { get; set; }
        public string Message { get; set; }
    }

    internal static class AsmxPartnerOperations
    {
        public static AsmxAccountVerificationResponse Verify(string provider, string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                return new AsmxAccountVerificationResponse { Success = false, Provider = provider, Message = "Account number is required." };

            AccountRecord account = new PartnerRepository().FindAccount(provider, accountNumber.Trim());
            if (account == null)
            {
                return new AsmxAccountVerificationResponse
                {
                    Success = true,
                    Exists = false,
                    Provider = provider,
                    Message = "Account not found."
                };
            }

            return new AsmxAccountVerificationResponse
            {
                Success = true,
                Exists = true,
                Provider = provider,
                AccountNumber = account.AccountNumber,
                AccountHolder = account.AccountHolder,
                AccountStatus = account.Status,
                Message = string.Equals(account.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase)
                    ? "Account verified successfully."
                    : "Account exists but is inactive."
            };
        }

        public static AsmxPaymentResponse Pay(string provider, AsmxPaymentRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.AccountNumber))
                return Failure(provider, "Account number is required.");
            if (request.Amount <= 0)
                return Failure(provider, "Amount must be greater than zero.");
            if (string.IsNullOrWhiteSpace(request.PaymentReference))
                return Failure(provider, "Payment reference is required.");

            request.AccountNumber = request.AccountNumber.Trim();
            request.PaymentReference = request.PaymentReference.Trim();
            if (string.IsNullOrWhiteSpace(request.Narrative))
                request.Narrative = "Bill payment received from NCB online banking.";

            try
            {
                PaymentResult result = new PartnerRepository().ProcessPayment(provider, new PaymentRequest
                {
                    AccountNumber = request.AccountNumber,
                    Amount = request.Amount,
                    PaymentReference = request.PaymentReference,
                    Source = request.Source,
                    Narrative = request.Narrative
                });

                return new AsmxPaymentResponse
                {
                    Success = true,
                    Provider = provider,
                    TransactionReference = result.TransactionReference,
                    AccountNumber = result.AccountNumber,
                    Amount = result.Amount,
                    PreviousBalance = result.PreviousBalance,
                    NewBalance = result.NewBalance,
                    Narrative = result.Narrative,
                    TransactionTimestamp = result.TransactionTimestamp,
                    Message = "Payment processed successfully."
                };
            }
            catch (PartnerAccountNotFoundException ex) { return Failure(provider, ex.Message); }
            catch (InactivePartnerAccountException ex) { return Failure(provider, ex.Message); }
            catch (DuplicatePaymentException ex) { return Failure(provider, ex.Message); }
        }

        private static AsmxPaymentResponse Failure(string provider, string message)
        {
            return new AsmxPaymentResponse { Success = false, Provider = provider, Message = message };
        }
    }

    [WebService(Namespace = "http://cit4009.example.com/flow")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ScriptService]
    public sealed class FlowService : WebService
    {
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public AsmxAccountVerificationResponse VerifyAccount(string accountNumber)
        {
            return AsmxPartnerOperations.Verify("FLOW", accountNumber);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public AsmxPaymentResponse SubmitPayment(AsmxPaymentRequest request)
        {
            return AsmxPartnerOperations.Pay("FLOW", request);
        }
    }

    [WebService(Namespace = "http://cit4009.example.com/sagicor")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ScriptService]
    public sealed class SagicorService : WebService
    {
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public AsmxAccountVerificationResponse VerifyAccount(string accountNumber)
        {
            return AsmxPartnerOperations.Verify("SAGICOR", accountNumber);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public AsmxPaymentResponse SubmitPayment(AsmxPaymentRequest request)
        {
            return AsmxPartnerOperations.Pay("SAGICOR", request);
        }
    }
}
