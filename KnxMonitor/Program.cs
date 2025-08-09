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

var light = LightFactory.CreateLight("L05.1", service, lightLogger);
await light.TurnOffAsync();
await light.TurnOnAsync();

var dimmer = DimmerFactory.CreateDimmer("D02.2", service, dimmerLogger);
await dimmer.TurnOnAsync();



Console.ReadKey();

// Cleanup
service.Dispose();

