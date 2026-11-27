using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;

namespace BillPaymentWebApp
{
    public partial class PayBills : System.Web.UI.Page
    {
        private sealed class BankAccountItem
        {
            public string AccountNumber { get; set; }
            public string AccountType { get; set; }
            public string Nickname { get; set; }
            public decimal Balance { get; set; }
        }

        private sealed class LinkedAccountItem
        {
            public string Biller { get; set; }
            public string AccountNumber { get; set; }
            public string Nickname { get; set; }
        }

        private sealed class SubmitPaymentResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public string Narrative { get; set; }
            public string TransactionReference { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserId"] == null)
            {
                Response.Redirect("Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                int userId = Convert.ToInt32(Session["UserId"]);
                BindBankAccounts(userId, Request.QueryString["account"]);
                BindLinkedAccounts(userId, Request.QueryString["biller"]);
                UpdateSelectedBalance();
            }
        }

        protected void ddlBankAccount_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSelectedBalance();
        }

        protected async void btnPayBill_Click(object sender, EventArgs e)
        {
            pnlReceipt.Visible = false;

            try
            {
                if (!chkConfirm.Checked)
                {
                    lblMessage.CssClass = "text-danger d-block mb-3";
                    lblMessage.Text = "Please tick the confirmation box before submitting your payment.";
                    return;
                }

                if (string.IsNullOrEmpty(ddlBankAccount.SelectedValue) || string.IsNullOrEmpty(ddlLinkedAccount.SelectedValue))
                {
                    lblMessage.CssClass = "text-danger d-block mb-3";
                    lblMessage.Text = "Please select both a JamBank account and a linked biller account.";
                    return;
                }

                if (!decimal.TryParse(txtAmount.Text.Trim(), NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
                {
                    lblMessage.CssClass = "text-danger d-block mb-3";
                    lblMessage.Text = "Please enter a valid payment amount greater than zero.";
                    return;
                }

                string bankAccountNumber = ddlBankAccount.SelectedValue;
                string[] parts = ddlLinkedAccount.SelectedValue.Split('|');
                if (parts.Length != 2)
                {
                    lblMessage.CssClass = "text-danger d-block mb-3";
                    lblMessage.Text = "Invalid linked biller account selected.";
                    return;
                }

                string biller = parts[0];
                string billerAccountNumber = parts[1];
                int userId = Convert.ToInt32(Session["UserId"]);
                string username = Session["Username"] != null ? Session["Username"].ToString() : $"User #{userId}";

                var accounts = GetBankAccounts(userId);
                var selectedAccount = accounts.FirstOrDefault(a => a.AccountNumber == bankAccountNumber);
                if (selectedAccount == null)
                {
                    lblMessage.CssClass = "text-danger d-block mb-3";
                    lblMessage.Text = "Selected JamBank account was not found.";
                    return;
                }

                if (amount > selectedAccount.Balance)
                {
                    lblMessage.CssClass = "text-danger d-block mb-3";
                    lblMessage.Text = "Insufficient funds in the selected JamBank account.";
                    return;
                }

                string bankNarrative = $"Bill payment to {biller} acct ...{GetLastFour(billerAccountNumber)} ({billerAccountNumber})";
                string partnerNarrative = $"JamBank payment from {username} ({bankAccountNumber}) to {biller} acct {billerAccountNumber}";

                SubmitPaymentResult paymentResult = await SubmitPaymentAsync(biller, billerAccountNumber, amount, partnerNarrative);
                if (!paymentResult.Success)
                {
                    lblMessage.CssClass = "text-danger d-block mb-3";
                    lblMessage.Text = string.IsNullOrWhiteSpace(paymentResult.Message)
                        ? "Partner system declined the payment."
                        : paymentResult.Message;
                    return;
                }

                decimal updatedBalance = ApplyBankDebitAndTransaction(
                    userId, bankAccountNumber, biller, billerAccountNumber, amount, bankNarrative, paymentResult.TransactionReference);

                BindBankAccounts(userId, bankAccountNumber);
                litSelectedBalance.Text = string.Format("{0:C}", updatedBalance);
                litUpdatedBalance.Text = string.Format("{0:C}", updatedBalance);
                litNarrative.Text = string.IsNullOrWhiteSpace(paymentResult.Narrative)
                    ? bankNarrative
                    : $"{bankNarrative} — {paymentResult.Narrative}";
                pnlReceipt.Visible = true;
                chkConfirm.Checked = false;
                txtAmount.Text = string.Empty;

                lblMessage.CssClass = "text-success d-block mb-3";
                lblMessage.Text = "Payment completed and recorded.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("PayBills submission failed: {0}", ex);
                lblMessage.CssClass = "text-danger d-block mb-3";
                lblMessage.Text = Context.IsDebuggingEnabled
                    ? "Unable to process your payment: " + Server.HtmlEncode(ex.GetType().Name + ": " + ex.Message)
                    : "Unable to process your payment right now. Please try again later.";
            }
        }

        private List<BankAccountItem> GetBankAccounts(int userId)
        {
            var connString = ConfigurationManager.ConnectionStrings["PartnerDatabase"];
            if (connString == null || string.IsNullOrWhiteSpace(connString.ConnectionString))
                throw new InvalidOperationException("The PartnerDatabase connection string is not configured.");

            var dbAccounts = new List<BankAccountItem>();
            using (var conn = new SqlConnection(connString.ConnectionString))
            using (var cmd = new SqlCommand("SELECT AccountNumber, AccountType, Nickname, Balance FROM dbo.JamBankAccounts WHERE UserId = @UserId", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dbAccounts.Add(new BankAccountItem
                        {
                            AccountNumber = reader["AccountNumber"].ToString(),
                            AccountType = reader["AccountType"].ToString(),
                            Nickname = reader["Nickname"].ToString(),
                            Balance = Convert.ToDecimal(reader["Balance"])
                        });
                    }
                }
            }

            return dbAccounts;
        }

        private List<LinkedAccountItem> GetLinkedAccounts(int userId)
        {
            var connString = ConfigurationManager.ConnectionStrings["PartnerDatabase"];
            if (connString == null || string.IsNullOrWhiteSpace(connString.ConnectionString))
                throw new InvalidOperationException("The PartnerDatabase connection string is not configured.");

            var dbLinked = new List<LinkedAccountItem>();
            using (var conn = new SqlConnection(connString.ConnectionString))
            using (var cmd = new SqlCommand(@"
                    SELECT Biller, AccountNumber, Nickname
                    FROM dbo.JamBankLinkedAccounts
                    WHERE UserId = @UserId AND IsVerified = 1", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dbLinked.Add(new LinkedAccountItem
                        {
                            Biller = reader["Biller"].ToString(),
                            AccountNumber = reader["AccountNumber"].ToString(),
                            Nickname = reader["Nickname"].ToString()
                        });
                    }
                }
            }

