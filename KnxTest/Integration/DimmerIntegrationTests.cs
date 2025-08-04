using FluentAssertions;
using KnxModel;
using KnxTest.Integration.Base;
using KnxTest.Integration.Interfaces;

namespace KnxTest.Integration
{
    [Collection("KnxService collection")]
    public class DimmerIntegrationTests : DeviceTestBase, ILockableDeviceTests
    {
        private readonly LockTestHelper _lockTestHelper;
        private IDimmer _device = null!; // Will be initialized in each test method
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
        public DimmerIntegrationTests(KnxServiceFixture fixture) : base(fixture)
        {
            _lockTestHelper = new LockTestHelper(_knxService);
        }
        private async Task InitializeDevice(string deviceId)
        {
            _device = DimmerFactory.CreateDimmer(deviceId, _knxService);
            await _device.InitializeAsync();
            
            Console.WriteLine($"Dimmer {deviceId} initialized - Switch: {_device.CurrentState.Switch}, Lock: {_device.CurrentState.Lock}");
        }
        #region ILockableDeviceTests Implementation
        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public virtual async Task CanLockAndUnlock(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            // Act & Assert
            await _lockTestHelper.CanLockAndUnlock(_device);
        }
        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public virtual async Task LockPreventsStateChanges(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            // Act & Assert
            await _lockTestHelper.LockPreventsStateChange(_device);
        }
        #endregion
        public override void Dispose()
        {
            _device?.RestoreSavedStateAsync().GetAwaiter().GetResult();
            _device?.Dispose();
        }
    }
}