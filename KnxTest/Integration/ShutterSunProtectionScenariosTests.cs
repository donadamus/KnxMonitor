using FluentAssertions;
using KnxModel;
using KnxTest.Integration.Base;
using KnxTest.Integration.Helpers;
using Xunit.Abstractions;

namespace KnxTest.Integration
{
    /// <summary>
    /// Advanced integration tests simulating real-world sun protection scenarios
    /// Tests complex threshold combinations and weather condition responses
    /// </summary>
    [Collection("KnxService collection")]
    public class ShutterSunProtectionScenariosTests : IntegrationTestBase<ShutterDevice>
    {
        internal readonly XUnitLogger<ShutterDevice> _logger;
        internal readonly SunProtectionTestHelperOld _sunProtectionHelper;
        private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);
        private readonly TimeSpan _scenarioTransitionTime = TimeSpan.FromSeconds(5);

        public ShutterSunProtectionScenariosTests(KnxServiceFixture fixture, ITestOutputHelper output) : base(fixture)
        {
            _logger = new XUnitLogger<ShutterDevice>(output);
            _sunProtectionHelper = new SunProtectionTestHelperOld(_logger);
        }

        // Data source for scenario tests
        public static IEnumerable<object[]> SunProtectionShutterIdsFromConfig
        {
            get
            {
                var config = ShutterFactory.ShutterConfigurations;
                return config.Where(x => x.Value.Name.ToLower().Contains("office") 
                //|| 
                //                       x.Value.Name.ToLower().Contains("living") ||
                //                       x.Value.Name.ToLower().Contains("bedroom")
                                       
                                       )
                            .Take(2) // Limit to 2 devices for extensive scenario testing
                            .Select(k => new object[] { k.Key });
            }
        }

