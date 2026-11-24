using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace BillPaymentWebApp
{
    public partial class Accounts : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Gate the page: anonymous users go to Login
            if (Session["UserId"] == null)
            {
                Response.Redirect("Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                litUsername.Text = Session["Username"] != null ? Session["Username"].ToString() : "";

                // Show when the user last logged on (recorded at successful logon)
                if (Session["LastLogin"] is DateTime last)
                {
                    litLastLogin.Text = string.Format(
                        "<small class='text-muted'>Last log on: {0:dddd, d MMMM yyyy} at {0:h:mm tt}</small>",
                        last);
                }

                LoadAccounts();
                LoadTransactions();
                LoadSummary();
            }
        }

        private List<object> GetAccountsStub()
        {
            // TODO: replace stub with real database query once the team schema is merged
            // SELECT AccountNumber, AccountType, Nickname, Balance FROM Accounts WHERE UserId = @userId
            return new List<object>
            {
                new { AccountNumber = "123456789", AccountType = "Savings", Nickname = "Main Savings", Balance = 25000.00m },
                new { AccountNumber = "987654321", AccountType = "Chequing", Nickname = "Daily Chequing", Balance = 4300.50m }
            };
        }

        private List<object> GetTransactionsStub()
        {
            // TODO: replace stub with real database query once the team schema is merged
            // SELECT TxnDate, Narrative, Amount, Status FROM Transactions WHERE UserId = @userId ORDER BY TxnDate DESC
            return new List<object>
            {
                new { TxnDate = DateTime.Today.AddDays(-1), Narrative = "Bill payment to FLOW acct ...4410", Amount = -125.00m, Status = "Completed" },
                new { TxnDate = DateTime.Today.AddDays(-3), Narrative = "Bill payment to SAGICOR LIFE ...9082", Amount = -350.00m, Status = "Completed" },
                new { TxnDate = DateTime.Today.AddDays(-8), Narrative = "Deposit - Payroll", Amount = 2500.00m, Status = "Completed" }
            };
        }

        private void LoadAccounts()
        {
            try
            {
                var accounts = GetAccountsStub();

                // Build the navy summary cards: chip graphic, nickname, masked number,
                // balance with a details anchor, and a gold Pay Bill button
                foreach (var acc in accounts)
                {
                    string number = acc.GetType().GetProperty("AccountNumber").GetValue(acc).ToString();
                    string type = acc.GetType().GetProperty("AccountType").GetValue(acc).ToString();
                    string nickname = acc.GetType().GetProperty("Nickname").GetValue(acc).ToString();
                    string balance = string.Format("{0:C}", acc.GetType().GetProperty("Balance").GetValue(acc));

                    var card = new HtmlGenericControl("div");
                    card.Attributes["class"] = "col-md-6 col-lg-4";

                    var inner = new StringBuilder();
                    inner.Append("<div class='account-card'>");
                    inner.Append("<div class='acc-chip'></div>");
                    inner.Append("<div>");
                    inner.Append($"<div class='acc-label'>{type}</div>");
                    inner.Append($"<div class='acc-nickname'>{Server.HtmlEncode(nickname)}</div>");
                    inner.Append($"<div class='acc-number'>&bull;&bull;&bull;&bull; {number.Substring(6)}</div>");
                    inner.Append("</div>");
                    inner.Append("<div class='d-flex justify-content-between align-items-end mt-3'>");
                    inner.Append("<div>");
                    inner.Append($"<div class='acc-balance'>{balance}</div>");
                    inner.Append("<a class='acc-details' href='#RecentTransactions'>View details</a>");
                    inner.Append("</div>");
                    inner.Append($"<a class='btn btn-gold btn-sm' href='PayBills.aspx?account={number}'>Pay Bill</a>");
                    inner.Append("</div>");
                    inner.Append("</div>");

                    card.InnerHtml = inner.ToString();
                    pnlAccountCards.Controls.Add(card);
                }
            }
            catch (Exception)
            {
                lblMessage.Text = "We could not load your accounts right now. Please try again later.";
            }
        }

        private void LoadTransactions()
        {
            try
            {
                var transactions = GetTransactionsStub();

                // Friendly empty state instead of a bare table
                pnlNoTxn.Visible = transactions.Count == 0;
                gvTransactions.Visible = transactions.Count > 0;

                gvTransactions.DataSource = transactions;
                gvTransactions.DataBind();
            }
            catch (Exception)
            {
                lblMessage.Text = "We could not load your transactions right now. Please try again later.";
            }
        }

        private void LoadSummary()
        {
            try
            {
                var accounts = GetAccountsStub();
                var transactions = GetTransactionsStub();

                decimal total = accounts.Sum(a => (decimal)a.GetType().GetProperty("Balance").GetValue(a));
                litTotalBalance.Text = string.Format("{0:C}", total);

                // TODO: populate the Linked Biller Accounts panel and this count from the database
                // once the team schema is merged.
                // SELECT COUNT(*) FROM LinkedUtilityAccounts WHERE UserId = @userId
                litBillers.Text = "2";

                int payMonth = transactions.Count(t =>
                {
                    var amount = (decimal)t.GetType().GetProperty("Amount").GetValue(t);
                    var date = (DateTime)t.GetType().GetProperty("TxnDate").GetValue(t);
                    return amount < 0 && date.Year == DateTime.Today.Year && date.Month == DateTime.Today.Month;
                });
                litPayMonth.Text = payMonth.ToString();
            }
            catch (Exception)
            {
                // Summary is decorative; never block the page on it
            }
        }

        protected void gvTransactions_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            // Color the Amount cell green for credits, red for debits
            object raw = DataBinder.Eval(e.Row.DataItem, "Amount");
            if (raw != null && decimal.TryParse(raw.ToString(), out decimal amount))
            {
                e.Row.Cells[2].CssClass = amount < 0 ? "txn-out" : "txn-in";
            }
        }

        protected void btnDownload_Click(object sender, EventArgs e)
        {
            // Export the current transaction list as a CSV statement download.
            // TODO: once the schema is merged this automatically reflects real data
            // because it reads from the same source as the grid.
            var transactions = GetTransactionsStub();

            var csv = new StringBuilder();
            csv.AppendLine("Date,Description,Amount,Status");
            foreach (var t in transactions)
            {
                string date = ((DateTime)t.GetType().GetProperty("TxnDate").GetValue(t)).ToString("yyyy-MM-dd");
                string narrative = t.GetType().GetProperty("Narrative").GetValue(t).ToString();
                string amount = t.GetType().GetProperty("Amount").GetValue(t).ToString();
                string status = t.GetType().GetProperty("Status").GetValue(t).ToString();
                csv.AppendLine(string.Format("{0},\"{1}\",{2},{3}", date, narrative.Replace("\"", "\"\""), amount, status));
            }

            Response.Clear();
            Response.ContentType = "text/csv";
            Response.AddHeader("content-disposition", "attachment;filename=JamBank-Statement.csv");
            Response.Write(csv.ToString());
            Response.End();
        }
    }
}
