using System;

namespace KnxModel
{
    /// <summary>
    /// Contains all KNX addresses for a shutter
    /// </summary>
    public record ShutterAddresses(
        string MovementControl,      // 4/0/X - UP/DOWN control
        string MovementFeedback,     // 4/0/{X+100} - UP/DOWN feedback
        string PositionControl,      // 4/2/X - absolute position control
        string PositionFeedback,     // 4/2/{X+100} - position feedback
        string LockControl,          // 4/3/X - lock control
        string LockFeedback,         // 4/3/{X+100} - lock feedback
        string StopControl,          // 4/1/X - stop/step control
        string MovementStatusFeedback // 4/1/{X+100} - movement status feedback
    ) : ILockableAddress;

    /// <summary>
    /// Current state of a shutter
    /// </summary>
    public record ShutterState(
        float Position,
        bool IsLocked,
        ShutterMovementState MovementState,
        DateTime LastUpdated
    ) : ILockableState;

    /// <summary>
    /// Direction of shutter movement
    /// </summary>
    public enum ShutterDirection
    {
        Up,     // false in KNX
        Down    // true in KNX
    }

    /// <summary>
    /// Current movement state of shutter
    /// </summary>
    public enum ShutterMovementState
    {
        Stopped,
        MovingUp,
        MovingDown,
        Unknown
    }
}
