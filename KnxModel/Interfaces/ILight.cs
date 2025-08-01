using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Interface representing a KNX light with all its operations and state management
    /// </summary>
    public interface ILight : IKnxDevice, ILockable
    {
        /// <summary>
        /// KNX addresses for light control and feedback
        /// </summary>
        LightAddresses Addresses { get; }

        /// <summary>
        /// Current state of the light
        /// </summary>
        LightState CurrentState { get; }

        /// <summary>
        /// Saved state for restoration after tests
        /// </summary>
        LightState? SavedState { get; }

        /// <summary>
        /// Turn light on or off
        /// </summary>
        /// <param name="isOn">True to turn on, false to turn off</param>
        Task SetStateAsync(bool isOn);

        /// <summary>
        /// Turn light on
        /// </summary>
        Task TurnOnAsync();

        /// <summary>
        /// Turn light off
        /// </summary>
        Task TurnOffAsync();

        /// <summary>
        /// Toggle light state (on->off, off->on)
        /// </summary>
        Task ToggleAsync();

        /// <summary>
        /// Read current light state from KNX bus
        /// </summary>
        Task<bool> ReadStateAsync();

        /// <summary>
        /// Wait for light to reach target state
        /// </summary>
        /// <param name="targetState">Target state to wait for</param>
        /// <param name="timeout">Maximum time to wait</param>
        /// <returns>True if state reached, false on timeout</returns>
        Task<bool> WaitForStateAsync(bool targetState, TimeSpan? timeout = null);
    }
}
