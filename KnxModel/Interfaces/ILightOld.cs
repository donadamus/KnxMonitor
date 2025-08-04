using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Interface representing a KNX light with all its operations and state management
    /// </summary>
    public interface ILightOld : IKnxDeviceOld, ILockableOld
    {
        /// <summary>
        /// KNX addresses for light control and feedback (overrides ILockable.Addresses)
        /// </summary>
        new LightAddresses Addresses { get; }

        /// <summary>
        /// Current state of the light (overrides ILockable.CurrentState)
        /// </summary>
        new LightState CurrentState { get; }

        /// <summary>
        /// Saved state for restoration after tests (overrides ILockable.SavedState)
        /// </summary>
        new LightState? SavedState { get; }

        /// <summary>
        /// Turn light on or off
        /// </summary>
        /// <param name="isOn">True to turn on, false to turn off</param>
        Task SetStateAsync(Switch switchState, TimeSpan? timeout = null);

        /// <summary>
        /// Turn light on
        /// </summary>
        Task TurnOnAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Turn light off
        /// </summary>
        Task TurnOffAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Toggle light state (on->off, off->on)
        /// </summary>
        Task ToggleAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Read current light state from KNX bus
        /// </summary>
        Task<Switch> ReadStateAsync();

        /// <summary>
        /// Wait for light to reach target state
        /// </summary>
        /// <param name="targetState">Target state to wait for</param>
        /// <param name="timeout">Maximum time to wait</param>
        /// <returns>True if state reached, false on timeout</returns>
        Task<bool> WaitForStateAsync(Switch targetState, TimeSpan? timeout = null);
        
    }
}
