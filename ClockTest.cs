using KnxService;
using KnxModel;
using KnxModel.Models;
using Microsoft.Extensions.Logging;

Console.WriteLine("Clock Device Real KNX Test...");

// Create logger
using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Information));

var clockLogger = loggerFactory.CreateLogger<ClockDevice>();

try
{
    // Create real KNX service (connects to 192.168.20.2)
    var knxService = new KnxService.KnxService();
    
    Console.WriteLine("KNX Service connected. Creating ClockDevice...");
    
    // Create ClockDevice with real service
    var clockConfig = new ClockConfiguration(
        InitialMode: ClockMode.Master,
        TimeStamp: TimeSpan.FromSeconds(30)
    );
    
    var clockDevice = new ClockDevice(
        id: "REAL_CLOCK_001",
        name: "Real Clock Device",
        configuration: clockConfig,
        knxService: knxService,
        logger: clockLogger,
        defaultTimeout: TimeSpan.FromSeconds(5)
    );
    
    Console.WriteLine("ClockDevice created. Sending future time to KNX bus...");
    
    // Send future time to real KNX bus
    var futureTime = DateTime.Now.AddDays(1);
    Console.WriteLine($"Sending time: {futureTime:yyyy-MM-dd HH:mm:ss}");
    
    await clockDevice.SendTimeAsync(futureTime);
    
    Console.WriteLine("Time sent to KNX bus! Check your bus monitor.");
    Console.WriteLine("Press any key to exit...");
    
    Console.ReadKey();
    
    // Cleanup
    clockDevice.Dispose();
    knxService.Dispose();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine("Make sure KNX gateway is accessible at 192.168.20.2");
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
