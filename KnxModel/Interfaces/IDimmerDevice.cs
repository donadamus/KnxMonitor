namespace KnxModel
{
    /// <summary>
    /// Interface for a Dimmer device  
    /// Combines basic device functionality with switching, locking and brightness control
    /// </summary>
    public interface IDimmerDevice : IKnxDeviceBase, ISwitchable, ILockableDevice, IPercentageControllable
    {
        // Dimmer is a composition of:
        // - Basic device functionality (IKnxDeviceBase)
        // - Switching capability (ISwitchable)
        // - Locking capability (ILockableDevice)
        // - Percentage control (IPercentageControllable - brightness)
        
        /// <summary>
        /// KNX addresses for this dimmer device
        /// </summary>
        DimmerAddresses DimmerAddresses { get; }
        
        // No additional methods needed - everything comes from composed interfaces
        // Note: Percentage controls brightness, Switch controls ON/OFF
        // DimmerAddresses extends LightAddresses with brightness controls
        // DimmerAddresses extends LightAddresses with brightness controls
    }
}
