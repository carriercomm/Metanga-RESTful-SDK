using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Metanga.SoftwareDevelopmentKit.Proxy;
using Metanga.SoftwareDevelopmentKit.Rest;

namespace Metanga.SoftwareDevelopmentKit
{
  /// <summary>
  /// Metanga Aggregate Exception
  /// </summary>
  [Serializable]
  public class MetangaAggregateException : MetangaException
  {
    /// <summary>
    /// Default constructor
    /// </summary>
    public MetangaAggregateException()
    {
    }

    /// <summary>
    /// Creates an exception with an error message
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception</param>
    public MetangaAggregateException(string message)
      : base(message)
    {
    }

    /// <summary>
    /// Creates an exception with an error message and inner exception
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception</param>
    /// <param name="inner">The exception that is the cause of the current exception</param>
    public MetangaAggregateException(string message, Exception inner)
      : base(message, inner)
    {
    }

    /// <summary>
    /// This constructor is required for serialization
    /// </summary>
    /// <param name="info">Contains all the data needed to serialize and deserialize an object</param>
    /// <param name="context">Describes the source and destination of a given serialized stream, and provides an additional caller-defined context</param>
    protected MetangaAggregateException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="errorId"></param>
    /// <param name="innerErrors"></param>
    public MetangaAggregateException(string message, Guid errorId, IEnumerable<ErrorData> innerErrors)
      : base(message, errorId)
    {
      if (innerErrors == null) throw new ArgumentNullException("innerErrors");
      Exceptions = innerErrors.Select(x => new EntityError(x.AssociatedEntity, new MetangaException(x.ErrorMessage, x.ErrorId)));
    }

    /// <summary>
    /// GetObjectData
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new ArgumentNullException("info");
      base.GetObjectData(info, context);
    }

    /// <summary>
    /// List of exceptions
    /// </summary>
    public IEnumerable<EntityError> Exceptions { get; set; }
  }

  /// <summary>
  /// Entity Error
  /// </summary>
  [Serializable]
  public class EntityError
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="entityReference"></param>
    /// <param name="metangaException"></param>
    /// <exception cref="NotImplementedException"></exception>
    public EntityError(Entity entityReference, MetangaException metangaException)
    {
      EntityReference = entityReference;
      Error = metangaException;
    }

    /// <summary>
    /// Entity Reference
    /// </summary>
    public Entity EntityReference { get; set; }
    /// <summary>
    /// Metanga Exception
    /// </summary>
    public MetangaException Error { get; set; }
  }
}
