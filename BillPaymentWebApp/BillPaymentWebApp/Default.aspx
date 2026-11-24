<%@ Page Title="Home" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="BillPaymentWebApp.Default" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="hero text-center" id="heroGuest" runat="server">
        <h1 class="display-5 fw-bold">Welcome to <span class="hero-gold">JamBank Online Banking</span></h1>
        <p class="lead mb-4">Pay your FLOW and SAGICOR LIFE bills quickly, securely, and from one place.</p>
        <a class="btn btn-gold btn-lg" href="Login.aspx">Log On to Continue</a>
    </div>

    <div id="heroMember" runat="server" visible="false">
        <div class="hero d-flex flex-column flex-md-row justify-content-between align-items-center" style="text-align: left;">
            <div class="mb-3 mb-md-0">
                <h1 class="h3 fw-bold mb-1"><asp:Literal ID="litGreeting" runat="server" />, <span class="hero-gold"><asp:Literal ID="litMemberName" runat="server" /></span></h1>
                <div class="small" style="opacity: .85;">Your JamBank accounts are ready. Pay bills to FLOW and SAGICOR LIFE in a few clicks.</div>
            </div>
            <a class="btn btn-gold btn-lg" href="Accounts.aspx">Go to My Accounts</a>
        </div>
    </div>

    <div class="row g-4 mt-2">
        <div class="col-md-4">
            <div class="card feature-card p-4">
                <div class="icon-badge mb-3">&#128179;</div>
                <h5>Link Your Accounts</h5>
                <p class="mb-0 text-muted">Connect your existing FLOW or SAGICOR LIFE account to your JamBank profile once, and it stays linked.</p>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card feature-card p-4">
                <div class="icon-badge mb-3">&#128181;</div>
                <h5>Pay Bills Online</h5>
                <p class="mb-0 text-muted">Make bill payments from your JamBank account directly to your FLOW or SAGICOR LIFE accounts.</p>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card feature-card p-4">
                <div class="icon-badge mb-3">&#128202;</div>
                <h5>Track Everything</h5>
                <p class="mb-0 text-muted">View your balances and every transaction narrative right after each payment.</p>
            </div>
        </div>
    </div>
</asp:Content>
