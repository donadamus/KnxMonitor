using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Base interface for all KNX devices - simplified version
    /// </summary>
    public interface IKnxDeviceBase : IIdentifable, IDisposable
    {
        /// <summary>
        /// Human-readable name of the device
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The sub-group number used for KNX addresses
        /// </summary>
        string SubGroup { get; }

        /// <summary>
        /// When was the device state last updated
        /// </summary>
        DateTime LastUpdated { get; set; }

        /// <summary>
        /// Initializes the device by reading state from KNX bus
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Saves the current state for later restoration (useful for testing)
        /// </summary>
        void SaveCurrentState();

        /// <summary>
        /// Restores the device to previously saved state
        /// </summary>
        Task RestoreSavedStateAsync(TimeSpan? timeout = null);
    }

}
