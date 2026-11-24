using System;
using System.Web.UI.WebControls;

namespace BillPaymentWebApp
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            bool loggedIn = Session["UserId"] != null;

            // Guests see the marketing hero; members see a personal dashboard
            heroGuest.Visible = !loggedIn;
            heroMember.Visible = loggedIn;

            if (loggedIn)
            {
                litMemberName.Text = Session["Username"] != null ? Session["Username"].ToString() : "";
                litGreeting.Text = DateTime.Now.Hour < 12 ? "Good morning" :
                                   DateTime.Now.Hour < 18 ? "Good afternoon" : "Good evening";
            }
        }
    }
}
