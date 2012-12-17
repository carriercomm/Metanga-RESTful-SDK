using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using Metanga.SoftwareDevelopmentKit.Proxy;
using Metanga.SoftwareDevelopmentKit.Rest;
using Newtonsoft.Json.Linq;

namespace Metanga.Example
{
  /// <summary>
  /// Executes Metanga SDK Examples
  /// </summary>
  internal class Program
  {
    /// <summary>
    /// Main entry point
    /// </summary>
    private static void Main()
    {
      PrintConsoleMessage("Opening connection to Metanga...");
      var client = OpenMetangaClient();
      if (client == null)
      {
        EndExample();
        return;
      }

      PrintConsoleMessage("Running Bulk Product Creation Example...");
      var externalProductId = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
      PrintConsoleMessage(String.Format("Products ExternalId will start with {0}", externalProductId));
      var products = new Collection<Product>();
      for (var i = 0; i < 10; i++)
      {
        // Creates a few Products with an external id that looks like this: 634885826154527969-140-A
        // The first section represents a value for this executation of the program. Will be the same for all products in this group.
        // The second section is an incremental counter for each iteration of this loop
        // The last section represents the type of product. "A" is a Reservation product. "B" is a Usage product.
        var suffix = i.ToString("00", CultureInfo.InvariantCulture);
        var reservationProduct = CreateReservationProduct(externalProductId + "-" + suffix + "-A");
        var usageProduct = CreateUsageProduct(externalProductId + "-" + suffix + "-B");
        products.Add(reservationProduct);
        products.Add(usageProduct);
      }
      var productIds = CreateEntityBulkExample(client, products);
      if (productIds == null)
      {
        EndExample();
        return;
      }

      PrintConsoleMessage("Running Bulk Product Retrieve Example...");
      var odataQuery = string.Format(CultureInfo.InvariantCulture, "$filter=startswith(ExternalId, '{0}')&$top=10", externalProductId);
      var retrievedProducts = RetrieveEntityBulkExample<Product>(client, odataQuery);
      if (retrievedProducts.Count() != 10 || !retrievedProducts.All(x=>productIds.Contains(x.EntityId.GetValueOrDefault())))
      {
        EndExample();
        return;
      }

      PrintConsoleMessage("Running Package Creation Example...");
      var externalPackageId = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
      var package = CreatePackage(externalPackageId, externalProductId + "-00"); // the "00" is to use the first pair of products created
      var packageId = CreateEntityExample(client, package);
      if (packageId == null)
      {
        EndExample();
        return;
      }

      PrintConsoleMessage("Running Enrollment Example...");
      var subscription = EnrollmentExample(client, externalPackageId, externalProductId + "-00");

      if (subscription != null)
      {
        PrintConsoleMessage("Running ModifySubscription Example...");
        ModifySubscriptionExample(client, subscription);

        PrintConsoleMessage("Meter Billable Events Example...");
        var billableEvent = new UsageEvent
        {
          Originator = new Account { ExternalId = subscription.Account.ExternalId },
          Product = new Product { ExternalId = externalProductId + "-00-B" }, // the "B" represents the usage product
          Quantity = 1000m,
          UnitOfMeasure = "1",
          StartTime = DateTime.Now
        };

        var batch = new UsageBatch
                      {
                        BatchId = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture),
                        BatchNamespace = "Metanga SDK",
                        BatchType = "Storage I/O"
                      };
        client.MeterUsageEvents(batch, new Collection<BillableEvent> { billableEvent });
      }

      //Examples for ElectronicEntity
      var externalAccountId = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);  
      var account = CreateAccount(externalAccountId);
      PrintConsoleMessage("Running Create account for Electronic payment Example...");
      var accountId = client.CreateEntity(account);
      // Create a credit card in Payment Broker and add the token to the account
      PrintConsoleMessage("Running Create credit card for Electronic payment Example...");
      var creditCardToken = CreateCreditCardInPaymentBroker(account);
      account.PaymentInstruments = new PaymentInstrumentMasked[] { new CreditCardMasked { InstrumentId = creditCardToken } };
      
