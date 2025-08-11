namespace KnxModel
{
    /// <summary>
    /// Interface for clock synchronization devices in KNX network
    /// </summary>
    public interface IClockDevice : IKnxDeviceBase
    {
        /// <summary>
        /// Current clock mode
        /// </summary>
        ClockMode Mode { get; }

        /// <summary>
        /// Current date and time known by the device
        /// </summary>
        DateTime CurrentDateTime { get; }

        /// <summary>
        /// Time interval for sending time telegrams (for Master mode)
        /// </summary>
        TimeSpan TimeStamp { get; }

        /// <summary>
        /// Time when last time telegram was received (for Slave/Master mode)
        /// </summary>
        DateTime? LastTimeReceived { get; }

        /// <summary>
        /// Indicates if the device has a valid time
        /// </summary>
        bool HasValidTime { get; }

        /// <summary>
        /// Send current time to KNX bus (Master mode)
        /// </summary>
        Task SendTimeAsync();

        /// <summary>
        /// Force synchronization with system time
        /// </summary>
        Task SynchronizeWithSystemTimeAsync();

        /// <summary>
        /// Switch to Master mode
        /// </summary>
        Task SwitchToMasterModeAsync();

        /// <summary>
        /// Switch to Slave mode
        /// </summary>
        Task SwitchToSlaveModeAsync();
    }
}
