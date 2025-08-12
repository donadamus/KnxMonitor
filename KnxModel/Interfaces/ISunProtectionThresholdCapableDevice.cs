namespace KnxModel
{
    /// <summary>
    /// Interface for devices capable of monitoring sun protection thresholds (brightness and temperature)
    /// </summary>
    public interface ISunProtectionThresholdCapableDevice
    {
        /// <summary>
        /// Current state of brightness threshold 1
        /// </summary>
        bool BrightnessThreshold1Active { get; }

        /// <summary>
        /// Current state of brightness threshold 2
        /// </summary>
        bool BrightnessThreshold2Active { get; }

        /// <summary>
        /// Current state of outdoor temperature threshold
        /// </summary>
        bool OutdoorTemperatureThresholdActive { get; }

        /// <summary>
        /// Reads the current state of brightness threshold 1 from KNX bus
        /// </summary>
        /// <returns>True if threshold is active/exceeded, false otherwise</returns>
        Task<bool> ReadBrightnessThreshold1StateAsync();

        /// <summary>
        /// Reads the current state of brightness threshold 2 from KNX bus
        /// </summary>
        /// <returns>True if threshold is active/exceeded, false otherwise</returns>
        Task<bool> ReadBrightnessThreshold2StateAsync();

        /// <summary>
        /// Reads the current state of outdoor temperature threshold from KNX bus
        /// </summary>
        /// <returns>True if threshold is active/exceeded, false otherwise</returns>
        Task<bool> ReadOutdoorTemperatureThresholdStateAsync();

        /// <summary>
        /// Waits for brightness threshold 1 to reach the specified state
        /// </summary>
        /// <param name="targetState">Target state to wait for</param>
        /// <param name="timeout">Maximum time to wait</param>
        /// <returns>True if target state was reached within timeout, false otherwise</returns>
        Task<bool> WaitForBrightnessThreshold1StateAsync(bool targetState, TimeSpan? timeout = null);

        /// <summary>
        /// Waits for brightness threshold 2 to reach the specified state
        /// </summary>
        /// <param name="targetState">Target state to wait for</param>
        /// <param name="timeout">Maximum time to wait</param>
        /// <returns>True if target state was reached within timeout, false otherwise</returns>
        Task<bool> WaitForBrightnessThreshold2StateAsync(bool targetState, TimeSpan? timeout = null);

        /// <summary>
        /// Waits for outdoor temperature threshold to reach the specified state
        /// </summary>
        /// <param name="targetState">Target state to wait for</param>
        /// <param name="timeout">Maximum time to wait</param>
        /// <returns>True if target state was reached within timeout, false otherwise</returns>
        Task<bool> WaitForOutdoorTemperatureThresholdStateAsync(bool targetState, TimeSpan? timeout = null);
    }
}