      var electronicPaymentSubmit = new ElectronicPayment
      {
        Payer = new Account { EntityId = accountId },
        PaymentInstrument = new CreditCardMasked { InstrumentId = creditCardToken },
        Amount = 110M,
        Currency = "USD",
        PaymentOperation = ElectronicPaymentOperation.Submit,
        Description = new Dictionary<string, string> { { "en-us", "This is the SALE transaction." } }
      };
      PrintConsoleMessage("Running Create Sale operation for Electronic payment Example...");
      var result = client.ProcessElectronicPayment(electronicPaymentSubmit);

      var electronicPaymentCredit = new ElectronicPayment
      {
        Amount = 100M,
        PaymentOperation = ElectronicPaymentOperation.Credit,
        Reference = new ElectronicPayment { EntityId = result.EntityId },
        Description = new Dictionary<string, string> { { "en-us", "This is the Credit transaction." } }
      };
      PrintConsoleMessage("Running Create Credit operation for Electronic payment Example...");
      var resultCredit = client.ProcessElectronicPayment(electronicPaymentCredit);

      var electronicPaymentReverse = new ElectronicPayment
      {
        PaymentOperation = ElectronicPaymentOperation.Reverse,
        Reference = new ElectronicPayment { EntityId = resultCredit.EntityId },
        Description = new Dictionary<string, string> { { "en-us", "This is the Reverce transaction." } }
      };
      PrintConsoleMessage("Running Create Reverce operation for Electronic payment Example...");
      var resultReverse = client.ProcessElectronicPayment(electronicPaymentReverse);


