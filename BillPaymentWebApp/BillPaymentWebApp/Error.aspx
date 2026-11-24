<%@ Page Title="Error" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Error.aspx.cs" Inherits="BillPaymentWebApp.Error" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="row justify-content-center my-5">
        <div class="col-md-6 text-center">
            <div class="panel py-5">
                <h2 style="font-size: 3rem;" class="mb-3">&#9888;&#65039;</h2>
                <h2 class="mb-3">Something went wrong</h2>
                <p class="text-muted mb-4">
                    We could not complete your request. This may be temporary;
                    please try again. If the problem continues, contact customer support.
                </p>
                <a class="btn btn-jam" href="Default.aspx">Return to Home</a>
            </div>
        </div>
    </div>
</asp:Content>
