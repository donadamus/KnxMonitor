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

        #region ILockableDeviceTests Implementation

        public abstract Task CanLockAndUnlock(string deviceId);
        public async Task TestCanLockAndUnlock(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.CanLockAndUnlock(Device!);

            await Task.CompletedTask;
        }
        public abstract Task LockPreventsStateChanges(string deviceId);
        public async Task TestLockPreventsStateChanges(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.LockPreventsStateChange(Device!);

            await Task.CompletedTask;

        }
        public abstract Task CanReadLockState(string deviceId);
        public async Task TestCanReadLockState(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.CanReadLockState(Device!);

            await Task.CompletedTask;
        }
        public abstract Task SwitchableDeviceTurnOffWhenLocked(string deviceId);
        public async Task TestSwitchableDeviceTurnOffWhenLocked(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.SwitchableDeviceTurnOffWhenLocked(Device!);

            await Task.CompletedTask;
        }

        #endregion

        #region ISwitchableDeviceTests Tests

        public abstract Task CanTurnOnAndTurnOff(string deviceId);
        public async Task TestCanTurnOnAndTurnOff(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Ensure device is unlocked before testing switch functionality
            await _lockTestHelper.EnsureDeviceIsUnlockedBeforeTest(Device!);

            // Act & Assert - Test switch functionality
            await _switchTestHelper.CanTurnOnAndTurnOff(Device!);

            await Task.CompletedTask;
        }
        public abstract Task CanToggleSwitch(string deviceId);
        public async Task TestCanToggleSwitch(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            // Ensure device is unlocked before testing toggle functionality
            await _lockTestHelper.EnsureDeviceIsUnlockedBeforeTest(Device!);

            // Act & Assert - Check toggle functionality
            await _switchTestHelper.CanToggleSwitch(Device!);

            await Task.CompletedTask;

        }
        public abstract Task CanReadSwitchState(string deviceId);
        public async Task TestCanReadSwitchState(string deviceId)
        {
            await InitializeDevice(deviceId);

            //Act $ Assert
            await _switchTestHelper.CanReadSwitchState(Device!);

            await Task.CompletedTask;
        }

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
