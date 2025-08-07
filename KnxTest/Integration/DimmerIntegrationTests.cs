using KnxModel;
using KnxTest.Integration.Base;

namespace KnxTest.Integration
{
    [Collection("KnxService collection")]
    public class DimmerIntegrationTests : LightIntegrationTestsBase<DimmerDevice>
    {
        public DimmerIntegrationTests(KnxServiceFixture fixture) : base(fixture)
        {
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
            await TestCanLockAndUnlock(deviceId);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task CanReadLockState(string deviceId)
        {
            await TestCanReadLockState(deviceId);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task CanReadSwitchState(string deviceId)
        {
            await TestCanReadSwitchState(deviceId);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task CanToggleSwitch(string deviceId)
        {
            await TestCanToggleSwitch(deviceId);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task CanTurnOnAndTurnOff(string deviceId)
        {
            await TestCanTurnOnAndTurnOff(deviceId);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task LockPreventsStateChanges(string deviceId)
        {
            await TestLockPreventsStateChanges(deviceId);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task SwitchableDeviceTurnOffWhenLocked(string deviceId)
        {
            await TestSwitchableDeviceTurnOffWhenLocked(deviceId);
        }

    }
}
