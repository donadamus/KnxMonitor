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

        /// <summary>
        /// Gets the saved lock state for restoration. Must be implemented by derived classes.
        /// </summary>
        protected abstract LockState? GetSavedLockState();

        /// <summary>
        /// Gets the current lock state. Must be implemented by derived classes.
        /// </summary>
        protected abstract LockState GetCurrentLockState();

        /// <summary>
        /// Gets the lockable addresses for this device. Must be implemented by derived classes.
        /// </summary>
        protected abstract LockableAddresses GetLockableAddresses();

        // Explicit interface implementation for ILockable
        LockableAddresses ILockable.Addresses => GetLockableAddresses();
        LockState ILockable.CurrentState => GetCurrentLockState();
        LockState? ILockable.SavedState => GetSavedLockState();

        /// <summary>
        /// Restores the lock state from saved state. Override in derived classes to add additional state restoration.
        /// Always call base.RestoreSavedStateAsync() to ensure lock state is restored.
        /// </summary>
        public override async Task RestoreSavedStateAsync()
        {
            await RestoreLockStateAsync();
        }

        /// <summary>
        /// Restores lock state from saved state using LockState record.
        /// </summary>
        protected virtual async Task RestoreLockStateAsync()
        {
            var savedLockState = GetSavedLockState();
            if (savedLockState == null)
            {
                Console.WriteLine($"No saved lock state available for {GetType().Name.ToLower()} {Id}");
                return;
            }

            var currentLockState = GetCurrentLockState();
            
            // Restore lock state if different and not unknown
            if (currentLockState.Lock != savedLockState.Lock && savedLockState.Lock != Lock.Unknown)
            {
                await SetLockAsync(savedLockState.Lock);
                Console.WriteLine($"{GetType().Name} {Id} lock restored to: {savedLockState.Lock}");
            }
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

        protected virtual async Task SetLockStateAsync(Lock lockState, TimeSpan? timeout = null)
        {
            var completionCondition = () => GetCurrentLockState().Lock == lockState;

            if (completionCondition())
            {
                Console.WriteLine($"Lock state for {GetType().Name.ToLower()} {Id} is already {lockState}. No action taken.");
                return;
            }

            Console.WriteLine($"{lockState} {GetType().Name.ToLower()} {Id}");

            var addresses = GetLockableAddresses();
            await SetBitFunctionAsync(
                addresses.LockControl,
                lockState == Lock.On,
                completionCondition,
                timeout
            );
        }

        public virtual async Task<Lock> ReadLockStateAsync()
        {
            try
            {
                var addresses = GetLockableAddresses();
                var lockState = await _knxService.RequestGroupValue<bool>(addresses.LockFeedback);
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
                condition: () => GetCurrentLockState().Lock == lockState,
                timeout: timeout,
                description: $"lock state {lockState}"
            );
        }

        protected override void ProcessKnxMessage(KnxGroupEventArgs e)
        {
            // Handle lock messages first
            var addresses = GetLockableAddresses();
            if (e.Destination == addresses.LockFeedback)
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
        /// Updates the current state with lock information - must be implemented by derived classes
        /// </summary>
        protected abstract void UpdateCurrentStateLock(Lock lockState);

        /// <summary>
        /// Process device-specific KNX messages - override in derived classes
        /// </summary>
        protected virtual void ProcessDeviceSpecificMessage(KnxGroupEventArgs e)
        {
            // Default implementation does nothing - override in derived classes
        }
    }
}
