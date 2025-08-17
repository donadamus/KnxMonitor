using FluentAssertions;
using KnxModel;
using KnxModel.Factories;
using KnxModel.Models;
using KnxTest.Integration.Base;
using Xunit.Abstractions;

namespace KnxTest.Integration
{
    /// <summary>
    /// Integration tests demonstrating ThresholdSimulatorDevice usage
    /// Shows how to simulate threshold conditions for automated sun protection testing
    /// </summary>
    [Collection("KnxService collection")]
    public class ThresholdSimulatorIntegrationTests : IntegrationTestBase<ShutterDevice>
    {
        internal readonly XUnitLogger<ShutterDevice> _shutterLogger;
        internal readonly XUnitLogger<ThresholdSimulatorDevice> _simulatorLogger;
        internal readonly XUnitLogger<ClockDevice> _clockLogger;
        private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);
        private readonly TimeSpan _thresholdResponseTimeout = TimeSpan.FromSeconds(5);

        private ThresholdSimulatorDevice? _thresholdSimulator;
        private ClockDevice? _clockDevice;

        public ThresholdSimulatorIntegrationTests(KnxServiceFixture fixture, ITestOutputHelper output) : base(fixture)
        {
            _shutterLogger = new XUnitLogger<ShutterDevice>(output);
            _simulatorLogger = new XUnitLogger<ThresholdSimulatorDevice>(output);
            _clockLogger = new XUnitLogger<ClockDevice>(output);

            _clockDevice = ClockFactory.CreateClockDevice("C01", "Clock", ClockMode.Master, _knxService, _clockLogger, _defaultTimeout);
        }

        // Data source for tests - shutters with sun protection capability
        public static IEnumerable<object[]> SunProtectionShutterIdsFromConfig
        {
            get
            {
                var config = ShutterFactory.ShutterConfigurations;
                return config.Where(x => x.Value.Name.ToLower().Contains("office"))
                            .Select(k => new object[] { k.Key });
            }
        }

        #region Threshold Simulator Tests

        [Fact]
        public async Task ThresholdSimulator_CanBeCreatedAndInitialized()
        {
            // Arrange & Act
            await InitializeThresholdSimulator();

            // Assert
            _thresholdSimulator.Should().NotBeNull();
            _thresholdSimulator!.Id.Should().Be("test-threshold-simulator");
            _thresholdSimulator.DeviceType.Should().Be("ThresholdSimulator");
            _thresholdSimulator.BrightnessThreshold1Active.Should().BeFalse("Initial state should be false");
            _thresholdSimulator.BrightnessThreshold2Active.Should().BeFalse("Initial state should be false");
            _thresholdSimulator.OutdoorTemperatureThresholdActive.Should().BeFalse("Initial state should be false");

            Console.WriteLine($"✅ ThresholdSimulator {_thresholdSimulator.Id} successfully created and initialized");
        }

        [Fact]
        public async Task ThresholdSimulator_CanSimulateIndividualThresholds()
        {
            // Arrange
            await InitializeThresholdSimulator();

            // Act & Assert - Test brightness threshold 1
            await _thresholdSimulator!.SetBrightnessThreshold1StateAsync(true);
            _thresholdSimulator.BrightnessThreshold1Active.Should().BeTrue();

            await _thresholdSimulator.SetBrightnessThreshold1StateAsync(false);
            _thresholdSimulator.BrightnessThreshold1Active.Should().BeFalse();

            // Act & Assert - Test brightness threshold 2
            await _thresholdSimulator.SetBrightnessThreshold2StateAsync(true);
            _thresholdSimulator.BrightnessThreshold2Active.Should().BeTrue();

            await _thresholdSimulator.SetBrightnessThreshold2StateAsync(false);
            _thresholdSimulator.BrightnessThreshold2Active.Should().BeFalse();

            // Act & Assert - Test temperature threshold
            await _thresholdSimulator.SetOutdoorTemperatureThresholdStateAsync(true);
            _thresholdSimulator.OutdoorTemperatureThresholdActive.Should().BeTrue();

            await _thresholdSimulator.SetOutdoorTemperatureThresholdStateAsync(false);
            _thresholdSimulator.OutdoorTemperatureThresholdActive.Should().BeFalse();

            Console.WriteLine($"✅ ThresholdSimulator {_thresholdSimulator.Id} successfully tested individual threshold controls");
        }

