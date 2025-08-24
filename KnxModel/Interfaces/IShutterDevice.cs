namespace KnxModel
{
    /// <summary>
    /// Interface for a Shutter device
    /// Combines basic device functionality with percentage control for position, locking, and activity status
    /// </summary>
    public interface IShutterDevice : IKnxDeviceBase, IMovementControllable, IPercentageControllable, ILockableDevice, IActivityStatusReadable, ISunProtectionBlockableDevice, ISunProtectionThresholdCapableDevice
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
    }
}
