using System;
using System.Runtime.Serialization;
using Metanga.SoftwareDevelopmentKit.Proxy;

namespace Metanga.SoftwareDevelopmentKit.Rest
{
  [DataContract(Namespace = "http://metanga.com")]
  internal class EnrollParameters
  {
    /// <summary>
    /// New Subscription
    /// </summary>
    [DataMember]
    public Subscription Subscription { get; set; }
    
    /// <summary>
    /// New Account
    /// </summary>
    [DataMember]
    public Account Account { get; set; }
  }

  [DataContract(Namespace = "http://metanga.com")]
  internal class PasswordCredential
  {
    /// <summary>
    /// The username of the account
    /// </summary>
    [DataMember]
    public string UserName { get; set; }
    /// <summary>
    /// The password of the account
    /// </summary>
    [DataMember]
    public string Password { get; set; }
  }

  /// <summary>
  /// Contains details about errors that occur while processing
  /// </summary>
  [DataContract(Namespace = "metanga.com")]
  internal class ErrorData
  {
      /// <summary>
      /// A description of the error
      /// </summary>
      [DataMember]
      public string ErrorMessage { get; set; }
      /// <summary>
      /// A unique id for the error
      /// </summary>
      [DataMember]
      public Guid ErrorId { get; set; }
  }
}
