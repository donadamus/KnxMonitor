using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Interface for devices that have readable activity/operation status
    /// This is READ-ONLY feedback from the device about its current operation state
    /// Examples: rolety się poruszają, pompa pracuje, brama się otwiera, wentylator kreci
    /// </summary>
    public interface IActivityStatusReadable
    {
        /// <summary>
        /// Current activity status (true = active/moving/running, false = inactive/stopped)
        /// </summary>
        bool IsActive { get; internal set; }
        
        /// <summary>
        /// Read current activity status from KNX bus
        /// </summary>
        Task<bool> ReadActivityStatusAsync();
        
        /// <summary>
        /// Wait for device to become inactive (stop operation)
        /// Useful for waiting until movement/operation completes
        /// </summary>
        /// <param name="timeout">Maximum time to wait (default: 30 seconds)</param>
        /// <returns>True if device stopped within timeout, false if timeout exceeded</returns>
        Task<bool> WaitForInactiveAsync(TimeSpan? timeout = null);
        
        /// <summary>
        /// Wait for device to become active (start operation)  
        /// Useful for confirming that operation has started
        /// </summary>
        /// <param name="timeout">Maximum time to wait (default: 5 seconds)</param>
        /// <returns>True if device started within timeout, false if timeout exceeded</returns>
        Task<bool> WaitForActiveAsync(TimeSpan? timeout = null);
    }
}
