using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Interface representing a KNX shutter with all its operations and state management
    /// </summary>
    public interface IShutter : IDisposable
    {
        /// <summary>
        /// Unique identifier for the shutter (e.g., "R1.1", "R6.2")
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Human-readable name of the shutter (e.g., "Bathroom", "Kinga's Room")
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The sub-group number used for KNX addresses (1-18)
        /// </summary>
        string SubGroup { get; }

        /// <summary>
        /// KNX addresses for shutter control and feedback
        /// </summary>
        ShutterAddresses Addresses { get; }

        /// <summary>
        /// Current state of the shutter
        /// </summary>
        ShutterState CurrentState { get; }

        /// <summary>
        /// Saved state for restoration after tests
        /// </summary>
        ShutterState? SavedState { get; }

        /// <summary>
        /// Initialize the shutter and read current state from KNX bus
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Save current state for later restoration
        /// </summary>
        Task SaveCurrentStateAsync();

        /// <summary>
        /// Restore previously saved state
        /// </summary>
        Task RestoreSavedStateAsync();

        /// <summary>
        /// Move shutter to absolute position (0-100%)
        /// </summary>
        /// <param name="position">Target position percentage (0-100)</param>
        /// <param name="timeout">Timeout for the operation</param>
        Task SetPositionAsync(int position, TimeSpan? timeout = null);

        /// <summary>
        /// Move shutter up or down
        /// </summary>
        /// <param name="direction">Movement direction</param>
        /// <param name="duration">How long to move (if null, moves until stopped)</param>
        Task MoveAsync(ShutterDirection direction, TimeSpan? duration = null);

        /// <summary>
        /// Stop shutter movement
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Set shutter lock state
        /// </summary>
        /// <param name="locked">True to lock, false to unlock</param>
        Task SetLockAsync(bool locked);

        /// <summary>
        /// Read current position from KNX bus
        /// </summary>
        Task<int> ReadPositionAsync();

        /// <summary>
        /// Read current lock state from KNX bus
        /// </summary>
        Task<bool> ReadLockStateAsync();

        /// <summary>
        /// Read current movement state from KNX bus
        /// </summary>
        Task<ShutterMovementState> ReadMovementStateAsync();

        /// <summary>
        /// Wait for shutter to reach target position
        /// </summary>
        /// <param name="targetPosition">Target position to wait for (0-100)</param>
        /// <param name="tolerance">Allowed deviation in percentage points</param>
        /// <param name="timeout">Maximum time to wait</param>
        Task<bool> WaitForPositionAsync(int targetPosition, double tolerance = 2.0, TimeSpan? timeout = null);

        /// <summary>
        /// Wait for shutter movement to stop
        /// </summary>
        /// <param name="timeout">Maximum time to wait</param>
        Task<bool> WaitForMovementStopAsync(TimeSpan? timeout = null);
    }

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
    );

    /// <summary>
    /// Current state of a shutter
    /// </summary>
    public record ShutterState(
        int Position,
        bool IsLocked,
        ShutterMovementState MovementState,
        DateTime LastUpdated
    );

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
