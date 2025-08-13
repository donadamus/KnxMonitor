using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KnxModel.Models.Helpers
{
    /// <summary>
    /// Helper class for implementing lockable device functionality
    /// Handles lock state management and KNX communication for ILockableDevice implementations
    /// </summary>
    public class LockableDeviceHelper<TDevice, TAddress> : DeviceHelperBase<TDevice, TAddress>
        where TDevice : IKnxDeviceBase, ILockableDevice
        where TAddress : ILockableAddress
    {
        public LockableDeviceHelper(TDevice owner,
            TAddress address,
            IKnxService knxService,
            ILogger<TDevice> logger,
            TimeSpan defaultTimeout) : base(owner, address, knxService, owner.Id, "LockableDevice", logger, defaultTimeout)
        {
        }

        /// <summary>
        /// Processes KNX lock feedback messages
        /// </summary>
        public void ProcessLockMessage(KnxGroupEventArgs e)
        {
            if (e.Destination == addresses.LockFeedback)
            {
                var isLocked = e.Value.AsBoolean();
                var lockState = isLocked ? Lock.On : Lock.Off;
                
                // Update state through internal access to the device base
                // This requires that TDevice inherits from LockableDeviceBase
                var deviceBase = owner as dynamic;
                deviceBase._currentLockState = lockState;
                deviceBase._lastUpdated = DateTime.Now;
                
                _logger.LogInformation("{DeviceType} {DeviceId} lock state updated via feedback: {LockState}", _deviceType, _deviceId, (isLocked ? "LOCKED" : "UNLOCKED"));
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
                address: addresses.LockControl,
                value: lockState == Lock.On,
                condition: () => owner.CurrentLockState == lockState,
                timeout: timeout ?? _defaultTimeout);
        }

        /// <summary>
        /// Reads the current lock state from KNX bus
        /// </summary>
        public async Task<Lock> ReadLockStateAsync()
        {
            try
            {
                var lockState = await _knxService.RequestGroupValue<bool>(addresses.LockFeedback);
                return lockState ? Lock.On : Lock.Off;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read lock state for {DeviceType} {DeviceId}: {Message}", _deviceType, _deviceId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Waits for the lock state to reach the specified value
        /// </summary>
        public async Task<bool> WaitForLockStateAsync(Lock lockState, TimeSpan timeout)
        {
            return await WaitForConditionAsync(
                () => owner.CurrentLockState == lockState,
                timeout,
                $"lock state {lockState}"
            );
        }
    }
}
