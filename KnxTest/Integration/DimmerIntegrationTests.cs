using KnxModel;
using KnxTest.Integration.Base;
using KnxTest.Integration.Interfaces;

namespace KnxTest.Integration
{
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


        internal override DimmerDevice? Device { get; set; }

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