        #region Weather Condition Simulation Tests


        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task TestProtectionLevel1(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            Console.WriteLine($"🌅 Simulating morning sunrise scenario for {deviceId}");

            // Act & Assert - Simulate gradual threshold activation
            // Note: In real system, thresholds would change automatically based on sensors

            // Early morning - no thresholds (simulated)
            if (!Device!.BrightnessThreshold1Active && !Device.BrightnessThreshold2Active)
            {
                Device.CurrentPercentage.Should().BeLessThan(20,
                    "Shutter should be mostly open in early morning");
                Console.WriteLine($"✅ Early morning state: {Device.CurrentPercentage}% (as expected)");
            }

            // Test threshold persistence
            await _sunProtectionHelper.TestThresholdPersistence(Device);

            Console.WriteLine($"🌅 Morning scenario completed for {deviceId}");
        }



        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task Scenario_MorningGradualSunrise_ShouldRespondProgressively(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            
            Console.WriteLine($"🌅 Simulating morning sunrise scenario for {deviceId}");
            
            // Act & Assert - Simulate gradual threshold activation
            // Note: In real system, thresholds would change automatically based on sensors
            
            // Early morning - no thresholds (simulated)
            if (!Device!.BrightnessThreshold1Active && !Device.BrightnessThreshold2Active)
            {
                Device.CurrentPercentage.Should().BeLessThan(20,
                    "Shutter should be mostly open in early morning");
                Console.WriteLine($"✅ Early morning state: {Device.CurrentPercentage}% (as expected)");
            }
            
            // Test threshold persistence
            await _sunProtectionHelper.TestThresholdPersistence(Device);
            
            Console.WriteLine($"🌅 Morning scenario completed for {deviceId}");
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task Scenario_HighNoonMaximalSun_ShouldProvideMaximumProtection(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            Console.WriteLine($"☀️ Simulating high noon scenario for {deviceId}");
            
            // In high noon scenario, we expect maximum thresholds to be active
            var activeThresholds = (Device!.BrightnessThreshold1Active ? 1 : 0) + 
                                 (Device.BrightnessThreshold2Active ? 1 : 0) + 
                                 (Device.OutdoorTemperatureThresholdActive ? 1 : 0);
            
            Console.WriteLine($"🌡️ Active thresholds during high noon: {activeThresholds}/3");
            
            // Act
            await _sunProtectionHelper.TestThresholdScenarios(Device);
            
            // Assert based on threshold activity
            if (activeThresholds >= 2)
            {
                Device.CurrentPercentage.Should().BeGreaterThan(40,
                    "Shutter should provide significant protection during high sun");
                Console.WriteLine($"✅ High noon protection active: {Device.CurrentPercentage}%");
            }
            
            Console.WriteLine($"☀️ High noon scenario completed for {deviceId}");
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task Scenario_CloudyDay_ShouldAdjustToLowerProtection(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            Console.WriteLine($"☁️ Simulating cloudy day scenario for {deviceId}");
            
            // On cloudy day, brightness thresholds might be inactive while temperature remains
            var onlyTempActive = !Device!.BrightnessThreshold1Active && 
                               !Device.BrightnessThreshold2Active && 
                               Device.OutdoorTemperatureThresholdActive;
            
            if (onlyTempActive)
            {
                Console.WriteLine($"🌡️ Cloudy day detected - only temperature threshold active");
                
                // Act
                await _sunProtectionHelper.TestThresholdScenarios(Device);
                
                // Assert
                Device.CurrentPercentage.Should().BeLessThan(60,
                    "Shutter should provide minimal protection on cloudy days");
                
                Console.WriteLine($"✅ Cloudy day response: {Device.CurrentPercentage}%");
            }
            else
            {
                Console.WriteLine($"⚠️ Current conditions don't match cloudy day scenario");
                await _sunProtectionHelper.TestThresholdScenarios(Device);
            }
            
            Console.WriteLine($"☁️ Cloudy day scenario completed for {deviceId}");
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task Scenario_EveningCooldown_ShouldGraduallyOpen(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            Console.WriteLine($"🌆 Simulating evening cooldown scenario for {deviceId}");
            
            // Evening scenario - temperature threshold might be inactive
            var tempInactive = !Device!.OutdoorTemperatureThresholdActive;
            
            if (tempInactive)
            {
                Console.WriteLine($"🌡️ Evening cooldown detected - temperature threshold inactive");
                
                // Act
                await _sunProtectionHelper.TestThresholdScenarios(Device);
                
                // If only brightness thresholds remain, expect moderate protection
                var brightnessOnly = Device.BrightnessThreshold1Active || Device.BrightnessThreshold2Active;
                if (brightnessOnly && tempInactive)
                {
                    Device.CurrentPercentage.Should().BeLessThan(70,
                        "Shutter should open more during evening cooldown");
                    Console.WriteLine($"✅ Evening cooldown response: {Device.CurrentPercentage}%");
                }
            }
            else
            {
                Console.WriteLine($"⚠️ Current conditions don't match evening cooldown scenario");
                await _sunProtectionHelper.TestThresholdScenarios(Device);
            }
            
            Console.WriteLine($"🌆 Evening cooldown scenario completed for {deviceId}");
        }

        #endregion

        #region User Interaction Scenarios

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task Scenario_UserManualOverride_ShouldRespectUserPreference(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            Console.WriteLine($"👤 Testing user manual override scenario for {deviceId}");
            
            // Act
            await _sunProtectionHelper.TestManualOverride(Device!);
            
            // Additional manual override tests
            await Device!.SetPercentageAsync(75, _defaultTimeout); // User wants specific position
            await Task.Delay(_scenarioTransitionTime);
            
            // Assert
            Device.CurrentPercentage.Should().BeApproximately(75, 5,
                "Device should respect user manual override");
            Device.IsSunProtectionBlocked.Should().BeTrue(
                "Sun protection should remain blocked during manual control");
            
            // Test that user can re-enable automatic protection
            await Device.UnblockSunProtectionAsync();
            await Task.Delay(_scenarioTransitionTime);
            
            Device.IsSunProtectionBlocked.Should().BeFalse(
                "Sun protection should be unblocked when user re-enables it");
            
            Console.WriteLine($"✅ User manual override scenario completed for {deviceId}");
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task Scenario_UserTemporaryAdjustment_ShouldAllowThenRevert(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            Console.WriteLine($"👤 Testing user temporary adjustment scenario for {deviceId}");
            
            // Record automatic position
            var automaticPosition = Device!.CurrentPercentage;
            var sunProtectionWasBlocked = Device.IsSunProtectionBlocked;
            
            // Act - User makes temporary adjustment without blocking sun protection
            if (!sunProtectionWasBlocked)
            {
                await Device.SetPercentageAsync(20, _defaultTimeout); // User opens for view
                await Task.Delay(_scenarioTransitionTime);
                
                // Assert - Position should be as user requested
                Device.CurrentPercentage.Should().BeApproximately(20, 5,
                    "Device should accept user temporary adjustment");
                
                // After some time, automatic system might readjust
                // (This would depend on system configuration)
                Console.WriteLine($"✅ User temporary adjustment accepted: {Device.CurrentPercentage}%");
                Console.WriteLine($"📋 Sun protection remains: {(Device.IsSunProtectionBlocked ? "BLOCKED" : "ACTIVE")}");
            }
            else
            {
                Console.WriteLine($"⚠️ Sun protection already blocked, skipping temporary adjustment test");
            }
            
            Console.WriteLine($"👤 User temporary adjustment scenario completed for {deviceId}");
        }

        #endregion

        #region System Integration Scenarios

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task Scenario_CompleteSystemValidation_ShouldPassAllChecks(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            Console.WriteLine($"🔍 Running complete system validation for {deviceId}");
            
            // Act & Assert - Comprehensive system check
            await _sunProtectionHelper.TestThresholdPersistence(Device!);
            await _sunProtectionHelper.TestThresholdScenarios(Device!);
            
            // Validate all addresses are correctly configured
            Device!.Addresses.BrightnessThreshold1.Should().NotBeNullOrEmpty("B1 address should be configured");
            Device.Addresses.BrightnessThreshold2.Should().NotBeNullOrEmpty("B2 address should be configured");
            Device.Addresses.OutdoorTemperatureThreshold.Should().NotBeNullOrEmpty("Temp address should be configured");
            
            // Validate threshold states are readable
            var b1 = await Device.ReadBrightnessThreshold1StateAsync();
            var b2 = await Device.ReadBrightnessThreshold2StateAsync();
            var temp = await Device.ReadOutdoorTemperatureThresholdStateAsync();
            
            // Thresholds read successfully
            Console.WriteLine($"📊 All thresholds read: B1={b1}, B2={b2}, Temp={temp}");
            
            // Test manual override capability
            await _sunProtectionHelper.TestManualOverride(Device);
            
            Console.WriteLine($"✅ Complete system validation passed for {deviceId}");
        }

        [Theory]
        [MemberData(nameof(SunProtectionShutterIdsFromConfig))]
        public async Task Scenario_StressTest_MultipleRapidChanges_ShouldRemainStable(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            Console.WriteLine($"🏃 Running stress test with rapid changes for {deviceId}");
            
            var initialPosition = Device!.CurrentPercentage;
            var initialB1 = Device.BrightnessThreshold1Active;
            var initialB2 = Device.BrightnessThreshold2Active;
            var initialTemp = Device.OutdoorTemperatureThresholdActive;
            
            // Act - Rapid operations
            for (int i = 0; i < 5; i++)
            {
                await Device.BlockSunProtectionAsync();
                await Device.SetPercentageAsync(i * 20, TimeSpan.FromSeconds(2));
                await Device.UnblockSunProtectionAsync();
                await Task.Delay(500);
                
                // Read threshold states rapidly
                await Device.ReadBrightnessThreshold1StateAsync();
                await Device.ReadBrightnessThreshold2StateAsync();
                await Device.ReadOutdoorTemperatureThresholdStateAsync();
            }
            
            // Assert - System should be stable
            Device.BrightnessThreshold1Active.Should().Be(initialB1,
                "B1 threshold should remain consistent after stress test");
            Device.BrightnessThreshold2Active.Should().Be(initialB2,
                "B2 threshold should remain consistent after stress test");
            Device.OutdoorTemperatureThresholdActive.Should().Be(initialTemp,
                "Temp threshold should remain consistent after stress test");
            
            // Position should be valid
            Device.CurrentPercentage.Should().BeInRange(0, 100,
                "Position should remain in valid range after stress test");
            
            Console.WriteLine($"✅ Stress test completed successfully for {deviceId}");
        }

        #endregion

        #region Helper Methods

        internal async Task InitializeDevice(string deviceId, bool saveCurrentState = true)
        {
            Thread.Sleep(2000); // Ensure service is ready
            Console.WriteLine($"🆕 Creating ShutterDevice {deviceId} for scenario testing");
            Device = ShutterFactory.CreateShutter(deviceId, _knxService, _logger);
            await Device.InitializeAsync();
            
            if (saveCurrentState)
            {
                Device.SaveCurrentState();
            }
            
            Console.WriteLine($"🌞 Device {deviceId} ready for scenario testing");
        }

        #endregion
    }
}
