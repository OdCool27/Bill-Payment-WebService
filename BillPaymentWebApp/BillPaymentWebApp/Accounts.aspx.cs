using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace BillPaymentWebApp
{
    public partial class Accounts : System.Web.UI.Page
    {
        private sealed class AccountItem
        {
            public string AccountNumber { get; set; }
            public string AccountType { get; set; }
            public string Nickname { get; set; }
            public decimal Balance { get; set; }
        }

        private sealed class TransactionItem
        {
            public DateTime TxnDate { get; set; }
            public string Narrative { get; set; }
            public decimal Amount { get; set; }
            public string Status { get; set; }
        }

        private sealed class LinkedAccountItem
        {
            public string Biller { get; set; }
            public string AccountNumber { get; set; }
            public string Nickname { get; set; }
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
                litUsername.Text = Session["Username"] != null ? Server.HtmlEncode(Session["Username"].ToString()) : "";

                if (Session["LastLogin"] is DateTime last)
                {
                    litLastLogin.Text = string.Format(
                        "<small class='text-muted'>Last log on: {0:dddd, d MMMM yyyy} at {0:h:mm tt}</small>", last);
                }

                LoadAccounts(userId);
                LoadLinkedAccounts(userId);
                LoadTransactions(userId);
                LoadSummary(userId);
            }
        }

        private string GetDatabaseConnectionString()
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["PartnerDatabase"];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
                throw new InvalidOperationException("The PartnerDatabase connection string is not configured.");

            return settings.ConnectionString;
        }

        private List<AccountItem> GetAccounts(int userId)
        {
            var accounts = new List<AccountItem>();
            using (var conn = new SqlConnection(GetDatabaseConnectionString()))
            using (var cmd = new SqlCommand(@"
                SELECT AccountNumber, AccountType, Nickname, Balance
                FROM dbo.JamBankAccounts
                WHERE UserId = @UserId", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        accounts.Add(new AccountItem
                        {
                            AccountNumber = Convert.ToString(reader["AccountNumber"]),
                            AccountType = Convert.ToString(reader["AccountType"]),
                            Nickname = Convert.ToString(reader["Nickname"]),
                            Balance = Convert.ToDecimal(reader["Balance"])
                        });
                    }
                }
            }

            return accounts;
        }

        private List<LinkedAccountItem> GetLinkedAccounts(int userId)
        {
            var linkedAccounts = new List<LinkedAccountItem>();
            using (var conn = new SqlConnection(GetDatabaseConnectionString()))
            using (var cmd = new SqlCommand(@"
                SELECT Biller, AccountNumber, Nickname
                FROM dbo.JamBankLinkedAccounts
                WHERE UserId = @UserId AND IsVerified = 1
                ORDER BY Nickname", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        linkedAccounts.Add(new LinkedAccountItem
                        {
                            Biller = Convert.ToString(reader["Biller"]),
                            AccountNumber = Convert.ToString(reader["AccountNumber"]),
                            Nickname = Convert.ToString(reader["Nickname"])
                        });
                    }
                }
            }

            return linkedAccounts;
        }

        private List<TransactionItem> GetTransactions(int userId)
        {
            var transactions = new List<TransactionItem>();
            using (var conn = new SqlConnection(GetDatabaseConnectionString()))
            using (var cmd = new SqlCommand(@"
                SELECT TxnDate, Narrative, Amount, Status
                FROM dbo.JamBankTransactions
                WHERE UserId = @UserId
                ORDER BY TxnDate DESC", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        transactions.Add(new TransactionItem
                        {
                            TxnDate = Convert.ToDateTime(reader["TxnDate"]),
                            Narrative = Convert.ToString(reader["Narrative"]),
                            Amount = Convert.ToDecimal(reader["Amount"]),
                            Status = Convert.ToString(reader["Status"])
                        });
                    }
                }
            }

            return transactions;
        }

        private void LoadAccounts(int userId)
        {
            try
            {
                List<AccountItem> accounts = GetAccounts(userId);
                foreach (AccountItem account in accounts)
                {
                    string number = account.AccountNumber ?? "";
                    string lastFour = number.Length <= 4 ? number : number.Substring(number.Length - 4);
                    var card = new HtmlGenericControl("div");
                    card.Attributes["class"] = "col-md-6 col-lg-4";
                    card.InnerHtml = string.Format(
                        "<div class='account-card'><div class='acc-chip'></div><div><div class='acc-label'>{0}</div><div class='acc-nickname'>{1}</div><div class='acc-number'>&bull;&bull;&bull;&bull; {2}</div></div><div class='d-flex justify-content-between align-items-end mt-3'><div><div class='acc-balance'>{3}</div><a class='acc-details' href='#RecentTransactions'>View details</a></div><a class='btn btn-gold btn-sm' href='PayBills.aspx?account={4}'>Pay Bill</a></div></div>",
                        Server.HtmlEncode(account.AccountType), Server.HtmlEncode(account.Nickname),
                        Server.HtmlEncode(lastFour), Server.HtmlEncode(string.Format("{0:C}", account.Balance)),
                        HttpUtility.UrlEncode(number));
                    pnlAccountCards.Controls.Add(card);
                }

                if (accounts.Count == 0)
                    lblMessage.Text = "No JamBank accounts were found for this profile.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Accounts page could not load bank accounts: {0}", ex);
                SetLoadError("bank accounts", ex);
            }
        }

        private void LoadLinkedAccounts(int userId)
        {
            try
            {
                List<LinkedAccountItem> linkedAccounts = GetLinkedAccounts(userId);
                pnlLinkedAccounts.Controls.Clear();
                litBillers.Text = linkedAccounts.Count.ToString();

                if (linkedAccounts.Count == 0)
                {
                    var empty = new HtmlGenericControl("p");
                    empty.Attributes["class"] = "text-muted mb-0";
                    empty.InnerText = "No biller accounts are linked yet.";
                    pnlLinkedAccounts.Controls.Add(empty);
                    return;
                }

                foreach (LinkedAccountItem linked in linkedAccounts)
                {
                    string number = linked.AccountNumber ?? "";
                    string lastFour = number.Length <= 4 ? number : number.Substring(number.Length - 4);
                    string label = string.Equals(linked.Biller, "SAGICOR", StringComparison.OrdinalIgnoreCase)
                        ? "SAGICOR LIFE"
                        : linked.Biller;
                    var row = new HtmlGenericControl("div");
                    row.Attributes["class"] = "biller-card";
                    row.InnerHtml = string.Format(
                        "<div><b>{0}</b><span>{1} &bull;&bull;&bull;&bull; {2} &middot; verified &amp; linked</span></div><a class='btn btn-gold btn-sm' href='PayBills.aspx?biller={3}'>Pay</a>",
                        Server.HtmlEncode(label), Server.HtmlEncode(linked.Nickname),
                        Server.HtmlEncode(lastFour), HttpUtility.UrlEncode(linked.Biller));
                    pnlLinkedAccounts.Controls.Add(row);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Accounts page could not load linked billers: {0}", ex);
                SetLoadError("linked biller accounts", ex);
            }
        }

        private void LoadTransactions(int userId)
        {
            try
            {
                List<TransactionItem> transactions = GetTransactions(userId);
                pnlNoTxn.Visible = transactions.Count == 0;
                gvTransactions.Visible = transactions.Count > 0;
                gvTransactions.DataSource = transactions;
                gvTransactions.DataBind();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Accounts page could not load transactions: {0}", ex);
                SetLoadError("transactions", ex);
            }
        }

        private void LoadSummary(int userId)
        {
            try
            {
                List<AccountItem> accounts = GetAccounts(userId);
                List<TransactionItem> transactions = GetTransactions(userId);
                litTotalBalance.Text = string.Format("{0:C}", accounts.Sum(account => account.Balance));
                litPayMonth.Text = transactions.Count(transaction =>
                    transaction.Amount < 0 && transaction.TxnDate.Year == DateTime.Today.Year &&
                    transaction.TxnDate.Month == DateTime.Today.Month).ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Accounts page could not load summary: {0}", ex);
                SetLoadError("account summary", ex);
            }
        }

        private void SetLoadError(string dataName, Exception ex)
        {
            lblMessage.CssClass = "text-danger d-block mb-2";
            lblMessage.Text = Context.IsDebuggingEnabled
                ? "We could not load " + Server.HtmlEncode(dataName) + ": " + Server.HtmlEncode(ex.GetType().Name + ": " + ex.Message)
                : "We could not load your banking information right now. Please try again later.";
        }

        protected void gvTransactions_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            object raw = DataBinder.Eval(e.Row.DataItem, "Amount");
            if (raw != null && decimal.TryParse(raw.ToString(), out decimal amount))
                e.Row.Cells[2].CssClass = amount < 0 ? "txn-out" : "txn-in";
        }

        protected void btnDownload_Click(object sender, EventArgs e)
        {
            List<TransactionItem> transactions = GetTransactions(Convert.ToInt32(Session["UserId"]));
            var csv = new StringBuilder();
            csv.AppendLine("Date,Description,Amount,Status");
            foreach (TransactionItem transaction in transactions)
            {
                string narrative = (transaction.Narrative ?? "").Replace("\"", "\"\"");
                csv.AppendLine(string.Format("{0},\"{1}\",{2},{3}",
                    transaction.TxnDate.ToString("yyyy-MM-dd"), narrative, transaction.Amount, transaction.Status));
            }

            Response.Clear();
            Response.ContentType = "text/csv";
            Response.AddHeader("content-disposition", "attachment;filename=JamBank-Statement.csv");
            Response.Write(csv.ToString());
            Response.End();
        }
    }
}
