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
        protected abstract TState UpdateLockState(bool isLocked);

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

        public async Task SetLockAsync(bool isLocked)
        {

            await SetBitFunctionAsync(
                Addresses.LockControl,
                isLocked,
                () => CurrentState.IsLocked == isLocked
            );
        }

        public async Task LockAsync()
        {
            await SetLockAsync(true);
        }

        public async Task UnlockAsync()
        {
            await SetLockAsync(false);
        }

        public async Task<bool> ReadLockStateAsync()
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

        public async Task<bool> WaitForLockStateAsync(bool targetLockState, TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? _defaultTimeout;
            Console.WriteLine($"Waiting for {GetType().Name.ToLower()} {Id} lock to become: {(targetLockState ? "LOCKED" : "UNLOCKED")}");

            // Create a task that completes when target lock state is reached
            var waitTask = Task.Run(async () =>
            {
                while (true)
                {
                    if (CurrentState.IsLocked == targetLockState)
                    {
                        Console.WriteLine($"✅ {GetType().Name} {Id} lock state achieved: {(targetLockState ? "LOCKED" : "UNLOCKED")}");
                        return true;
                    }

                    await Task.Delay(_pollingIntervalMs); // Check every 50ms
                }
            });

            // Create timeout task
            var timeoutTask = Task.Delay(effectiveTimeout);

            // Wait for either state to be reached or timeout
            var completedTask = await Task.WhenAny(waitTask, timeoutTask);

            if (completedTask == waitTask)
            {
                return await waitTask; // State reached
            }
            else
            {
                // Timeout occurred
                Console.WriteLine($"⚠️ WARNING: {GetType().Name} {Id} lock state timeout - expected {(targetLockState ? "LOCKED" : "UNLOCKED")}, current {(CurrentState.IsLocked ? "LOCKED" : "UNLOCKED")}");
                Console.WriteLine($"This may indicate: missing feedback or hardware communication issue");
                return false;
            }
        }
    }
}
