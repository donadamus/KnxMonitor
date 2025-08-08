using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Implementation of IShutter that manages a KNX shutter device
    /// </summary>
    public class ShutterOld : LockableKnxDeviceBase, IShutterOld
    {
        private static readonly TimeSpan _defaultMoveTimeout = TimeSpan.FromSeconds(30);
        private ShutterAddresses _addresses = null!; // Initialized in constructor
        protected ShutterState _currentState = null!; // Initialized in constructor
        protected ShutterState? _savedState;

        /// <summary>
        /// KNX addresses for shutter control and feedback
        /// </summary>
        public ShutterAddresses Addresses => _addresses;

        /// <summary>
        /// Current state of the shutter
        /// </summary>
        public ShutterState CurrentState
        {
            get => _currentState;
            protected set => _currentState = value;
        }

        /// <summary>
        /// Saved state for restoration after tests
        /// </summary>
        public ShutterState? SavedState
        {
            get => _savedState;
            protected set => _savedState = value;
        }

        /// <summary>
        /// Creates a new Shutter instance
        /// </summary>
        /// <param name="id">Unique identifier (e.g., "R1.1")</param>
        /// <param name="name">Human-readable name</param>
        /// <param name="subGroup">KNX sub-group number (1-18)</param>
        /// <param name="knxService">KNX service for communication</param>
        public ShutterOld(string id, string name, string subGroup, IKnxService knxService, TimeSpan? timeout = null)
            : base(id, name, subGroup, knxService, timeout == null ? _defaultMoveTimeout : timeout)
        {
            _addresses = CreateAddresses();
            _currentState = CreateDefaultState();
            StartListeningToFeedback();
        }

        protected virtual ShutterAddresses CreateAddresses()
        {
            // Calculate KNX addresses based on sub-group using centralized configuration
            return new ShutterAddresses(
                MovementControl: KnxAddressConfiguration.CreateShutterMovementAddress(SubGroup),
                MovementFeedback: KnxAddressConfiguration.CreateShutterMovementFeedbackAddress(SubGroup),
                PercentageControl: KnxAddressConfiguration.CreateShutterPositionAddress(SubGroup),
                PercentageFeedback: KnxAddressConfiguration.CreateShutterPositionFeedbackAddress(SubGroup),
                LockControl: KnxAddressConfiguration.CreateShutterLockAddress(SubGroup),
                LockFeedback: KnxAddressConfiguration.CreateShutterLockFeedbackAddress(SubGroup),
                StopControl: KnxAddressConfiguration.CreateShutterStopAddress(SubGroup),
                MovementStatusFeedback: KnxAddressConfiguration.CreateShutterMovementStatusFeedbackAddress(SubGroup)
            );
        }

        protected virtual ShutterState CreateDefaultState()
        {
            // Initialize with default state
            return new ShutterState(
                Position: 0.0f,
                Lock: Lock.Unknown,
                MovementState: ShutterMovementState.Unknown,
                LastUpdated: DateTime.Now
            );
        }

        public override async Task InitializeAsync()
        {
            try
            {
                CurrentState = await ReadCurrentStateAsync();
                Console.WriteLine($"Initialized shutter {Id}: {ToString()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize shutter {Id}, using default state: {ex.Message}");
                CurrentState = CreateDefaultState();
            }
        }

        public override void SaveCurrentState()
        {
            SavedState = CurrentState;
            Console.WriteLine($"Saved current state for shutter {Id} - Position: {CurrentState.Position}%, Lock: {CurrentState.Lock}");
        }

        public override async Task RestoreSavedStateAsync()
        {
            if (SavedState == null)
            {
                throw new InvalidOperationException($"No saved state available for shutter {Id}. Call SaveCurrentState() first.");
            }

            Console.WriteLine($"Restoring shutter {Id} to saved state - Position: {SavedState.Position}%, Lock: {SavedState.Lock}");

            try
            {
                // First restore shutter-specific state (position)
                if (Math.Abs(CurrentState.Position - SavedState.Position) > 1.0f) // Allow 1% tolerance
                {
                    // Check if device is currently locked - if so, unlock it temporarily to allow state changes
                    if (CurrentState.Lock == Lock.On)
                    {
                        Console.WriteLine($"Shutter {Id} is locked, temporarily unlocking to allow position restoration");
                        await SetLockAsync(Lock.Off);
                    }

                    await SetPositionAsync(SavedState.Position);
                    Console.WriteLine($"Shutter {Id} position restored to: {SavedState.Position}%");
                }

                // Then call base implementation to restore lock state
                await base.RestoreSavedStateAsync();

                Console.WriteLine($"Shutter {Id} successfully restored to saved state");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to restore shutter {Id} state: {ex.Message}");
                throw;
            }
        }

        #region Lock Implementation (inherited from LockableKnxDeviceBase)

        protected override LockState? GetSavedLockState() => SavedState;
        protected override LockState GetCurrentLockState() => CurrentState;
        protected override LockableAddresses GetLockableAddresses() => _addresses;

        protected override void UpdateCurrentStateLock(Lock lockState)
        {
            CurrentState = CurrentState with { Lock = lockState, LastUpdated = DateTime.Now };
        }

        public virtual ShutterState UpdateLockState(Lock lockState) => 
            CurrentState with { Lock = lockState, LastUpdated = DateTime.Now };

        #endregion

        protected virtual async Task<ShutterState> ReadCurrentStateAsync()
        {
            var position = await ReadPositionAsync();
            var isLocked = await ReadLockStateAsync();
            var movementState = await ReadMovementStateAsync();

            return new ShutterState(
                Position: position,
                Lock: isLocked,
                MovementState: movementState,
                LastUpdated: DateTime.Now
            );
        }

        protected override void ProcessDeviceSpecificMessage(KnxGroupEventArgs e)
        {
            // Handle shutter-specific (non-lock) messages
            if (e.Destination == Addresses.PercentageFeedback)
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
                var movementState = isActive ? ShutterMovementState.Active : ShutterMovementState.Inactive;
                CurrentState = CurrentState with { MovementState = movementState, LastUpdated = DateTime.Now };
                Console.WriteLine($"Shutter {Id} movement state updated via feedback: {(isActive ? "Active" : "Inactive")} -> {movementState}");
            }
        }

        protected override void ProcessKnxMessage(KnxGroupEventArgs e)
        {
            // Handle lock messages first
            if (e.Destination == Addresses.LockFeedback)
            {
                var isLocked = e.Value.AsBoolean();
                var lockState = isLocked ? Lock.On : Lock.Off;
                CurrentState = CurrentState with { Lock = lockState, LastUpdated = DateTime.Now };
                Console.WriteLine($"Shutter {Id} lock state updated via feedback: {(isLocked ? "LOCKED" : "UNLOCKED")}");
            }
            else
            {
                // Handle device-specific messages
                ProcessDeviceSpecificMessage(e);
            }
        }

        public async Task SetPositionAsync(float position, TimeSpan? timeout = null)
        {
            if (position < 0.0f || position > 100.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "Position must be between 0.0 and 100.0.");
            }
            
            Console.WriteLine($"Setting shutter {Id} position to {position}%");

            // Wait for position to be reached (considering relay delay and mechanical precision)
            // One byte step = 100/255 ≈ 0.39%, but mechanical systems may have 1-2 steps tolerance
            const float byteTolerancePercent = 1.0f; // Allow for relay delay and mechanical precision
            await SetFloatFunctionAsync(Addresses.PercentageControl, position, () => Math.Abs(CurrentState.Position - position) <= byteTolerancePercent, timeout);
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

        public async Task StopAsync(TimeSpan? timeout = null)
        {
            Console.WriteLine($"Stopping shutter {Id}");
            _knxService.WriteGroupValue(Addresses.StopControl, true);
            
            // Wait for movement to actually stop (with reasonable timeout)
            var success = await WaitForMovementStopAsync(timeout);
            if (!success)
            {
                Console.WriteLine($"⚠️ WARNING: Shutter {Id} may not have stopped within expected time");
            }
        }

        public async Task<float> ReadPositionAsync()
        {
            try
            {
                return await _knxService.RequestGroupValue<float>(Addresses.PercentageFeedback);
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
                
                return isActive ? ShutterMovementState.Active : ShutterMovementState.Inactive;
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
            
            Console.WriteLine($"Waiting for shutter {Id} to reach target {targetPosition}% (allowing for mechanical precision)");

            return await WaitForConditionAsync(
                condition: () => Math.Abs(CurrentState.Position - targetPosition) <= tolerance,
                timeout: timeout,
                description: $"position {targetPosition}% (tolerance ±{tolerance}%)"
            );
        }

        public async Task<bool> WaitForMovementStopAsync(TimeSpan? timeout = null)
        {
            Console.WriteLine($"Waiting for shutter {Id} movement to stop");
            
            return await WaitForConditionAsync(
                condition: () => CurrentState.MovementState == ShutterMovementState.Inactive,
                timeout: timeout ?? _defaultMoveTimeout,
                description: "movement to stop"
            );
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
                    Lock: isLocked,
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

        public override string ToString()
        {
            return $"Shutter {Id} ({Name}) - Position: {CurrentState.Position}%, Locked: {CurrentState.Lock}, Movement: {CurrentState.MovementState}";
        }

        public override void Dispose()
        {
            StopListeningToFeedback();
        }
    }
}
