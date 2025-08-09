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
        
        // Note: Shutters support both percentage control (0-100%) and direct UP/DOWN commands
        // 0% = fully open, 100% = fully closed
        // IsActive = true when shutter is moving, false when stopped
        // 
        // IMPORTANT: Physical shutters measure position by timing open/close operations.
        // This can become desynchronized, so UP/DOWN commands via MovementControl are used
        // in OpenAsync/CloseAsync methods instead of percentage 0%/100% for better reliability.
        // A 2-second cooldown is enforced between commands to prevent device synchronization issues.
        
        /// <summary>
        /// Open shutter completely using UP command (MovementControl = 1)
        /// More reliable than SetPercentageAsync(0) due to timing-based position tracking
        /// </summary>
        Task OpenAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Close shutter completely using DOWN command (MovementControl = 0)
        /// More reliable than SetPercentageAsync(100) due to timing-based position tracking
        /// </summary>
        Task CloseAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Stop shutter movement
        /// </summary>
        Task StopAsync(TimeSpan? timeout = null);
    }
}
