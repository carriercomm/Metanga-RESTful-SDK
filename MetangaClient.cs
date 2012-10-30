using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Metanga.SoftwareDevelopmentKit.Proxy;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
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
    private static readonly Uri RestServiceBulk = new Uri("RestService/bulk", UriKind.Relative);
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

    #endregion

    #region Private static methods

    private static void CheckResponse(HttpResponseMessage response, HttpStatusCode expectedStatusCode)
    {
      if (response.IsSuccessStatusCode && response.StatusCode == expectedStatusCode)
        return;
      GenerateExceptionFromResponse(response);
    }

    private static void GenerateExceptionFromResponse(HttpResponseMessage httpResponseMessage)
    {
      if (httpResponseMessage == null)
        throw new ArgumentNullException("httpResponseMessage");
      using (var streamReader = new StreamReader(httpResponseMessage.Content.ReadAsStreamAsync().Result, Encoding))
      {
        var jsonTextReader = new JsonTextReader(streamReader);
        var errorJsonObject = (dynamic) JsonSerializer.Deserialize(jsonTextReader);

        var errorId = Guid.Parse(errorJsonObject.ErrorId.ToString());
        var errorMessage = errorJsonObject.ErrorMessage.ToString();
        throw new MetangaException(errorMessage, errorId);
      }
    }

    private static Guid GetEntityIdFromResponse(HttpResponseMessage httpResponseMessage)
    {
      if (httpResponseMessage == null)
        throw new ArgumentNullException("httpResponseMessage");
      using (var streamReader = new StreamReader(httpResponseMessage.Content.ReadAsStreamAsync().Result, Encoding))
      {
        var jsonTextReader = new JsonTextReader(streamReader);
        var jsonObject = JToken.Load(jsonTextReader);
        var entityId = jsonObject.ToString();
        return Guid.Parse(entityId);
      }
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

    private static StreamContent GetJsonContent(JToken jsonValue, Stream httpStream)
    {
      if (jsonValue == null) throw new ArgumentNullException("jsonValue");
      if (httpStream == null) throw new ArgumentNullException("httpStream");
      using (var jsonTextWriter = new JsonTextWriter(new StreamWriter(httpStream, Encoding)) {CloseOutput = false})
      {
        jsonValue.WriteTo(jsonTextWriter);
        jsonTextWriter.Flush();
      }
      httpStream.Seek(0, SeekOrigin.Begin);
      var jsonContent = new StreamContent(httpStream);
      var contentType = new MediaTypeHeaderValue("application/json");
      jsonContent.Headers.ContentType = contentType;
      return jsonContent;
    }

    private static StreamContent SerializeObjectToJsonContent(object entity, Stream entityStream)
    {
      if (entityStream == null) throw new ArgumentNullException("entityStream");
      var jsonTextWriter = new JsonTextWriter(new StreamWriter(entityStream, Encoding)) {CloseOutput = false};
      JsonSerializer.Serialize(jsonTextWriter, entity);
      jsonTextWriter.Flush();
      entityStream.Seek(0, SeekOrigin.Begin);
      var streamContent = new StreamContent(entityStream);
      streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
      return streamContent;
    }

    private static StreamContent SerializeToJsonContent(Entity entity, Stream entityStream)
    {
      if (entityStream == null) throw new ArgumentNullException("entityStream");
      var jsonTextWriter = new JsonTextWriter(new StreamWriter(entityStream, Encoding)) {CloseOutput = false};
      JsonSerializer.Serialize(jsonTextWriter, entity);
      jsonTextWriter.Flush();
      entityStream.Seek(0, SeekOrigin.Begin);
      var streamContent = new StreamContent(entityStream);
      streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
      return streamContent;
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
      if (contentType != MetangaContentType.Json)
        throw new NotImplementedException("Processing of data in the format of XML has not been implemented yet.");

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
    private MetangaContentType ContentType { get; set; }

    #endregion

    #region Public methods

    /// <summary>
    /// Enroll method
    /// </summary>
    /// <param name="subscription">new subscription entity</param>
    /// <param name="account">new account entity</param>
    /// <param name="invoiceAction">Invoice action</param>
    /// <returns>return new invoice</returns>
    public Invoice Enroll(Subscription subscription, Account account, InvoiceAction invoiceAction)
    {
      var enrollmentAddress = new Uri(ServiceAddress, RestServiceEnrollment);
      var enrollParams = new {Subscription = subscription, Account = account};
      using (var credentialStream = new MemoryStream())
      {
        var enrollParamsContent = SerializeObjectToJsonContent(enrollParams, credentialStream);
        Invoice returnInvoice;
        using (var httpClient = new HttpClient())
        {
          httpClient.DefaultRequestHeaders.Add("X-Metanga-SessionId", SessionId.ToString());
          httpClient.DefaultRequestHeaders.Add("X-Metanga-InvoiceAction", invoiceAction.ToString());
          var response = httpClient.PostAsync(enrollmentAddress, enrollParamsContent).Result;
          CheckResponse(response, HttpStatusCode.Created);
          var responseContent = response.Content.ReadAsStreamAsync();
          using (var streamReader = new StreamReader(responseContent.Result, Encoding))
          {
            var jsonTextReader = new JsonTextReader(streamReader);
            returnInvoice = (Invoice) JsonSerializer.Deserialize(jsonTextReader, typeof (Invoice));
          }
        }
        return returnInvoice;
      }
    }

    /// <summary>
    /// Create an entity to database and return EntityId. If error occurred, MetangaException should be raised
    /// </summary>
    /// <param name="subscription">Metanga subscription entity to Subscribe</param>
    /// <param name="invoiceAction">Invoice action.</param>
    /// <returns>Metanga ID of newely created entity</returns>
    public Invoice Subscribe(Subscription subscription, InvoiceAction invoiceAction)
    {
      var subscribeAddress = new Uri(ServiceAddress, RestServiceSubscribe);
      //var subscribeParams = new { Subscription = subscription };
      using (var credentialStream = new MemoryStream())
      {
        var subscriptionSerialized = SerializeObjectToJsonContent(subscription, credentialStream);
        Invoice returnInvoice;
        using (var httpClient = new HttpClient())
        {
          httpClient.DefaultRequestHeaders.Add("X-Metanga-SessionId", SessionId.ToString());
          httpClient.DefaultRequestHeaders.Add("X-Metanga-InvoiceAction", invoiceAction.ToString());
          var response = httpClient.PostAsync(subscribeAddress, subscriptionSerialized).Result;
          CheckResponse(response, HttpStatusCode.Created);
          var responseContent = response.Content.ReadAsStreamAsync();
          using (var streamReader = new StreamReader(responseContent.Result, Encoding))
          {
            var jsonTextReader = new JsonTextReader(streamReader);
            returnInvoice = (Invoice) JsonSerializer.Deserialize(jsonTextReader, typeof (Invoice));
          }
        }
        return returnInvoice;
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
    /// Modify subscription. If error occurred, MetangaException should be raised
    /// </summary>
    /// <param name="subscription">Metanga subscription entity to Modify</param>
    /// <param name="effectiveDate">Effective date for termination,</param>
    /// <param name="invoiceAction">Invoice action.</param>
    /// <returns>Metanga ID of newely created entity</returns>
    public Invoice Modify(Subscription subscription, DateTime? effectiveDate, InvoiceAction invoiceAction)
    {
      var subscribeAddress = new Uri(ServiceAddress, RestServiceSubscribe);
      
      using (var credentialStream = new MemoryStream())
      {
        var subscriptionSerialized = SerializeObjectToJsonContent(subscription, credentialStream);
        Invoice returnInvoice;
        using (var httpClient = new HttpClient())
        {
          if (effectiveDate.HasValue)
          {
            var dateInIsoFormat = effectiveDate.Value.ToString("s", CultureInfo.InvariantCulture);
            httpClient.DefaultRequestHeaders.Add("X-Metanga-EffectiveDate", dateInIsoFormat);
          }

          httpClient.DefaultRequestHeaders.Add("X-Metanga-SessionId", SessionId.ToString());
          httpClient.DefaultRequestHeaders.Add("X-Metanga-InvoiceAction", invoiceAction.ToString());
          var response = httpClient.PutAsync(subscribeAddress, subscriptionSerialized).Result;
          CheckResponse(response, HttpStatusCode.Created);
          var responseContent = response.Content.ReadAsStreamAsync();
          using (var streamReader = new StreamReader(responseContent.Result, Encoding))
          {
            var jsonTextReader = new JsonTextReader(streamReader);
            returnInvoice = (Invoice)JsonSerializer.Deserialize(jsonTextReader, typeof(Invoice));
          }
        }
        return returnInvoice;
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
        var enrollParamsContent = SerializeObjectToJsonContent(newEntities, credentialStream);
        IEnumerable<Guid> entitiesGuids;
        using (var httpClient = new HttpClient())
        {
          PopulateSessionHeader(httpClient, null);
          var response = httpClient.PostAsync(enrollmentAddress, enrollParamsContent).Result;
          CheckResponse(response, HttpStatusCode.Created);
          var responseContent = response.Content.ReadAsStreamAsync();
          using (var streamReader = new StreamReader(responseContent.Result, Encoding))
          {
            var jsonTextReader = new JsonTextReader(streamReader);
            entitiesGuids = JsonSerializer.Deserialize<IEnumerable<Guid>>(jsonTextReader);
          }
        }
        return entitiesGuids;
      }
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
        var enrollParamsContent = SerializeObjectToJsonContent(newEntities, credentialStream);
        using (var httpClient = new HttpClient())
        {
          PopulateSessionHeader(httpClient, null);
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
      var requestMessage = new HttpRequestMessage(HttpMethod.Delete, serviceUri);
      using (var httpClient = new HttpClient())
      using (var entityStream = new MemoryStream())
      {
        PopulateSessionHeader(httpClient, null);
        var entityContent = SerializeObjectToJsonContent(deletedEntities, entityStream);
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
        return GetEntityIdFromResponse(response);
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
        PopulateSessionHeader(httpClient, entityIdentificator.Value);
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
    /// <returns>Collection of entities of a certain type.</returns>
    public IEnumerable<T> RetrieveEntitiesBulk<T>() where T : Entity, new()
    {
      var serviceUri = CombineUri(RestServiceBulk.ToString(), typeof(T).Name);
      IEnumerable<T> entity;
      using (var httpClient = new HttpClient())
      {
        PopulateSessionHeader(httpClient, null);
        using (var result = httpClient.GetAsync(serviceUri))
        {
          var response = result.Result;
          CheckResponse(response, HttpStatusCode.OK);
          using (var streamReader = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding))
          {
            var jsonTextReader = new JsonTextReader(streamReader);
            entity = JsonSerializer.Deserialize<IEnumerable<T>>(jsonTextReader);
          }
        }
      }
      return entity;
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
//        PopulateSessionHeader(httpClient, TypeOfIdExternal);
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
      var credentials = (dynamic)new JObject();
      credentials.UserName = UserName;
      credentials.Password = Password;
      using (var credentialStream = new MemoryStream())
      {
        StreamContent credentialsContent = GetJsonContent(credentials, credentialStream);
        dynamic sessionObject;
        using (var httpClient = new HttpClient())
        {
          var response = httpClient.PostAsync(sessionAddress, credentialsContent).Result;
          CheckResponse(response, HttpStatusCode.Created);
          var responseContent = response.Content.ReadAsStreamAsync();
          using (var streamReader = new StreamReader(responseContent.Result, Encoding))
          {
            var jsonTextReader = new JsonTextReader(streamReader);
            sessionObject = JToken.Load(jsonTextReader);
          }
        }
        return new Guid(sessionObject.Value);
      }
    }
    private HttpResponseMessage ProcessEntity(Entity entity, ProcessRequest method, Dictionary<string, string> addCustomHeaders = null)
    {
      var entityName = entity.GetType().Name;
      var serviceUri = CombineUri(entityName);
      using (var httpClient = new HttpClient())
      {
        PopulateSessionHeader(httpClient, string.Empty, addCustomHeaders);

        using (var entityStream = new MemoryStream())
        {
          var entityContent = SerializeToJsonContent(entity, entityStream);
          var response = method(httpClient, serviceUri, entityContent).Result;
          return response;
        }
      }
    }
    private void PopulateSessionHeader(HttpClient httpClient, string typeId, Dictionary<string, string> addCustomHeaders = null)
    {
      httpClient.DefaultRequestHeaders.Add("X-Metanga-SessionId", SessionId.ToString());
      if (!string.IsNullOrEmpty(typeId))
        httpClient.DefaultRequestHeaders.Add("X-Metanga-ReferenceType", typeId);
      httpClient.DefaultRequestHeaders.Add("Accept", ContentType == MetangaContentType.Json ? "application/json" : "application/xml");

      if (addCustomHeaders != null)
        foreach (var header in addCustomHeaders)
          httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
    }
    private T RetrieveEntity<T>(string typeOfId, string value) where T : Entity, new()
    {
      var serviceUri = CombineUri(typeof(T).Name, value);
      T entity;

      using (var httpClient = new HttpClient())
      {
        PopulateSessionHeader(httpClient, typeOfId);
        using (var result = httpClient.GetAsync(serviceUri))
        {
          var response = result.Result;
          CheckResponse(response, HttpStatusCode.OK);
          using (var streamReader = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding))
          {
            var jsonTextReader = new JsonTextReader(streamReader);
            entity = (T)JsonSerializer.Deserialize(jsonTextReader, typeof(T));
          }
        }
      }
      return entity;
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
