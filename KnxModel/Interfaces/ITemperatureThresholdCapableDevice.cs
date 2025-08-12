namespace KnxModel
{
    /// <summary>
    /// Interface for devices that can receive and store outdoor temperature threshold state
    /// for sun protection functionality.
    /// </summary>
    public interface ITemperatureThresholdCapableDevice : IIdentifable
    {
        /// <summary>
        /// Indicates whether outdoor temperature threshold is currently active (exceeded).
        /// </summary>
        bool IsTemperatureThresholdActive { get; }

        /// <summary>
        /// Event raised when outdoor temperature threshold state changes.
        /// </summary>
        event EventHandler<bool>? TemperatureThresholdStateChanged;

        /// <summary>
        /// Read outdoor temperature threshold state from the KNX bus.
        /// </summary>
        /// <returns>True if threshold is exceeded, false otherwise.</returns>
        Task<bool> ReadTemperatureThresholdStateAsync();

        /// <summary>
        /// Wait for a specific temperature threshold state.
        /// </summary>
        /// <param name="targetState">Expected threshold state (true/false).</param>
        /// <param name="timeout">Maximum time to wait.</param>
        /// <returns>True if target state was reached within timeout, false otherwise.</returns>
        Task<bool> WaitForTemperatureThresholdStateAsync(bool targetState, TimeSpan? timeout = null);
    }
}
