using System;

namespace KnxModel
{
    // ... existing code ...

    public static class LockExtensions
    {
        public static Lock Opposite(this Lock lockState) => lockState switch
        {
            Lock.Off => Lock.On,
            Lock.On => Lock.Off,
            _ => Lock.Unknown
        };

        public static Switch Opposite(this Switch switchState) => switchState switch
        {
            Switch.Off => Switch.On,
            Switch.On => Switch.Off,
            _ => Switch.Unknown
        };

        public static Switch ToSwitch(this bool switchState) => switchState switch
        {
            true => Switch.On,
            false => Switch.Off
        };
         public static bool ToBool(this Switch switchState) => switchState switch
        {
            Switch.On => true,
            Switch.Off => false,
            _ => false
        };
   }

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
    ) : LockableAddresses(LockControl, LockFeedback), ISwitchableAddress;

public record LockableAddresses(
        string LockControl,
        string LockFeedback
    ) : ILockableAddress;

    public interface ISwitchableAddress
    {
        string Control { get; }
        string Feedback { get; }
    }

    public interface ILockableAddress
    {
        string LockControl { get; }
        string LockFeedback { get; }
    }
    public interface IPercentageControllableAddress
    {
        string PercantageControl { get; }
        string PercantageFeedback { get; }
    }
    public interface IBrightnessControllableAddress
    {
        string BrightnessControl { get; }
        string BrightnessFeedback { get; }
    }


    /// <summary>
    /// Current state of a KNX light
    /// </summary>
    /// <param name="IsOn">Whether the light is currently on</param>
    /// <param name="IsLocked">Whether the light is currently locked</param>
    /// <param name="LastUpdated">When the state was last updated</param>
    public record LightState(
        Switch Switch,
        Lock Lock,
        DateTime LastUpdated
    ) : LockState(Lock, LastUpdated);

    public record LockState(
        Lock Lock,
        DateTime LastUpdated
    ) : BaseState(LastUpdated), ILockableState;

    public record BaseState(
        DateTime LastUpdated
    );

    public interface ILockableState
    {
        Lock Lock { get; }
        DateTime LastUpdated { get; }
    }

    public enum Switch
    {
        Off,    // false in KNX
        On,     // true in KNX
        Unknown // state not known
    }
    public enum Lock
    {
        Off,    // false in KNX
        On,     // true in KNX
        Unknown // state not known
    }
}
