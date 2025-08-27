using FluentAssertions;
using KnxModel;
using KnxModel.Factories;
using KnxTest.Integration.Base;
using KnxTest.Integration.Helpers;
using KnxTest.Integration.Interfaces;
using Microsoft.Extensions.Logging;
using KnxModel.Models;
using Xunit.Abstractions;

namespace KnxTest.Integration
{
    [Collection("KnxService collection")]
    public class ShutterIntegrationTests :LockableIntegrationTestBase<ShutterDevice>, IPercentageControllableDeviceTests
    {
        internal readonly PercentageControllTestHelper _percentageTestHelper;
        internal readonly SunProtectionTestHelper _sunProtectionTestHelper;

        internal readonly XUnitLogger<ShutterDevice> _logger;
        private readonly ITestOutputHelper output;

        public ShutterIntegrationTests(KnxServiceFixture fixture, ITestOutputHelper output) : base(fixture)
        {
            _logger = new XUnitLogger<ShutterDevice>(output);
            _percentageTestHelper = new PercentageControllTestHelper(_logger);
            _sunProtectionTestHelper = new SunProtectionTestHelper(_logger);
            this.output=output;
        }

        // Data source for tests - only pure lights (not dimmers)
        public static IEnumerable<object[]> ShutterIdsFromConfig
        {
            get
            {
                var config = ShutterFactory.ShutterConfigurations;
                return config//.Where(x => x.Value.Name.ToLower().Contains("off"))
                            .Select(k => new object[] { k.Key });
            }
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public async Task CanAdjustPercentage(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await EnsureSunProtectionIsBloced(Device!);
            await _percentageTestHelper.CanAdjustPercentage(Device!);
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public override async Task CanLockAndUnlock(string deviceId)
        {
            await InitializeDevice(deviceId);
            await _lockTestHelper.CanLockAndUnlock(Device!);
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public override async Task CanReadLockState(string deviceId)
        {
            await InitializeDevice(deviceId);
            await _lockTestHelper.CanReadLockState(Device!);
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public async Task CanReadPercentage(string deviceId)
        {
            await InitializeDevice(deviceId);
            await _percentageTestHelper.CanReadPercentage(Device!);
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public async Task CanSetPercentage(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await EnsureSunProtectionIsBloced(Device!);
            await _percentageTestHelper.CanSetPercentage(Device!);
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public async Task CanSetSpecificPercentages(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await EnsureSunProtectionIsBloced(Device!);
            await _percentageTestHelper.CanSetSpecificPercentages(Device!);
        }

        private async Task EnsureSunProtectionIsBloced(ShutterDevice device)
        {
    //        device..Should().NotBe(Lock.Unknown,
    //"Device lock state should be known before test");

            if (!device.SunProtectionBlocked)
            {
                await BlockSunProtection(device);
            }

            device.SunProtectionBlocked.Should().BeTrue(
                $"Device {device.Id} should have sun protection blocked before test");
            Console.WriteLine($"✅ Device {device.Id} is now unlocked");

        }

        private async Task BlockSunProtection(ShutterDevice device)
        {
            await device.BlockSunProtectionAsync(TimeSpan.Zero);
            await device.ReadSunProtectionBlockStateAsync();
            device.SunProtectionBlocked.Should().BeTrue(
                $"Device {device.Id} should have sun protection blocked after blocking");
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public async Task CanSetToMaximum(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await EnsureSunProtectionIsBloced(Device!);
            await _percentageTestHelper.CanSetToMaximum(Device!);
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public async Task CanSetToMinimum(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await EnsureSunProtectionIsBloced(Device!);
            await _percentageTestHelper.CanSetToMinimum(Device!);
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public async Task CanWaitForPercentageState(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await EnsureSunProtectionIsBloced(Device!);
            await _percentageTestHelper.CanWaitForPercentageState(Device!);
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public override async Task LockPreventsStateChanges(string deviceId)
        {
            await InitializeDevice(deviceId);
            await _lockTestHelper.LockPreventsStateChange(Device!);
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public async Task PercentageRangeValidation(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await EnsureSunProtectionIsBloced(Device!);
            await _percentageTestHelper.PercentageRangeValidation(Device!);
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public override async Task SwitchableDeviceTurnOffWhenLocked(string deviceId)
        {
            await InitializeDevice(deviceId);
            await _lockTestHelper.SwitchableDeviceTurnOffWhenLocked(Device!);
        }


        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public async Task TestSunProtection(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            var devices = new List<ShutterDevice>();
            foreach (var id in ShutterIdsFromConfig.Select(x => x[0].ToString()).Distinct())
            {
                var device = ShutterFactory.CreateShutter(id!, _knxService, _logger);
                await device.InitializeAsync();
                //await device.ReadSunProtectionBlockStateAsync();
                device.SaveCurrentState();
                devices.Add(device);
                await device.BlockSunProtectionAsync();
            }


            await Device!.UnblockSunProtectionAsync();
            await Device!.OpenAsync();

            var clockLogger = new XUnitLogger<ClockDevice>(output);
            var clockDevice = ClockFactory.CreateMasterClockDevice("1", "Clock", _knxService, clockLogger, TimeSpan.FromSeconds(1));
            
            var thresholdLogger = new XUnitLogger<ThresholdSimulatorDevice>(output);
            var threshold = ThresholdSimulatorFactory.CreateThresholdSimulator("1","Threshold Simulator", _knxService, thresholdLogger, TimeSpan.FromSeconds(1));

            await threshold.BlockBrightnessThresholdMonitoringAsync();



            var fakeDate = new DateTime(2025, 08, 25, 15, 00, 00);

            await clockDevice.SendTimeAsync(fakeDate);
            await clockDevice.SwitchToMasterModeAsync();
            Thread.Sleep(10000);
            await threshold.SetOutdoorTemperatureThresholdStateAsync(true);
            Thread.Sleep(30000);
            await threshold.SetBrightnessThreshold1StateAsync(true);
            Thread.Sleep(20000);
            await threshold.SetBrightnessThreshold2StateAsync(true);

            await clockDevice.SwitchToSlaveModeAsync();
            Thread.Sleep(1000);

            await clockDevice.SendTimeAsync(DateTime.Now);
            clockLogger.LogInformation($"Sent {DateTime.Now}");
            await clockDevice.SwitchToSlaveModeAsync();
            await threshold.SetOutdoorTemperatureThresholdStateAsync(false);
            Thread.Sleep(1000);
            await threshold.SetBrightnessThreshold1StateAsync(false);
            Thread.Sleep(1000);
            await threshold.SetBrightnessThreshold2StateAsync(false);
            await threshold.UnblockBrightnessThresholdMonitoringAsync();

            foreach (var item in devices)
            {
                await item.RestoreSavedStateAsync();
            }
        }

        internal override async Task InitializeDevice(string deviceId, bool saveCurrentState = true)
        {
            Thread.Sleep(3000); // Ensure service is ready
            _logger.LogInformation($"🆕 Creating new ShutterDevice {deviceId}");
            Device = ShutterFactory.CreateShutter(deviceId, _knxService, _logger);
            await Device.InitializeAsync();
            if (saveCurrentState)
            {
                Device.SaveCurrentState();
            }
            _logger.LogInformation($"Shutter {deviceId} initialized - Percentage: {Device.CurrentPercentage}, Lock: {Device.CurrentLockState}");
        }
    }
}
