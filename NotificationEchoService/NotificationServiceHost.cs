using System;
using System.ServiceModel;
using System.ServiceModel.Web;


namespace NotificationEchoService
{

  /// <summary>
  /// WCF Service Host
  /// </summary>
  class NotificationServiceHost: IDisposable
  {

    private ServiceHost _notificationReceiverHost;

    private readonly string _machineName;
    
    public NotificationServiceHost(string machineName)
    {
      _machineName = machineName;
    }

    public NotificationServiceHost()
      : this(Environment.MachineName)
    {
    }

    public void StopHost()
    {
      if (_notificationReceiverHost != null && _notificationReceiverHost.State == CommunicationState.Opened)
        _notificationReceiverHost.Close();
    }
    /// <summary>
    /// Start host
    /// </summary>
    public void StartHost()
    {
      
      var restDemoService = new NotificationServiceReceiver();
      _notificationReceiverHost = new WebServiceHost(restDemoService)
                                    {
                                      OpenTimeout = new TimeSpan(0, 0, 20),
                                      CloseTimeout = new TimeSpan(0, 0, 20)
                                    };
      _notificationReceiverHost.AddServiceEndpoint(typeof(NotificationServiceReceiver), new WebHttpBinding(), String.Format("http://{0}.metratech.com/NotificationServiceHost", _machineName)); 
      
      _notificationReceiverHost.Open();
    }

    
    public void Dispose()
    {
      StopHost();
      _notificationReceiverHost = null;
    }
  }
}
