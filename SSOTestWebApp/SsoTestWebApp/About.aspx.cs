using System;
using System.Security.Cryptography;
using System.Text;

namespace SsoTestWebApp
{
  public partial class About : System.Web.UI.Page
  {
    public string SomeUrl;

    protected void Page_Load(object sender, EventArgs e)
    {
      const string sFullName = "Test_Remote_Auth_3";
      const string sEmail = "remoteauth3@metratech.com";

      const string sToken = "aQjlToXdizUZJjbjGBgk3mwLIRUtspOaetGZnAIrgPqzeWZo";
      const string sReturnUrl = "https://metanga1342789136.zendesk.com/access/remote/";
      var sTimeStamp = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
      const string sExternalId = "D1993237-9701-4B2F-A289-040468D55154";

      string sMessage = sFullName + sEmail + sExternalId + sToken + sTimeStamp;
      string sDigest = Md5(sMessage);

      SomeUrl = sReturnUrl + "?name=" + Server.UrlEncode(sFullName) +
              "&email=" + Server.UrlEncode(sEmail) +
              "&external_id=" + Server.UrlEncode(sExternalId) +
              "&timestamp=" + sTimeStamp +
              "&hash=" + sDigest;
      

    }

    public string Md5(string strChange)
    {
      //Change the syllable into UTF8 code
      byte[] pass = Encoding.UTF8.GetBytes(strChange);

      MD5 md5 = new MD5CryptoServiceProvider();
      md5.ComputeHash(pass);
      string strPassword = ByteArrayToHexString(md5.Hash);
      return strPassword;
    }

    public static string ByteArrayToHexString(byte[] bytes)
    {
      // important bit, you have to change the byte array to hex string or zenddesk will reject
      const string hexAlphabet = "0123456789abcdef";

      var result = new StringBuilder();

      foreach (byte b in bytes)
      {
        result.Append(hexAlphabet[b >> 4]);
        result.Append(hexAlphabet[b & 0xF]);
      }
      return result.ToString();
    }
  }
}
