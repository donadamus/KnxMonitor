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
        where TState : class
        where TAddresses : class
    {
        /// <summary>
        /// Creates a new lockable KNX device instance
        /// </summary>
        protected LockableKnxDevice(string id, string name, string subGroup, IKnxService knxService, TimeSpan defaultTimeout)
            : base(id, name, subGroup, knxService, defaultTimeout)
        {
        }

        /// <summary>
        /// Gets the lock control address from device addresses
        /// </summary>
        protected abstract string GetLockControlAddress();

        /// <summary>
        /// Gets the lock feedback address from device addresses
        /// </summary>
        protected abstract string GetLockFeedbackAddress();

        /// <summary>
        /// Gets the current lock state from device state
        /// </summary>
        protected abstract bool GetCurrentLockState();

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
            if (address == GetLockControlAddress() || address == GetLockFeedbackAddress())
            {
                var isLocked = value.AsBoolean();
                CurrentState = UpdateLockState(isLocked);
                Console.WriteLine($"{GetType().Name} {Id} lock state updated: {(isLocked ? "LOCKED" : "UNLOCKED")}");
                return true;
            }
            return false;
        }

        public async Task SetLockAsync(bool isLocked)
        {
            Console.WriteLine($"{(isLocked ? "Locking" : "Unlocking")} {GetType().Name.ToLower()} {Id}");
            _knxService.WriteGroupValue(GetLockControlAddress(), isLocked);
            
            // Wait for lock state change to be confirmed via feedback
            var timeout = TimeSpan.FromSeconds(5);
            
            // Create a task that completes when target lock state is reached
            var waitTask = Task.Run(async () =>
            {
                while (true)
                {
                    if (GetCurrentLockState() == isLocked)
                    {
                        Console.WriteLine($"✅ {GetType().Name} {Id} lock state confirmed: {(isLocked ? "LOCKED" : "UNLOCKED")}");
                        return true;
                    }
                    await Task.Delay(_pollingIntervalMs); // Check every 50ms
                }
            });

            // Create timeout task
            var timeoutTask = Task.Delay(timeout);

            // Wait for either state to be reached or timeout
            var completedTask = await Task.WhenAny(waitTask, timeoutTask);

            if (completedTask == waitTask)
            {
                await waitTask; // State reached
            }
            else
            {
                // Timeout occurred
                Console.WriteLine($"⚠️ WARNING: {GetType().Name} {Id} lock state change not confirmed within timeout");
            }
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
                return await _knxService.RequestGroupValue<bool>(GetLockFeedbackAddress());
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
                    if (GetCurrentLockState() == targetLockState)
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
                Console.WriteLine($"⚠️ WARNING: {GetType().Name} {Id} lock state timeout - expected {(targetLockState ? "LOCKED" : "UNLOCKED")}, current {(GetCurrentLockState() ? "LOCKED" : "UNLOCKED")}");
                Console.WriteLine($"This may indicate: missing feedback or hardware communication issue");
                return false;
            }
        }
    }
}
