using System;

namespace KnxModel
{
    /// <summary>
    /// Addresses for controlling a KNX dimmer device
    /// Extends LightAddresses with brightness control addresses
    /// </summary>
    /// <param name="SwitchControl">Address for controlling the dimmer switch state (maps to Control)</param>
    /// <param name="SwitchFeedback">Address for receiving dimmer switch state feedback (maps to Feedback)</param>
    /// <param name="BrightnessControl">Address for controlling the dimmer brightness (0-100%)</param>
    /// <param name="BrightnessFeedback">Address for receiving dimmer brightness feedback</param>
    /// <param name="LockControl">Address for controlling the dimmer lock state</param>
    /// <param name="LockFeedback">Address for receiving dimmer lock state feedback</param>
    public record DimmerAddresses(
        string SwitchControl,
        string SwitchFeedback,
        string BrightnessControl,
        string BrightnessFeedback,
        string LockControl,
        string LockFeedback
    ) : LightAddresses(SwitchControl, SwitchFeedback, LockControl, LockFeedback);

    /// <summary>
    /// Current state of a KNX dimmer
    /// Extends LightState with brightness information
    /// </summary>
    /// <param name="IsOn">Whether the dimmer is currently on</param>
    /// <param name="Brightness">Current brightness level (0-100%)</param>
    /// <param name="IsLocked">Whether the dimmer is currently locked</param>
    /// <param name="LastUpdated">When the state was last updated</param>
    public record DimmerState(
        Switch Switch,
        float Brightness,
        Lock Lock,
        DateTime LastUpdated
    ) : LightState(Switch, Lock, LastUpdated);



    public enum KnxDeviceType
    {
        Dimmer, // Represents a KNX dimmer device
        Light,  // Represents a KNX light device
        Shutter // Represents a KNX shutter device
    }
}
