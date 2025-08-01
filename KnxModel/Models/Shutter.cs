using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Implementation of IShutter that manages a KNX shutter device
    /// </summary>
    public class Shutter : LockableKnxDevice<ShutterState, ShutterAddresses>, IShutter
    {
        private readonly TimeSpan _defaultMoveTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Creates a new Shutter instance
        /// </summary>
        /// <param name="id">Unique identifier (e.g., "R1.1")</param>
        /// <param name="name">Human-readable name</param>
        /// <param name="subGroup">KNX sub-group number (1-18)</param>
        /// <param name="knxService">KNX service for communication</param>
        public Shutter(string id, string name, string subGroup, IKnxService knxService)
            : base(id, name, subGroup, knxService, TimeSpan.FromSeconds(30))
        {
        }

        protected override ShutterAddresses CreateAddresses()
        {
            // Calculate KNX addresses based on sub-group using centralized configuration
            return new ShutterAddresses(
                MovementControl: KnxAddressConfiguration.CreateShutterMovementAddress(SubGroup),
                MovementFeedback: KnxAddressConfiguration.CreateShutterMovementFeedbackAddress(SubGroup),
                PositionControl: KnxAddressConfiguration.CreateShutterPositionAddress(SubGroup),
                PositionFeedback: KnxAddressConfiguration.CreateShutterPositionFeedbackAddress(SubGroup),
                LockControl: KnxAddressConfiguration.CreateShutterLockAddress(SubGroup),
                LockFeedback: KnxAddressConfiguration.CreateShutterLockFeedbackAddress(SubGroup),
                StopControl: KnxAddressConfiguration.CreateShutterStopAddress(SubGroup),
                MovementStatusFeedback: KnxAddressConfiguration.CreateShutterMovementStatusFeedbackAddress(SubGroup)
            );
        }

        protected override ShutterState CreateDefaultState()
        {
            // Initialize with default state
            return new ShutterState(
                Position: 0.0f,
                IsLocked: false,
                MovementState: ShutterMovementState.Unknown,
                LastUpdated: DateTime.Now
            );
        }

        #region LockableKnxDevice Implementation

        protected override string GetLockControlAddress() => Addresses.LockControl;
        protected override string GetLockFeedbackAddress() => Addresses.LockFeedback;
        protected override bool GetCurrentLockState() => CurrentState.IsLocked;
        protected override ShutterState UpdateLockState(bool isLocked) => 
            CurrentState with { IsLocked = isLocked, LastUpdated = DateTime.Now };

        #endregion

        protected override async Task<ShutterState> ReadCurrentStateAsync()
        {
            var position = await ReadPositionAsync();
            var isLocked = await ReadLockStateAsync();
            var movementState = await ReadMovementStateAsync();

            return new ShutterState(
                Position: position,
                IsLocked: isLocked,
                MovementState: movementState,
                LastUpdated: DateTime.Now
            );
        }

        protected override void ProcessKnxMessage(KnxGroupEventArgs e)
        {
            // Check if this is a lock message first
            if (ProcessLockMessage(e.Destination, e.Value))
            {
                return; // Lock message was processed
            }

            // Handle non-lock messages
            if (e.Destination == Addresses.PositionFeedback)
            {
                var positionPercent = e.Value.AsPercentageValue();
                CurrentState = CurrentState with { Position = positionPercent, LastUpdated = DateTime.Now };
                Console.WriteLine($"Shutter {Id} position updated via feedback: {positionPercent}%");
            }
            else if (e.Destination == Addresses.MovementFeedback)
            {
                var isMoving = e.Value.AsBoolean();
                Console.WriteLine($"Shutter {Id} movement feedback: {(isMoving ? "Moving" : "Stopped")}");
                // Movement feedback can help us understand when movement starts/stops
            }
            else if (e.Destination == Addresses.MovementStatusFeedback)
            {
                var isActive = e.Value.AsBoolean(); // DataType 1.011: Inactive/Active
                var movementState = isActive ? ShutterMovementState.MovingUp : ShutterMovementState.Stopped;
                CurrentState = CurrentState with { MovementState = movementState, LastUpdated = DateTime.Now };
                Console.WriteLine($"Shutter {Id} movement state updated via feedback: {(isActive ? "Active" : "Inactive")} -> {movementState}");
            }
        }

        public override async Task RestoreSavedStateAsync()
        {
            if (SavedState == null)
            {
                throw new InvalidOperationException($"No saved state available for shutter {Id}. Call SaveCurrentStateAsync() first.");
            }

            Console.WriteLine($"Restoring shutter {Id} to saved state - Position: {SavedState.Position}%, Locked: {SavedState.IsLocked}");

            try
            {
                // First unlock if currently locked
                if (CurrentState.IsLocked && !SavedState.IsLocked)
                {
                    await SetLockAsync(false);
                    await Task.Delay(1000); // Wait for unlock
                }

                // Restore position (use mechanical precision tolerance)
                if (Math.Abs(CurrentState.Position - SavedState.Position) > 1.0)
                {
                    await SetPositionAsync(SavedState.Position);
                    await WaitForPositionAsync(SavedState.Position, tolerance: 1.0); // Match mechanical precision
                }

                // Restore lock state
                if (CurrentState.IsLocked != SavedState.IsLocked)
                {
                    await SetLockAsync(SavedState.IsLocked);
                }

                Console.WriteLine($"Shutter {Id} successfully restored to saved state");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to restore shutter {Id} state: {ex.Message}");
                throw;
            }
        }

        public async Task SetPositionAsync(float position, TimeSpan? timeout = null)
        {
            if (position < 0.0f || position > 100.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "Position must be between 0.0 and 100.0.");
            }
            
            var effectiveTimeout = timeout ?? _defaultTimeout;
            Console.WriteLine($"Setting shutter {Id} position to {position}%");

            _knxService.WriteGroupValue(Addresses.PositionControl, position);
            
            // Wait for position to be reached (considering relay delay and mechanical precision)
            // One byte step = 100/255 ≈ 0.39%, but mechanical systems may have 1-2 steps tolerance
            const double byteTolerancePercent = 1.0; // Allow for relay delay and mechanical precision
            var success = await WaitForPositionAsync(position, tolerance: byteTolerancePercent, timeout: effectiveTimeout);
            if (!success)
            {
                Console.WriteLine($"⚠️ WARNING: Shutter {Id} did not reach target position {position}% within {byteTolerancePercent}% tolerance");
            }
        }

        public async Task MoveAsync(ShutterDirection direction, TimeSpan? duration = null)
        {
            var isDown = direction == ShutterDirection.Down;
            Console.WriteLine($"Moving shutter {Id} {direction}");

            _knxService.WriteGroupValue(Addresses.MovementControl, isDown);

            if (duration.HasValue)
            {
                // Określony czas - czekaj i stop
                await Task.Delay(duration.Value);
                await StopAsync();
            }
            else
            {
                // Brak duration - czekaj aż ruch się zatrzyma automatycznie
                await WaitForMovementStopAsync();
            }
        }

        public async Task StopAsync()
        {
            Console.WriteLine($"Stopping shutter {Id}");
            _knxService.WriteGroupValue(Addresses.StopControl, true);
            
            // Wait for movement to actually stop (with reasonable timeout)
            var success = await WaitForMovementStopAsync(TimeSpan.FromSeconds(5));
            if (!success)
            {
                Console.WriteLine($"⚠️ WARNING: Shutter {Id} may not have stopped within expected time");
            }
        }

        public async Task<float> ReadPositionAsync()
        {
            try
            {
                return await _knxService.RequestGroupValue<float>(Addresses.PositionFeedback);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read position for shutter {Id}: {ex.Message}");
                throw;
            }
        }

        public async Task<ShutterMovementState> ReadMovementStateAsync()
        {
            try
            {
                var isActive = await _knxService.RequestGroupValue<bool>(Addresses.MovementStatusFeedback);
                
                return isActive ? ShutterMovementState.MovingUp : ShutterMovementState.Stopped;
                // Note: We can't distinguish UP/DOWN from this feedback alone
                // Would need additional feedback or state tracking for direction
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read movement state for shutter {Id}: {ex.Message}");
                return ShutterMovementState.Unknown;
            }
        }

        public async Task<bool> WaitForPositionAsync(float targetPosition, double tolerance = 2.0, TimeSpan? timeout = null)
        {
            if (targetPosition < 0.0f || targetPosition > 100.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(targetPosition), "Target position must be between 0.0 and 100.0.");
            }
            
            var effectiveTimeout = timeout ?? _defaultTimeout;
            Console.WriteLine($"Waiting for shutter {Id} to reach target {targetPosition}% (allowing for mechanical precision)");

            var lastMovementState = CurrentState.MovementState;

            // Create a task that completes when target position is reached OR movement stops
            var waitTask = Task.Run(async () =>
            {
                while (true)
                {
                    // Check if we reached target position within tolerance (considering byte precision)
                    if (Math.Abs(CurrentState.Position - targetPosition) <= tolerance)
                    {
                        var precision = Math.Abs(CurrentState.Position - targetPosition);
                        Console.WriteLine($"✅ Shutter {Id} reached target position: {CurrentState.Position}% (target: {targetPosition}%, precision: ±{precision:F2}%)");
                        return true;
                    }

                    // Check if movement stopped without reaching target
                    if (CurrentState.MovementState == ShutterMovementState.Stopped && 
                        lastMovementState != ShutterMovementState.Stopped)
                    {
                        Console.WriteLine($"❌ Shutter {Id} stopped at {CurrentState.Position}% before reaching target {targetPosition}%");
                        Console.WriteLine($"This may indicate: obstruction, manual stop, or hardware limit reached");
                        return false;
                    }

                    lastMovementState = CurrentState.MovementState;
                    await Task.Delay(_pollingIntervalMs); // Check every 200ms
                }
            });

            // Create timeout task
            var timeoutTask = Task.Delay(effectiveTimeout);

            // Wait for either position to be reached or timeout
            var completedTask = await Task.WhenAny(waitTask, timeoutTask);

            if (completedTask == waitTask)
            {
                return await waitTask; // Position reached or movement stopped
            }
            else
            {
                // Timeout occurred
                Console.WriteLine($"⚠️ WARNING: Shutter {Id} position timeout - target {targetPosition}%, current {CurrentState.Position}%");
                Console.WriteLine($"Movement state: {CurrentState.MovementState}");
                Console.WriteLine($"This may indicate: slow movement, missing position feedback, or hardware issue");
                return false;
            }
        }

        public async Task<bool> WaitForMovementStopAsync(TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? _defaultMoveTimeout;
            Console.WriteLine($"Waiting for shutter {Id} movement to stop");

            // Create a task that completes when movement stops
            var waitTask = Task.Run(async () =>
            {
                while (CurrentState.MovementState != ShutterMovementState.Stopped)
                {
                    await Task.Delay(_pollingIntervalMs); // Check internal state every 200ms
                }
                Console.WriteLine($"Shutter {Id} movement stopped via feedback");
                return true;
            });

            // Create timeout task
            var timeoutTask = Task.Delay(effectiveTimeout);

            // Wait for either movement to stop or timeout
            var completedTask = await Task.WhenAny(waitTask, timeoutTask);

            if (completedTask == waitTask)
            {
                return await waitTask; // Movement stopped
            }
            else
            {
                // Timeout occurred - indicates a problem!
                Console.WriteLine($"⚠️ WARNING: Shutter {Id} movement timeout - NO FEEDBACK RECEIVED!");
                Console.WriteLine($"This may indicate: missing feedback configuration, hardware issue, or communication problem");
                
                return false;
            }
        }

        /// <summary>
        /// Opens the shutter completely (0% position)
        /// </summary>
        /// <param name="timeout">Maximum time to wait for completion</param>
        public async Task OpenAsync(TimeSpan? timeout = null)
        {
            Console.WriteLine($"Opening shutter {Id} completely");
            await SetPositionAsync(0.0f, timeout);
        }

        /// <summary>
        /// Closes the shutter completely (100% position)
        /// </summary>
        /// <param name="timeout">Maximum time to wait for completion</param>
        public async Task CloseAsync(TimeSpan? timeout = null)
        {
            Console.WriteLine($"Closing shutter {Id} completely");
            await SetPositionAsync(100.0f, timeout);
        }

        /// <summary>
        /// Moves the shutter to a preset position (e.g., 50% for partial shade)
        /// </summary>
        /// <param name="presetName">Name of the preset for logging</param>
        /// <param name="position">Position percentage (0-100)</param>
        /// <param name="timeout">Maximum time to wait for completion</param>
        public async Task MoveToPresetAsync(string presetName, float position, TimeSpan? timeout = null)
        {
            Console.WriteLine($"Moving shutter {Id} to preset '{presetName}' ({position}%)");
            await SetPositionAsync(position, timeout);
        }

        private async Task RefreshCurrentStateAsync()
        {
            try
            {
                var position = await ReadPositionAsync();
                var isLocked = await ReadLockStateAsync();
                var movementState = await ReadMovementStateAsync();

                CurrentState = new ShutterState(
                    Position: position,
                    IsLocked: isLocked,
                    MovementState: movementState,
                    LastUpdated: DateTime.Now
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to refresh state for shutter {Id}: {ex.Message}");
                // Don't throw - just keep the last known state
            }
        }

        // Note: StartListeningToFeedback, StopListeningToFeedback, OnKnxGroupMessageReceived and Dispose are now handled by base class

        public override string ToString()
        {
            return $"Shutter {Id} ({Name}) - Position: {CurrentState.Position}%, Locked: {CurrentState.IsLocked}, Movement: {CurrentState.MovementState}";
        }
    }
}