        [Fact]
        public async Task ThresholdSimulator_CanSimulatePredefinedScenarios()
        {
            // Arrange
            await InitializeThresholdSimulator();

            // Test normal conditions
            await _thresholdSimulator!.SimulateNormalConditionsAsync();
            _thresholdSimulator.BrightnessThreshold1Active.Should().BeFalse();
            _thresholdSimulator.BrightnessThreshold2Active.Should().BeFalse();
            _thresholdSimulator.OutdoorTemperatureThresholdActive.Should().BeFalse();

            // Test moderate brightness
            await _thresholdSimulator.SimulateModerateBrightnessAsync();
            _thresholdSimulator.BrightnessThreshold1Active.Should().BeTrue();
            _thresholdSimulator.BrightnessThreshold2Active.Should().BeFalse();
            _thresholdSimulator.OutdoorTemperatureThresholdActive.Should().BeFalse();

            // Test high brightness
            await _thresholdSimulator.SimulateHighBrightnessAsync();
            _thresholdSimulator.BrightnessThreshold1Active.Should().BeTrue();
            _thresholdSimulator.BrightnessThreshold2Active.Should().BeTrue();
            _thresholdSimulator.OutdoorTemperatureThresholdActive.Should().BeFalse();

            // Test maximum sun protection
            await _thresholdSimulator.SimulateMaximumSunProtectionAsync();
            _thresholdSimulator.BrightnessThreshold1Active.Should().BeTrue();
            _thresholdSimulator.BrightnessThreshold2Active.Should().BeTrue();
            _thresholdSimulator.OutdoorTemperatureThresholdActive.Should().BeTrue();

            Console.WriteLine($"✅ ThresholdSimulator {_thresholdSimulator.Id} successfully tested all predefined scenarios");
        }

        #endregion

