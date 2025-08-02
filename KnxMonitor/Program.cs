using KnxService;
using KnxModel;

Console.WriteLine("KNX Bus Monitor Start...");

var service = new KnxService.KnxService();

service.GroupMessageReceived += (sender, args) =>
{
    Console.WriteLine($"Received Group Address: {args.Destination}, Value: {args.Value}");
};

// Create test dimmers
var dimmer1 = new Dimmer("DIM1", "Test Dimmer 1", "1", service);
var dimmer2 = new Dimmer("DIM2", "Test Dimmer 2", "2", service);

Console.WriteLine("=== KNX Dimmer Test ===");
Console.WriteLine($"Created {dimmer1}");
Console.WriteLine($"Created {dimmer2}");

Console.WriteLine($"\nDimmer addresses:");
Console.WriteLine($"DIM1 - Switch: {dimmer1.Addresses.SwitchControl} -> {dimmer1.Addresses.SwitchFeedback}");
Console.WriteLine($"DIM1 - Brightness: {dimmer1.Addresses.BrightnessControl} -> {dimmer1.Addresses.BrightnessFeedback}");
Console.WriteLine($"DIM1 - Lock: {dimmer1.Addresses.LockControl} -> {dimmer1.Addresses.LockFeedback}");
Console.WriteLine($"DIM2 - Switch: {dimmer2.Addresses.SwitchControl} -> {dimmer2.Addresses.SwitchFeedback}");
Console.WriteLine($"DIM2 - Brightness: {dimmer2.Addresses.BrightnessControl} -> {dimmer2.Addresses.BrightnessFeedback}");
Console.WriteLine($"DIM2 - Lock: {dimmer2.Addresses.LockControl} -> {dimmer2.Addresses.LockFeedback}");

Console.WriteLine("\nDimmers created and ready for testing.");
Console.WriteLine("You can now test the dimmers manually or run integration tests.");
Console.WriteLine("Naciśnij dowolny klawisz, aby zakończyć...");

Console.ReadKey();

// Cleanup
dimmer1.Dispose();
dimmer2.Dispose();
service.Dispose();

