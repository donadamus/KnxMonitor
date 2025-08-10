namespace KnxModel
{
    public interface ISunProtectionBlockableDevice : IIdentifable
    {
        /// <summary>
        /// Indicates whether sun protection control is currently locked.
        /// </summary>
        bool IsSunProtectionBlocked { get; }

        /// <summary>
        /// Block sun protection control (prevent changes).
        /// </summary>
        /// <param name="timeout">Maximum time to wait for operation.</param>
        Task BlockSunProtectionAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Unblock sun protection control (allow changes).
        /// </summary>
        /// <param name="timeout">Maximum time to wait for operation.</param>
        Task UnblockSunProtectionAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Set block state for sun protection control.
        /// </summary>
        /// <param name="blocked">True to block, false to unblock.</param>
        /// <param name="timeout">Maximum time to wait for operation.</param>
        Task SetSunProtectionBlockStateAsync(bool blocked, TimeSpan? timeout = null);

        /// <summary>
        /// Read block state for sun protection control from the KNX bus.
        /// </summary>
        Task<bool> ReadSunProtectionBlockStateAsync();

        /// <summary>
        /// Wait for a specific block state for sun protection control.
        /// </summary>
        /// <param name="targetState">Expected block state (true/false).</param>
        /// <param name="timeout">Maximum time to wait.</param>
        Task<bool> WaitForSunProtectionBlockStateAsync(bool targetState, TimeSpan? timeout = null);
    }
}