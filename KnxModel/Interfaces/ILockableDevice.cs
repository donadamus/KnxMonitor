using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Interface for devices that can be locked/unlocked
    /// </summary>
    public interface ILockableDevice :IIdentifable
    {
        /// <summary>
        /// Current lock state
        /// </summary>
        Lock CurrentLockState { get; }

        /// <summary>
        /// Sets the lock state asynchronously with an optional timeout.
        /// </summary>
        /// <param name="lockState">The desired lock state to set. This cannot be null.</param>
        /// <param name="timeout">An optional timeout specifying the maximum duration to wait for the lock operation to complete.  If null,
        /// the operation will use the default timeout.</param>
        Task SetLockAsync(Lock lockState, TimeSpan? timeout = null);
        /// <summary>
        /// Lock the device
        /// </summary>
        /// <param name="timeout">Maximum time to wait for lock operation</param>
        Task LockAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Unlock the device
        /// </summary>
        /// <param name="timeout">Maximum time to wait for unlock operation</param>
        Task UnlockAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Read current lock state from KNX bus
        /// </summary>
        Task<Lock> ReadLockStateAsync();

        /// <summary>
        /// Wait for specific lock state
        /// </summary>
        Task<bool> WaitForLockStateAsync(Lock targetState, TimeSpan timeout);
    }
}
