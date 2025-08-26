using System;
using System.Threading.Tasks;

namespace KnxModel
{

    public interface IIdentifable
    {
        /// <summary>
        /// Unique identifier for the device
        /// </summary>
        string Id { get; }
    }

    /// <summary>
    /// Interface for devices that can be switched ON/OFF
    /// </summary>
    public interface ISwitchable : IIdentifable
    {
        Switch? SavedSwitchState { get; internal set; }

        Switch CurrentSwitchState { get; internal set; }

        /// <summary>
        /// Turn the device ON
        /// </summary>
        Task TurnOnAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Turn the device OFF
        /// </summary>
        Task TurnOffAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Toggle between ON and OFF
        /// </summary>
        Task ToggleAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Read current switch state from KNX bus
        /// </summary>
        Task<Switch> ReadSwitchStateAsync();

        /// <summary>
        /// Wait for specific switch state
        /// </summary>
        Task<bool> WaitForSwitchStateAsync(Switch targetState, TimeSpan? timeout = null);
    }
}
