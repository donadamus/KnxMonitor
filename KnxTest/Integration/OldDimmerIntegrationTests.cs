using FluentAssertions;
using KnxModel;
using KnxTest.Integration.Base;
using KnxTest.Integration.Interfaces;

namespace KnxTest.Integration
{
    [Collection("KnxService collection")]
    public class OldDimmerIntegrationTests(KnxServiceFixture fixture) : OldLockableDeviceTestBase<IDimmerOld>(fixture)
    {
        // Data source for tests - only pure dimmers (not lights)
        public static IEnumerable<object[]> DimmerIdsFromConfig
        {
            get
            {
                var config = DimmerFactory.DimmerConfigurations;
                return config.Where(x => x.Value.Name.ToLower().Contains("dimmer"))
                            .Select(k => new object[] { k.Key });
            }
        }

        protected override async Task InitializeDevice(string deviceId)
        {
            _device = DimmerFactory.CreateDimmerOld(deviceId, _knxService);
            await _device.InitializeAsync();
            
            Console.WriteLine($"Dimmer {deviceId} initialized - Switch: {_device.CurrentState.Switch}, Lock: {_device.CurrentState.Lock}");
        }
        #region ILockableDeviceTests Implementation
        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task CanLockAndUnlock(string deviceId)
        {
            await AssertCanLockAndUnlock(deviceId);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task LockPreventsStateChanges(string deviceId)
        {
            await AssertLockPreventsStateChanges(deviceId);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task CanReadLockState(string deviceId)
        {
            await AssertCanReadLockState(deviceId);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task DeviceAutoOffWhenLocked(string deviceId)
        {
            // Dimmer specific test for auto-off when locked
            await AssertDeviceAutoOffWhenLocked(deviceId);
        }
        #endregion

    }
}