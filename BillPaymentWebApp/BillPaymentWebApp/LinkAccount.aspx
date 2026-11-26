<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="LinkAccount.aspx.cs" Inherits="BillPaymentWebApp.LinkAccount" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="row justify-content-center my-4">
        <div class="col-md-7 col-lg-6">
            <div class="panel">
                <h3 class="mb-1">Link Biller Account</h3>
                <p class="text-muted small mb-4">Verify and link an existing FLOW or SAGICOR LIFE account to your JamBank profile.</p>

                <asp:Label ID="lblMessage" runat="server" CssClass="d-block mb-3" />

                <div class="mb-3">
                    <label for="ddlBiller" class="form-label">Biller</label>
                    <asp:DropDownList ID="ddlBiller" runat="server" CssClass="form-select soft-input">
                        <asp:ListItem Value="" Text="-- Select Biller --" />
                        <asp:ListItem Value="FLOW" Text="FLOW" />
                        <asp:ListItem Value="SAGICOR" Text="SAGICOR LIFE" />
                    </asp:DropDownList>
                </div>

                <div class="mb-3">
                    <label for="txtAccountNumber" class="form-label">Account / Policy Number</label>
                    <asp:TextBox ID="txtAccountNumber" runat="server" CssClass="form-control soft-input" MaxLength="30" placeholder="Enter FLOW or SAGICOR LIFE account number" />
                </div>

                <div class="mb-4">
                    <label for="txtNickname" class="form-label">Nickname (Optional)</label>
                    <asp:TextBox ID="txtNickname" runat="server" CssClass="form-control soft-input" MaxLength="50" placeholder="e.g., Home Internet or Life Policy" />
                </div>

                <asp:Button ID="btnLinkAccount" runat="server" Text="Verify &amp; Link Account" CssClass="btn btn-jam w-100" OnClick="btnLinkAccount_Click" />
            </div>
        </div>
    </div>
</asp:Content>