      PrintConsoleMessage("Closing connection to Metanga...");
      CloseMetangaClient(client);
      EndExample();
    }

    private static void PrintConsoleMessage(string message)
    {
      Console.WriteLine("{0}: {1}", DateTime.Now.ToString("s", CultureInfo.InvariantCulture), message);
    }

    /// <summary>
    /// Enrolls an account to a predefined package. Then shows invoice information.
    /// </summary>
    /// <param name="client">A Metanga client that has been initialized.</param>
    /// <param name="externalPackageId">The Id of the package to use for the subscription</param>
    /// <param name="externalProductId">The Id of the product to use for the subscription</param>
    private static Subscription EnrollmentExample(MetangaClient client, string externalPackageId, string externalProductId)
    {
      // Generate an id that will be different for each execution of this example
      var externalAccountId = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
      var externalSubscriptionId = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);

      // Populate an account and a subscription.
      var account = CreateAccount(externalAccountId);
      var subscription = CreateSubscription(externalPackageId, externalProductId, externalAccountId, externalSubscriptionId);

      // Create a credit card in Payment Broker and add the token to the account
      var creditCardToken = CreateCreditCardInPaymentBroker(account);
      account.PaymentInstruments = new[] {new CreditCardMasked {InstrumentId = creditCardToken}};

      bool enrollmentSucceeded;
      Invoice invoice = null;
      try
      {
        // Create the account and the subscription. An initial invoice will be generated by
        // Metanga if the package has activation fees or recurring charges.
        invoice = client.Enroll(subscription, account, InvoiceAction.InvoiceNow);
        enrollmentSucceeded = true;
      }
      catch (MetangaException e)
      {
        PrintConsoleMessage(String.Format("An error has occurred during enrollment: Id={0}, Message={1}", e.ErrorId, e.Message));
        enrollmentSucceeded = false;
      }

      if (!enrollmentSucceeded) return null;

      PrintConsoleMessage(String.Format("Account has been successfully enrolled. Account Id: {0}", externalAccountId));
      if (invoice != null) DisplayInvoiceDetails(client, invoice);

      return subscription;
    }

    private static void ModifySubscriptionExample(MetangaClient client, Subscription subscription)
    {
      Invoice invoice;
      try
      {
        // Create the account and the subscription. An initial invoice will be generated by
        // Metanga if the package has activation fees or recurring charges.
        var modifyDate = DateTime.Today;
        var effectiveDate = new DateTime(modifyDate.Year, modifyDate.Month, modifyDate.Day);
        subscription.SubscriptionPackageProducts[0].Quantity = 1500m;
        invoice = client.Modify(subscription, effectiveDate, InvoiceAction.InvoiceNow);
      }
      catch (MetangaException e)
      {
        PrintConsoleMessage((String.Format("An error has occurred during subscription modification: Id={0}, Message={1}", e.ErrorId, e.Message)));
        return;
      }

      PrintConsoleMessage("Subscription has been successfully modified.");
      if (invoice != null) DisplayInvoiceDetails(client, invoice);
    }

    private static void DisplayInvoiceDetails(MetangaClient client, Invoice invoice)
    {
      PrintConsoleMessage(String.Format("An invoice has been created for {0} {1}", invoice.InvoiceCurrency,
                                        invoice.InvoiceSalesAmount + invoice.InvoiceTaxAmount));
      foreach (var charge in invoice.Charges)
      {
        if (!charge.Product.EntityId.HasValue) throw new InvalidOperationException("Product in Charge element has no EntityId"); // should not happen!
        var product = client.RetrieveEntity<Product>(charge.Product.EntityId.Value);
        PrintConsoleMessage(String.Format("  - {0}: {1} {2} (Qty={3} {4} - {5})", product.Name["en-us"], invoice.InvoiceCurrency, charge.ChargeAmount, charge.Quantity, charge.StartTime.ToShortDateString(), charge.EndTime.ToShortDateString()));
      }
    }

    private static Guid? CreateEntityExample(MetangaClient client, Entity entity)
    {
      Guid? entityId = null;
      try
      {
        entityId = client.CreateEntity(entity);
      }
      catch (MetangaException e)
      {
        PrintConsoleMessage(String.Format("An error has occurred during entity creation: Id={0}, Message={1}", e.ErrorId, e.Message));
      }
      return entityId;
    }

    private static IEnumerable<Guid> CreateEntityBulkExample(MetangaClient client, IEnumerable<Entity> entities)
    {
      IEnumerable<Guid> entityId = null;
      try
      {
        entityId = client.CreateEntityBulk(entities);
      }
      catch (MetangaException e)
      {
        PrintConsoleMessage(String.Format("An error has occurred during entity creation: Id={0}, Message={1}", e.ErrorId, e.Message));
      }
      return entityId;
    }

    private static IEnumerable<T> RetrieveEntityBulkExample<T>(MetangaClient client, string odataQuery) where T: Entity, new()
    {
      IEnumerable<T> entities= null;
      try
      {
        entities = client.RetrieveEntitiesBulk<T>(odataQuery);
      }
      catch (MetangaException e)
      {
        PrintConsoleMessage(String.Format("An error has occurred during entity creation: Id={0}, Message={1}", e.ErrorId, e.Message));
      }
      return entities;
    }

    /// <summary>
    /// Creates a credit card in the payment broker. The account object provides contact information to be
    /// associated to the credit card. This method sets hardcoded values for the credit card.
    /// In a production scenario, this method should not be invoked from a server that is not PCI compliant.
    /// Instead, it should be implemented in JavaScript and executed directly from the end user's browser.
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
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

    private static SampleProduct CreateReservationProduct(string externalProductId)
    {
      // Create a Reservation Product for Cloud Storage. You can buy online storage at a price of $0.10 / Gigabyte / Month
      return new SampleProduct
               {
                 ExternalId = externalProductId,
                 Name = new Dictionary<string, string> { { "en-us", externalProductId } },
                 PriceSchedule = CreatePriceSchedule(0.10m, "GIBBY", "MO"),
                 Taxable = true,
                 // A time-based product will automatically prorate charges based on start and end date
                 TimeBased = true,
                 // A list of out-of-box unit group codes can be found here: https://demo.mybillaccess.com/WebServices/html/T_Metanga_Domain_Configuration_UnitsOfMeasure_UnitGroup.htm
                 UnitGroup = new UnitGroup { EntityId = new Guid("1e559a2e-70b5-43a7-a4b7-f9b01c6067aa") }
               };
    }

    private static SampleProduct CreateUsageProduct(string externalProductId)
    {
      // Create a Usage Product for Cloud Storage. You pay $0.01 for each I/O operation.
      return new SampleProduct
      {
        ExternalId = externalProductId,
        Name = new Dictionary<string, string> { { "en-us", externalProductId } },
        PriceSchedule = CreatePriceSchedule(0.10m, "1", null),
        Taxable = true,
        // A time-based product will automatically prorate charges based on start and end date
        TimeBased = false,
        // A null unit group means that the product does not require a unit
        // Billable events should use a unit of "1", which represents "Each"
        UnitGroup = null
      };
    }

    private static SamplePackage CreatePackage(string externalPackageId, string externalProductId)
    {
      // Create a Package to bill for Cloud Storage. This is a special package with a discounted price of $0.09 / Gigabyte / Month

      var reservationPackageProduct = new PackageProduct
                             {
                               EventModel = EventModel.Recurring,
                               Product = new Product { ExternalId = externalProductId + "-A" },
                               PriceSchedule = CreatePriceSchedule(0.09m, "GIBBY", "MO")
                             };

      var usagePackageProduct = new PackageProduct
      {
        EventModel = EventModel.None,
        Product = new Product { ExternalId = externalProductId + "-B" },
        PriceSchedule = CreatePriceSchedule(0.005m, "1", null)
      };

      return new SamplePackage
      {
        ExternalId = externalPackageId,
        Name = new Dictionary<string, string> { { "en-us", externalPackageId } },
        AdvanceRecurringEvents = true,
        RecurringChargeCycle = new [] { "MO" },
        PackageProducts = new [] { reservationPackageProduct, usagePackageProduct }
      };
    }

    /// <summary>
    /// Creates a simple Price Schedule for the Cloud Storage product
    /// </summary>
    /// <param name="price">The US$ price to charge for each Gigabyte / Month of use</param>
    /// <param name="usageUnitId">The usage unit for the given price</param>
    /// <param name="timeUnitId">The time unit for the given price</param>
    /// <returns>A price schedule object that can be associated in a product, package, or subscription</returns>
    private static PriceSchedule CreatePriceSchedule(Decimal price, string usageUnitId, string timeUnitId)
    {
      var unitPrice = new UnitPrice
                        {
                          UsageUnitId = usageUnitId,
                          TimeUnitId = timeUnitId,
                          Price = new Dictionary<string, Decimal> {{"USD", price}}
                        };
      var interval = new PriceScheduleInterval
                       {
                          StartDate = new DateTime(2010, 1, 1),
                          UnitPrices = new [] { unitPrice }
                       };
      return new PriceSchedule { Intervals = new [] { interval }};
    }
    
    /// <summary>
    /// Populate an account object with some hardcoded values
    /// </summary>
    /// <param name="externalAccountId">An external id that can be used to subsequently retrieve, update, or delete the account</param>
    /// <returns></returns>
    private static SampleAccount CreateAccount(string externalAccountId)
    {
      return new SampleAccount
               {
                 ExternalId = externalAccountId,
                 Name = new Dictionary<string, string> {{"en-us", externalAccountId}},
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
                 BillingCycleEndDate = new DateTime(2012, 1, 31), // Last Day of Month. For anniversary billing use today - 1 day.
                 Currency = "USD",
                 Payer = new Account {ExternalId = externalAccountId} // Self-Paid
               };
    }

    /// <summary>
    /// Populates a subscription object with some hardcoded values.
    /// </summary>
    /// <param name="externalPackageId">The external id of the package to be subscribed</param>
    /// <param name="externalProductId">The external id of the product in this package</param>
    /// <param name="externalAccountId">The external id of the account that is being subscribed</param>
    /// <param name="externalSubscriptionId">An external id that can be used to subsequently retrieve, update, or delete the subscription</param>
    /// <returns></returns>
    private static SampleSubscription CreateSubscription(string externalPackageId, string externalProductId, string externalAccountId, string externalSubscriptionId)
    {
      // This product is for a cloud storage product, priced at $10 / Megabit / Mo.
      // Enroll this customer for 1000 Gigabytes
      const Decimal quantity = 1000m;
      const string unit = "GIBBY";

      // Start enrollment on the first day of the current month
      var today = DateTime.Today;
      var enrollmentDate = new DateTime(today.Year, today.Month, 1);

      // Assemble the subscription object
      var reservationSubscriptionPackageProduct = new SubscriptionPackageProduct
      {
        Product = new Product { ExternalId = externalProductId + "-A" },
        Quantity = quantity,
        UnitId = unit,
        StartDate = enrollmentDate
      };

      var usageSubscriptionPackageProduct = new SubscriptionPackageProduct
      {
        Product = new Product { ExternalId = externalProductId + "-B" },
        StartDate = enrollmentDate
      };

      var subscriptionPackageProducts = new[] { reservationSubscriptionPackageProduct, usageSubscriptionPackageProduct };

      return new SampleSubscription
      {
        ExternalId = externalSubscriptionId,
        Account = new Account { ExternalId = externalAccountId },
        Package = new Package { ExternalId = externalPackageId },
        RecurringCycleUnitId = "MO",
        SubscriptionPackageProducts = subscriptionPackageProducts
      };
    }

    /// <summary>
    /// Opens a connection to the Metanga service. If there is a MetangaException, it
    /// displays an appropriate message in the console.
    /// </summary>
    /// <returns></returns>
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
      }
      catch (MetangaException e)
      {
        PrintConsoleMessage(String.Format("An error has occurred while connecting to Metanga: Id={0}, Message={1}", e.ErrorId, e.Message));
        return null;
      }
      return client;
    }

    /// <summary>
    /// Closes the connection to the Metanga service. If there is a MetangaException, it
    /// displays an appropriate message in the console.
    /// </summary>
    /// <param name="client">The MetangaClient to be closed</param>
    private static void CloseMetangaClient(MetangaClient client)
    {
      try
      {
        client.Close();
      }
      catch (MetangaException e)
      {
        PrintConsoleMessage(String.Format("An error has occurred while closing connection to Metanga: Id={0}, Message={1}", e.ErrorId, e.Message));
      }
    }

    /// <summary>
    /// Before exiting, ask a user to press a key to finish and wait for the key.
    /// Very useful when running from Visual Studio
    /// </summary>
    private static void EndExample()
    {
      PrintConsoleMessage("Press any key to finish");
      Console.ReadKey();
    }
  }

  /// <summary>
  /// A helper class to create a credit card in Payment Broker
  /// </summary>
  public static class CreditCard
  {
    /// <summary>
    /// A helper method to create a credit card in Payment Broker
    /// </summary>
    /// <param name="paymentBrokerAddress">The URL to payment broker</param>
    /// <param name="address1">CC Contact Address, Line 1</param>
    /// <param name="address2">CC Contact Address, Line 2</param>
    /// <param name="address3">CC Contact Address, Line 3</param>
    /// <param name="cardVerificationNumber">The CCV is used to verify that the credit card number is not stolen</param>
    /// <param name="city">CC Contact Address, City</param>
    /// <param name="country">Should be a 2-letter ISO 3166-1 country code</param>
    /// <param name="creditCardNumber">CC Number</param>
    /// <param name="creditCardType">CC Type: Visa, Master Card, Discover, etc.</param>
    /// <param name="email">Email address</param>
    /// <param name="expirationDate">CC expiration date</param>
    /// <param name="firstName">CC Contact First Name</param>
    /// <param name="lastName">CC Contact Last Name</param>
    /// <param name="middleName">CC Contact Middle Name</param>
    /// <param name="phoneNumber">CC Contact Phone Number</param>
    /// <param name="postal">Zip or Postal Code</param>
    /// <param name="state">State or Province</param>
    /// <returns></returns>
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
        String.Format(CultureInfo.InvariantCulture,
                      "/paymentmethod/creditcard?address1={0}&address2={1}&address3={2}&cardVerificationNumber={3}&city={4}&country={5}&creditCardNumber={6}&creditCardType={7}&email={8}&expirationDate={9}&firstName={10}&lastName={11}&middleName={12}&phoneNumber={13}&postal={14}&state={15}",
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