namespace KnxModel
{
    /// <summary>
    /// Interface for devices that can receive and store brightness threshold states
    /// for sun protection functionality.
    /// </summary>
    public interface IBrightnessThresholdCapableDevice : IIdentifable
    {
        /// <summary>
        /// Indicates whether brightness threshold 1 is currently active (exceeded).
        /// </summary>
        bool IsBrightnessThreshold1Active { get; }

        /// <summary>
        /// Indicates whether brightness threshold 2 is currently active (exceeded).
        /// </summary>
        bool IsBrightnessThreshold2Active { get; }

        /// <summary>
        /// Event raised when brightness threshold 1 state changes.
        /// </summary>
        event EventHandler<bool>? BrightnessThreshold1StateChanged;

        /// <summary>
        /// Event raised when brightness threshold 2 state changes.
        /// </summary>
        event EventHandler<bool>? BrightnessThreshold2StateChanged;

        /// <summary>
        /// Read brightness threshold 1 state from the KNX bus.
        /// </summary>
        /// <returns>True if threshold is exceeded, false otherwise.</returns>
        Task<bool> ReadBrightnessThreshold1StateAsync();

        /// <summary>
        /// Read brightness threshold 2 state from the KNX bus.
        /// </summary>
        /// <returns>True if threshold is exceeded, false otherwise.</returns>
        Task<bool> ReadBrightnessThreshold2StateAsync();

        /// <summary>
        /// Wait for a specific brightness threshold 1 state.
        /// </summary>
        /// <param name="targetState">Expected threshold state (true/false).</param>
        /// <param name="timeout">Maximum time to wait.</param>
        /// <returns>True if target state was reached within timeout, false otherwise.</returns>
        Task<bool> WaitForBrightnessThreshold1StateAsync(bool targetState, TimeSpan? timeout = null);

        /// <summary>
        /// Wait for a specific brightness threshold 2 state.
        /// </summary>
        /// <param name="targetState">Expected threshold state (true/false).</param>
        /// <param name="timeout">Maximum time to wait.</param>
        /// <returns>True if target state was reached within timeout, false otherwise.</returns>
        Task<bool> WaitForBrightnessThreshold2StateAsync(bool targetState, TimeSpan? timeout = null);
    }
}
