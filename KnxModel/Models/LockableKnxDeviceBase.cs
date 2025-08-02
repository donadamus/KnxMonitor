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

        public async Task SetLockAsync(bool isLocked, TimeSpan? timeout = null)
        {
            await SetLockStateAsync(isLocked, timeout);
        }

        public async Task LockAsync(TimeSpan? timeout = null)
        {
            await SetLockAsync(true, timeout);
        }

        public async Task UnlockAsync(TimeSpan? timeout = null)
        {
            await SetLockAsync(false, timeout);
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
        protected abstract void UpdateCurrentStateLock(bool isLocked);

        /// <summary>
        /// Gets the current lock state from the device state - must be implemented by derived classes
        /// </summary>
        protected abstract bool GetCurrentLockState();

        protected virtual async Task SetLockStateAsync(bool isLocked, TimeSpan? timeout = null)
        {
            Console.WriteLine($"{(isLocked ? "Locking" : "Unlocking")} {GetType().Name.ToLower()} {Id}");
            
            await SetBitFunctionAsync(
                GetLockControlAddress(),
                isLocked,
                () => GetCurrentLockState() == isLocked,
                timeout
            );
        }

        public virtual async Task<bool> ReadLockStateAsync()
        {
            try
            {
                return await _knxService.RequestGroupValue<bool>(GetLockFeedbackAddress());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read lock state for {GetType().Name.ToLower()} {Id}: {ex.Message}");
                throw;
            }
        }

        public virtual async Task<bool> WaitForLockStateAsync(bool targetLockState, TimeSpan? timeout = null)
        {
            Console.WriteLine($"Waiting for {GetType().Name.ToLower()} {Id} lock to become: {(targetLockState ? "LOCKED" : "UNLOCKED")}");
            
            return await WaitForConditionAsync(
                condition: () => GetCurrentLockState() == targetLockState,
                timeout: timeout,
                description: $"lock state {(targetLockState ? "LOCKED" : "UNLOCKED")}"
            );
        }

        protected override void ProcessKnxMessage(KnxGroupEventArgs e)
        {
            // Handle lock messages first
            if (e.Destination == GetLockFeedbackAddress())
            {
                var isLocked = e.Value.AsBoolean();
                UpdateCurrentStateLock(isLocked);
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
