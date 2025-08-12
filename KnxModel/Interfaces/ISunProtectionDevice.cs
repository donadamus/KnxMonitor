namespace KnxModel
{
    /// <summary>
    /// Interface for devices with comprehensive sun protection capabilities,
    /// including threshold monitoring and protection blocking.
    /// </summary>
    public interface ISunProtectionDevice : 
        IBrightnessThresholdCapableDevice, 
        ITemperatureThresholdCapableDevice, 
        ISunProtectionBlockableDevice
    {
        /// <summary>
        /// Gets the current sun protection activation level based on active thresholds.
        /// </summary>
        /// <returns>
        /// 0 - No protection (no thresholds active)
        /// 1 - Light protection (only one brightness threshold or temperature threshold active)
        /// 2 - Full protection (both brightness thresholds active)
        /// </returns>
        int GetSunProtectionLevel();

        /// <summary>
        /// Determines if any sun protection should be active based on current threshold states.
        /// </summary>
        /// <returns>True if any threshold is active and protection should be engaged.</returns>
        bool ShouldActivateSunProtection();

        /// <summary>
        /// Event raised when the overall sun protection level changes.
        /// </summary>
        event EventHandler<int>? SunProtectionLevelChanged;
    }
}
