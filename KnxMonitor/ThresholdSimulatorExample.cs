using KnxModel;
using KnxModel.Factories;
using KnxModel.Models;
using Microsoft.Extensions.Logging;

namespace KnxMonitor
{
    /// <summary>
    /// Example demonstrating ThresholdSimulatorDevice usage
    /// Shows how to simulate different sun protection scenarios for testing
    /// </summary>
    public class ThresholdSimulatorExample
    {
        public static async Task RunExampleAsync(IKnxService knxService, ILogger<ThresholdSimulatorDevice> logger)
        {
            Console.WriteLine("=== ThresholdSimulatorDevice Example ===\n");

            // Create threshold simulator
            var thresholdSimulator = ThresholdSimulatorFactory.CreateSunProtectionSimulator(knxService, logger);

            try
            {
                Console.WriteLine("1. Initializing ThresholdSimulator...");
                await thresholdSimulator.InitializeAsync();

                Console.WriteLine($"   Simulator: {thresholdSimulator.Name} (ID: {thresholdSimulator.Id})");
                Console.WriteLine($"   Initial state: B1={thresholdSimulator.BrightnessThreshold1Active}, B2={thresholdSimulator.BrightnessThreshold2Active}, Temp={thresholdSimulator.OutdoorTemperatureThresholdActive}");

                Console.WriteLine("\n2. Saving initial state...");
                thresholdSimulator.SaveCurrentState();

                Console.WriteLine("\n3. Simulating different threshold scenarios...");

                // Scenario 1: Normal conditions (sunrise)
                Console.WriteLine("\n   🌅 Scenario 1: Sunrise - Normal conditions");
                await thresholdSimulator.SimulateNormalConditionsAsync();
                await Task.Delay(1000);
                Console.WriteLine($"      Result: B1={thresholdSimulator.BrightnessThreshold1Active}, B2={thresholdSimulator.BrightnessThreshold2Active}, Temp={thresholdSimulator.OutdoorTemperatureThresholdActive}");

                // Scenario 2: Moderate brightness (mid-morning)
                Console.WriteLine("\n   🌞 Scenario 2: Mid-morning - Moderate brightness");
                await thresholdSimulator.SimulateModerateBrightnessAsync();
                await Task.Delay(1000);
                Console.WriteLine($"      Result: B1={thresholdSimulator.BrightnessThreshold1Active}, B2={thresholdSimulator.BrightnessThreshold2Active}, Temp={thresholdSimulator.OutdoorTemperatureThresholdActive}");

                // Scenario 3: High brightness (midday)
                Console.WriteLine("\n   ☀️ Scenario 3: Midday - High brightness");
                await thresholdSimulator.SimulateHighBrightnessAsync();
                await Task.Delay(1000);
                Console.WriteLine($"      Result: B1={thresholdSimulator.BrightnessThreshold1Active}, B2={thresholdSimulator.BrightnessThreshold2Active}, Temp={thresholdSimulator.OutdoorTemperatureThresholdActive}");

                // Scenario 4: Maximum protection (hot summer day)
                Console.WriteLine("\n   🔥 Scenario 4: Hot summer day - Maximum protection");
                await thresholdSimulator.SimulateMaximumSunProtectionAsync();
                await Task.Delay(1000);
                Console.WriteLine($"      Result: B1={thresholdSimulator.BrightnessThreshold1Active}, B2={thresholdSimulator.BrightnessThreshold2Active}, Temp={thresholdSimulator.OutdoorTemperatureThresholdActive}");

                Console.WriteLine("\n4. Testing individual threshold controls...");

                // Individual threshold testing
                Console.WriteLine("\n   🎛️ Individual threshold control demonstration:");
                
                await thresholdSimulator.SetBrightnessThreshold1StateAsync(true);
                Console.WriteLine($"      After setting B1=true: B1={thresholdSimulator.BrightnessThreshold1Active}");
                
                await thresholdSimulator.SetBrightnessThreshold2StateAsync(true);
                Console.WriteLine($"      After setting B2=true: B2={thresholdSimulator.BrightnessThreshold2Active}");
                
                await thresholdSimulator.SetOutdoorTemperatureThresholdStateAsync(false);
                Console.WriteLine($"      After setting Temp=false: Temp={thresholdSimulator.OutdoorTemperatureThresholdActive}");

                Console.WriteLine("\n5. Testing custom scenario...");
                await thresholdSimulator.SimulateCustomThresholdStatesAsync(false, true, true);
                Console.WriteLine($"      Custom scenario result: B1={thresholdSimulator.BrightnessThreshold1Active}, B2={thresholdSimulator.BrightnessThreshold2Active}, Temp={thresholdSimulator.OutdoorTemperatureThresholdActive}");

                Console.WriteLine("\n6. Restoring saved state...");
                await thresholdSimulator.RestoreSavedStateAsync();
                Console.WriteLine($"      Restored state: B1={thresholdSimulator.BrightnessThreshold1Active}, B2={thresholdSimulator.BrightnessThreshold2Active}, Temp={thresholdSimulator.OutdoorTemperatureThresholdActive}");

                Console.WriteLine("\n7. Simulating day progression...");
                await SimulateDayProgressionAsync(thresholdSimulator);

                Console.WriteLine("\nThresholdSimulatorDevice example completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during ThresholdSimulator example: {ex.Message}");
                throw;
            }
            finally
            {
                thresholdSimulator.Dispose();
            }
        }

