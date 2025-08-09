using KnxModel;
using KnxTest.Integration.Base;
using KnxTest.Integration.Interfaces;

namespace KnxTest.Integration
{
    public class ShutterIntegrationTests :LockableIntegrationTestBase<ShutterDevice>, IPercentageControllableDeviceTests
    {
        internal readonly PercentageControllTestHelper _percentageTestHelper;
        public ShutterIntegrationTests(KnxServiceFixture fixture) : base(fixture)
        {
            _percentageTestHelper = new PercentageControllTestHelper();
        }

        public Task CanAdjustPercentage(string deviceId)
        {
            throw new NotImplementedException();
        }

        public override Task CanLockAndUnlock(string deviceId)
        {
            throw new NotImplementedException();
        }

        public override Task CanReadLockState(string deviceId)
        {
            throw new NotImplementedException();
        }

        public Task CanReadPercentage(string deviceId)
        {
            throw new NotImplementedException();
        }

        public Task CanSetPercentage(string deviceId)
        {
            throw new NotImplementedException();
        }

        public Task CanSetSpecificPercentages(string deviceId)
        {
            throw new NotImplementedException();
        }

        public Task CanSetToMaximum(string deviceId)
        {
            throw new NotImplementedException();
        }

        public Task CanSetToMinimum(string deviceId)
        {
            throw new NotImplementedException();
        }

        public Task CanWaitForPercentageState(string deviceId)
        {
            throw new NotImplementedException();
        }

        public override Task LockPreventsStateChanges(string deviceId)
        {
            throw new NotImplementedException();
        }

        public Task PercentageRangeValidation(string deviceId)
        {
            throw new NotImplementedException();
        }

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


    [Collection("KnxService collection")]
    public class DimmerIntegrationTests : LightIntegrationTestsBase<DimmerDevice>, IPercentageControllableDeviceTests
    {
        internal readonly PercentageControllTestHelper _percentageTestHelper;
        public DimmerIntegrationTests(KnxServiceFixture fixture) : base(fixture)
        {
            _percentageTestHelper = new PercentageControllTestHelper();
        }

        // Data source for tests - only pure lights (not dimmers)
        public static IEnumerable<object[]> DimmerIdsFromConfig
        {
            get
            {
                var config = DimmerFactory.DimmerConfigurations;
                return config//.Where(x => x.Value.Name.ToLower().Contains("off"))
                            .Select(k => new object[] { k.Key });
            }
        }

        internal override async Task InitializeDevice(string deviceId, bool saveCurrentState = true)
        {
            Console.WriteLine($"🆕 Creating new DimmerDevice {deviceId}");
            Device = DimmerFactory.CreateDimmer(deviceId, _knxService);
            await Device.InitializeAsync();
            if (saveCurrentState)
            {
                Device.SaveCurrentState();
            }

            Console.WriteLine($"Light {deviceId} initialized - Switch: {Device.CurrentSwitchState}, Lock: {Device.CurrentLockState}");
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task CanLockAndUnlock(string deviceId)
        {
            await InitializeDevice(deviceId);
            await _lockTestHelper.CanLockAndUnlock(Device!);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task CanReadLockState(string deviceId)
        {
            await InitializeDevice(deviceId);
            await _lockTestHelper.CanReadLockState(Device!);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task CanReadSwitchState(string deviceId)
        {
            await InitializeDevice(deviceId);
            await _switchTestHelper.CanReadSwitchState(Device!);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task CanToggleSwitch(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await _switchTestHelper.CanToggleSwitch(Device!);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task CanTurnOnAndTurnOff(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await _switchTestHelper.CanTurnOnAndTurnOff(Device!);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task LockPreventsStateChanges(string deviceId)
        {
            await InitializeDevice(deviceId);
            await _lockTestHelper.LockPreventsStateChange(Device!);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task SwitchableDeviceTurnOffWhenLocked(string deviceId)
        {
            await InitializeDevice(deviceId);
            await _lockTestHelper.SwitchableDeviceTurnOffWhenLocked(Device!);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public async Task CanSetPercentage(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await _percentageTestHelper.CanSetPercentage(Device!);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public async Task CanReadPercentage(string deviceId)
        {
            await InitializeDevice(deviceId);
            await _percentageTestHelper.CanReadPercentage(Device!);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public async Task PercentageRangeValidation(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await _percentageTestHelper.PercentageRangeValidation(Device!);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public async Task CanAdjustPercentage(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await _percentageTestHelper.CanAdjustPercentage(Device!);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public async Task CanSetToMinimum(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await _percentageTestHelper.CanSetToMinimum(Device!);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public async Task CanSetToMaximum(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await _percentageTestHelper.CanSetToMaximum(Device!);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public async Task CanWaitForPercentageState(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await _percentageTestHelper.CanWaitForPercentageState(Device!);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public async Task CanSetSpecificPercentages(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await _percentageTestHelper.CanSetSpecificPercentages(Device!);
        }
    }
}
