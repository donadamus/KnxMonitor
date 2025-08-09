using KnxService;
using KnxModel;
using Microsoft.Extensions.Logging;

Console.WriteLine("KNX Bus Monitor Start...");

// Create logger factory and loggers
using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Information));

var lightLogger = loggerFactory.CreateLogger<LightDevice>();
var dimmerLogger = loggerFactory.CreateLogger<DimmerDevice>();

var service = new KnxService.KnxService();

service.GroupMessageReceived += (sender, args) =>
{
    Console.WriteLine($"Received Group Address: {args.Destination}, Value: {args.Value}");
};

//var light = LightFactory.CreateLight("L05.1", service, lightLogger);
//await light.TurnOffAsync();
//await light.TurnOnAsync();

//var dimmer = DimmerFactory.CreateDimmer("D02.2", service, dimmerLogger);
//await dimmer.TurnOnAsync();

var shutter = ShutterFactory.CreateShutter("R05.1", service, loggerFactory.CreateLogger<ShutterDevice>());
await shutter.SetPercentageAsync(10, TimeSpan.FromSeconds(20));
await shutter.SetPercentageAsync(100, TimeSpan.FromSeconds(20));
await shutter.SetPercentageAsync(10, TimeSpan.FromSeconds(20));
await shutter.SetPercentageAsync(50, TimeSpan.FromSeconds(20));
await shutter.SetPercentageAsync(100, TimeSpan.FromSeconds(20));
await shutter.SetPercentageAsync(0, TimeSpan.FromSeconds(20));

Console.ReadKey();

// Cleanup
service.Dispose();