            return dbLinked;
        }

        private void BindBankAccounts(int userId, string preselectAccount)
        {
            ddlBankAccount.Items.Clear();
            var accounts = GetBankAccounts(userId);
            foreach (var acc in accounts)
            {
                var item = new ListItem($"{acc.Nickname} ({acc.AccountType} •••• {GetLastFour(acc.AccountNumber)})", acc.AccountNumber);
                item.Attributes["data-balance"] = acc.Balance.ToString(CultureInfo.InvariantCulture);
                ddlBankAccount.Items.Add(item);
            }

            if (!string.IsNullOrEmpty(preselectAccount) && ddlBankAccount.Items.FindByValue(preselectAccount) != null)
            {
                ddlBankAccount.SelectedValue = preselectAccount;
            }
        }

        private void BindLinkedAccounts(int userId, string preselectBiller)
        {
            ddlLinkedAccount.Items.Clear();
            ddlLinkedAccount.Items.Add(new ListItem("-- Select Linked Account --", ""));

            var linked = GetLinkedAccounts(userId);
            foreach (var itemData in linked)
            {
                string label = $"{itemData.Nickname} [{itemData.Biller} •••• {GetLastFour(itemData.AccountNumber)}]";
                string val = $"{itemData.Biller}|{itemData.AccountNumber}";
                ddlLinkedAccount.Items.Add(new ListItem(label, val));
            }

            if (!string.IsNullOrEmpty(preselectBiller))
            {
                ListItem match = ddlLinkedAccount.Items.Cast<ListItem>()
                    .FirstOrDefault(i => i.Value.StartsWith(preselectBiller + "|", StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    ddlLinkedAccount.SelectedValue = match.Value;
                }
            }
        }

        private void UpdateSelectedBalance()
        {
            if (ddlBankAccount.SelectedItem != null &&
                decimal.TryParse(ddlBankAccount.SelectedItem.Attributes["data-balance"], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal bal))
            {
                litSelectedBalance.Text = string.Format("{0:C}", bal);
            }
            else
            {
                litSelectedBalance.Text = "$0.00";
            }
        }

        private async Task<SubmitPaymentResult> SubmitPaymentAsync(string biller, string accountNumber, decimal amount, string narrative)
        {
            string baseUrl = ConfigurationManager.AppSettings["PartnerServiceBaseUrl"] ?? "http://localhost:53890";
            string provider = biller.Equals("SAGICOR", StringComparison.OrdinalIgnoreCase) ? "sagicor" : "flow";
            string url = $"{baseUrl.TrimEnd('/')}/api/{provider}/payments";
            string paymentReference = "NCB-" + Guid.NewGuid().ToString("N").ToUpperInvariant();

            var payload = new
            {
                accountNumber = accountNumber,
                amount = amount,
                paymentReference = paymentReference,
                source = "NCB",
                narrative = narrative
            };

            var serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(payload);

            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                string body;
                try
                {
                    body = await client.UploadStringTaskAsync(new Uri(url), "POST", json);
                }
                catch (WebException)
                {
                    return new SubmitPaymentResult { Success = false, Message = "Partner payment service returned an error." };
                }

                IDictionary<string, object> data = GetAsmxResponseData(serializer.DeserializeObject(body));
                return data == null
                    ? new SubmitPaymentResult { Success = false, Message = "Invalid partner service response." }
                    : new SubmitPaymentResult
                    {
                        Success = ReadBoolean(data, "Success"),
                        Message = ReadString(data, "Message"),
                        Narrative = ReadString(data, "Narrative"),
                        TransactionReference = ReadString(data, "TransactionReference")
                    };
            }
        }

