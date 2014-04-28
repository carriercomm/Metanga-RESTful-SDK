using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using Atp.Saml2;
using SsoTestWebApp.Code;

namespace SsoTestWebApp
{
  public partial class ViewMetanga : System.Web.UI.Page
  {
    protected void Page_Load(object sender, EventArgs e)
    {

    }

    // Get consumer service URL from the application settings.  
    private string GetAbsoluteUrl(string relativeUrl)
    {
      var u = new Uri(Request.Url, ResolveUrl(relativeUrl));
      return u.ToString();
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      try
      {
        #region Custom Attributes
        // If you need to add custom attributes, uncomment the following code  
        var attributeStatement = new AttributeStatement();
        attributeStatement.Attributes.Add(new Atp.Saml2.Attribute("email", SamlAttributeNameFormat.Basic, null,
                                                                                     "john@test.com"));
        attributeStatement.Attributes.Add(new Atp.Saml2.Attribute("FirstName", SamlAttributeNameFormat.Basic, null,
                                                                                     "John"));
        attributeStatement.Attributes.Add(new Atp.Saml2.Attribute("LastName", SamlAttributeNameFormat.Basic, null, "Smith"));

        if (Session["username"] != null && Session["previoususername"] != null)
        {
          if (!Session["username"].ToString().ToLower().Equals(Session["previoususername"].ToString().ToLower()))
            attributeStatement.Attributes.Add(new Atp.Saml2.Attribute("samlsessionstate", SamlAttributeNameFormat.Basic, null, "new"));
        }
        #endregion

        // Set External Account Id for Metanga
        var externalAccountId = "XAF10964";
        if (Session["username"] != null)
        {
          externalAccountId = Session["username"].ToString();
        }
        else
        {
          Session["username"] = externalAccountId;
          Session["previoususername"] = externalAccountId;
        }

        var consumerServiceUrl = Helper.GetUrl("LinkSelfcareLogin");

        // Use the local user's local identity.  
        var subject = new Subject(new NameId(User.Identity.Name)) {NameId = {NameIdentifier = externalAccountId}};
        subject.SubjectConfirmations.Add(new SubjectConfirmation(SamlSubjectConfirmationMethod.Bearer)
                                            {
                                              SubjectConfirmationData = new SubjectConfirmationData { Recipient = consumerServiceUrl }
                                            });

        // Create a new authentication statement.  
        var authnStatement = new AuthnStatement
        {
          AuthnContext = new AuthnContext
          {
            AuthnContextClassRef = new AuthnContextClassRef(SamlAuthenticateContext.Password)
          }
        };

        var issuer = new Issuer(GetAbsoluteUrl("~/"));
        var samlAssertion = new Assertion { Issuer = issuer, Subject = subject };
        samlAssertion.Statements.Add(authnStatement);
        samlAssertion.Statements.Add(attributeStatement);

        // Get the PFX certificate with Private Key.  
        var filePath = Path.Combine(HttpRuntime.AppDomainAppPath, "metangasso.pfx");
        const string pwd = "123";
        var x509Certificate = new X509Certificate2(filePath, pwd, X509KeyStorageFlags.MachineKeySet);

        if (!x509Certificate.HasPrivateKey)
          return;

        // Create a SAML response object.  
        var samlResponse = new Response
        {
          // Assign the consumer service url.  
          Destination = consumerServiceUrl,
          Issuer = issuer,
          Status = new Status(SamlPrimaryStatusCode.Success, null)
        };

        // Add assertion to the SAML response object.  
        samlResponse.Assertions.Add(samlAssertion);

        // Sign the SAML response with the certificate.  
        samlResponse.Sign(x509Certificate);

        var targetUrl = Helper.GetUrl("LinkSelfcareBilling") + "?SSO=true";
        if (Session["SsoLink"] != null)
        {
          targetUrl = Session["SsoLink"].ToString();
        }

        // Send the SAML response to the service provider.  
        samlResponse.SendPostBindingForm(Response.OutputStream, consumerServiceUrl, targetUrl);
      }
      catch (Exception exception)
      {
        Trace.Write("IdentityProvider", "An Error occurred", exception);
      }
    }
  }
}