        /// <summary>
        /// Simulates a complete day progression from sunrise to sunset
        /// </summary>
        private static async Task SimulateDayProgressionAsync(ThresholdSimulatorDevice simulator)
        {
            Console.WriteLine("\n   🌅 Day progression simulation:");

            var scenarios = new (string Description, Func<Task> Action)[]
            {
                ("06:00 - Sunrise", () => simulator.SimulateNormalConditionsAsync()),
                ("09:00 - Morning", () => simulator.SimulateModerateBrightnessAsync()),
                ("12:00 - Midday", () => simulator.SimulateHighBrightnessAsync()),
                ("14:00 - Afternoon", () => simulator.SimulateMaximumSunProtectionAsync()),
                ("16:00 - Late afternoon", () => simulator.SimulateHighBrightnessAsync()),
                ("18:00 - Evening", () => simulator.SimulateModerateBrightnessAsync()),
                ("20:00 - Sunset", () => simulator.SimulateNormalConditionsAsync())
            };

            foreach (var (timeDescription, scenarioAction) in scenarios)
            {
                Console.WriteLine($"\n      {timeDescription}");
                await scenarioAction();
                await Task.Delay(500); // Small delay between scenarios
                Console.WriteLine($"         Thresholds: B1={simulator.BrightnessThreshold1Active}, B2={simulator.BrightnessThreshold2Active}, Temp={simulator.OutdoorTemperatureThresholdActive}");
            }

            Console.WriteLine("\n   🌙 Day simulation completed - returned to normal evening conditions");
        }

        /// <summary>
        /// Demonstrates integration with actual shutter devices (if available)
        /// </summary>
        public static async Task RunIntegratedExampleAsync(
            IKnxService knxService, 
            ILogger<ThresholdSimulatorDevice> simulatorLogger,
            ILogger<ShutterDevice> shutterLogger)
        {
            Console.WriteLine("=== Integrated ThresholdSimulator + ShutterDevice Example ===\n");

            var simulator = ThresholdSimulatorFactory.CreateSunProtectionSimulator(knxService, simulatorLogger);
            
            try
            {
                await simulator.InitializeAsync();
                
                // Try to create a test shutter device (you would use a real device ID here)
                Console.WriteLine("Creating test shutter device for integration...");
                // var shutter = ShutterFactory.CreateShutter("test-shutter", knxService, shutterLogger);
                // await shutter.InitializeAsync();
                
                Console.WriteLine("🎭 Running integrated threshold simulation...");
                
                // Simulate normal conditions and show how shutter would respond
                await simulator.SimulateNormalConditionsAsync();
                Console.WriteLine("   Normal conditions simulated - shutters should remain in current position");
                
                await Task.Delay(2000);
                
                // Simulate high brightness
                await simulator.SimulateHighBrightnessAsync();
                Console.WriteLine("   High brightness simulated - shutters should respond with sun protection");
                
                await Task.Delay(2000);
                
                // Return to normal
                await simulator.SimulateNormalConditionsAsync();
                Console.WriteLine("   Returned to normal conditions - shutters should adjust accordingly");
                
                Console.WriteLine("\nIntegrated example completed!");
            }
            finally
            {
                simulator.Dispose();
            }
        }

