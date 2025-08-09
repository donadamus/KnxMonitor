using KnxModel;
using KnxTest.Integration.Base;
using KnxTest.Integration.Interfaces;

namespace KnxTest.Integration
{
    [Collection("KnxService collection")]
    public class ShutterIntegrationTests :LockableIntegrationTestBase<ShutterDevice>, IPercentageControllableDeviceTests
    {
        internal readonly PercentageControllTestHelper _percentageTestHelper;
        public ShutterIntegrationTests(KnxServiceFixture fixture) : base(fixture)
        {
            _percentageTestHelper = new PercentageControllTestHelper();
        }

        // Data source for tests - only pure lights (not dimmers)
        public static IEnumerable<object[]> ShutterIdsFromConfig
        {
            get
            {
                var config = ShutterFactory.ShutterConfigurations;
                return config.Where(x => x.Value.Name.ToLower().Contains("off"))
                            .Select(k => new object[] { k.Key });
            }
        }

        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public Task CanAdjustPercentage(string deviceId)
        {
            throw new NotImplementedException();
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
        public override Task CanReadLockState(string deviceId)
        {
            throw new NotImplementedException();
        }
        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public Task CanReadPercentage(string deviceId)
        {
            throw new NotImplementedException();
        }
        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public Task CanSetPercentage(string deviceId)
        {
            throw new NotImplementedException();
        }
        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public Task CanSetSpecificPercentages(string deviceId)
        {
            throw new NotImplementedException();
        }
        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public Task CanSetToMaximum(string deviceId)
        {
            throw new NotImplementedException();
        }
        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public Task CanSetToMinimum(string deviceId)
        {
            throw new NotImplementedException();
        }
        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public Task CanWaitForPercentageState(string deviceId)
        {
            throw new NotImplementedException();
        }
        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public override Task LockPreventsStateChanges(string deviceId)
        {
            throw new NotImplementedException();
        }
        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public Task PercentageRangeValidation(string deviceId)
        {
            throw new NotImplementedException();
        }
        [Theory]
        [MemberData(nameof(ShutterIdsFromConfig))]
        public override Task SwitchableDeviceTurnOffWhenLocked(string deviceId)
        {
            throw new NotImplementedException();
        }

        internal override async Task InitializeDevice(string deviceId, bool saveCurrentState = true)
        {
            Console.WriteLine($"🆕 Creating new DimmerDevice {deviceId}");
            Device = ShutterFactory.CreateShutter(deviceId, _knxService);
            await Device.InitializeAsync();
            if (saveCurrentState)
            {
                Device.SaveCurrentState();
            }
            Console.WriteLine($"Shutter {deviceId} initialized - Percentage: {Device.CurrentPercentage}, Lock: {Device.CurrentLockState}");
        }
    }
}