        private static IDictionary<string, object> GetAsmxResponseData(object response)
        {
            var envelope = response as IDictionary<string, object>;
            if (envelope == null)
                return null;

            foreach (KeyValuePair<string, object> item in envelope)
            {
                if (string.Equals(item.Key, "d", StringComparison.OrdinalIgnoreCase))
                    return item.Value as IDictionary<string, object>;
            }

            return envelope;
        }

        private static bool ReadBoolean(IDictionary<string, object> values, string name)
        {
            foreach (KeyValuePair<string, object> item in values)
            {
                if (string.Equals(item.Key, name, StringComparison.OrdinalIgnoreCase))
                    return item.Value is bool && (bool)item.Value;
            }

            return false;
        }

        private static string ReadString(IDictionary<string, object> values, string name)
        {
            foreach (KeyValuePair<string, object> item in values)
            {
                if (string.Equals(item.Key, name, StringComparison.OrdinalIgnoreCase))
                    return item.Value == null ? null : Convert.ToString(item.Value);
            }

            return null;
        }

        private decimal ApplyBankDebitAndTransaction(
            int userId, string accountNumber, string biller, string billerAccountNumber,
            decimal amount, string narrative, string externalRef)
        {
            var accounts = GetBankAccounts(userId);
            var target = accounts.First(a => a.AccountNumber == accountNumber);
            decimal newBalance = target.Balance - amount;

            var connString = ConfigurationManager.ConnectionStrings["PartnerDatabase"];
            if (connString == null || string.IsNullOrWhiteSpace(connString.ConnectionString))
                throw new InvalidOperationException("The PartnerDatabase connection string is not configured.");

            using (var conn = new SqlConnection(connString.ConnectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    using (var debitCmd = new SqlCommand(@"
                        UPDATE dbo.JamBankAccounts
                        SET Balance = Balance - @Amount
                        OUTPUT INSERTED.Balance
                        WHERE UserId = @UserId AND AccountNumber = @AccountNumber AND Balance >= @Amount", conn, tx))
                    {
                        debitCmd.Parameters.AddWithValue("@Amount", amount);
                        debitCmd.Parameters.AddWithValue("@UserId", userId);
                        debitCmd.Parameters.AddWithValue("@AccountNumber", accountNumber);
                        object scalar = debitCmd.ExecuteScalar();
                        if (scalar == null || scalar == DBNull.Value)
                            throw new InvalidOperationException("The JamBank account balance was not updated; confirm the selected account and available balance.");

                        newBalance = Convert.ToDecimal(scalar);
                    }

                    using (var txnCmd = new SqlCommand(@"
                        INSERT INTO dbo.JamBankTransactions (UserId, TxnDate, Narrative, Amount, Status, Biller, BillerAccountNumber, ExternalReference)
                        VALUES (@UserId, @TxnDate, @Narrative, @Amount, 'Completed', @Biller, @BillerAccountNumber, @ExternalReference)", conn, tx))
                    {
                        txnCmd.Parameters.AddWithValue("@UserId", userId);
                        txnCmd.Parameters.AddWithValue("@TxnDate", DateTime.Now);
                        txnCmd.Parameters.AddWithValue("@Narrative", narrative);
                        txnCmd.Parameters.AddWithValue("@Amount", -amount);
                        txnCmd.Parameters.AddWithValue("@Biller", biller);
                        txnCmd.Parameters.AddWithValue("@BillerAccountNumber", billerAccountNumber);
                        txnCmd.Parameters.AddWithValue("@ExternalReference", (object)externalRef ?? DBNull.Value);
                        txnCmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
            }

            return newBalance;
        }

        private static string GetLastFour(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= 4 ? value : value.Substring(value.Length - 4);
        }
    }
}
