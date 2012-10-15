using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Net.Http;
using Metanga.SoftwareDevelopmentKit.Proxy;
using Metanga.SoftwareDevelopmentKit.Rest;
using Newtonsoft.Json.Linq;

namespace Metanga.Example
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Opening connection to Metanga...");
      var client = OpenMetangaClient();
      if (client == null)
      {
        EndExample();
        return;
      }

      Console.WriteLine("Running Enrollment Example...");
      EnrollmentExample(client);
      Console.WriteLine("Closing connection to Metanga...");
      CloseMetangaClient(client);
      EndExample();
    }

    private static void EnrollmentExample(MetangaClient client)
    {
      var externalAccountId = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
      var externalSubscriptionId = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);

      var account = CreateAccount(externalAccountId);
      var subscription = CreateSubscription(externalAccountId, externalSubscriptionId);
      var creditCardToken = CreateCreditCardInPaymentBroker(account);

      account.PaymentInstruments = new[] { new CreditCardMasked { InstrumentId = creditCardToken } }; // add the payment instrument id returned by payment broker to the account

      bool enrollmentSucceeded;
      Invoice invoice = null;
      try
      {
        invoice = client.Enroll(subscription, account, InvoiceAction.InvoiceNow);
        enrollmentSucceeded = true;
      }
      catch (MetangaException e)
      {
        Console.WriteLine("An error has occurred during enrollment: Id={0}, Message={1}", e.ErrorId, e.Message);
        enrollmentSucceeded = false;
      }

      if (!enrollmentSucceeded) return;

      Console.WriteLine("Account has been successfully enrolled. Account Id: {0}", externalAccountId);
      if (invoice != null)
      {
        Console.WriteLine("An invoice has been created for {0} {1}", invoice.InvoiceCurrency,
                          invoice.InvoiceSalesAmount + invoice.InvoiceTaxAmount);
        foreach (var charge in invoice.Charges)
        {
          if (!charge.Product.EntityId.HasValue)
            throw new InvalidOperationException("Product in Charge element has no EntityId"); // should not happen!
          var product = client.RetrieveEntity<Product>(charge.Product.EntityId.Value);
          Console.WriteLine("  - {0}: {1} {2}", product.Name["en-us"], invoice.InvoiceCurrency, charge.ChargeAmount);
        }
      }
    }

    private static void CloseMetangaClient(MetangaClient client)
    {
      try
      {
        client.Close();
      }
      catch (MetangaException e)
      {
        Console.WriteLine("An error has occurred while closing connection to Metanga: Id={0}, Message={1}", e.ErrorId, e.Message);
      }
    }

    private static void EndExample()
    {
      Console.WriteLine("Press any key to finish");
      Console.ReadKey();
    }

    private static MetangaClient OpenMetangaClient()
    {
      var metangaAddress = ConfigurationManager.AppSettings["MetangaAddress"];
      var username = ConfigurationManager.AppSettings["MetangaUsername"];
      var password = ConfigurationManager.AppSettings["MetangaPassword"];
      var address = new Uri(metangaAddress, UriKind.Absolute);
      MetangaClient client;
      try
      {
        client = MetangaClient.Initialize(address, username, password, MetangaContentType.Json);
      } catch (MetangaException e)
      {
        Console.WriteLine("An error has occurred while connecting to Metanga: Id={0}, Message={1}", e.ErrorId, e.Message);
        return null;
      }
      return client;
    }

    private static Guid CreateCreditCardInPaymentBroker(Account account)
    {
      var paymentBrokerAddress = ConfigurationManager.AppSettings["PaymentBrokerAddress"];
      const string creditCardNumber = "4111111111111111";
      const string creditCardType = "Visa";
      const string cardVerificationNumber = "123";
      const string cardExpiration = "12/2018";

      // Send request to payment broker to obtain a credit card token
      return CreditCard.CreateCreditCard(paymentBrokerAddress, account.Address1, "", "",
                                         cardVerificationNumber, account.City, account.Country,
                                         creditCardNumber, creditCardType,
                                         account.Email, cardExpiration, account.FirstName,
                                         account.LastName, account.MiddleInitial, account.PhoneNumber,
                                         account.Zip, account.State);
    }

    private static SampleSubscription CreateSubscription(string externalAccountId, string externalSubscriptionId)
    {
      var packageId = new Guid("7923810c-8c96-4ff6-ac11-adae1303d8c3");
      var productId = new Guid("5ae6e359-2534-474f-8e48-1906f6f158fc");
      const int quantity = 5;
      const string unit = "MABIT";
      var enrollmentDate = new DateTime(2012, 8, 1);

      var subscriptionPackageProduct = new SubscriptionPackageProduct
                                         {
                                           Product = new Product { EntityId = productId },
                                           Quantity = quantity,
                                           UnitId = unit,
                                           StartDate = enrollmentDate
                                         };

      var subscriptionPackageProducts = new [] {subscriptionPackageProduct};

      return new SampleSubscription
               {
                 ExternalId = externalSubscriptionId,
                 Account = new Account { ExternalId = externalAccountId },
                 Package = new Package { EntityId = packageId },
                 RecurringCycleUnitId = "MO",
                 SubscriptionPackageProducts = subscriptionPackageProducts
               };
    }

    private static SampleAccount CreateAccount(string externalAccountId)
    {
      return new SampleAccount
               {
                 ExternalId = externalAccountId,
                 Name = new Dictionary<string, string> { { "en-us", externalAccountId }},
                 FirstName = "John",
                 MiddleInitial = "M",
                 LastName = "Jones",
                 Email = "john@mycompany.com",
                 Language = "en-us",
                 Address1 = "50 Main Street",
                 City = "Waltham",
                 State = "MA",
                 Zip = "02451",
                 Country = "US",
                 PhoneNumber = "555-555-5555",
                 BillingCycleUnit = "MO", // Monthly
                 BillingCycleEndDate = new DateTime(2012, 1, 31), // Last Day of Month
                 Currency = "USD",
                 Payer = new Account { ExternalId = externalAccountId } // Self-Paid
               };
    }
  }

  public static class CreditCard
  {
    public static Guid CreateCreditCard(
      string paymentBrokerAddress,
      string address1,
      string address2,
      string address3,
      string cardVerificationNumber,
      string city,
      string country,
      string creditCardNumber,
      string creditCardType,
      string email,
      string expirationDate,
      string firstName,
      string lastName,
      string middleName,
      string phoneNumber,
      string postal,
      string state)
    {
      var createCreditCardQuery =
        paymentBrokerAddress +
          String.Format(CultureInfo.InvariantCulture, "/paymentmethod/creditcard?address1={0}&address2={1}&address3={2}&cardVerificationNumber={3}&city={4}&country={5}&creditCardNumber={6}&creditCardType={7}&email={8}&expirationDate={9}&firstName={10}&lastName={11}&middleName={12}&phoneNumber={13}&postal={14}&state={15}",
            address1,
            address2,
            address3,
            cardVerificationNumber,
            city,
            country,
            creditCardNumber,
            creditCardType,
            email,
            expirationDate,
            firstName,
            lastName,
            middleName,
            phoneNumber,
            postal,
            state
          );

      var metangaUri = new Uri(createCreditCardQuery);
      using (var httpClient = new HttpClient())
      {
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        using (var response = httpClient.GetAsync(metangaUri))
        using (var responseTask = response.Result.Content.ReadAsStringAsync())
        {
          var responseJson = JToken.Parse(responseTask.Result);
          return responseJson["ResponseValue"].ToObject<Guid>();
        }
      }
    }
  }
}
