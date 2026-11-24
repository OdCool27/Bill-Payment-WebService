<%@ Page Title="My Accounts" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Accounts.aspx.cs" Inherits="BillPaymentWebApp.Accounts" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="d-flex justify-content-between align-items-center mt-4 mb-3">
        <div>
            <h2 class="mb-0">My JamBank Accounts</h2>
            <asp:Literal ID="litLastLogin" runat="server" />
        </div>
        <div class="d-flex align-items-center gap-3">
            <asp:Button ID="btnDownload" runat="server" Text="Download statement"
                CssClass="btn-ghost" OnClick="btnDownload_Click" />
            <span class="text-muted">Welcome back, <strong><asp:Literal ID="litUsername" runat="server" /></strong></span>
        </div>
    </div>

    <asp:Label ID="lblMessage" runat="server" CssClass="text-danger d-block mb-2" />

    <div class="row g-3 mb-4">
        <div class="col-6 col-md-4">
            <div class="stat-card"><div class="stat-k">Total balance</div><div class="stat-v"><asp:Literal ID="litTotalBalance" runat="server" /></div></div>
        </div>
        <div class="col-6 col-md-4">
            <div class="stat-card"><div class="stat-k">Linked billers</div><div class="stat-v"><asp:Literal ID="litBillers" runat="server" /></div></div>
        </div>
        <div class="col-12 col-md-4">
            <div class="stat-card"><div class="stat-k">Payments this month</div><div class="stat-v"><asp:Literal ID="litPayMonth" runat="server" /></div></div>
        </div>
    </div>

    <asp:Panel ID="pnlAccountCards" runat="server" CssClass="row g-4 mb-4" />

    <div class="panel mb-4">
        <h5 class="mb-3">Linked Biller Accounts</h5>
        <div class="biller-card">
            <div>
                <b>FLOW</b>
                <span>Account &bull;&bull;&bull;&bull; 4410 &middot; verified &amp; linked</span>
            </div>
            <a class="btn btn-gold btn-sm" href="PayBills.aspx?biller=FLOW">Pay</a>
        </div>
        <div class="biller-card">
            <div>
                <b>SAGICOR LIFE</b>
                <span>Policy &bull;&bull;&bull;&bull; 9082 &middot; verified &amp; linked</span>
            </div>
            <a class="btn btn-gold btn-sm" href="PayBills.aspx?biller=SAGICOR">Pay</a>
        </div>
    </div>

    <div class="panel" id="RecentTransactions">
        <div class="d-flex justify-content-between align-items-center mb-3">
            <h5 class="mb-0">Recent Transactions</h5>
        </div>

        <asp:Panel ID="pnlNoTxn" runat="server" CssClass="text-center text-muted py-4" Visible="false">
            <p class="mb-0">You have no recent transactions. When you pay a bill, it will appear here with its narrative.</p>
        </asp:Panel>

        <asp:GridView ID="gvTransactions" runat="server" CssClass="table table-striped align-middle" AutoGenerateColumns="false" OnRowDataBound="gvTransactions_RowDataBound">
            <Columns>
                <asp:BoundField DataField="TxnDate" HeaderText="Date" DataFormatString="{0:yyyy-MM-dd}" />
                <asp:BoundField DataField="Narrative" HeaderText="Description" />
                <asp:BoundField DataField="Amount" HeaderText="Amount" DataFormatString="{0:C}" />
                <asp:TemplateField HeaderText="Status">
                    <ItemTemplate>
                        <span class='badge-completed'><%# Eval("Status") %></span>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
        <p class="text-muted small mb-0">Every bill payment is recorded here with its narrative.</p>
    </div>
</asp:Content>
