<%@ Page Title="Log On" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="BillPaymentWebApp.Login" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="row justify-content-center my-4">
        <div class="col-lg-10 col-xl-9">
            <div class="login-split">
                <div class="login-brand">
                    <img src="Content/images/jambank-logo.jpg" alt="JamBank" class="login-logo rounded-circle" />
                    <h2>Jam<span>Bank</span></h2>
                    <p class="mb-0">Online Banking &middot; Jamaica</p>
                    <div class="login-points">
                        &bull; Pay FLOW &amp; SAGICOR LIFE bills in minutes<br />
                        &bull; View all account balances in one place<br />
                        &bull; Every payment recorded with its narrative<br />
                        &bull; Bank-grade encryption on every session
                    </div>
                </div>
                <div class="login-form">
                    <h3>Log On</h3>
                    <p class="text-muted small mb-3">Welcome back. Access your accounts below.</p>

                    <asp:Panel ID="pnlTimeout" runat="server" CssClass="alert alert-warning py-2 mb-3" Visible="false">
                        <small>Your session has expired. Please log on again.</small>
                    </asp:Panel>

                    <label class="form-label small text-muted" for="txtUsername">Username</label>
                    <asp:TextBox ID="txtUsername" runat="server" CssClass="form-control soft-input" />

                    <div class="d-flex justify-content-between align-items-center mt-3 mb-1">
                        <label class="form-label small text-muted mb-0" for="txtPassword">Password</label>
                        <span class="login-forgot">Forgot password?</span>
                    </div>
                    <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="form-control soft-input" />

                    <div class="form-check small text-muted mt-3 mb-3">
                        <input class="form-check-input" type="checkbox" id="chkRemember" />
                        <label class="form-check-label" for="chkRemember">Remember my username on this device</label>
                    </div>

                    <asp:Button ID="btnLogin" runat="server" Text="Log on" CssClass="btn btn-jam w-100" OnClick="btnLogin_Click" />
                    <asp:Label ID="lblError" runat="server" CssClass="text-danger d-block mt-3" />

                    <p class="text-muted" style="font-size: .7rem; margin: .5rem 0 0;">Demo credentials: testuser / password123</p>

                    <div class="login-secure">
                        <span class="text-success">&#128274;</span>
                        Secured with HTTPS encryption. JamBank will never ask for your password by email or phone.
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
