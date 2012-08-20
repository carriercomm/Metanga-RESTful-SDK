using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
      // Example of an Enrollment to a Package that has a Reservation Product
      // Please set the following values to appropriate values
      var paymentBrokerAddress = "https://tenant.mypaymentaccess.com";
      var address = new Uri("https://tenant.mybillaccess.com", UriKind.Absolute);
      var username = "username";
      var password = "password";
      var externalAccountId = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
      var externalSubscriptionId = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
      var creditCardNumber = "4111111111111111";
      var creditCardType = "Visa";
      var cardVerificationNumber = "123";
      var cardExpiration = "12/2018";
      var packageId = new Guid("7923810c-8c96-4ff6-ac11-adae1303d8c3");
      var productId = new Guid("5ae6e359-2534-474f-8e48-1906f6f158fc");
      var quantity = 5;
      var unit = "MABIT";
      var enrollmentDate = new DateTime(2012, 8, 1);

      var account = new SampleAccount
                {
                  ExternalId = externalAccountId,
                  Name = new Dictionary<string, string> { { "en-us", externalAccountId }},
                  FirstName = "John",
                  MiddleInitial = "M",
                  LastName = "Jones",
                  Email = "john@mycompany.com",
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

      var subscriptionPackageProduct = new SubscriptionPackageProduct
                                         {
                                           Product = new Product { EntityId = productId },
                                           Quantity = quantity,
                                           UnitId = unit,
                                           StartDate = enrollmentDate
                                         };

      var subscriptionPackageProducts = new Collection<SubscriptionPackageProduct> {subscriptionPackageProduct};
      
      var subscription = new SampleSubscription
                           {
                             ExternalId = externalSubscriptionId,
                             Account = new Account { ExternalId = externalAccountId },
                             Package = new Package { EntityId = packageId },
                             RecurringCycleUnitId = "MO",
                             SubscriptionPackageProducts = subscriptionPackageProducts
                           };


      // Send request to payment broker to obtain a credit card token
      var creditCardToken = CreditCard.CreateCreditCard(paymentBrokerAddress, account.Address1, "", "",
                                                        cardVerificationNumber, account.City, account.Country,
                                                        creditCardNumber, creditCardType,
                                                        account.Email, cardExpiration, account.FirstName,
                                                        account.LastName, account.MiddleInitial, account.PhoneNumber,
                                                        account.Zip, account.State);

      account.PaymentInstrumentId = creditCardToken; // add the payment instrument id returned by payment broker to the account

      var client = MetangaClient.Initialize(address, username, password, MetangaContentType.Json);
      client.Enroll(subscription, account, InvoiceAction.InvoiceNext);
      client.Close();

      Console.WriteLine("Account has been successfully enrolled. Account Id: {0}", externalAccountId);
      Console.ReadKey();
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
