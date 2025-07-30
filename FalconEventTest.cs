using System;
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;

// Test program to check what properties are available in Falcon's GroupMessageReceived event
public class FalconEventTest
{
    public static void TestFalconEventProperties()
    {
        var parameters = new IpTunnelingConnectorParameters()
        {
            HostAddress = "192.168.20.2",
            AutoReconnect = true,
        };

        var knxBus = new KnxBus(parameters);
        
        // Let's see what properties are available in the args
        knxBus.GroupMessageReceived += (sender, args) =>
        {
            Console.WriteLine("=== Falcon GroupMessageReceived Event Properties ===");
            Console.WriteLine($"Type of args: {args.GetType().FullName}");
            
            // Check available properties using reflection
            var properties = args.GetType().GetProperties();
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(args);
                    Console.WriteLine($"{prop.Name} ({prop.PropertyType.Name}): {value}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{prop.Name} ({prop.PropertyType.Name}): Error - {ex.Message}");
                }
            }
            Console.WriteLine("=================================================");
        };
    }
}
