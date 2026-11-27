using System;

namespace BillPaymentWebApp.PartnerServices
{
    public sealed class VerifyRequest
    {
        public string AccountNumber { get; set; }
    }

    public sealed class PaymentRequest
    {
        public string AccountNumber { get; set; }
        public decimal Amount { get; set; }
        public string PaymentReference { get; set; }
        public string Source { get; set; }
        public string Narrative { get; set; }
    }

    public sealed class AccountRecord
    {
        public long AccountId { get; set; }
        public string AccountNumber { get; set; }
        public string AccountHolder { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; }
    }

    public sealed class PaymentResult
    {
        public string TransactionReference { get; set; }
        public string AccountNumber { get; set; }
        public decimal Amount { get; set; }
        public decimal PreviousBalance { get; set; }
        public decimal NewBalance { get; set; }
        public string Narrative { get; set; }
        public DateTime TransactionTimestamp { get; set; }
    }

    public sealed class PartnerValidationException : Exception
    {
        public PartnerValidationException(string message) : base(message) { }
    }

    public sealed class DuplicatePaymentException : Exception
    {
        public DuplicatePaymentException() : base("Payment reference has already been processed.") { }
    }

    public sealed class PartnerAccountNotFoundException : Exception
    {
        public PartnerAccountNotFoundException() : base("Account not found.") { }
    }

    public sealed class InactivePartnerAccountException : Exception
    {
        public InactivePartnerAccountException() : base("Account is inactive and cannot receive payments.") { }
    }
}
