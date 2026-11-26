using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;

namespace BillPaymentWebApp
{
    public partial class PayBills : System.Web.UI.Page
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

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
            public string Reference { get; set; }
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
                    userId, bankAccountNumber, biller, billerAccountNumber, amount, bankNarrative, paymentResult.Reference);

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
            catch (Exception)
            {
                lblMessage.CssClass = "text-danger d-block mb-3";
                lblMessage.Text = "Unable to process your payment right now. Please try again later.";
            }
        }

        private List<BankAccountItem> GetBankAccounts(int userId)
        {
            var connString = ConfigurationManager.ConnectionStrings["JamBankConnection"];
            if (connString != null)
            {
                var dbAccounts = new List<BankAccountItem>();
                using (var conn = new SqlConnection(connString.ConnectionString))
                using (var cmd = new SqlCommand("SELECT AccountNumber, AccountType, Nickname, Balance FROM Accounts WHERE UserId = @UserId", conn))
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
                if (dbAccounts.Count > 0)
                {
                    return dbAccounts;
                }
            }

            return new List<BankAccountItem>
            {
                new BankAccountItem { AccountNumber = "123456789", AccountType = "Savings", Nickname = "Main Savings", Balance = 25000.00m },
                new BankAccountItem { AccountNumber = "987654321", AccountType = "Chequing", Nickname = "Daily Chequing", Balance = 4300.50m }
            };
        }

        private List<LinkedAccountItem> GetLinkedAccounts(int userId)
        {
            var connString = ConfigurationManager.ConnectionStrings["JamBankConnection"];
            if (connString != null)
            {
                var dbLinked = new List<LinkedAccountItem>();
                using (var conn = new SqlConnection(connString.ConnectionString))
                using (var cmd = new SqlCommand(@"
                    SELECT Biller, AccountNumber, Nickname
                    FROM LinkedUtilityAccounts
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
                if (dbLinked.Count > 0)
                {
                    return dbLinked;
                }
            }

            return new List<LinkedAccountItem>
            {
                new LinkedAccountItem { Biller = "FLOW", AccountNumber = "4410", Nickname = "FLOW Internet" },
                new LinkedAccountItem { Biller = "SAGICOR", AccountNumber = "9082", Nickname = "Sagicor Life Policy" }
            };
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
            string baseUrl = ConfigurationManager.AppSettings["PartnerServiceBaseUrl"] ?? "https://localhost:44301/api/partner";
            string url = $"{baseUrl.TrimEnd('/')}/submit-payment";

            var payload = new
            {
                biller = biller,
                accountNumber = accountNumber,
                amount = amount,
                narrative = narrative
            };

            var serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(payload);

            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                HttpResponseMessage response = await Http.PostAsync(url, content);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new SubmitPaymentResult { Success = false, Message = "Partner payment service returned an error." };
                }

                var parsed = serializer.Deserialize<SubmitPaymentResult>(body);
                return parsed ?? new SubmitPaymentResult { Success = true, Narrative = narrative };
            }
        }

        private decimal ApplyBankDebitAndTransaction(
            int userId, string accountNumber, string biller, string billerAccountNumber,
            decimal amount, string narrative, string externalRef)
        {
            var accounts = GetBankAccounts(userId);
            var target = accounts.First(a => a.AccountNumber == accountNumber);
            decimal newBalance = target.Balance - amount;

            var connString = ConfigurationManager.ConnectionStrings["JamBankConnection"];
            if (connString != null)
            {
                using (var conn = new SqlConnection(connString.ConnectionString))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        using (var debitCmd = new SqlCommand(@"
                            UPDATE Accounts
                            SET Balance = Balance - @Amount
                            OUTPUT INSERTED.Balance
                            WHERE UserId = @UserId AND AccountNumber = @AccountNumber AND Balance >= @Amount", conn, tx))
                        {
                            debitCmd.Parameters.AddWithValue("@Amount", amount);
                            debitCmd.Parameters.AddWithValue("@UserId", userId);
                            debitCmd.Parameters.AddWithValue("@AccountNumber", accountNumber);
                            object scalar = debitCmd.ExecuteScalar();
                            if (scalar != null && scalar != DBNull.Value)
                            {
                                newBalance = Convert.ToDecimal(scalar);
                            }
                        }

                        using (var txnCmd = new SqlCommand(@"
                            INSERT INTO Transactions (UserId, TxnDate, Narrative, Amount, Status, Biller, BillerAccountNumber, ExternalReference)
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