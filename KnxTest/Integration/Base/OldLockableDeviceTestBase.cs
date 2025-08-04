using KnxModel;
using KnxTest.Integration.Interfaces;

namespace KnxTest.Integration.Base
{
    public abstract class OldLockableDeviceTestBase<T> : DeviceTestBase, ILockableDeviceTests
        where T : ILockableOld
    {
        protected T _device; // Will be initialized in each test method
        protected readonly OldLockTestHelper _lockTestHelper;
        protected OldLockableDeviceTestBase(KnxServiceFixture fixture) : base(fixture)
        {
            _lockTestHelper = new OldLockTestHelper();
            _device = default!; // Initialize to default, will be set in derived classes
        }
        // Abstract methods for lock-specific tests
        public abstract Task CanLockAndUnlock(string deviceId);
        public abstract Task LockPreventsStateChanges(string deviceId);
        public abstract Task CanReadLockState(string deviceId);
        public abstract Task DeviceAutoOffWhenLocked(string deviceId);
        protected abstract Task InitializeDevice(string deviceId);
        /// <summary>
        /// Verifies that the specified device can be successfully locked and unlocked.
        /// </summary>
        /// <remarks>This method initializes the device with the provided <paramref name="deviceId"/> and
        /// performs a lock and unlock operation to ensure the device behaves as expected. It is intended for use in
        /// test scenarios.</remarks>
        /// <param name="deviceId">The unique identifier of the device to be tested. Cannot be null or empty.</param>
        /// <returns></returns>
        protected async Task AssertCanLockAndUnlock(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.CanLockAndUnlock(_device);
        }
        /// <summary>
        /// Verifies that a lock on the specified device prevents state changes.
        /// </summary>
        /// <remarks>This method initializes the specified device and ensures that any attempts to change
        /// its state  are blocked when the device is locked. It is intended for use in test scenarios.</remarks>
        /// <param name="deviceId">The unique identifier of the device to test.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected async Task AssertLockPreventsStateChanges(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.LockPreventsStateChange(_device);
        }

        protected async Task AssertCanReadLockState(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.CanReadLockState(_device);
        }

        protected async Task AssertDeviceAutoOffWhenLocked(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.DeviceAutoOffWhenLocked(_device);
        }

        public override void Dispose()
        {
            _device.RestoreSavedStateAsync().GetAwaiter().GetResult();
            _device.Dispose();
        }

    }
}