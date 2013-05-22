using System;
using System.Runtime.Serialization;

namespace Metanga.SoftwareDevelopmentKit.Rest
{
  /// <summary>
  /// This is an exception type that should be used by classes at the service boundary.
  /// It represents an error that is consumable by SDK
  /// </summary>
  [Serializable]
  public class MetangaException : Exception
  {
    /// <summary>
    /// ErrorId
    /// </summary>
    public Guid ErrorId { get; private set; }

    /// <summary>
    /// Default constructor
    /// </summary>
    public MetangaException()
    {
    }

    /// <summary>
    /// Creates an exception with an error message
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception</param>
    public MetangaException(string message)
      : base(message)
    {
    }

    /// <summary>
    /// Creates an exception with an error message and exception ID
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception</param>
    /// <param name="errorId">Metanga error Id</param>
    public MetangaException(string message, Guid errorId)
      : base(message)
    {
      ErrorId = errorId;
    }

    /// <summary>
    /// Creates an exception with an error message and inner exception
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception</param>
    /// <param name="inner">The exception that is the cause of the current exception</param>
    public MetangaException(string message, Exception inner)
      : base(message, inner)
    {
    }

    /// <summary>
    /// This constructor is required for serialization
    /// </summary>
    /// <param name="info">Contains all the data needed to serialize and deserialize an object</param>
    /// <param name="context">Describes the source and destination of a given serialized stream, and provides an additional caller-defined context</param>
    protected MetangaException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    /// <summary>
    /// GetObjectData
    /// </summary>
    /// <param name="info">Serialization info</param>
    /// <param name="context">Streaming context</param>
    /// <exception cref="ArgumentNullException"></exception>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new ArgumentNullException("info");
      base.GetObjectData(info, context);
    }
  }
}
