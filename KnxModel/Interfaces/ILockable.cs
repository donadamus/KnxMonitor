using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Interface for devices that support lock functionality
    /// </summary>
    public interface ILockable
    {
        /// <summary>
        /// Set device lock state
        /// </summary>
        /// <param name="isLocked">True to lock, false to unlock</param>
        /// <param name="timeout">Timeout for the operation. If null, default timeout is used.</param>
        Task SetLockAsync(bool isLocked, TimeSpan? timeout = null);

        /// <summary>
        /// Lock the device
        /// </summary>
        Task LockAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Unlock the device
        /// </summary>
        Task UnlockAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Read current device lock state from KNX bus
        /// </summary>
        Task<bool> ReadLockStateAsync();

        /// <summary>
        /// Wait for device to reach target lock state
        /// </summary>
        /// <param name="targetLockState">Target lock state to wait for</param>
        /// <param name="timeout">Maximum time to wait</param>
        /// <returns>True if lock state reached, false on timeout</returns>
        Task<bool> WaitForLockStateAsync(bool targetLockState, TimeSpan? timeout = null);
    }
}
