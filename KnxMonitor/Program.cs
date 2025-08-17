using KnxService;
using KnxModel;
using Microsoft.Extensions.Logging;

Console.WriteLine("KNX Bus Monitor Start...");

// Create logger factory and loggers
using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Information));

var lightLogger = loggerFactory.CreateLogger<LightDevice>();
var dimmerLogger = loggerFactory.CreateLogger<DimmerDevice>();
var clockLogger = loggerFactory.CreateLogger<ClockDevice>();

var service = new KnxService.KnxService();

service.GroupMessageReceived += (sender, args) =>
{
    Console.WriteLine($"Received Group Address: {args.Destination}, Value: {args.Value}");
};

Console.WriteLine("Creating ClockDevice and sending future time...");

// Create ClockDevice with real KNX service
var clockConfig = new ClockConfiguration(
    InitialMode: ClockMode.Master,
    TimeStamp: TimeSpan.FromSeconds(30)
);

var clockDevice = new ClockDevice(
    id: "REAL_CLOCK_001",
    name: "Real Clock Device",
    configuration: clockConfig,
    knxService: service,
    logger: clockLogger,
    defaultTimeout: TimeSpan.FromSeconds(5)
);

// Send future time to real KNX bus
var futureTime = DateTime.Now.AddDays(1);
Console.WriteLine($"Sending future time to KNX bus: {futureTime:yyyy-MM-dd HH:mm:ss}");

await clockDevice.SendTimeAsync(futureTime);

Console.WriteLine("Time sent to KNX bus! Check your bus monitor.");

// Also send current time
Console.WriteLine("Now sending current time...");
await clockDevice.SendTimeAsync();

Console.WriteLine("Current time also sent! Check your bus monitor.");

Console.ReadKey();

// Cleanup
clockDevice.Dispose();
service.Dispose();

