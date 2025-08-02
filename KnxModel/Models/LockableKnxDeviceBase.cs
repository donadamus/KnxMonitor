using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Abstract base class for KNX devices that support lock functionality without generics
    /// </summary>
    public abstract class LockableKnxDeviceBase : KnxDeviceBase, ILockable
    {
        protected LockableKnxDeviceBase(string id, string name, string subGroup, IKnxService knxService, TimeSpan? timeout = null)
            : base(id, name, subGroup, knxService, timeout)
        {
        }

        public async Task SetLockAsync(Lock lockState, TimeSpan? timeout = null)
        {
            await SetLockStateAsync(lockState, timeout);
        }

        public async Task LockAsync(TimeSpan? timeout = null)
        {
            await SetLockAsync(Lock.On, timeout);
        }

        public async Task UnlockAsync(TimeSpan? timeout = null)
        {
            await SetLockAsync(Lock.Off, timeout);
        }

        /// <summary>
        /// Gets the KNX address for lock control - must be implemented by derived classes
        /// </summary>
        protected abstract string GetLockControlAddress();

        /// <summary>
        /// Gets the KNX address for lock feedback - must be implemented by derived classes
        /// </summary>
        protected abstract string GetLockFeedbackAddress();

        /// <summary>
        /// Updates the current state with lock information - must be implemented by derived classes
        /// </summary>
        protected abstract void UpdateCurrentStateLock(Lock lockState);

        /// <summary>
        /// Gets the current lock state from the device state - must be implemented by derived classes
        /// </summary>
        protected abstract Lock GetCurrentLockState();

        protected virtual async Task SetLockStateAsync(Lock lockState, TimeSpan? timeout = null)
        {
            Console.WriteLine($"{lockState} {GetType().Name.ToLower()} {Id}");
            
            await SetBitFunctionAsync(
                GetLockControlAddress(),
                lockState == Lock.On,
                () => GetCurrentLockState() == lockState,
                timeout
            );
        }

        public virtual async Task<Lock> ReadLockStateAsync()
        {
            try
            {
                var lockState = await _knxService.RequestGroupValue<bool>(GetLockFeedbackAddress());
                return lockState ? Lock.On : Lock.Off;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read lock state for {GetType().Name.ToLower()} {Id}: {ex.Message}");
                throw;
            }
        }

        public virtual async Task<bool> WaitForLockStateAsync(Lock lockState, TimeSpan? timeout = null)
        {
            Console.WriteLine($"Waiting for {GetType().Name.ToLower()} {Id} lock to become: {lockState}");
            
            return await WaitForConditionAsync(
                condition: () => GetCurrentLockState() == lockState,
                timeout: timeout,
                description: $"lock state {lockState}"
            );
        }

        protected override void ProcessKnxMessage(KnxGroupEventArgs e)
        {
            // Handle lock messages first
            if (e.Destination == GetLockFeedbackAddress())
            {
                var isLocked = e.Value.AsBoolean();
                var lockState = isLocked ? Lock.On : Lock.Off;
                UpdateCurrentStateLock(lockState);
                Console.WriteLine($"{GetType().Name} {Id} lock state updated via feedback: {(isLocked ? "LOCKED" : "UNLOCKED")}");
            }
            else
            {
                // Handle device-specific messages
                ProcessDeviceSpecificMessage(e);
            }
        }

        /// <summary>
        /// Process device-specific KNX messages - override in derived classes
        /// </summary>
        protected virtual void ProcessDeviceSpecificMessage(KnxGroupEventArgs e)
        {
            // Default implementation does nothing - override in derived classes
        }
    }
}
