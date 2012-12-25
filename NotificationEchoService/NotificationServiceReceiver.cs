using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Xml;

namespace NotificationEchoService
{
  [ServiceContract]
  [ServiceBehavior(InstanceContextMode= InstanceContextMode.Single)]
  public class NotificationServiceReceiver
  {

    /// <summary>
    /// It is always Useful to have one method that allows you to test if the service
    /// is running from a simple browser session
    /// </summary>
    /// <returns></returns>
    [WebInvoke(UriTemplate = "IsAlive", Method = "GET")]
    public Stream TestPage()
    {
      if (WebOperationContext.Current != null)
        WebOperationContext.Current.OutgoingResponse.ContentType = "text/html; charset=utf-8";

      Console.WriteLine("Service is Alive");
      
      return new MemoryStream(Encoding.UTF8.GetBytes("<html><body>Yes, I am alive</body></html>"));
    }

    /// <summary>
    /// Post method which can receive content. Content is assumed to be valid XML (Configure your EndPoint to use REST/XML) 
    /// and will render it to the console window.
    /// </summary>
    /// <param name="stream"></param>
    [WebInvoke(UriTemplate = "", Method = "POST")]
    public void Post(Stream stream)
    {
      using (var reader = new StreamReader(stream))
      {
        var content = reader.ReadToEnd();
        Console.Out.WriteLine(PrintXml(content));
      }
      return;
    }

    /// <summary>
    /// Credit from http://stackoverflow.com/questions/1123718/format-xml-string-to-print-friendly-xml-string
    /// </summary>
    /// <param name="xml"></param>
    /// <returns></returns>
    private static String PrintXml(String xml)
    {
      string result;

      var mStream = new MemoryStream();
      var writer = new XmlTextWriter(mStream, Encoding.Unicode);
      var document = new XmlDocument();

      try
      {
        // Load the XmlDocument with the XML.
        document.LoadXml(xml);

        writer.Formatting = Formatting.Indented;

        // Write the XML into a formatting XmlTextWriter
        document.WriteContentTo(writer);
        writer.Flush();
        mStream.Flush();

        // Have to rewind the MemoryStream in order to read
        // its contents.
        mStream.Position = 0;

        // Read MemoryStream contents into a StreamReader.
        var sReader = new StreamReader(mStream);

        // Extract the text from the StreamReader.
        var formattedXml = sReader.ReadToEnd();

        result = formattedXml;
      }
      catch (XmlException xmlException)
      {
        return xmlException.Message;
      }

      mStream.Close();
      writer.Close();

      return result;
    }
    
  }
}