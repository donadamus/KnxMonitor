using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Interface representing a KNX shutter with all its operations and state management
    /// </summary>
    public interface IShutter : IKnxDevice, ILockable
    {

        /// <summary>
        /// KNX addresses for shutter control and feedback (overrides ILockable.Addresses)
        /// </summary>
        new ShutterAddresses Addresses { get; }

        /// <summary>
        /// Current state of the shutter (overrides ILockable.CurrentState)
        /// </summary>
        new ShutterState CurrentState { get; }

        /// <summary>
        /// Saved state for restoration after tests (overrides ILockable.SavedState)
        /// </summary>
        new ShutterState? SavedState { get; }

        /// <summary>
        /// Move shutter to absolute position (0.0-100.0%)
        /// </summary>
        /// <param name="position">Target position percentage (0.0-100.0)</param>
        /// <param name="timeout">Timeout for the operation</param>
        Task SetPositionAsync(float position, TimeSpan? timeout = null);

        /// <summary>
        /// Move shutter up or down
        /// </summary>
        /// <param name="direction">Movement direction</param>
        /// <param name="duration">How long to move (if null, moves until stopped)</param>
        Task MoveAsync(ShutterDirection direction, TimeSpan? duration = null);

        /// <summary>
        /// Stop shutter movement
        /// </summary>
        Task StopAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Read current position from KNX bus
        /// </summary>
        Task<float> ReadPositionAsync();

        /// <summary>
        /// Read current movement state from KNX bus
        /// </summary>
        Task<ShutterMovementState> ReadMovementStateAsync();

        /// <summary>
        /// Wait for shutter to reach target position
        /// </summary>
        /// <param name="targetPosition">Target position to wait for (0.0-100.0)</param>
        /// <param name="tolerance">Allowed deviation in percentage points</param>
        /// <param name="timeout">Maximum time to wait</param>
        Task<bool> WaitForPositionAsync(float targetPosition, double tolerance = 2.0, TimeSpan? timeout = null);

        /// <summary>
        /// Wait for shutter movement to stop
        /// </summary>
        /// <param name="timeout">Maximum time to wait</param>
        Task<bool> WaitForMovementStopAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Open shutter completely (0% position)
        /// </summary>
        /// <param name="timeout">Maximum time to wait</param>
        Task OpenAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Close shutter completely (100% position)
        /// </summary>
        /// <param name="timeout">Maximum time to wait</param>
        Task CloseAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Move shutter to a preset position
        /// </summary>
        /// <param name="presetName">Name of the preset for logging</param>
        /// <param name="position">Position percentage (0-100)</param>
        /// <param name="timeout">Maximum time to wait</param>
        Task MoveToPresetAsync(string presetName, float position, TimeSpan? timeout = null);
    }
}
