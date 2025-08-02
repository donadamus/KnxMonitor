using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Abstract base class for KNX devices that support lock functionality
    /// </summary>
    /// <typeparam name="TState">Type of device state (must have IsLocked property)</typeparam>
    /// <typeparam name="TAddresses">Type of device addresses (must have LockControl and LockFeedback properties)</typeparam>
    public abstract class LockableKnxDevice<TState, TAddresses> : KnxDevice<TState, TAddresses>, ILockable
        where TState : class, ILockableState
        where TAddresses : class, ILockableAddress
    {
        /// <summary>
        /// Creates a new lockable KNX device instance
        /// </summary>
        protected LockableKnxDevice(string id, string name, string subGroup, IKnxService knxService, TimeSpan defaultTimeout)
            : base(id, name, subGroup, knxService, defaultTimeout)
        {
        }

        /// <summary>
        /// Updates the device state with new lock state
        /// </summary>
        public abstract TState UpdateLockState(bool isLocked);

        /// <summary>
        /// Processes a lock-related KNX message and updates the device state if applicable
        /// Call this from your ProcessKnxMessage implementation
        /// </summary>
        /// <param name="address">The KNX address that received the message</param>
        /// <param name="value">The value from the KNX message</param>
        /// <returns>True if the message was a lock message and was processed</returns>
        protected bool ProcessLockMessage(string address, KnxValue value)
        {
            if (address == Addresses.LockControl || address == Addresses.LockFeedback)
            {
                var isLocked = value.AsBoolean();
                CurrentState = UpdateLockState(isLocked);
                Console.WriteLine($"{GetType().Name} {Id} lock state updated: {(isLocked ? "LOCKED" : "UNLOCKED")}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Processes KNX messages - handles lock messages automatically, then calls device-specific processing
        /// </summary>
        protected override void ProcessKnxMessage(KnxGroupEventArgs e)
        {
            // First, try to process as lock message
            if (ProcessLockMessage(e.Destination, e.Value))
            {
                return; // Lock message was processed, no need for device-specific handling
            }

            // If not a lock message, delegate to device-specific implementation
            ProcessDeviceSpecificMessage(e);
        }

        /// <summary>
        /// Processes device-specific KNX messages (non-lock messages)
        /// Override this in derived classes to handle device-specific feedback
        /// </summary>
        /// <param name="e">KNX group event arguments</param>
        protected virtual void ProcessDeviceSpecificMessage(KnxGroupEventArgs e)
        {
            // Default implementation - no device-specific processing
            // Override in derived classes (Light, Shutter) to add specific logic
        }

        public async Task SetLockAsync(bool isLocked, TimeSpan? timeout = null)
        {
            await SetLockStateAsync(isLocked, timeout);
        }

        protected virtual async Task SetLockStateAsync(bool isLocked, TimeSpan? timeout = null)
        {
            await SetBitFunctionAsync(
                Addresses.LockControl,
                isLocked,
                () => CurrentState.IsLocked == isLocked,
                timeout
            );
        }

        public async Task LockAsync(TimeSpan? timeout = null)
        {
            await SetLockAsync(true, timeout);
        }

        public async Task UnlockAsync(TimeSpan? timeout = null)
        {
            await SetLockAsync(false, timeout);
        }

        public virtual async Task<bool> ReadLockStateAsync()
        {
            try
            {
                return await _knxService.RequestGroupValue<bool>(Addresses.LockFeedback);
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
                condition: () => CurrentState.IsLocked == targetLockState,
                timeout: timeout,
                description: $"lock state {(targetLockState ? "LOCKED" : "UNLOCKED")}"
            );
        }

    }
}