        /// <summary>
        /// Advanced example demonstrating brightness threshold monitoring device blocking
        /// Shows how to achieve complete testing isolation
        /// </summary>
        public static async Task RunBlockingExampleAsync(IKnxService knxService, ILogger<ThresholdSimulatorDevice> logger)
        {
            Console.WriteLine("=== Brightness Threshold Monitoring Block Example ===\n");

            var thresholdSimulator = ThresholdSimulatorFactory.CreateThresholdSimulator(
                "blocking-test-simulator", 
                "Blocking Test Simulator", 
                knxService, 
                logger, 
                TimeSpan.FromSeconds(10)
            );

            try
            {
                Console.WriteLine("1. Initializing ThresholdSimulator for blocking test...");
                await thresholdSimulator.InitializeAsync();

                Console.WriteLine("\n2. Demonstrating brightness threshold monitoring block control...");

                // Show initial state
                Console.WriteLine($"   Initial block state: {thresholdSimulator.BrightnessThresholdMonitoringBlocked}");

                // Block monitoring device
                Console.WriteLine("\n   🚫 Blocking brightness threshold monitoring device...");
                await thresholdSimulator.BlockBrightnessThresholdMonitoringAsync();
                Console.WriteLine($"      Block state: {thresholdSimulator.BrightnessThresholdMonitoringBlocked}");

                // Read state from KNX bus to verify
                Console.WriteLine("\n   📡 Reading block state from KNX bus...");
                var readState = await thresholdSimulator.ReadBrightnessThresholdMonitoringBlockStateAsync();
                Console.WriteLine($"      KNX bus block state: {readState}");

                // Unblock monitoring device
                Console.WriteLine("\n   ✅ Unblocking brightness threshold monitoring device...");
                await thresholdSimulator.UnblockBrightnessThresholdMonitoringAsync();
                Console.WriteLine($"      Block state: {thresholdSimulator.BrightnessThresholdMonitoringBlocked}");

                Console.WriteLine("\n3. Demonstrating testing isolation mode...");

                // Enter testing isolation with specific conditions
                Console.WriteLine("\n   🔒 Entering testing isolation with bright conditions...");
                await thresholdSimulator.SimulateTestingIsolationAsync(
                    brightness1: true,
                    brightness2: true,
                    temperature: false
                );

                Console.WriteLine($"      Monitoring blocked: {thresholdSimulator.BrightnessThresholdMonitoringBlocked}");
                Console.WriteLine($"      Brightness1 threshold: {thresholdSimulator.BrightnessThreshold1Active}");
                Console.WriteLine($"      Brightness2 threshold: {thresholdSimulator.BrightnessThreshold2Active}");
                Console.WriteLine($"      Temperature threshold: {thresholdSimulator.OutdoorTemperatureThresholdActive}");

                Console.WriteLine("\n   ⏱️ Simulating test execution time...");
                await Task.Delay(3000);

                // Exit testing isolation
                Console.WriteLine("\n   🔓 Exiting testing isolation...");
                await thresholdSimulator.ExitTestingIsolationAsync();

                Console.WriteLine($"      Monitoring blocked: {thresholdSimulator.BrightnessThresholdMonitoringBlocked}");
                Console.WriteLine($"      All thresholds cleared: B1={thresholdSimulator.BrightnessThreshold1Active}, B2={thresholdSimulator.BrightnessThreshold2Active}, Temp={thresholdSimulator.OutdoorTemperatureThresholdActive}");

                Console.WriteLine("\n4. Demonstrating state save/restore with block state...");

                // Set complex state and save
                await thresholdSimulator.BlockBrightnessThresholdMonitoringAsync();
                await thresholdSimulator.SetBrightnessThreshold1StateAsync(true);
                await thresholdSimulator.SetBrightnessThreshold2StateAsync(false);
                await thresholdSimulator.SetOutdoorTemperatureThresholdStateAsync(true);

                Console.WriteLine($"   Complex state set: Block={thresholdSimulator.BrightnessThresholdMonitoringBlocked}, B1={thresholdSimulator.BrightnessThreshold1Active}, B2={thresholdSimulator.BrightnessThreshold2Active}, Temp={thresholdSimulator.OutdoorTemperatureThresholdActive}");

                thresholdSimulator.SaveCurrentState();
                Console.WriteLine("   State saved");

                // Change state
                await thresholdSimulator.ExitTestingIsolationAsync();
                Console.WriteLine($"   State changed: Block={thresholdSimulator.BrightnessThresholdMonitoringBlocked}, B1={thresholdSimulator.BrightnessThreshold1Active}, B2={thresholdSimulator.BrightnessThreshold2Active}, Temp={thresholdSimulator.OutdoorTemperatureThresholdActive}");

                // Restore saved state
                await thresholdSimulator.RestoreSavedStateAsync();
                Console.WriteLine($"   State restored: Block={thresholdSimulator.BrightnessThresholdMonitoringBlocked}, B1={thresholdSimulator.BrightnessThreshold1Active}, B2={thresholdSimulator.BrightnessThreshold2Active}, Temp={thresholdSimulator.OutdoorTemperatureThresholdActive}");

                // Final cleanup
                await thresholdSimulator.UnblockBrightnessThresholdMonitoringAsync();
                Console.WriteLine("\n   Final cleanup completed - monitoring unblocked");

                Console.WriteLine("\nBlocking example completed!");
            }
            finally
            {
                thresholdSimulator.Dispose();
            }
        }
    }
}
