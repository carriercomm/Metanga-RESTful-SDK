using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Metanga.SoftwareDevelopmentKit.Proxy;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;
using Newtonsoft.Json.Serialization;

namespace Metanga.SoftwareDevelopmentKit.Rest
{
  /// <summary>
  /// 
  /// </summary>
  public class MetangaClient
  {
    #region Constants

    private static readonly Uri RestServiceSession = new Uri("RestService/session", UriKind.Relative);
    private static readonly Uri RestServiceEnrollment = new Uri("RestService/enrollment", UriKind.Relative);
    private static readonly Uri RestServiceSubscribe = new Uri("RestService/subscribe", UriKind.Relative);
    private static readonly Uri RestServiceTransitionSubscription = new Uri("RestService/transitionSubscription", UriKind.Relative);
    private static readonly Uri RestServiceMeterUsageEvents = new Uri("RestService/meterusageevents", UriKind.Relative);
    private static readonly Uri RestServiceProcessElectronicPayment = new Uri("RestService/ProcessElectronicPayment", UriKind.Relative);
    private static readonly Uri RestServiceBulk = new Uri("RestService/bulk", UriKind.Relative);
    private static readonly Uri RestServiceAccount = new Uri("RestService/"+typeof(Account).Name, UriKind.Relative);
    private const string TypeOfIdMetanga = "Metanga";
    private const string TypeOfIdExternal = "External";

    #endregion

    #region Public static methods

    /// <summary>
    /// Initialize address and credentials 
    /// </summary>
    /// <param name="address">Address REST service</param>
    /// <param name="userName">User name</param>
    /// <param name="password">Password</param>
    /// <param name="contentType">REST content type (JSON, XML)</param>
    /// <returns></returns>
    public static MetangaClient Initialize(Uri address, string userName, string password, MetangaContentType contentType)
    {
      return new MetangaClient(address, userName, password, contentType);
    }

    #endregion

    #region Private static fields

    private static readonly Encoding Encoding = new UTF8Encoding(false, true);
    private static readonly JsonSerializer JsonSerializer = CreateJsonSerializer();
    private readonly MediaTypeWithQualityHeaderValue _contentFormatHeader;
    #endregion

    #region Private static methods

    private void CheckResponse(HttpResponseMessage response, HttpStatusCode expectedStatusCode)
    {
      if (response.IsSuccessStatusCode && response.StatusCode == expectedStatusCode)
        return;
      GenerateExceptionFromResponse(response);
    }


    private void GenerateExceptionFromResponse(HttpResponseMessage httpResponseMessage)
    {
      if (httpResponseMessage == null)
        throw new ArgumentNullException("httpResponseMessage");

      var stream = httpResponseMessage.Content.ReadAsStreamAsync().Result;
      var errorData = DeserializeContent<ErrorData>(stream);
      if (errorData.InnerErrors != null)
        throw new MetangaAggregateException(errorData);

      throw new MetangaException(errorData.ErrorMessage, errorData.ErrorId);

    }
    
    private static JsonSerializer CreateJsonSerializer()
    {
      var jsonSerializerSettings = new JsonSerializerSettings();
      jsonSerializerSettings.Converters.Add(new IsoDateTimeConverter());
      jsonSerializerSettings.Converters.Add(new StringEnumConverter());
      jsonSerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
      jsonSerializerSettings.Binder = new ClientJsonSerializationBinder();
      var serializer = JsonSerializer.Create(jsonSerializerSettings);
      return serializer;
    }

    private StreamContent SerializeContent(object content, Stream entityStream)
    {
      if (entityStream == null) throw new ArgumentNullException("entityStream");

      switch (ContentType)
      {
        case MetangaContentType.Json:
          SerializeToJson(content, entityStream);
          break;
        case MetangaContentType.Xml:
          SerializeToXml(content, entityStream);
          break;
        default:
          throw new NotSupportedException();
      }

      entityStream.Seek(0, SeekOrigin.Begin);
      var streamContent = new StreamContent(entityStream);
      streamContent.Headers.ContentType = _contentFormatHeader;

      return streamContent;
    }

    private static void SerializeToJson (object content, Stream entityStream)
    {
      var jsonTextWriter = new JsonTextWriter(new StreamWriter(entityStream, Encoding)) { CloseOutput = false };
      JsonSerializer.Serialize(jsonTextWriter, content);
      jsonTextWriter.Flush();
    }

