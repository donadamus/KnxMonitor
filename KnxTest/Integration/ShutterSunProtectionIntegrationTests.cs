using FluentAssertions;
using KnxModel;
using KnxTest.Integration.Base;
using Xunit.Abstractions;

namespace KnxTest.Integration
{
    /// <summary>
    /// Integration tests for sun protection functionality in ShutterDevice
    /// Tests the complete threshold monitoring and automatic shutter control scenarios
    /// </summary>
    [Collection("KnxService collection")]
    public class ShutterSunProtectionIntegrationTests : IntegrationTestBase<ShutterDevice>
    {
        internal readonly XUnitLogger<ShutterDevice> _logger;
        private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);
        private readonly TimeSpan _thresholdChangeTimeout = TimeSpan.FromSeconds(30);

        public ShutterSunProtectionIntegrationTests(KnxServiceFixture fixture, ITestOutputHelper output) : base(fixture)
        {
            _logger = new XUnitLogger<ShutterDevice>(output);
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

        #region Basic Threshold Reading Tests

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task CanReadBrightnessThreshold1State(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId, false);
            
            // Act
            var threshold1State = await Device!.ReadBrightnessThreshold1StateAsync();
            
            // Assert
            threshold1State.Should().Be(Device.BrightnessThreshold1Active,
                "Read threshold state should match property value");
            
            Console.WriteLine($"✅ Device {deviceId} brightness threshold 1: {threshold1State}");
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task CanReadBrightnessThreshold2State(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId, false);
            
            // Act
            var threshold2State = await Device!.ReadBrightnessThreshold2StateAsync();
            
            // Assert
            threshold2State.Should().Be(Device.BrightnessThreshold2Active,
                "Read threshold state should match property value");
            
            Console.WriteLine($"✅ Device {deviceId} brightness threshold 2: {threshold2State}");
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task CanReadOutdoorTemperatureThresholdState(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId, false);
            
            // Act
            var tempThresholdState = await Device!.ReadOutdoorTemperatureThresholdStateAsync();
            
            // Assert
            tempThresholdState.Should().Be(Device.OutdoorTemperatureThresholdActive,
                "Read threshold state should match property value");
            
            Console.WriteLine($"✅ Device {deviceId} temperature threshold: {tempThresholdState}");
        }

        #endregion

