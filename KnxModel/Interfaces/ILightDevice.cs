namespace KnxModel
{
    /// <summary>
    /// Interface for a simple Light device
    /// Combines basic device functionality with switching and locking
    /// </summary>
    public interface ILightDevice : IKnxDeviceBase, ISwitchable, ILockableDevice
    {
        // Light is a composition of:
        // - Basic device functionality (IKnxDeviceBase)
        // - Switching capability (ISwitchable) 
        // - Locking capability (ILockableDevice)
        
        /// <summary>
        /// KNX addresses for this light device
        /// </summary>
        LightAddresses LightAddresses { get; }
        
        // No additional methods needed - everything comes from composed interfaces
    }
}