    private static void SerializeToXml(object content, Stream entityStream)
    {
      var isEntity = content is Entity;
      Type type;
      if (isEntity) 
        type = typeof (Entity);
      else if (content is IEnumerable<Entity>)
        type = typeof (IEnumerable<Entity>);
      else
        type = content.GetType();
      var serializer = new DataContractSerializer(type);
      serializer.WriteObject(entityStream, content);
    }

    private static KeyValuePair<string, string> GetEntityIdentificator(Entity entity)
    {
      return !string.IsNullOrEmpty(entity.ExternalId)
               ? new KeyValuePair<string, string>(entity.ExternalId, TypeOfIdExternal)
               : new KeyValuePair<string, string>(entity.EntityId.GetValueOrDefault().ToString(), TypeOfIdMetanga);
    }

    #endregion

    #region Constructors

    private MetangaClient(Uri address, string userName, string password, MetangaContentType contentType)
    {
      if (address == null)
        throw new ArgumentNullException("address");
      if (string.IsNullOrEmpty(userName))
        throw new ArgumentNullException("userName");
      if (string.IsNullOrEmpty(password))
        throw new ArgumentNullException("password");

      ServiceAddress = address;
      UserName = userName;
      Password = password;
      ContentType = contentType;

      string contentFormat;
      switch (ContentType)
      {
        case MetangaContentType.Json:
          contentFormat = "application/json";
          break;
        case MetangaContentType.Xml:
          contentFormat = "application/xml";
          break;
        default:
          throw new NotSupportedException();
      }
      _contentFormatHeader = new MediaTypeWithQualityHeaderValue(contentFormat);

      SessionId = CreateSession();
    }

    #endregion

    #region Delegates

    private delegate Task<HttpResponseMessage> ProcessRequest(
      HttpClient httpClient, Uri serviceUri, StreamContent messageContent);

    #endregion

    #region Public properties

    /// <summary>
    /// Current Session Id
    /// </summary>
    public Guid SessionId { get; private set; }

    #endregion

    #region Private properties

    /// <summary>
    /// Address REST service
    /// </summary>
    private Uri ServiceAddress { get; set; }

