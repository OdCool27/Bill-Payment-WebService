using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Net;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace BillPaymentWebApp
{
    public partial class LinkAccount : System.Web.UI.Page
    {
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

                AccountVerificationResult verification = await CheckAccountExistsAsync(biller, accountNumber);
                if (!verification.Success || !verification.Exists)
                {
                    lblMessage.CssClass = "text-danger d-block mb-3";
                    lblMessage.Text = $"No active {biller} account found for '{accountNumber}'. Please check the number and try again.";
                    return;
                }

                if (!string.Equals(verification.AccountStatus, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                {
                    lblMessage.CssClass = "text-danger d-block mb-3";
                    lblMessage.Text = $"The {biller} account '{accountNumber}' exists but is inactive and cannot be linked for payments.";
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
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("LinkAccount submission failed: {0}", ex);
                lblMessage.CssClass = "text-danger d-block mb-3";
                lblMessage.Text = Context.IsDebuggingEnabled
                    ? "Unable to verify or link your account: " + Server.HtmlEncode(ex.GetType().Name + ": " + ex.Message)
                    : "Unable to verify or link your account right now. Please try again later.";
            }
        }

        private async Task<AccountVerificationResult> CheckAccountExistsAsync(string biller, string accountNumber)
        {
            string baseUrl = ConfigurationManager.AppSettings["PartnerServiceBaseUrl"] ?? "http://localhost:53890";
            string provider = biller.Equals("SAGICOR", StringComparison.OrdinalIgnoreCase) ? "sagicor" : "flow";
            string url = $"{baseUrl.TrimEnd('/')}/api/{provider}/accounts/verify";
            var payload = new { accountNumber = accountNumber };
            var serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(payload);

            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                string responseJson = await client.UploadStringTaskAsync(new Uri(url), "POST", json);
                IDictionary<string, object> data = GetAsmxResponseData(serializer.DeserializeObject(responseJson));
                if (data == null)
                    return new AccountVerificationResult();

                return new AccountVerificationResult
                {
                    Success = ReadBoolean(data, "Success"),
                    Exists = ReadBoolean(data, "Exists"),
                    AccountStatus = ReadString(data, "AccountStatus")
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

        private void SaveLinkedBillerAccount(int userId, string biller, string accountNumber, string nickname)
        {
            var connString = ConfigurationManager.ConnectionStrings["PartnerDatabase"];
            if (connString == null || string.IsNullOrWhiteSpace(connString.ConnectionString))
                throw new InvalidOperationException("The PartnerDatabase connection string is not configured.");

            using (var conn = new SqlConnection(connString.ConnectionString))
            using (var cmd = new SqlCommand(@"
                IF NOT EXISTS (
                    SELECT 1 FROM dbo.JamBankLinkedAccounts
                    WHERE UserId = @UserId AND Biller = @Biller AND AccountNumber = @AccountNumber
                )
                BEGIN
                    INSERT INTO dbo.JamBankLinkedAccounts (UserId, Biller, AccountNumber, Nickname, IsVerified)
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

        private sealed class AccountVerificationResult
        {
            public bool Success { get; set; }
            public bool Exists { get; set; }
            public string AccountStatus { get; set; }
        }

    }
}
