using System;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace BillPaymentWebApp
{
    public partial class Site : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            bool loggedIn = Session["UserId"] != null;

            // Session-aware navigation: members see their tools, guests see Log On
            navMember.Visible = loggedIn;
            navAuth.Visible = !loggedIn;
            navLogout.Visible = loggedIn;
            navUser.Visible = loggedIn;

            if (loggedIn && Session["Username"] != null)
            {
                litNavUser.Text = Server.HtmlEncode(Session["Username"].ToString());
            }

            // Highlight the link for the page currently being viewed
            string path = Request.AppRelativeCurrentExecutionFilePath ?? string.Empty;
            Mark(navHome, path == "~/Default.aspx");
            Mark(navAccounts, path == "~/Accounts.aspx");
            Mark(navPay, path == "~/PayBills.aspx");
            Mark(navLink, path == "~/LinkAccount.aspx");
        }

        private void Mark(HtmlAnchor link, bool active)
        {
            link.Attributes["class"] = active ? "nav-link active" : "nav-link";
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            // End the session and return to the logon screen
            Session.Clear();
            Session.Abandon();
            Response.Redirect("Login.aspx");
        }
    }
}