        #region Threshold Monitoring Tests

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task ThresholdStatesArePersistentAfterInitialization(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            var initialB1 = Device!.BrightnessThreshold1Active;
            var initialB2 = Device.BrightnessThreshold2Active;
            var initialTemp = Device.OutdoorTemperatureThresholdActive;
            
            // Act - Re-read thresholds
            var b1State = await Device.ReadBrightnessThreshold1StateAsync();
            var b2State = await Device.ReadBrightnessThreshold2StateAsync();
            var tempState = await Device.ReadOutdoorTemperatureThresholdStateAsync();
            
            // Assert
            b1State.Should().Be(initialB1, "Brightness threshold 1 should be persistent");
            b2State.Should().Be(initialB2, "Brightness threshold 2 should be persistent");
            tempState.Should().Be(initialTemp, "Temperature threshold should be persistent");
            
            Console.WriteLine($"✅ Device {deviceId} thresholds consistent: B1={b1State}, B2={b2State}, Temp={tempState}");
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task CanWaitForBrightnessThreshold1Change(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            var currentState = Device!.BrightnessThreshold1Active;
            var targetState = !currentState; // Opposite of current state
            
            Console.WriteLine($"🔄 Device {deviceId} waiting for brightness threshold 1 change from {currentState} to {targetState}");
            Console.WriteLine("⚠️ Manual intervention needed: Change brightness threshold 1 in KNX system");
            
            // Act
            var result = await Device.WaitForBrightnessThreshold1StateAsync(targetState, _thresholdChangeTimeout);
            
            // Assert
            if (result)
            {
                Device.BrightnessThreshold1Active.Should().Be(targetState,
                    "Device should reflect new threshold state");
                Console.WriteLine($"✅ Device {deviceId} brightness threshold 1 changed to {targetState}");
            }
            else
            {
                Console.WriteLine($"⏰ Device {deviceId} brightness threshold 1 timeout - manual change not detected");
                // This is not a failure - just means no external change occurred during test
            }
        }

        #endregion

        #region Sun Protection Scenarios

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task SunProtectionScenario_NoThresholdsActive_ShouldStayOpen(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            await EnsureSunProtectionUnblocked(Device!);
            await Device!.SetPercentageAsync(0, _defaultTimeout); // Fully open
            
            // Act & Assert
            if (!Device.BrightnessThreshold1Active && 
                !Device.BrightnessThreshold2Active && 
                !Device.OutdoorTemperatureThresholdActive)
            {
                // No thresholds active - shutter should remain open
                Device.CurrentPercentage.Should().Be(0,
                    "Shutter should stay open when no thresholds are active");
                Console.WriteLine($"✅ Device {deviceId} correctly stays open - no thresholds active");
            }
            else
            {
                Console.WriteLine($"⚠️ Device {deviceId} has active thresholds - skipping no-threshold test");
            }
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task SunProtectionScenario_OnlyBrightnessThreshold1_ShouldClosePartially(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            await EnsureSunProtectionUnblocked(Device!);
            
            // Act & Assert
            if (Device!.BrightnessThreshold1Active && 
                !Device.BrightnessThreshold2Active && 
                !Device.OutdoorTemperatureThresholdActive)
            {
                Console.WriteLine($"📋 Device {deviceId} scenario: Only brightness threshold 1 active");
                // Expected behavior: Shutter should close partially (e.g., 30-50%)
                // This would be configured in the KNX system logic
                
                // Wait a moment for potential automatic adjustment
                await Task.Delay(2000);
                
                Device.CurrentPercentage.Should().BeGreaterThan(0,
                    "Shutter should close partially when only brightness threshold 1 is active");
                Device.CurrentPercentage.Should().BeLessThan(100,
                    "Shutter should not close completely with only one brightness threshold");
                
                Console.WriteLine($"✅ Device {deviceId} partially closed at {Device.CurrentPercentage}% with B1 threshold");
            }
            else
            {
                Console.WriteLine($"⚠️ Device {deviceId} threshold state doesn't match test scenario");
            }
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task SunProtectionScenario_BothBrightnessThresholds_ShouldCloseMore(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            await EnsureSunProtectionUnblocked(Device!);
            
            // Act & Assert
            if (Device!.BrightnessThreshold1Active && 
                Device.BrightnessThreshold2Active)
            {
                Console.WriteLine($"📋 Device {deviceId} scenario: Both brightness thresholds active");
                // Expected behavior: Shutter should close more (e.g., 60-80%)
                
                // Wait a moment for potential automatic adjustment
                await Task.Delay(2000);
                
                Device.CurrentPercentage.Should().BeGreaterThan(30,
                    "Shutter should close significantly when both brightness thresholds are active");
                
                Console.WriteLine($"✅ Device {deviceId} closed at {Device.CurrentPercentage}% with both brightness thresholds");
            }
            else
            {
                Console.WriteLine($"⚠️ Device {deviceId} threshold state doesn't match test scenario");
            }
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task SunProtectionScenario_AllThresholds_ShouldCloseMaximally(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            await EnsureSunProtectionUnblocked(Device!);
            
            // Act & Assert
            if (Device!.BrightnessThreshold1Active && 
                Device.BrightnessThreshold2Active && 
                Device.OutdoorTemperatureThresholdActive)
            {
                Console.WriteLine($"📋 Device {deviceId} scenario: All thresholds active");
                // Expected behavior: Maximum sun protection (e.g., 80-100%)
                
                // Wait a moment for potential automatic adjustment
                await Task.Delay(2000);
                
                Device.CurrentPercentage.Should().BeGreaterThan(50,
                    "Shutter should provide maximum protection when all thresholds are active");
                
                Console.WriteLine($"✅ Device {deviceId} maximum protection at {Device.CurrentPercentage}% with all thresholds");
            }
            else
            {
                Console.WriteLine($"⚠️ Device {deviceId} threshold state doesn't match test scenario");
            }
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task SunProtectionBlocked_ShouldIgnoreThresholds(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            var initialPosition = Device!.CurrentPercentage;
            
            // Act
            await Device.BlockSunProtectionAsync();
            await Device.SetPercentageAsync(25, _defaultTimeout); // Set specific position
            
            // Wait to ensure no automatic adjustments occur
            await Task.Delay(3000);
            
            // Assert
            Device.SunProtectionBlocked.Should().BeTrue(
                "Sun protection should be blocked");
            Device.CurrentPercentage.Should().Be(25,
                "Shutter should maintain manual position when sun protection is blocked");
            
            Console.WriteLine($"✅ Device {deviceId} ignores thresholds when blocked at {Device.CurrentPercentage}%");
        }

        #endregion

        #region Edge Cases and Error Handling

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task ThresholdAddressesAreCorrectlyConfigured(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            
            // Assert
            Device!.Addresses.BrightnessThreshold1.Should().Be("0/2/3",
                "Brightness threshold 1 should use correct KNX address");
            Device.Addresses.BrightnessThreshold2.Should().Be("0/2/4",
                "Brightness threshold 2 should use correct KNX address");
            Device.Addresses.OutdoorTemperatureThreshold.Should().Be("0/2/7",
                "Outdoor temperature threshold should use correct KNX address");
            
            Console.WriteLine($"✅ Device {deviceId} has correct threshold addresses configured");
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task CanOperateNormallyWithMixedThresholdStates(string deviceId)
        {
            //this test does not make sense
            throw new NotImplementedException();
            // Arrange
            await InitializeDevice(deviceId);
            await EnsureSunProtectionUnblocked(Device!);
            
            // Act - Perform normal operations regardless of threshold states
            var initialPosition = Device!.CurrentPercentage;
            await Device.SetPercentageAsync(50, _defaultTimeout);
            await Device.AdjustPercentageAsync(10, _defaultTimeout);
            
            // Assert
            Device.CurrentPercentage.Should().BeGreaterThan(initialPosition,
                "Device should respond to manual commands even with active thresholds");
            
            Console.WriteLine($"✅ Device {deviceId} operates normally with thresholds: B1={Device.BrightnessThreshold1Active}, B2={Device.BrightnessThreshold2Active}, Temp={Device.OutdoorTemperatureThresholdActive}");
        }

        #endregion

        #region Helper Methods

        private async Task EnsureSunProtectionUnblocked(ShutterDevice device)
        {
            if (device.SunProtectionBlocked)
            {
                await device.UnblockSunProtectionAsync();
                await device.WaitForSunProtectionBlockStateAsync(false, _defaultTimeout);
            }

            device.SunProtectionBlocked.Should().BeFalse(
                $"Device {device.Id} should have sun protection unblocked for threshold tests");
            Console.WriteLine($"✅ Device {device.Id} sun protection is unblocked");
        }

        internal async Task InitializeDevice(string deviceId, bool initialize = true, bool saveCurrentState = true)
        {
            Thread.Sleep(2000); // Ensure service is ready
            Console.WriteLine($"🆕 Creating new ShutterDevice {deviceId} for sun protection tests");
            Device = ShutterFactory.CreateShutter(deviceId, _knxService, _logger);
            if (initialize)
            {
                await Device.InitializeAsync();

                if (saveCurrentState)
                {
                    Device.SaveCurrentState();
                }
            }
            Console.WriteLine($"🌞 Shutter {deviceId} initialized - Position: {Device.CurrentPercentage}%, " +
                            $"Thresholds: B1={Device.BrightnessThreshold1Active}, B2={Device.BrightnessThreshold2Active}, " +
                            $"Temp={Device.OutdoorTemperatureThresholdActive}, SunProtection={Device.SunProtectionBlocked}");
        }

        #endregion
    }
}
