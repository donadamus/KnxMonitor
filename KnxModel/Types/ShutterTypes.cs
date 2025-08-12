using System;

namespace KnxModel
{
    /// <summary>
    /// Contains all KNX addresses for a shutter
    /// </summary>
    public record ShutterAddresses(
        string MovementControl,      // 4/0/X - UP/DOWN control
        string MovementFeedback,     // 4/0/{X+100} - UP/DOWN feedback
        string PercentageControl,      // 4/2/X - absolute position control
        string PercentageFeedback,     // 4/2/{X+100} - position feedback
        string LockControl,          // 4/3/X - lock control
        string LockFeedback,         // 4/3/{X+100} - lock feedback
        string SunProtectionBlockControl,          // 4/4/X - sun protection block control
        string SunProtectionBlockFeedback,         // 4/4/X - sun protection block feedback (same as control)
        string SunProtectionStatus,               // 4/4/{X+100} - current sun protection state
        string StopControl,          // 4/1/X - stop/step control
        string MovementStatusFeedback, // 4/1/{X+100} - movement status feedback
        string BrightnessThreshold1,   // 0/2/3 - brightness threshold 1 feedback
        string BrightnessThreshold2,   // 0/2/4 - brightness threshold 2 feedback  
        string OutdoorTemperatureThreshold // 0/2/7 - outdoor temperature threshold feedback
    ) : LockableAddresses(LockControl, LockFeedback), ILockableAddress, IPercentageControllableAddress;

    /// <summary>
    /// Current state of a shutter
    /// </summary>
    public record ShutterState(
        float Position,
        Lock Lock,
        ShutterMovementState MovementState,
        DateTime LastUpdated
    ) : LockState(Lock, LastUpdated);

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
        Inactive,
        Active,
        Unknown
    }
}
