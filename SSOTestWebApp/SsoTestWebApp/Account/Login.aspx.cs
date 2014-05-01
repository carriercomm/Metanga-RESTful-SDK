using System;

namespace SsoTestWebApp.Account
{
  public partial class Login : System.Web.UI.Page
  {
    protected void LoginButton_Click(object sender, EventArgs e)
    {

      Session["previoususername"] = Session["username"] ?? string.Empty;

      Session["username"] = UserName.Text;

      Response.Redirect("../Default.aspx", false);
    }
  }
}
