using KnxModel;
using KnxTest.Integration.Interfaces;

namespace KnxTest.Integration.Base
{
    /// <summary>
    /// Integration tests for Light devices using new architecture
    /// Inherits from DeviceTestBase and implements ILockableDeviceTests interface
    /// </summary>
    [Collection("KnxService collection")]
    public abstract class LightIntegrationTestsBase<TDevice> : IntegrationTestBase, ILockableDeviceTests, ISwitchableDeviceTests
        where TDevice : ILightDevice
    {
        internal readonly LockTestHelper _lockTestHelper;
        internal readonly SwitchTestHelper _switchTestHelper;
        internal abstract TDevice? Device { get; set; }

        public LightIntegrationTestsBase(KnxServiceFixture fixture) : base(fixture)
        {
            _lockTestHelper = new LockTestHelper();
            _switchTestHelper = new SwitchTestHelper();
        }
        // ===== DEVICE INITIALIZATION =====

        internal abstract Task InitializeDevice(string deviceId, bool saveCurrentState = true);

        /// <summary>
        /// Initialize device and ensure it's unlocked for testing
        /// </summary>
        protected async Task InitializeDeviceAndEnsureUnlocked(string deviceId, bool saveCurrentState = true)
        {
            await InitializeDevice(deviceId, saveCurrentState);
            await _lockTestHelper.EnsureDeviceIsUnlockedBeforeTest(Device!);
        }

        #region ILockableDeviceTests Implementation

        public abstract Task CanLockAndUnlock(string deviceId);
        public abstract Task LockPreventsStateChanges(string deviceId);
        public abstract Task CanReadLockState(string deviceId);
        public abstract Task SwitchableDeviceTurnOffWhenLocked(string deviceId);

        #endregion

        #region ISwitchableDeviceTests Tests

        public abstract Task CanTurnOnAndTurnOff(string deviceId);
        public abstract Task CanToggleSwitch(string deviceId);
        public abstract Task CanReadSwitchState(string deviceId);

        #endregion

        #region Cleanup

        public override void Dispose()
        {
            try
            {
                if (Device != null)
                    Device.RestoreSavedStateAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to restore device state during cleanup: {ex.Message}");
            }
            finally
            {
                Device?.Dispose();
                base.Dispose();
            }
        }
        #endregion
    }
}
