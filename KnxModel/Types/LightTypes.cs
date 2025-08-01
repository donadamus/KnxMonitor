using System;

namespace KnxModel
{
    /// <summary>
    /// KNX addresses for light control and feedback
    /// </summary>
    /// <param name="Control">Address for controlling the light state (on/off)</param>
    /// <param name="Feedback">Address for receiving light state feedback</param>
    /// <param name="LockControl">Address for controlling the light lock state</param>
    /// <param name="LockFeedback">Address for receiving light lock state feedback</param>
    public record LightAddresses(
        string Control,
        string Feedback,
        string LockControl,
        string LockFeedback
    ) : ILockableAddress;

    public interface ILockableAddress
    {
        string LockControl { get; }
        string LockFeedback { get; }
    }

    /// <summary>
    /// Current state of a KNX light
    /// </summary>
    /// <param name="IsOn">Whether the light is currently on</param>
    /// <param name="IsLocked">Whether the light is currently locked</param>
    /// <param name="LastUpdated">When the state was last updated</param>
    public record LightState(
        bool IsOn,
        bool IsLocked,
        DateTime LastUpdated
    ) : ILockableState;

    public interface ILockableState
    {
        bool IsLocked { get; }
    }
}
