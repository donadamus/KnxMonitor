namespace KnxModel
{
    /// <summary>
    /// Interface for a Shutter device
    /// Combines basic device functionality with percentage control for position, locking, and activity status
    /// </summary>
    public interface IShutterDevice : IKnxDeviceBase, IPercentageControllable, ILockableDevice, IActivityStatusReadable
    {
        // Shutter is a composition of:
        // - Basic device functionality (IKnxDeviceBase)
        // - Percentage control (IPercentageControllable - position 0-100%)
        // - Locking capability (ILockableDevice)
        // - Activity status monitoring (IActivityStatusReadable - is moving?)
        
        /// <summary>
        /// KNX addresses for this shutter device
        /// </summary>
        ShutterAddresses ShutterAddresses { get; }
        
        // Note: Shutters don't need ISwitchable - they use percentage for open/close
        // 0% = fully open, 100% = fully closed
        // IsActive = true when shutter is moving, false when stopped
        
        /// <summary>
        /// Open shutter completely (0% position)
        /// </summary>
        Task OpenAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Close shutter completely (100% position)  
        /// </summary>
        Task CloseAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Stop shutter movement
        /// </summary>
        Task StopAsync(TimeSpan? timeout = null);
    }
}
