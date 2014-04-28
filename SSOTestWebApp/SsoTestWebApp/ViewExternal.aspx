<%@ Page Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="ViewExternal.aspx.cs" Inherits="SsoTestWebApp.ViewExternal" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
  <div>
    Viewing Metanga pages...<br />
    <br />
  </div>
  <iframe id="externalFrame" src="ViewMetanga.aspx<%=Request.Url.Query %>" width="100%" height="600px">
        Frames are not supported
    </iframe>
</asp:Content>