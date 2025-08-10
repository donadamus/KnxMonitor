using KnxModel;
using KnxTest.Integration.Base;
using KnxTest.Integration.Interfaces;
using Xunit.Abstractions;

namespace KnxTest.Integration
{
    [Collection("KnxService collection")]
    public class ShutterIntegrationTests :LockableIntegrationTestBase<ShutterDevice>, IPercentageControllableDeviceTests
    {
        internal readonly PercentageControllTestHelper _percentageTestHelper;
        internal readonly XUnitLogger<ShutterDevice> _logger;
        public ShutterIntegrationTests(KnxServiceFixture fixture, ITestOutputHelper output) : base(fixture)
        {
            _percentageTestHelper = new PercentageControllTestHelper(output);
            _logger = new XUnitLogger<ShutterDevice>(output);
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
            await _percentageTestHelper.CanSetPercentage(Device!);
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public async Task CanSetSpecificPercentages(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await _percentageTestHelper.CanSetSpecificPercentages(Device!);
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public async Task CanSetToMaximum(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await _percentageTestHelper.CanSetToMaximum(Device!);
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public async Task CanSetToMinimum(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await _percentageTestHelper.CanSetToMinimum(Device!);
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public async Task CanWaitForPercentageState(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
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
            await _percentageTestHelper.PercentageRangeValidation(Device!);
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public override async Task SwitchableDeviceTurnOffWhenLocked(string deviceId)
        {
            await InitializeDevice(deviceId);
            await _lockTestHelper.SwitchableDeviceTurnOffWhenLocked(Device!);
        }

        internal override async Task InitializeDevice(string deviceId, bool saveCurrentState = true)
        {
            Thread.Sleep(3000); // Ensure service is ready
            Console.WriteLine($"🆕 Creating new ShutterDevice {deviceId}");
            Device = ShutterFactory.CreateShutter(deviceId, _knxService, _logger);
            await Device.InitializeAsync();
            if (saveCurrentState)
            {
                Device.SaveCurrentState();
            }
            Console.WriteLine($"Shutter {deviceId} initialized - Percentage: {Device.CurrentPercentage}, Lock: {Device.CurrentLockState}");
        }
    }
}
