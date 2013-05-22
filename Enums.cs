using System.Runtime.Serialization;

namespace Metanga.SoftwareDevelopmentKit.Rest
{
  /// <summary>
  /// Enum of Content format for RESTful
  /// </summary>
  public enum MetangaContentType 
  {
    /// <summary>
    /// JSON
    /// </summary>
    [EnumMember]
    Json = 0,
    /// <summary>
    /// XML
    /// </summary>
    [EnumMember]
    Xml = 1,
  }
}
