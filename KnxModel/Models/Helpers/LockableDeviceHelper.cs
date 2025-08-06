using System;
using System.Threading.Tasks;

namespace KnxModel.Models.Helpers
{
    /// <summary>
    /// Helper class for implementing lockable device functionality
    /// Handles lock state management and KNX communication for ILockableDevice implementations
    /// </summary>
    public class LockableDeviceHelper : DeviceHelperBase
    {
        private readonly Func<ILockableAddress> _getAddresses;
        private readonly Action<Lock> _updateLockState;
        private readonly Func<Lock> _getCurrentLockState;

        public LockableDeviceHelper(
            IKnxService knxService,
            string deviceId,
            string deviceType,
            Func<ILockableAddress> getAddresses,
            Action<Lock> updateLockState,
            Func<Lock> getCurrentLockState) : base(knxService, deviceId, deviceType)
        {
            _getAddresses = getAddresses ?? throw new ArgumentNullException(nameof(getAddresses));
            _updateLockState = updateLockState ?? throw new ArgumentNullException(nameof(updateLockState));
            _getCurrentLockState = getCurrentLockState ?? throw new ArgumentNullException(nameof(getCurrentLockState));
        }

        /// <summary>
        /// Processes KNX lock feedback messages
        /// </summary>
        public void ProcessLockMessage(KnxGroupEventArgs e)
        {
            var addresses = _getAddresses();
            if (e.Destination == addresses.LockFeedback)
            {
                var isLocked = e.Value.AsBoolean();
                var lockState = isLocked ? Lock.On : Lock.Off;
                _updateLockState(lockState);
                Console.WriteLine($"{_deviceType} {_deviceId} lock state updated via feedback: {(isLocked ? "LOCKED" : "UNLOCKED")}");
            }
        }

        /// <summary>
        /// Sets the lock state
        /// </summary>
        public async Task LockAsync(TimeSpan? timeout = null)
        {
            await SetLockAsync(Lock.On, timeout);
        }

        /// <summary>
        /// Unlocks the device
        /// </summary>
        public async Task UnlockAsync(TimeSpan? timeout = null)
        {
            await SetLockAsync(Lock.Off, timeout);
        }

        /// <summary>
        /// Sets the lock state to the specified value
        /// </summary>
        public async Task SetLockAsync(Lock lockState, TimeSpan? timeout = null)
        {
            await SetBitFunctionAsync(
                address: _getAddresses().LockControl,
                value: lockState == Lock.On,
                condition: () => _getCurrentLockState() == lockState,
                timeout: timeout ?? _defaultTimeout);
        }

        /// <summary>
        /// Reads the current lock state from KNX bus
        /// </summary>
        public async Task<Lock> ReadLockStateAsync()
        {
            try
            {
                var addresses = _getAddresses();
                var lockState = await _knxService.RequestGroupValue<bool>(addresses.LockFeedback);
                return lockState ? Lock.On : Lock.Off;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read lock state for {_deviceType} {_deviceId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Waits for the lock state to reach the specified value
        /// </summary>
        public async Task<bool> WaitForLockStateAsync(Lock lockState, TimeSpan timeout)
        {
            return await WaitForConditionAsync(
                () => _getCurrentLockState() == lockState,
                timeout,
                "lock state {lockState}"
            );
        }
    }
}
