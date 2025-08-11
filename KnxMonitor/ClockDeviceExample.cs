using KnxModel;
using KnxModel.Factories;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace KnxMonitor
{
    /// <summary>
    /// Example demonstrating ClockDevice usage with different modes
    /// </summary>
    public class ClockDeviceExample
    {
        public static async Task RunExampleAsync(IKnxService knxService, ILogger<ClockDevice> logger)
        {
            Console.WriteLine("=== ClockDevice Example ===\n");

            // Create different clock devices
            var masterClock = ClockFactory.CreateMasterClockDevice(
                "clock-master", 
                "Master Clock", 
                knxService, 
                logger, 
                TimeSpan.FromSeconds(5)
            );

            var slaveClock = ClockFactory.CreateSlaveClockDevice(
                "clock-slave", 
                "Slave Clock", 
                knxService, 
                logger, 
                TimeSpan.FromSeconds(5)
            );

            var adaptiveClock = ClockFactory.CreateSlaveMasterClockDevice(
                "clock-adaptive", 
                "Adaptive Clock", 
                knxService, 
                logger, 
                TimeSpan.FromSeconds(5)
            );

            try
            {
                Console.WriteLine("1. Initializing Master Clock (sends time every 30s)");
                await masterClock.InitializeAsync();
                await Task.Delay(100); // Let it send initial time

                Console.WriteLine("\n2. Initializing Slave Clock (receives time only)");
                await slaveClock.InitializeAsync();

                Console.WriteLine("\n3. Initializing Adaptive Clock (slave first, then master if no time received)");
                await adaptiveClock.InitializeAsync();

                Console.WriteLine("\n4. Demonstrating mode switches...");
                
                // Save states
                Console.WriteLine("   Saving current states...");
                masterClock.SaveCurrentState();
                slaveClock.SaveCurrentState();
                adaptiveClock.SaveCurrentState();

                // Switch adaptive clock to Master mode
                Console.WriteLine("   Switching adaptive clock to Master mode...");
                await adaptiveClock.SwitchToMasterModeAsync();
                await Task.Delay(100);

                // Switch it back to Slave mode
                Console.WriteLine("   Switching adaptive clock back to Slave mode...");
                await adaptiveClock.SwitchToSlaveModeAsync();

                Console.WriteLine("\n5. Clock device information:");
                Console.WriteLine($"   Master Clock: {masterClock.Name} (Mode: {masterClock.Mode}, Valid Time: {masterClock.HasValidTime})");
                Console.WriteLine($"   Slave Clock:  {slaveClock.Name} (Mode: {slaveClock.Mode}, Valid Time: {slaveClock.HasValidTime})");
                Console.WriteLine($"   Adaptive Clock: {adaptiveClock.Name} (Mode: {adaptiveClock.Mode}, Valid Time: {adaptiveClock.HasValidTime})");

                Console.WriteLine("\n6. Time synchronization demo...");
                await masterClock.SynchronizeWithSystemTimeAsync();
                await slaveClock.SynchronizeWithSystemTimeAsync();

                Console.WriteLine("\n7. Manual time sending (Master clock)...");
                await masterClock.SendTimeAsync();

                Console.WriteLine("\n8. Restoring saved states...");
                await masterClock.RestoreSavedStateAsync();
                await slaveClock.RestoreSavedStateAsync();
                await adaptiveClock.RestoreSavedStateAsync();

                Console.WriteLine("\nClockDevice example completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ClockDevice example: {ex.Message}");
            }
            finally
            {
                // Cleanup
                masterClock?.Dispose();
                slaveClock?.Dispose();
                adaptiveClock?.Dispose();
            }
        }
    }
}
