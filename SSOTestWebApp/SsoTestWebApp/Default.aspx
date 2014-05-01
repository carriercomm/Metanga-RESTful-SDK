<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
  CodeBehind="Default.aspx.cs" Inherits="SsoTestWebApp._Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
  <h2>Welcome to Single Sign On test site for Metanga!
  </h2>
  <p>
    Please, use "Log In" link in the right upper corner to enter External Account Id for Metanga.
  </p>
  <p>
    If you don't enter External Account Id value in the "User Name" field the default user XAF10964 will be used.
  </p>
</asp:Content>
