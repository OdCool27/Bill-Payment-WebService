using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace BillPaymentWebApp
{
    public partial class LinkAccount : System.Web.UI.Page
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserId"] == null)
            {
                Response.Redirect("Login.aspx");
                return;
            }
        }

        protected async void btnLinkAccount_Click(object sender, EventArgs e)
        {
            try
            {
                string biller = ddlBiller.SelectedValue.Trim();
                string accountNumber = txtAccountNumber.Text.Trim();
                string nickname = string.IsNullOrWhiteSpace(txtNickname.Text)
                    ? $"{biller} - {accountNumber}"
                    : txtNickname.Text.Trim();

                if (string.IsNullOrEmpty(biller) || string.IsNullOrEmpty(accountNumber))
                {
                    lblMessage.CssClass = "text-danger d-block mb-3";
                    lblMessage.Text = "Please select a biller and enter a valid account or policy number.";
                    return;
                }

                bool exists = await CheckAccountExistsAsync(biller, accountNumber);
                if (!exists)
                {
                    lblMessage.CssClass = "text-danger d-block mb-3";
                    lblMessage.Text = $"No active {biller} account found for '{accountNumber}'. Please check the number and try again.";
                    return;
                }

                int userId = Convert.ToInt32(Session["UserId"]);
                SaveLinkedBillerAccount(userId, biller, accountNumber, nickname);

                lblMessage.CssClass = "text-success d-block mb-3";
                lblMessage.Text = $"Verified and linked {biller} account '{accountNumber}'.";
                ddlBiller.SelectedIndex = 0;
                txtAccountNumber.Text = string.Empty;
                txtNickname.Text = string.Empty;
            }
            catch (Exception)
            {
                lblMessage.CssClass = "text-danger d-block mb-3";
                lblMessage.Text = "Unable to verify or link your account right now. Please try again later.";
            }
        }

        private async Task<bool> CheckAccountExistsAsync(string biller, string accountNumber)
        {
            string baseUrl = ConfigurationManager.AppSettings["PartnerServiceBaseUrl"] ?? "https://localhost:44301/api/partner";
            string url = $"{baseUrl.TrimEnd('/')}/check-account-exists?biller={Uri.EscapeDataString(biller)}&accountNumber={Uri.EscapeDataString(accountNumber)}";

            HttpResponseMessage response = await Http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Partner account verification failed.");
            }

            string json = await response.Content.ReadAsStringAsync();
            var serializer = new JavaScriptSerializer();
            var payload = serializer.Deserialize<AccountExistsResult>(json);
            return payload != null && payload.Exists;
        }

        private void SaveLinkedBillerAccount(int userId, string biller, string accountNumber, string nickname)
        {
            var connString = ConfigurationManager.ConnectionStrings["JamBankConnection"];
            if (connString == null)
            {
                return;
            }

            using (var conn = new SqlConnection(connString.ConnectionString))
            using (var cmd = new SqlCommand(@"
                IF NOT EXISTS (
                    SELECT 1 FROM LinkedUtilityAccounts
                    WHERE UserId = @UserId AND Biller = @Biller AND AccountNumber = @AccountNumber
                )
                BEGIN
                    INSERT INTO LinkedUtilityAccounts (UserId, Biller, AccountNumber, Nickname, IsVerified)
                    VALUES (@UserId, @Biller, @AccountNumber, @Nickname, 1);
                END", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Biller", biller);
                cmd.Parameters.AddWithValue("@AccountNumber", accountNumber);
                cmd.Parameters.AddWithValue("@Nickname", nickname);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private sealed class AccountExistsResult
        {
            public bool Exists { get; set; }
        }
    }
}