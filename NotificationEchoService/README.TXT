Notifications Echo Service

Summary

The Notifications Echo Service Sample application demonstrates a simple WCF Host 
that can be used as an API Notification Endpoint for Metanga. The application is
a console application and as a result will only work while running. Run from the
commandline or through the debugger.

Steps to Use:

1) Code Changes: In the NotificationServiceHost.cs file you will need to replace the code:

       String.Format("http://{0}.metratech.com/NotificationServiceHost", _machineName) 

with something that represents the actual service endpint that you expect to have visible
when running the application on your machine. After you make this change you can run the
application and check its status by using the IsAlive service and a browser. Just open any
browser and browse to your serviceendpoint /IsAlive. Example:

http://metangadev02.metratech.com/NotificationServiceHost/IsAlive

You should see the text "Yes, I'm Alive" in your browser window. If you see this then your 
service is running and should work fine.

2) Metanga Configuration: In Metanga you will want to add an API Endpoint that represents your
service endpoint entered above. You should select REST as the type of endpoint and the format 
should be XML and not JSON. The Sample does nto use Authentication so leaving "Client Certificate"
enabled is fine. Next you should create an API Notification in Metanga that uses your new endpoint.
With the console application running, you should be able to "Test Notification" within Metanga and 
see the XML appear in the console window.