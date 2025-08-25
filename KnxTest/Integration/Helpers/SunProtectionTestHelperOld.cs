using FluentAssertions;
using KnxModel;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace KnxTest.Integration.Helpers
{
    /// <summary>
    /// Helper class for testing sun protection threshold scenarios
    /// </summary>
    public class SunProtectionTestHelperOld
    {
        private readonly XUnitLogger<ShutterDevice> _logger;
        private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);
        private readonly TimeSpan _adjustmentWaitTime = TimeSpan.FromSeconds(3);

        public SunProtectionTestHelperOld(XUnitLogger<ShutterDevice> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Tests complete threshold monitoring cycle with different scenarios
        /// </summary>
        public async Task TestThresholdScenarios(ShutterDevice device)
        {
            _logger.LogInformation($"🌞 Starting threshold scenario tests for device {device.Id}");

            // Record current threshold states
            var currentB1 = device.BrightnessThreshold1Active;
            var currentB2 = device.BrightnessThreshold2Active;
            var currentTemp = device.OutdoorTemperatureThresholdActive;

            _logger.LogInformation($"📊 Current thresholds - B1: {currentB1}, B2: {currentB2}, Temp: {currentTemp}");

            // Test based on actual current state
            await TestCurrentThresholdConfiguration(device, currentB1, currentB2, currentTemp);
        }

        /// <summary>
        /// Tests the device response based on current threshold configuration
        /// </summary>
        private async Task TestCurrentThresholdConfiguration(ShutterDevice device, bool b1, bool b2, bool temp)
        {
            var activeCount = (b1 ? 1 : 0) + (b2 ? 1 : 0) + (temp ? 1 : 0);
            _logger.LogInformation($"🔢 Testing configuration with {activeCount} active thresholds");

            // Ensure sun protection is unblocked for testing
            if (device.IsSunProtectionBlocked)
            {
                await device.UnblockSunProtectionAsync();
                await Task.Delay(_adjustmentWaitTime);
            }

            // Set initial position
            await device.SetPercentageAsync(0, _defaultTimeout); // Start fully open
            await Task.Delay(_adjustmentWaitTime);

            var finalPosition = device.CurrentPercentage;

            switch (activeCount)
            {
                case 0:
                    await ValidateNoThresholdScenario(device, finalPosition);
                    break;
                case 1:
                    await ValidateSingleThresholdScenario(device, finalPosition, b1, b2, temp);
                    break;
                case 2:
                    await ValidateDoubleThresholdScenario(device, finalPosition, b1, b2, temp);
                    break;
                case 3:
                    await ValidateAllThresholdScenario(device, finalPosition);
                    break;
            }
        }

        private async Task ValidateNoThresholdScenario(ShutterDevice device, float position)
        {
            _logger.LogInformation($"🟢 No threshold scenario - expecting minimal closure");
            
            // With no thresholds, shutter should stay relatively open
            position.Should().BeLessThan(30, 
                "Shutter should remain mostly open when no thresholds are active");
            
            _logger.LogInformation($"✅ No threshold validation passed - position: {position}%");
        }

        private async Task ValidateSingleThresholdScenario(ShutterDevice device, float position, bool b1, bool b2, bool temp)
        {
            var activeThreshold = b1 ? "B1" : (b2 ? "B2" : "Temp");
            _logger.LogInformation($"🟡 Single threshold scenario - {activeThreshold} active");

            // With one threshold, expect moderate protection
            position.Should().BeInRange(0, 60, 
                $"Shutter should provide moderate protection with single threshold ({activeThreshold})");
            
            _logger.LogInformation($"✅ Single threshold validation passed - {activeThreshold} position: {position}%");
        }

        private async Task ValidateDoubleThresholdScenario(ShutterDevice device, float position, bool b1, bool b2, bool temp)
        {
            var activeThresholds = new List<string>();
            if (b1) activeThresholds.Add("B1");
            if (b2) activeThresholds.Add("B2");
            if (temp) activeThresholds.Add("Temp");
            
            var description = string.Join("+", activeThresholds);
            _logger.LogInformation($"🟠 Double threshold scenario - {description} active");

            // With two thresholds, expect increased protection
            position.Should().BeInRange(30, 80, 
                $"Shutter should provide increased protection with two thresholds ({description})");
            
            _logger.LogInformation($"✅ Double threshold validation passed - {description} position: {position}%");
        }

        private async Task ValidateAllThresholdScenario(ShutterDevice device, float position)
        {
            _logger.LogInformation($"🔴 All threshold scenario - maximum protection expected");

            // With all thresholds, expect maximum protection
            position.Should().BeGreaterThanOrEqualTo(60, 
                "Shutter should provide maximum protection when all thresholds are active");
            
            _logger.LogInformation($"✅ All threshold validation passed - position: {position}%");
        }

        /// <summary>
        /// Tests that manual override works even with active thresholds
        /// </summary>
        public async Task TestManualOverride(ShutterDevice device)
        {
            _logger.LogInformation($"🔧 Testing manual override capabilities for device {device.Id}");

            // Block sun protection to enable manual control
            await device.BlockSunProtectionAsync();
            await Task.Delay(_adjustmentWaitTime);

            device.IsSunProtectionBlocked.Should().BeTrue(
                "Sun protection should be blocked for manual override test");

            // Test manual positioning
            await device.SetPercentageAsync(33, _defaultTimeout);
            await Task.Delay(_adjustmentWaitTime);

            device.CurrentPercentage.Should().BeApproximately(33, 5,
                "Device should accept manual position when sun protection is blocked");

            // Test manual adjustment
            await device.AdjustPercentageAsync(17, _defaultTimeout);
            await Task.Delay(_adjustmentWaitTime);

            device.CurrentPercentage.Should().BeApproximately(50, 5,
                "Device should accept manual adjustments when sun protection is blocked");

            _logger.LogInformation($"✅ Manual override test passed - final position: {device.CurrentPercentage}%");
        }

        /// <summary>
        /// Tests threshold state persistence and consistency
        /// </summary>
        public async Task TestThresholdPersistence(ShutterDevice device)
        {
            _logger.LogInformation($"💾 Testing threshold state persistence for device {device.Id}");

            // Read initial states
            var initialB1 = await device.ReadBrightnessThreshold1StateAsync();
            var initialB2 = await device.ReadBrightnessThreshold2StateAsync();
            var initialTemp = await device.ReadOutdoorTemperatureThresholdStateAsync();

            // Wait and re-read
            await Task.Delay(1000);

            var secondB1 = await device.ReadBrightnessThreshold1StateAsync();
            var secondB2 = await device.ReadBrightnessThreshold2StateAsync();
            var secondTemp = await device.ReadOutdoorTemperatureThresholdStateAsync();

            // Verify consistency
            secondB1.Should().Be(initialB1, "Brightness threshold 1 should be persistent");
            secondB2.Should().Be(initialB2, "Brightness threshold 2 should be persistent");
            secondTemp.Should().Be(initialTemp, "Temperature threshold should be persistent");

            // Verify property consistency
            device.BrightnessThreshold1Active.Should().Be(secondB1, 
                "Property should match last read value for B1");
            device.BrightnessThreshold2Active.Should().Be(secondB2, 
                "Property should match last read value for B2");
            device.OutdoorTemperatureThresholdActive.Should().Be(secondTemp, 
                "Property should match last read value for Temp");

            _logger.LogInformation($"✅ Threshold persistence validated - B1: {secondB1}, B2: {secondB2}, Temp: {secondTemp}");
        }

        /// <summary>
        /// Generates a comprehensive test report for threshold functionality
        /// </summary>
        public async Task GenerateThresholdReport(ShutterDevice device)
        {
            _logger.LogInformation($"📋 Generating threshold report for device {device.Id}");

            // Read all current states
            var b1 = await device.ReadBrightnessThreshold1StateAsync();
            var b2 = await device.ReadBrightnessThreshold2StateAsync();
            var temp = await device.ReadOutdoorTemperatureThresholdStateAsync();
            
            var position = device.CurrentPercentage;
            var sunProtectionBlocked = device.IsSunProtectionBlocked;

            // Calculate protection level
            var activeThresholds = (b1 ? 1 : 0) + (b2 ? 1 : 0) + (temp ? 1 : 0);
            var protectionLevel = activeThresholds switch
            {
                0 => "None",
                1 => "Low",
                2 => "Medium", 
                3 => "High",
                _ => "Unknown"
            };

            _logger.LogInformation($"""
                📊 THRESHOLD REPORT FOR {device.Id}
                ═══════════════════════════════════════
                🌞 Brightness Threshold 1:     {(b1 ? "ACTIVE" : "inactive")}
                🌞 Brightness Threshold 2:     {(b2 ? "ACTIVE" : "inactive")}
                🌡️ Temperature Threshold:       {(temp ? "ACTIVE" : "inactive")}
                🛡️ Protection Level:            {protectionLevel} ({activeThresholds}/3)
                🎚️ Current Position:            {position:F1}%
                🔒 Sun Protection Blocked:      {(sunProtectionBlocked ? "YES" : "NO")}
                📍 KNX Addresses:
                   - B1: {device.Addresses.BrightnessThreshold1}
                   - B2: {device.Addresses.BrightnessThreshold2}
                   - Temp: {device.Addresses.OutdoorTemperatureThreshold}
                ═══════════════════════════════════════
                """);
        }
    }
}