    /// <summary>
    /// User Name
    /// </summary>
    private string UserName { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    private string Password { get; set; }

    /// <summary>
    /// REST content type (JSON, XML)
    /// </summary>
    private  MetangaContentType ContentType { get; set; }

    #endregion

    #region Public methods

    /// <summary>
    /// Takes a Subscription object and an Account object as a parameter and based on the values populated in this object it first creates a new account and then subscribes this account to a specified package. 
    /// This method returns a reference to an invoice object.
    /// </summary>
    /// <param name="subscription">Represents the subscription which will be created in the system by this call.</param>
    /// <param name="account">Represents the account which will be created in the system by this call.</param>
    /// <param name="invoiceAction">The purpose of this parameter is to indicate what invoice action will be taken. It can have the next values:
    /// <ul>
    /// <li><i>InvoiceNext</i> - The newly calculated charges will be placed into invoices with naturally billing dates. Metanga will leave all new invoices open, and will return an open invoice for the current billing period.</li>
    /// <li><i>InvoiceNow</i> - The method will return a closed ad-hoc invoice (Invoice Date = Current Date), which incorporates any charges with the next or before billing date.</li>
    /// <li><i>InvoiceQuote</i> - The method will return an invoice object, which incorporates any charges with the next or before billing date, but these charges won't be saved into database.</li>
    /// </ul>
    /// </param>
    /// <returns>Returns a reference to an invoice object which includes charges generated by this call.</returns>
    public Invoice Enroll(Subscription subscription, Account account, InvoiceAction invoiceAction)
    {
      var enrollmentAddress = new Uri(ServiceAddress, RestServiceEnrollment);
      var enrollParams = new  EnrollParameters {Subscription = subscription, Account = account};
      using (var credentialStream = new MemoryStream())
      {
        var enrollParamsContent = SerializeContent(enrollParams, credentialStream);
        using (var httpClient = new HttpClient())
        {
          PopulateMetangaHeaders(httpClient, null, new Dictionary<string, string> { { "X-Metanga-InvoiceAction", invoiceAction.ToString() } });

          var response = httpClient.PostAsync(enrollmentAddress, enrollParamsContent).Result;
          CheckResponse(response, HttpStatusCode.Created);
          var responseContent = response.Content.ReadAsStreamAsync().Result;
          return DeserializeContent<Invoice>(responseContent);
        }
      }
    }

    /// <summary>
    /// Takes a Subscription object as a parameter and based on the values populated in this object it subscribes a specified account to a specified package. 
    /// This method returns a reference to an invoice object.
    /// </summary>
    /// <param name="subscription">Represents the subscription which will be created in the system by this call.</param>    
    /// <param name="invoiceAction">The purpose of this parameter is to indicate what invoice action will be taken. It can have the next values:
    /// <ul>
    /// <li><i>InvoiceNext</i> - The newly calculated charges will be placed into invoices with naturally billing dates. Metanga will leave all new invoices open, and will return an open invoice for the current billing period.</li>
    /// <li><i>InvoiceNow</i> - The method will return a closed ad-hoc invoice (Invoice Date = Current Date), which incorporates any charges with the next or before billing date.</li>
    /// <li><i>InvoiceQuote</i> - The method will return an invoice object, which incorporates any charges with the next or before billing date, but these charges won't be saved into database.</li>
    /// </ul>
    /// </param>
    /// <returns>Returns a reference to an invoice object which includes charges generated by this call.</returns>
    public Invoice Subscribe(Subscription subscription, InvoiceAction invoiceAction)
    {
      var subscribeAddress = new Uri(ServiceAddress, RestServiceSubscribe);
      using (var credentialStream = new MemoryStream())
      {
        var subscriptionSerialized = SerializeContent(subscription, credentialStream);
        using (var httpClient = new HttpClient())
        {
          PopulateMetangaHeaders(httpClient, null, new Dictionary<string, string> { { "X-Metanga-InvoiceAction", invoiceAction.ToString() } });

          var response = httpClient.PostAsync(subscribeAddress, subscriptionSerialized).Result;
          CheckResponse(response, HttpStatusCode.Created);
          var responseContent = response.Content.ReadAsStreamAsync().Result;
          return DeserializeContent<Invoice>(responseContent);
        }
      }
    }

    /// <summary>
    /// Create an entity to database and return EntityId. If error occurred, MetangaException should be raised
    /// </summary>
    /// <param name="subscription">Metanga subscription entity to Subscribe</param>
    /// <returns>Metanga ID of newely created entity</returns>
    public Invoice Subscribe(Subscription subscription)
    {
      return Subscribe(subscription, InvoiceAction.InvoiceNext);
    }

    /// <summary>
    /// Modifies an existing subscription with the values supplied in the Subscription object passed in. The Unsubscribe and Change Quantity for Reservation product actions will be covered by the ModifySubscription method.  
    /// By providing the End Date, the account will be unsubscribed. This method returns a reference to an invoice object.
    /// </summary>
    /// <param name="subscription">Represents the subscription which will be created in the system by this call.</param>
    /// <param name="invoiceAction">The purpose of this parameter is to indicate what invoice action will be taken. It can have the next values:
    /// <ul>
    /// <li><i>InvoiceNext</i> - The newly calculated charges will be placed into invoices with naturally billing dates. Metanga will leave all new invoices open, and will return an open invoice for the current billing period.</li>
    /// <li><i>InvoiceNow</i> - The method will return a closed ad-hoc invoice (Invoice Date = Current Date), which incorporates any charges with the next or before billing date.</li>
    /// <li><i>InvoiceQuote</i> - The method will return an invoice object, which incorporates any charges with the next or before billing date, but these charges won't be saved into database.</li>
    /// </ul>
    /// </param>
    /// <param name="effectiveDate">This effective date will be used to determine when a quantity/unit change will become effective.</param>    
    /// <returns>Returns a reference to an invoice object which includes charges generated by this call.</returns>
    public Invoice Modify(Subscription subscription, DateTime? effectiveDate, InvoiceAction invoiceAction)
    {
      var subscribeAddress = new Uri(ServiceAddress, RestServiceSubscribe);
      
      using (var credentialStream = new MemoryStream())
      {
        var subscriptionSerialized = SerializeContent(subscription, credentialStream);
        using (var httpClient = new HttpClient())
        {
          var dictionaryHeaders = new Dictionary<string, string> {{"X-Metanga-InvoiceAction", invoiceAction.ToString()}};
          if (effectiveDate.HasValue)
          {
            var dateInIsoFormat = effectiveDate.Value.ToString("s", CultureInfo.InvariantCulture);
            dictionaryHeaders.Add("X-Metanga-EffectiveDate", dateInIsoFormat); 
          }

          PopulateMetangaHeaders(httpClient, null, dictionaryHeaders);

          var response = httpClient.PutAsync(subscribeAddress, subscriptionSerialized).Result;
          CheckResponse(response, HttpStatusCode.Created);
          var responseContent = response.Content.ReadAsStreamAsync().Result;
          return DeserializeContent<Invoice>(responseContent);
        }
      }
    }

    /// <summary>
    /// Modify subscription. If error occurred, MetangaException should be raised
    /// </summary>
    /// <param name="subscription">Metanga subscription entity to Modify</param>
    /// <param name="effectiveDate">Effective date for termination,</param>
    /// <returns>Metanga ID of newely created entity</returns>
    public Invoice Modify(Subscription subscription, DateTime? effectiveDate)
    {
      return Modify(subscription, effectiveDate, InvoiceAction.InvoiceNext);
    }

    ///<summary>
    /// This method will create and / or update a collection of subscriptions. The charges calculated by each operation
    /// will be aggregated into a single invoice.  
    /// Method only accepts subscriptions with the same payer account.
    /// </summary>
    /// <param name="subscriptionOperations">A collection of operations to be processed</param>
    /// <param name="effectiveDate">This effective date will be used to determine when a quantity/unit change will become effective (will be used when updating subscription).</param>
    /// <param name="invoiceAction">The purpose of this parameter is to indicate what invoice action will be taken. It can have the next values:
    /// <ul>
    /// <li><i>InvoiceNext</i> - The newly calculated charges will be placed into invoices with naturally billing dates. Metanga will leave all new invoices open, and will return an open invoice for the current billing period.</li>
    /// <li><i>InvoiceNow</i> - The method will return a closed ad-hoc invoice (Invoice Date = Current Date), which incorporates any charges with the next or before billing date.</li>
    /// <li><i>InvoiceQuote</i> - The method will return an invoice object, which incorporates any charges with the next or before billing date, but these charges won't be saved into database.</li>
    /// </ul>
    /// </param>
    /// <returns>Returns a reference to an invoice object which includes charges generated by this call.</returns>
    public Invoice TransitionSubscription(IEnumerable<SubscriptionOperation> subscriptionOperations, DateTime? effectiveDate, InvoiceAction invoiceAction)
    {
      var transitionSubscriptionAddress = new Uri(ServiceAddress, RestServiceTransitionSubscription);

      using (var credentialStream = new MemoryStream())
      {
        var subscriptionSerialized = SerializeContent(subscriptionOperations, credentialStream);
        using (var httpClient = new HttpClient())
        {
          var dictionaryHeaders = new Dictionary<string, string> { { "X-Metanga-InvoiceAction", invoiceAction.ToString() } };
          if (effectiveDate.HasValue)
          {
            var dateInIsoFormat = effectiveDate.Value.ToString("s", CultureInfo.InvariantCulture);
            dictionaryHeaders.Add("X-Metanga-EffectiveDate", dateInIsoFormat);
          }

          PopulateMetangaHeaders(httpClient, null, dictionaryHeaders);

          var response = httpClient.PostAsync(transitionSubscriptionAddress, subscriptionSerialized).Result;
          CheckResponse(response, HttpStatusCode.Created);
          var responseContent = response.Content.ReadAsStreamAsync().Result;
          return DeserializeContent<Invoice>(responseContent);
        }
      }
    }

    /// <summary>
    /// Submit usage events to be rated and charged by Metanga.
    /// A maximum of 1000 events can be submitted at a time
    /// </summary>
    /// <param name="batch">The batch information for this group of events</param>
    /// <param name="billableEvents">Collection of events to be rated</param>
    public void MeterUsageEvents(UsageBatch batch, IEnumerable<BillableEvent> billableEvents)
    {
      var meterUsageAddress = new Uri(ServiceAddress, RestServiceMeterUsageEvents);
      var meterParams = new MeterUsageEventsParameters { Batch = batch, BillableEvents = billableEvents };
      using (var credentialStream = new MemoryStream())
      {
        var meterParamsContent = SerializeContent(meterParams, credentialStream);
        using (var httpClient = new HttpClient())
        {
          PopulateMetangaHeaders(httpClient, null);

          var response = httpClient.PostAsync(meterUsageAddress, meterParamsContent).Result;
          CheckResponse(response, HttpStatusCode.Created);
        }
      }
    }

    #region Electronic Payments

    /// <summary>
    /// Process payment operation using the credit card or the bank account payment instruments.
    /// (Submit, credit or reverse operation)
    /// </summary>
    /// <param name="electronicPayment">The entity, populated with data required to process the funds withdrawing operation.</param>
    /// <returns></returns>
    public ElectronicPayment ProcessElectronicPayment(ElectronicPayment electronicPayment)
    {
      var electronicPaymentAddress = new Uri(ServiceAddress, RestServiceProcessElectronicPayment);
      using (var credentialStream = new MemoryStream())
      {
        var electronicPaymentParamsContent = SerializeContent(electronicPayment, credentialStream);
        using (var httpClient = new HttpClient())
        {
          PopulateMetangaHeaders(httpClient, null);

          var response = httpClient.PostAsync(electronicPaymentAddress, electronicPaymentParamsContent).Result;
          CheckResponse(response, HttpStatusCode.Created);
          var responseContent = response.Content.ReadAsStreamAsync().Result;
          return DeserializeContent<ElectronicPayment>(responseContent);
        }
      }
    }
    #endregion

    #region RetieveStatement

    ///<summary>
    /// Retieve Balance Statement for a certain time range and account
    ///</summary>
    ///<param name="account">Account for which statement will be retrieved</param>
    ///<param name="startDate">Date from which Statement will be calculated</param>
    ///<param name="endDate">Date up to which Statement will be calculated</param>
    /// <returns>KeyValue pair - currency : Statement object</returns>
    public Dictionary<string, Statement> RetrieveStatement(Entity account, DateTime? startDate, DateTime? endDate)
    {
      using (var httpClient = new HttpClient())
      {
        var entityIdentificator = GetEntityIdentificator(account);
        PopulateMetangaHeaders(httpClient, entityIdentificator.Value);
        
        var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
        if (startDate.HasValue)
          queryString["startDateTime"] = startDate.Value.ToString("s", CultureInfo.InvariantCulture);
        if (endDate.HasValue)
          queryString["endDateTime"] = endDate.Value.ToString("s", CultureInfo.InvariantCulture);

        var relativeUri = new Uri(RestServiceAccount + "/" + entityIdentificator.Key + "/statement" + (queryString.Count > 0 ? "?" + queryString : string.Empty), UriKind.Relative);

        var serviceUri = new Uri(ServiceAddress, relativeUri);
        var response = httpClient.GetAsync(serviceUri).Result;
        CheckResponse(response, HttpStatusCode.OK);
        var responseContent = response.Content.ReadAsStreamAsync().Result;
        return DeserializeContent<Dictionary<string, Statement>>(responseContent);
      }
    }

    #endregion

    /// <summary>
    /// <para><strong><font color="green">Please note, this is only beta-version of functionality. You should use it for testing purposes.</font></strong></para>
    /// By using this method, you are able to create a number of different entities in Metanga in one bulk operation.
    /// </summary>
    /// <param name="newEntities">The collection of entities to be created.</param>
    /// <returns>Collection of guids of the created Entities.</returns>
    public IEnumerable<Guid> CreateEntityBulk(IEnumerable<Entity> newEntities)
    {
      var enrollmentAddress = new Uri(ServiceAddress, RestServiceBulk);
      using (var credentialStream = new MemoryStream())
      {
        var entities = newEntities.ToList();
        var enrollParamsContent = SerializeContentForBulk(entities, credentialStream);
        using (var httpClient = new HttpClient())
        {
          var entityCount = entities.Count;
          if (entityCount > 100) httpClient.Timeout = new TimeSpan(0, 0, entityCount); // for more than 100 entities, set a timeout that allows for 1 second
          PopulateMetangaHeaders(httpClient, null);
          var response = httpClient.PostAsync(enrollmentAddress, enrollParamsContent).Result;
          CheckResponse(response, HttpStatusCode.Created);
          var responseContent = response.Content.ReadAsStreamAsync().Result;
          return DeserializeContent<IEnumerable<Guid>>(responseContent);
        }
      }
    }

    private StreamContent SerializeContentForBulk(IEnumerable<Entity> newEntities, Stream credentialStream)
    {
      if (ContentType == MetangaContentType.Json)
      {
        var entityCollection = new Collection<Entity>();
        foreach (var entity in newEntities)
          entityCollection.Add(entity);
        return SerializeContent(entityCollection, credentialStream);
      }
      return SerializeContent(newEntities, credentialStream);
    }


    /// <summary>
    /// <para><strong><font color="green">Please note, this is only beta-version of functionality. You should use it for testing purposes.</font></strong></para>
    /// By using this method, you are able to update a number of different entities in Metanga in one bulk operation.
    /// </summary>
    /// <param name="newEntities">The collection of entities to be updated.</param>
    public void UpdateEntityBulk(IEnumerable<Entity> newEntities)
    {
      var enrollmentAddress = new Uri(ServiceAddress, RestServiceBulk);
      using (var credentialStream = new MemoryStream())
      {
        var entities = newEntities.ToList();
        var enrollParamsContent = SerializeContentForBulk(entities, credentialStream);
        using (var httpClient = new HttpClient())
        {
          var entityCount = entities.Count;
          if (entityCount > 100) httpClient.Timeout = new TimeSpan(0, 0, entityCount); // for more than 100 entities, set a timeout that allows for 1 second
          PopulateMetangaHeaders(httpClient, null);
          var response = httpClient.PutAsync(enrollmentAddress, enrollParamsContent).Result;
          CheckResponse(response, HttpStatusCode.OK);
        }
      }
    }

    /// <summary>
    /// <para><strong><font color="green">Please note, this is only beta-version of functionality. You should use it for testing purposes.</font></strong></para>
    /// By using this method, you are able to delete a number of different entities in Metanga in one bulk operation.
    /// </summary>
    /// <param name="deletedEntities">The collection of entities to be updated.</param>
    public void DeleteEntityBulk(IEnumerable<Entity> deletedEntities)
    {
      var serviceUri = new Uri(ServiceAddress, RestServiceBulk);
      using(var requestMessage = new HttpRequestMessage(HttpMethod.Delete, serviceUri))
      using (var httpClient = new HttpClient())
      using (var entityStream = new MemoryStream())
      {
        var entities = deletedEntities.ToList();
        var entityCount = entities.Count;
        if (entityCount > 100) httpClient.Timeout = new TimeSpan(0, 0, entityCount); // for more than 100 entities, set a timeout that allows for 1 second
        PopulateMetangaHeaders(httpClient, null);
        var entityContent = SerializeContentForBulk(entities, entityStream);
        requestMessage.Content = entityContent;
        using (var response = httpClient.SendAsync(requestMessage).Result)
          CheckResponse(response, HttpStatusCode.OK);
      }
    }
    
    /// <summary>
    /// Create an entity to database and return EntityId. If error occurred, MetangaException should be raised
    /// </summary>
    /// <param name="entity">Metanga entity to create</param>
    /// <returns>Metanga ID of newely created entity</returns>
    public Guid CreateEntity(Entity entity)
    {
      if (entity == null)
        throw new ArgumentNullException("entity");
      using (var response = ProcessEntity(entity, (httpClient, serviceUri, messageContent) => httpClient.PostAsync(serviceUri, messageContent)))
      {
        CheckResponse(response, HttpStatusCode.Created);
        var responseContent = response.Content.ReadAsStreamAsync().Result;
        return DeserializeContent<Guid>(responseContent);
      }
    }

    /// <summary>
    /// Update an entity to database. If error occurred,  MetangaException should be raised
    /// </summary>
    /// <param name="entity">Metanga entity to update</param>
    public void UpdateEntity(Entity entity)
    {
      using (var response = ProcessEntity(entity, (httpClient, serviceUri, messageContent) => httpClient.PutAsync(serviceUri, messageContent)))
        CheckResponse(response, HttpStatusCode.OK);
    }
    
    /// <summary>
    /// Delete an entity from database. If error occurred,  MetangaException should be raised
    /// </summary>
    /// <param name="entity">Metanga entity to delete</param>
    public void DeleteEntity(Entity entity)
    {
      if (entity == null)
        throw new ArgumentNullException("entity");
      using (var httpClient = new HttpClient())
      {
        var entityIdentificator = GetEntityIdentificator(entity);
        PopulateMetangaHeaders(httpClient, entityIdentificator.Value);
        var serviceUri = CombineUri(entity.GetType().Name, entityIdentificator.Key);
        using (var response = httpClient.DeleteAsync(serviceUri).Result)
          CheckResponse(response, HttpStatusCode.OK);
      }
    }
    
    /// <summary>
    /// Retrieve an entity by EntityId from database. If error occurred,  MetangaException should be raised
    /// </summary>
    /// <typeparam name="T">Type of Metanga entity </typeparam>
    /// <param name="entityId">Metanga entity Id</param>
    /// <returns>Metanga entity</returns>
    public T RetrieveEntity<T>(Guid entityId) where T : Entity, new()
    {
      return RetrieveEntity<T>(TypeOfIdMetanga, entityId.ToString());
    }

    /// <summary>
    /// Retrieve an entity by ExternalId from database. If error occurred,  MetangaException should be raised
    /// </summary>
    /// <typeparam name="T">Type of Metanga entity </typeparam>
    /// <param name="externalId">External entity Id</param>
    /// <returns>Metanga entity</returns>
    public T RetrieveEntity<T>(string externalId) where T : Entity, new()
    {
      return RetrieveEntity<T>(TypeOfIdExternal, externalId);
    }


    /// <summary>
    /// <para><strong><font color="green">Please note, this is only beta-version of functionality. You should use it for testing purposes.</font></strong></para>
    /// By using this method, you are able to retrieve all the entities of a certain type in one bulk operetion.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="requestParameters">OData URI Query string which is used to determine filtering options</param>
    /// <returns>Collection of entities of a certain type.</returns>
    public IEnumerable<T> RetrieveEntitiesBulk<T>(string requestParameters) where T : Entity, new()
    {
      var typeName = ResolveBaseEntityType(typeof(T)).Name;
      var serviceUri = CombineUri(string.Format(CultureInfo.InvariantCulture, "{0}?{1}", typeName, requestParameters));
      
      using (var httpClient = new HttpClient())
      {
        PopulateMetangaHeaders(httpClient, null);
        using (var result = httpClient.GetAsync(serviceUri))
        {
          var response = result.Result;
          CheckResponse(response, HttpStatusCode.OK);
          var responseContent = response.Content.ReadAsStreamAsync().Result;
          switch (ContentType)
          {
            case MetangaContentType.Json:
              return DeserializeContent<IEnumerable<T>>(responseContent);
            case MetangaContentType.Xml:
              return DeserializeContent<IEnumerable<T>>(responseContent).Select(x=>x as T);
            default:
              throw new NotSupportedException();
          }
        }
      }
    }
    /// <summary>
    ///  <para><strong><font color="green">Please note, this is only beta-version of functionality. You should use it for testing purposes.</font></strong></para>
    /// By using this method, you are able to retrieve all the entities of a certain type in one bulk operetion.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <returns>Collection of entities of a certain type.</returns>
    public IEnumerable<T> RetrieveEntitiesBulk<T>() where T : Entity, new()
    {
      return RetrieveEntitiesBulk<T>(null);
    }

    /// <summary>
    /// Close session
    /// </summary>
    public void Close()
    {
      //TODO: there is a strange error. We have to commnet it for now
      //Error Message: Metanga.SoftwareDevelopmentKit.Rest.MetangaException: Session verification failed. Session 81452fcf-013a-2bbc-cd94-4d68b1ecc977 has expired.
//      using (var httpClient = new HttpClient())
//      {
//        PopulateMetangaHeader(httpClient, TypeOfIdExternal);
//        var serviceUri = new Uri(ServiceAddress, RestServiceSession);
//        using (var response = httpClient.DeleteAsync(serviceUri).Result)
//          CheckResponse(response, HttpStatusCode.OK);
//      }
      SessionId = Guid.Empty;
    }

    #endregion

    #region Private methods

    private Guid CreateSession()
    {
      var sessionAddress = new Uri(ServiceAddress, RestServiceSession);
      var credentials = new PasswordCredential {Password = Password, UserName = UserName};
      using (var httpClient = new HttpClient())
      {
        using (var credentialStream = new MemoryStream())
        {
          var serializedContent = SerializeContent(credentials, credentialStream);
          var response = httpClient.PostAsync(sessionAddress, serializedContent).Result;
          CheckResponse(response, HttpStatusCode.Created);
          var responseContent = response.Content.ReadAsStreamAsync().Result;
          return DeserializeContent<Guid>(responseContent);
        }
      }
    }

    private HttpResponseMessage ProcessEntity(Entity entity, ProcessRequest method, Dictionary<string, string> addCustomHeaders = null)
    {
      var entityName = entity.GetType().Name;
      var serviceUri = CombineUri(entityName);
      using (var httpClient = new HttpClient())
      {
        PopulateMetangaHeaders(httpClient, string.Empty, addCustomHeaders);

        using (var entityStream = new MemoryStream())
        {
          var entityContent = SerializeContent(entity, entityStream);
          var response = method(httpClient, serviceUri, entityContent).Result;
          return response;
        }
      }
    }

    private void PopulateMetangaHeaders(HttpClient httpClient, string typeId, Dictionary<string, string> addCustomHeaders = null)
    {
      httpClient.DefaultRequestHeaders.Add("X-Metanga-SessionId", SessionId.ToString());
      if (!string.IsNullOrEmpty(typeId))
        httpClient.DefaultRequestHeaders.Add("X-Metanga-ReferenceType", typeId);
      
      //Populate Accept header
      httpClient.DefaultRequestHeaders.Accept.Clear();
      httpClient.DefaultRequestHeaders.Accept.Add(_contentFormatHeader);

      if (addCustomHeaders != null)
        foreach (var header in addCustomHeaders)
          httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
    }

    private T RetrieveEntity<T>(string typeOfId, string value) where T : Entity, new()
    {
      var serviceUri = CombineUri(typeof(T).Name, value);

      using (var httpClient = new HttpClient())
      {
        PopulateMetangaHeaders(httpClient, typeOfId);
        using (var result = httpClient.GetAsync(serviceUri))
        {
          var response = result.Result;
          CheckResponse(response, HttpStatusCode.OK);
          var stream = response.Content.ReadAsStreamAsync().Result;
          return DeserializeContent<T>(stream);
        }
      }
    }

    private T DeserializeContent<T>(Stream stream)
    {
      using (var streamReader = new StreamReader(stream, Encoding))
      {
        switch (ContentType)
        {
          case MetangaContentType.Json:
            {
              var jsonTextReader = new JsonTextReader(streamReader);
              return (T) JsonSerializer.Deserialize(jsonTextReader, typeof (T));
            }
          case MetangaContentType.Xml:
            {
              var serializer = new DataContractSerializer(typeof(T));
              var xmlReader = XmlReader.Create(streamReader);
              return (T)serializer.ReadObject(xmlReader);
            }
          default:
            throw new NotSupportedException();
        }
      }
    }

    private Uri CombineUri(params string[] segments)
    {
      const string restService = "RestService";
      var uri = new StringBuilder();
      if (!segments[0].Contains(restService))
        uri.Append(restService);
      foreach (var segment in segments)
        uri.Append("/" + segment);
      var relativeUri = new Uri(uri.ToString(), UriKind.Relative);
      return new Uri(ServiceAddress, relativeUri);
    }

    private static Type ResolveBaseEntityType(Type type)
    {
      if (type.BaseType == null || type.BaseType == typeof(Entity) || type.BaseType == typeof(ExtensibleEntity) || type.BaseType == typeof(ManualPayment) || type.BaseType == typeof(Payment))
        return type;
      return ResolveBaseEntityType(type.BaseType);
    }

    #endregion
  }

  /// <summary>
  /// For abstract classes and interfaces, Metanga returns types that are compatible with
  /// the default names created by the WSDL Proxy. However, the assembly name is not
  /// populated by Metanga and has to be set by the a binder.
  /// </summary>
  internal class ClientJsonSerializationBinder : DefaultSerializationBinder
  {
    private const string AssemblyName = "Metanga.SoftwareDevelopmentKit";

    public override Type BindToType(string assemblyName, string typeName)
    {
      if(!string.IsNullOrEmpty(typeName))
        typeName = typeName.Replace("metanga.com.", AssemblyName+".Proxy.");
      return base.BindToType(AssemblyName, typeName);
    }

    public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
    {
      if (serializedType == null) throw new ArgumentNullException("serializedType");
      assemblyName = AssemblyName;
      typeName = "metanga.com." + serializedType.Name;
    }


  }

}
