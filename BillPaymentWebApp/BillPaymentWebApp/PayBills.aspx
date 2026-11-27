<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Async="true" CodeBehind="PayBills.aspx.cs" Inherits="BillPaymentWebApp.PayBills" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="row justify-content-center my-4">
        <div class="col-lg-7">
            <div class="panel">
                <h3 class="mb-1">Pay Bills</h3>
                <p class="text-muted small mb-4">Select a JamBank account and a linked FLOW or SAGICOR LIFE account to complete payment.</p>

                <asp:Label ID="lblMessage" runat="server" CssClass="d-block mb-3" />

                <div class="mb-3">
                    <label for="ddlBankAccount" class="form-label">From JamBank Account</label>
                    <asp:DropDownList ID="ddlBankAccount" runat="server" CssClass="form-select soft-input" AutoPostBack="true" OnSelectedIndexChanged="ddlBankAccount_SelectedIndexChanged" />
                    <div class="form-text">
                        Available Balance: <strong><asp:Literal ID="litSelectedBalance" runat="server" Text="$0.00" /></strong>
                    </div>
                </div>

                <div class="mb-3">
                    <label for="ddlLinkedAccount" class="form-label">To Linked Biller Account</label>
                    <asp:DropDownList ID="ddlLinkedAccount" runat="server" CssClass="form-select soft-input" />
                </div>

                <div class="mb-3">
                    <label for="txtAmount" class="form-label">Amount</label>
                    <asp:TextBox ID="txtAmount" runat="server" CssClass="form-control soft-input" placeholder="0.00" />
                </div>

                <div class="form-check mb-4">
                    <asp:CheckBox ID="chkConfirm" runat="server" CssClass="form-check-input" />
                    <label class="form-check-label" for="chkConfirm">Confirm payment details and authorize debit from my JamBank account</label>
                </div>

                <asp:Button ID="btnPayBill" runat="server" Text="Pay Bill" CssClass="btn btn-jam w-100" OnClick="btnPayBill_Click" />

                <asp:Panel ID="pnlReceipt" runat="server" Visible="false" CssClass="mt-4 p-3 border rounded bg-light">
                    <h5 class="mb-2">Payment Receipt</h5>
                    <p class="mb-1"><strong>Updated Bank Balance:</strong> <asp:Literal ID="litUpdatedBalance" runat="server" /></p>
                    <p class="mb-0"><strong>Transaction Narrative:</strong> <asp:Literal ID="litNarrative" runat="server" /></p>
                </asp:Panel>
            </div>
        </div>
    </div>
</asp:Content>
