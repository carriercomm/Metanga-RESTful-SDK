<%@ Page Title="About Us" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
  CodeBehind="About.aspx.cs" Inherits="SsoTestWebApp.About" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
  <h2>About
  </h2>
  <iframe id="externalFrame" src="<%=SomeUrl %>" width="100%" height="600px">
    </iframe>

</asp:Content>
