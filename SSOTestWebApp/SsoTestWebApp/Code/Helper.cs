using System;
using System.Web;

namespace SsoTestWebApp.Code
{
  public static class Helper
  {
    public static string GetUrl(string link)
    {
      var domainTmpVar = System.Configuration.ConfigurationManager.AppSettings["DomainTempVar"];
      string domain;
      if (ReadCookie("CurrEnv") == "0")
        domain = System.Configuration.ConfigurationManager.AppSettings["DomainNameAzure"];
      else
      {
        WriteCookie("CurrEnv", "1");
        domain = System.Configuration.ConfigurationManager.AppSettings["DomainNameLocal"].Replace("*", Environment.MachineName);
      }

      var getDelim = link == "LinkSelfcareUpdateAccount" ? "&" : "?";
      return System.Configuration.ConfigurationManager.AppSettings[link].Replace(domainTmpVar, domain) + getDelim + "site=SelfCare&SSO=true";
    }

    public static string ReadCookie(string name)
    {
      var cookie = HttpContext.Current.Request.Cookies[name];
      return cookie == null ? "" : cookie.Value;
    }

    public static void WriteCookie(string name, string value)
    {
      var cookie = new HttpCookie(name) { Value = value };

      HttpContext.Current.Response.Cookies.Add(cookie);
    }
  }
}