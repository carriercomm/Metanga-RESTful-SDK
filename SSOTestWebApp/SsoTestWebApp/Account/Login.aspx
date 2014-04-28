<%@ Page Title="Log In" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
  CodeBehind="Login.aspx.cs" Inherits="SsoTestWebApp.Account.Login" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
  <h2>Change User
  </h2>
  <p>
    Please enter your username.
        
  </p>

  <span class="failureNotification">
    <asp:Literal ID="FailureText" runat="server"></asp:Literal>
  </span>
  <asp:ValidationSummary ID="LoginUserValidationSummary" runat="server" CssClass="failureNotification"
    ValidationGroup="LoginUserValidationGroup" />
  <div class="accountInfo">
    <fieldset class="login">
      <legend>Account Information</legend>
      <p>
        <asp:Label ID="UserNameLabel" runat="server" AssociatedControlID="UserName">Username:</asp:Label>
        <asp:TextBox ID="UserName" runat="server" CssClass="textEntry"></asp:TextBox>
        <asp:RequiredFieldValidator ID="UserNameRequired" runat="server" ControlToValidate="UserName"
          CssClass="failureNotification" ErrorMessage="User Name is required." ToolTip="User Name is required."
          ValidationGroup="LoginUserValidationGroup">*</asp:RequiredFieldValidator>
      </p>
      <p>
      </p>
      <p>
        &nbsp;
      </p>
    </fieldset>
    <p class="submitButton">
      <asp:Button ID="LoginButton" runat="server" Text="Log In"
        ValidationGroup="LoginUserValidationGroup" OnClick="LoginButton_Click" />
    </p>
  </div>

</asp:Content>
