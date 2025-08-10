namespace KnxModel
{
    public interface ISunProtectionCapableDevice : IIdentifable
    {
        /// <summary>
        /// Indicates whether sun protection is currently enabled.
        /// </summary>
        bool IsSunProtectionBlocked { get; }

        /// <summary>
        /// Enable sun protection mode.
        /// </summary>
        /// <param name="timeout">Maximum time to wait for operation.</param>
        Task BlockSunProtectionAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Disable sun protection mode.
        /// </summary>
        /// <param name="timeout">Maximum time to wait for operation.</param>
        Task UnblockSunProtectionAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Set sun protection state.
        /// </summary>
        /// <param name="enabled">True to enable, false to disable.</param>
        /// <param name="timeout">Maximum time to wait for operation.</param>
        Task SetSunProtectionBlockAsync(bool enabled, TimeSpan? timeout = null);

        /// <summary>
        /// Read current sun protection state from the KNX bus.
        /// </summary>
        Task<bool> ReadSunProtectionBlockStateAsync();

        /// <summary>
        /// Wait for a specific sun protection state.
        /// </summary>
        /// <param name="targetState">Expected sun protection state (true/false).</param>
        /// <param name="timeout">Maximum time to wait.</param>
        Task<bool> WaitForSunProtectionBlockStateAsync(bool targetState, TimeSpan? timeout = null);
    }
}
