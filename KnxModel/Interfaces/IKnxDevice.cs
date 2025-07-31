using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Base interface for all KNX devices
    /// </summary>
    public interface IKnxDevice : IDisposable
    {
        /// <summary>
        /// Unique identifier for the device
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Human-readable name of the device
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The sub-group number used for KNX addresses
        /// </summary>
        string SubGroup { get; }

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
        Task RestoreSavedStateAsync();
    }
}
