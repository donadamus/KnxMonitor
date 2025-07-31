using System;

namespace KnxModel
{
    /// <summary>
    /// KNX addresses for light control and feedback
    /// </summary>
    /// <param name="Control">Address for controlling the light state (on/off)</param>
    /// <param name="Feedback">Address for receiving light state feedback</param>
    public record LightAddresses(
        string Control,
        string Feedback
    );

    /// <summary>
    /// Current state of a KNX light
    /// </summary>
    /// <param name="IsOn">Whether the light is currently on</param>
    /// <param name="LastUpdated">When the state was last updated</param>
    public record LightState(
        bool IsOn,
        DateTime LastUpdated
    );
}
