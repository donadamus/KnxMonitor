using System;

namespace KnxModel
{
    /// <summary>
    /// Clock synchronization mode
    /// </summary>
    public enum ClockMode
    {
        /// <summary>
        /// Master mode - sends time telegrams to KNX bus
        /// </summary>
        Master,

        /// <summary>
        /// Slave mode - receives time telegrams from KNX bus
        /// </summary>
        Slave,

        /// <summary>
        /// Slave/Master mode - starts as Slave, switches to Master if no telegrams received
        /// </summary>
        SlaveMaster
    }

    /// <summary>
    /// Contains KNX addresses for clock device
    /// </summary>
    public record ClockAddresses(
        string TimeControl          // 0/0/1 - time synchronization address
    );

    /// <summary>
    /// Current state of a clock device
    /// </summary>
    public record ClockState(
        DateTime CurrentDateTime,
        ClockMode Mode,
        bool HasValidTime,
        DateTime? LastTimeReceived,
        DateTime LastUpdated
    ) : BaseState(LastUpdated);

    /// <summary>
    /// Clock device configuration
    /// </summary>
    public record ClockConfiguration(
        ClockMode InitialMode,
        TimeSpan TimeStamp
    );
}
