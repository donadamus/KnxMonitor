using KnxService;
using KnxModel;

Console.WriteLine("KNX Bus Monitor Start...");

var service = new KnxService.KnxService();

service.GroupMessageReceived += (sender, args) =>
{
    Console.WriteLine($"Received Group Address: {args.Destination}, Value: {args.Value}");
};

var light = LightFactory.CreateLight("L05.1", service);
light.TurnOffAsync();
light.TurnOnAsync();

var dimmer = DimmerFactory.CreateDimmer("D02.2", service);
dimmer.TurnOnAsync();



Console.ReadKey();

// Cleanup
service.Dispose();