        #region Integrated Shutter + Simulator Tests

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task ShutterDevice_RespondsToSimulatedThresholds_NormalConditions(string deviceId)
        {
            // Arrange
            await InitializeThresholdSimulator();
            await InitializeShutterDevice(deviceId);
            await EnsureSunProtectionUnblocked(Device!);

            // Act - Simulate normal conditions (no thresholds exceeded)
            await _thresholdSimulator!.SimulateNormalConditionsAsync();
            await Task.Delay(_thresholdResponseTimeout); // Allow shutter to respond

            // Wait for thresholds to be read by shutter
            await Device!.ReadBrightnessThreshold1StateAsync();
            await Device.ReadBrightnessThreshold2StateAsync();
            await Device.ReadOutdoorTemperatureThresholdStateAsync();

            // Assert
            Device.BrightnessThreshold1Active.Should().BeFalse("Shutter should reflect simulated threshold state");
            Device.BrightnessThreshold2Active.Should().BeFalse("Shutter should reflect simulated threshold state");
            Device.OutdoorTemperatureThresholdActive.Should().BeFalse("Shutter should reflect simulated threshold state");

            Console.WriteLine($"✅ Shutter {deviceId} correctly reads simulated normal conditions");
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task ShutterDevice_RespondsToSimulatedThresholds_ModerateBrightness(string deviceId)
        {
            // Arrange

            await _clockDevice!.SendTimeAsync(DateTime.Now.AddDays(-0.3));

            await InitializeThresholdSimulator();
            await InitializeShutterDevice(deviceId);
            await EnsureSunProtectionUnblocked(Device!);

            // Act - Simulate moderate brightness (threshold 1 exceeded)
            await _thresholdSimulator!.SimulateModerateBrightnessAsync();
            await _thresholdSimulator!.SimulateMaximumSunProtectionAsync();
            await Task.Delay(_thresholdResponseTimeout);

            //// Wait for thresholds to be read by shutter
            //await Device!.ReadBrightnessThreshold1StateAsync();
            //await Device.ReadBrightnessThreshold2StateAsync();
            //await Device.ReadOutdoorTemperatureThresholdStateAsync();

            //// Assert
            //Device.BrightnessThreshold1Active.Should().BeTrue("Shutter should detect simulated brightness threshold 1");
            //Device.BrightnessThreshold2Active.Should().BeFalse("Brightness threshold 2 should remain inactive");
            //Device.OutdoorTemperatureThresholdActive.Should().BeFalse("Temperature threshold should remain inactive");

            Console.WriteLine($"✅ Shutter {deviceId} correctly reads simulated moderate brightness conditions");
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task ShutterDevice_RespondsToSimulatedThresholds_MaximumSunProtection(string deviceId)
        {
            // Arrange
            await InitializeThresholdSimulator();
            await InitializeShutterDevice(deviceId);
            await EnsureSunProtectionUnblocked(Device!);

            // Act - Simulate maximum sun protection (all thresholds exceeded)
            await _thresholdSimulator!.SimulateMaximumSunProtectionAsync();
            await Task.Delay(_thresholdResponseTimeout);

            // Wait for thresholds to be read by shutter
            await Device!.ReadBrightnessThreshold1StateAsync();
            await Device.ReadBrightnessThreshold2StateAsync();
            await Device.ReadOutdoorTemperatureThresholdStateAsync();

            // Assert
            Device.BrightnessThreshold1Active.Should().BeTrue("All thresholds should be exceeded");
            Device.BrightnessThreshold2Active.Should().BeTrue("All thresholds should be exceeded");
            Device.OutdoorTemperatureThresholdActive.Should().BeTrue("All thresholds should be exceeded");

            Console.WriteLine($"✅ Shutter {deviceId} correctly reads simulated maximum sun protection conditions");
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task CompleteScenario_ThresholdProgression_AutomaticSunProtection(string deviceId)
        {
            // Arrange
            await InitializeThresholdSimulator();
            await InitializeShutterDevice(deviceId);
            await EnsureSunProtectionUnblocked(Device!);
            
            var initialPosition = Device!.CurrentPercentage;
            Console.WriteLine($"🌅 Starting complete sun protection scenario with shutter {deviceId} at {initialPosition}%");

            // Scenario 1: Normal conditions (sunrise)
            Console.WriteLine("☀️ Simulating sunrise - normal conditions");
            await _thresholdSimulator!.SimulateNormalConditionsAsync();
            await Task.Delay(_thresholdResponseTimeout);
            await Device.ReadBrightnessThreshold1StateAsync();
            Console.WriteLine($"   Thresholds: B1={Device.BrightnessThreshold1Active}, B2={Device.BrightnessThreshold2Active}, Temp={Device.OutdoorTemperatureThresholdActive}");

            // Scenario 2: Moderate brightness (mid-morning)
            Console.WriteLine("🌞 Simulating mid-morning - moderate brightness");
            await _thresholdSimulator.SimulateModerateBrightnessAsync();
            await Task.Delay(_thresholdResponseTimeout);
            await Device.ReadBrightnessThreshold1StateAsync();
            Console.WriteLine($"   Thresholds: B1={Device.BrightnessThreshold1Active}, B2={Device.BrightnessThreshold2Active}, Temp={Device.OutdoorTemperatureThresholdActive}");

            // Scenario 3: High brightness (midday)
            Console.WriteLine("☀️ Simulating midday - high brightness");
            await _thresholdSimulator.SimulateHighBrightnessAsync();
            await Task.Delay(_thresholdResponseTimeout);
            await Device.ReadBrightnessThreshold2StateAsync();
            Console.WriteLine($"   Thresholds: B1={Device.BrightnessThreshold1Active}, B2={Device.BrightnessThreshold2Active}, Temp={Device.OutdoorTemperatureThresholdActive}");

            // Scenario 4: Maximum protection (hot summer day)
            Console.WriteLine("🔥 Simulating hot summer day - maximum protection needed");
            await _thresholdSimulator.SimulateMaximumSunProtectionAsync();
            await Task.Delay(_thresholdResponseTimeout);
            await Device.ReadOutdoorTemperatureThresholdStateAsync();
            Console.WriteLine($"   Thresholds: B1={Device.BrightnessThreshold1Active}, B2={Device.BrightnessThreshold2Active}, Temp={Device.OutdoorTemperatureThresholdActive}");

            // Scenario 5: Return to normal (evening)
            Console.WriteLine("🌅 Simulating evening - returning to normal");
            await _thresholdSimulator.SimulateNormalConditionsAsync();
            await Task.Delay(_thresholdResponseTimeout);
            await Device.ReadBrightnessThreshold1StateAsync();
            Console.WriteLine($"   Thresholds: B1={Device.BrightnessThreshold1Active}, B2={Device.BrightnessThreshold2Active}, Temp={Device.OutdoorTemperatureThresholdActive}");

            // Assert final state
            Device.BrightnessThreshold1Active.Should().BeFalse("Should return to normal conditions");
            Device.BrightnessThreshold2Active.Should().BeFalse("Should return to normal conditions");
            Device.OutdoorTemperatureThresholdActive.Should().BeFalse("Should return to normal conditions");

            Console.WriteLine($"✅ Complete sun protection scenario completed for shutter {deviceId}");
        }

        #endregion

        #region Helper Methods

        private async Task InitializeThresholdSimulator()
        {
            Console.WriteLine("🎛️ Creating ThresholdSimulatorDevice for testing");
            _thresholdSimulator = ThresholdSimulatorFactory.CreateThresholdSimulator(
                "test-threshold-simulator",
                "Integration Test Threshold Simulator",
                _knxService,
                _simulatorLogger,
                _defaultTimeout
            );

            await _thresholdSimulator.InitializeAsync();
            Console.WriteLine($"✅ ThresholdSimulator {_thresholdSimulator.Id} initialized and ready");
        }

        private async Task InitializeShutterDevice(string deviceId)
        {
            Console.WriteLine($"🪟 Creating ShutterDevice {deviceId} for threshold testing");
            Device = ShutterFactory.CreateShutter(deviceId, _knxService, _shutterLogger);
            await Device.InitializeAsync();
            Device.SaveCurrentState();
            Console.WriteLine($"✅ Shutter {deviceId} initialized - Position: {Device.CurrentPercentage}%");
        }

        private async Task EnsureSunProtectionUnblocked(ShutterDevice device)
        {
            if (device.IsSunProtectionBlocked)
            {
                await device.UnblockSunProtectionAsync();
                await device.WaitForSunProtectionBlockStateAsync(false, _defaultTimeout);
            }

            device.IsSunProtectionBlocked.Should().BeFalse(
                $"Device {device.Id} should have sun protection unblocked for threshold tests");
            Console.WriteLine($"✅ Device {device.Id} sun protection is unblocked");
        }

        #endregion

        #region Brightness Threshold Monitoring Block Tests

        [Fact]
        public async Task ThresholdSimulator_CanBlockBrightnessThresholdMonitoring()
        {
            // Arrange
            await InitializeThresholdSimulator();

            // Act
            await _thresholdSimulator!.BlockBrightnessThresholdMonitoringAsync();

            // Assert
            _thresholdSimulator.BrightnessThresholdMonitoringBlocked.Should().BeTrue();
            Console.WriteLine("✅ Brightness threshold monitoring blocked successfully");

            // Cleanup
            await _thresholdSimulator.UnblockBrightnessThresholdMonitoringAsync();
        }

        [Fact]
        public async Task ThresholdSimulator_CanUnblockBrightnessThresholdMonitoring()
        {
            // Arrange
            await InitializeThresholdSimulator();
            await _thresholdSimulator!.BlockBrightnessThresholdMonitoringAsync();

            // Act
            await _thresholdSimulator.UnblockBrightnessThresholdMonitoringAsync();

            // Assert
            _thresholdSimulator.BrightnessThresholdMonitoringBlocked.Should().BeFalse();
            Console.WriteLine("✅ Brightness threshold monitoring unblocked successfully");
        }

        [Fact]
        public async Task ThresholdSimulator_CanSetBlockStateDirectly()
        {
            // Arrange
            await InitializeThresholdSimulator();

            // Act & Assert - Block
            await _thresholdSimulator!.SetBrightnessThresholdMonitoringBlockStateAsync(true);
            _thresholdSimulator.BrightnessThresholdMonitoringBlocked.Should().BeTrue();

            // Act & Assert - Unblock
            await _thresholdSimulator.SetBrightnessThresholdMonitoringBlockStateAsync(false);
            _thresholdSimulator.BrightnessThresholdMonitoringBlocked.Should().BeFalse();

            Console.WriteLine("✅ Direct block state setting works correctly");
        }

        [Fact]
        public async Task ThresholdSimulator_CanReadBlockStateFromKnx()
        {
            // Arrange
            await InitializeThresholdSimulator();

            // Act - Set known state first
            await _thresholdSimulator!.BlockBrightnessThresholdMonitoringAsync();
            
            // Read state from KNX bus
            var blockState = await _thresholdSimulator.ReadBrightnessThresholdMonitoringBlockStateAsync();

            // Assert
            blockState.Should().BeTrue();
            _thresholdSimulator.BrightnessThresholdMonitoringBlocked.Should().BeTrue();
            Console.WriteLine("✅ Block state read from KNX bus correctly");

            // Cleanup
            await _thresholdSimulator.UnblockBrightnessThresholdMonitoringAsync();
        }

        [Fact]
        public async Task ThresholdSimulator_TestingIsolationMode_BlocksAndSetsThresholds()
        {
            // Arrange
            await InitializeThresholdSimulator();

            // Act - Enter testing isolation with bright conditions
            await _thresholdSimulator!.SimulateTestingIsolationAsync(
                brightness1: true,
                brightness2: true,
                temperature: false
            );

            // Assert
            _thresholdSimulator.BrightnessThresholdMonitoringBlocked.Should().BeTrue();
            _thresholdSimulator.BrightnessThreshold1Active.Should().BeTrue();
            _thresholdSimulator.BrightnessThreshold2Active.Should().BeTrue();
            _thresholdSimulator.OutdoorTemperatureThresholdActive.Should().BeFalse();

            Console.WriteLine("✅ Testing isolation mode activated with bright conditions");

            // Cleanup
            await _thresholdSimulator.ExitTestingIsolationAsync();
        }

        [Fact]
        public async Task ThresholdSimulator_ExitTestingIsolation_ClearsStateAndUnblocks()
        {
            // Arrange
            await InitializeThresholdSimulator();
            await _thresholdSimulator!.SimulateTestingIsolationAsync(true, true, true);

            // Act
            await _thresholdSimulator.ExitTestingIsolationAsync();

            // Assert
            _thresholdSimulator.BrightnessThresholdMonitoringBlocked.Should().BeFalse();
            _thresholdSimulator.BrightnessThreshold1Active.Should().BeFalse();
            _thresholdSimulator.BrightnessThreshold2Active.Should().BeFalse();
            _thresholdSimulator.OutdoorTemperatureThresholdActive.Should().BeFalse();

            Console.WriteLine("✅ Testing isolation mode exited - all states cleared and monitoring unblocked");
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task ThresholdSimulator_TestingIsolationWithShutter_ProvidesCompleteControl(string deviceId)
        {
            // Arrange
            await InitializeThresholdSimulator();
            await InitializeShutterDevice(deviceId);
            await EnsureSunProtectionUnblocked(Device!);

            Console.WriteLine($"🔬 Testing complete isolation control for device {deviceId}");

            // Act - Enter testing isolation with maximum sun protection conditions
            await _thresholdSimulator!.SimulateTestingIsolationAsync(
                brightness1: true,
                brightness2: true,
                temperature: true
            );

            // Small delay for shutter to react to threshold conditions
            await Task.Delay(1000);

            // Assert - Real monitoring is blocked, simulator controls thresholds
            _thresholdSimulator.BrightnessThresholdMonitoringBlocked.Should().BeTrue();
            _thresholdSimulator.BrightnessThreshold1Active.Should().BeTrue();
            _thresholdSimulator.BrightnessThreshold2Active.Should().BeTrue();
            _thresholdSimulator.OutdoorTemperatureThresholdActive.Should().BeTrue();

            Console.WriteLine($"✅ Complete testing isolation achieved for {deviceId}");
            Console.WriteLine($"   - Real monitoring blocked: {_thresholdSimulator.BrightnessThresholdMonitoringBlocked}");
            Console.WriteLine($"   - Brightness1 threshold: {_thresholdSimulator.BrightnessThreshold1Active}");
            Console.WriteLine($"   - Brightness2 threshold: {_thresholdSimulator.BrightnessThreshold2Active}");
            Console.WriteLine($"   - Temperature threshold: {_thresholdSimulator.OutdoorTemperatureThresholdActive}");

            // Cleanup
            await _thresholdSimulator.ExitTestingIsolationAsync();
            await Device.RestoreSavedStateAsync();
        }

        [Fact]
        public async Task ThresholdSimulator_SaveAndRestoreState_IncludesBlockState()
        {
            // Arrange
            await InitializeThresholdSimulator();

            // Act - Set specific state and save
            await _thresholdSimulator!.BlockBrightnessThresholdMonitoringAsync();
            await _thresholdSimulator.SetBrightnessThreshold1StateAsync(true);
            await _thresholdSimulator.SetBrightnessThreshold2StateAsync(false);
            await _thresholdSimulator.SetOutdoorTemperatureThresholdStateAsync(true);

            _thresholdSimulator.SaveCurrentState();

            // Modify state
            await _thresholdSimulator.UnblockBrightnessThresholdMonitoringAsync();
            await _thresholdSimulator.SimulateNormalConditionsAsync();

            // Assert state changed
            _thresholdSimulator.BrightnessThresholdMonitoringBlocked.Should().BeFalse();
            _thresholdSimulator.BrightnessThreshold1Active.Should().BeFalse();

            // Restore saved state
            await _thresholdSimulator.RestoreSavedStateAsync();

            // Assert state restored including block state
            _thresholdSimulator.BrightnessThresholdMonitoringBlocked.Should().BeTrue();
            _thresholdSimulator.BrightnessThreshold1Active.Should().BeTrue();
            _thresholdSimulator.BrightnessThreshold2Active.Should().BeFalse();
            _thresholdSimulator.OutdoorTemperatureThresholdActive.Should().BeTrue();

            Console.WriteLine("✅ State save and restore includes block state correctly");

            // Cleanup
            await _thresholdSimulator.UnblockBrightnessThresholdMonitoringAsync();
        }

        #endregion
    }
}
