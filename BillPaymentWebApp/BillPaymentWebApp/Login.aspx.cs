using System;

namespace BillPaymentWebApp
{
    public partial class Login : System.Web.UI.Page
    {
        private const int MaxAttempts = 3;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Friendly notice when the user arrives with an expired session
            if (Request.QueryString["timeout"] == "1")
            {
                pnlTimeout.Visible = true;
            }

            // Keep a locked profile locked across postbacks
            if (Session["LockedOut"] != null)
            {
                LockProfile();
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                if (Session["LockedOut"] != null)
                {
                    return; // button is disabled anyway; belt and braces
                }

                string username = txtUsername.Text.Trim();
                string password = txtPassword.Text;

                // Basic validation with a friendly message
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    lblError.Text = "Please enter both username and password.";
                    return;
                }

                // TODO: replace stub with real database check once the team schema is merged
                if (username == "testuser" && password == "password123")
                {
                    // Successful log on: reset failed attempts
                    Session["FailedAttempts"] = 0;

                    // Store the logged-in user in session for page gating
                    Session["UserId"] = 1;
                    Session["Username"] = username;
                    Session["LastLogin"] = DateTime.Now;

                    Response.Redirect("Accounts.aspx");
                }
                else
                {
                    int attempts = Session["FailedAttempts"] != null
                        ? (int)Session["FailedAttempts"]
                        : 0;
                    attempts++;
                    Session["FailedAttempts"] = attempts;

                    if (attempts >= MaxAttempts)
                    {
                        Session["LockedOut"] = true;
                        LockProfile();
                    }
                    else
                    {
                        lblError.Text = string.Format(
                            "Invalid username or password. {0} attempt(s) remaining before your profile is temporarily locked.",
                            MaxAttempts - attempts);
                    }
                }
            }
            catch (Exception)
            {
                // User-friendly error handling (non-functional requirement)
                lblError.Text = "Something went wrong. Please try again later.";
            }
        }

        private void LockProfile()
        {
            btnLogin.Enabled = false;
            btnLogin.CssClass = "btn btn-jam w-100 disabled";
            txtUsername.Enabled = false;
            txtPassword.Enabled = false;
            lblError.CssClass = "text-danger d-block mt-3";
            lblError.Text = "Your profile has been temporarily locked after too many failed attempts. Please contact customer support.";
        }
    }
}
