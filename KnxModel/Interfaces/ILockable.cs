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
        Task SetLockAsync(bool isLocked);

        /// <summary>
        /// Lock the device
        /// </summary>
        Task LockAsync();

        /// <summary>
        /// Unlock the device
        /// </summary>
        Task UnlockAsync();

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
