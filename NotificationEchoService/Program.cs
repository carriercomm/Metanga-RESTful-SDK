using System;

namespace NotificationEchoService
{
  class Program
  {
    static void Main()
    {

      using (var service = new NotificationServiceHost())
      {
        service.StartHost();
        
        // The service can now be accessed.
        Console.WriteLine("The service is ready.");
        Console.WriteLine("Press <ENTER> to terminate service.");
        Console.ReadLine();
      }


    }
  }
}
