using System;
using System.Web.UI.WebControls;
using SsoTestWebApp.Code;

namespace SsoTestWebApp
{
  public partial class SiteMaster : System.Web.UI.MasterPage
  {
    protected void Page_Load(object sender, EventArgs e)
    {
      if (IsPostBack) return;

      if (Helper.ReadCookie("CurrEnv") == "") Helper.WriteCookie("CurrEnv", "1");

      if (Helper.ReadCookie("CurrEnv") == "1")
        Radio1.Checked = true;
      else
      {
        Helper.WriteCookie("CurrEnv", "0");
        Radio2.Checked = true;
      }
    }

    protected void NavigationMenu_MenuItemClick(object sender, MenuEventArgs e)
    {
      switch (e.Item.Value)
      {
        case "billing":
          Session["SsoLink"] = Helper.GetUrl("LinkSelfcareBilling");
          break;
        case "updatecreditcard":
          Session["SsoLink"] = Helper.GetUrl("LinkSelfcareUpdateCreditCard");
          break;
        case "paymenthistory":
          Session["SsoLink"] = Helper.GetUrl("LinkSelfcarePaymentHistory");
          break;
        case "updateaccount":
          Session["SsoLink"] = Helper.GetUrl("LinkSelfcareUpdateAccount");
          break;
      }
      Response.Redirect("~/ViewExternal.aspx", false);
    }

    protected void OnCheckedChanged(object sender, EventArgs e)
    {
      Helper.WriteCookie("CurrEnv", Radio1.Checked ? "1" : "0");
      Response.Redirect(@"~\", false);
    }
  }
}
