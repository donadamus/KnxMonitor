using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Implementation of IShutter that manages a KNX shutter device
    /// </summary>
    public class Shutter : IShutter
    {
        private readonly IKnxService _knxService;
        private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _defaultMoveTimeout = TimeSpan.FromSeconds(30);
        private bool _isListeningToFeedback = false;

        public string Id { get; }
        public string Name { get; }
        public string SubGroup { get; }
        public ShutterAddresses Addresses { get; }
        public ShutterState CurrentState { get; private set; }
        public ShutterState? SavedState { get; private set; }

        /// <summary>
        /// Creates a new Shutter instance
        /// </summary>
        /// <param name="id">Unique identifier (e.g., "R1.1")</param>
        /// <param name="name">Human-readable name</param>
        /// <param name="subGroup">KNX sub-group number (1-18)</param>
        /// <param name="knxService">KNX service for communication</param>
        public Shutter(string id, string name, string subGroup, IKnxService knxService)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SubGroup = subGroup ?? throw new ArgumentNullException(nameof(subGroup));
            _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));

            // Calculate KNX addresses based on sub-group using centralized configuration
            Addresses = new ShutterAddresses(
                MovementControl: KnxAddressConfiguration.CreateShutterMovementAddress(subGroup),
                MovementFeedback: KnxAddressConfiguration.CreateShutterMovementFeedbackAddress(subGroup),
                PositionControl: KnxAddressConfiguration.CreateShutterPositionAddress(subGroup),
                PositionFeedback: KnxAddressConfiguration.CreateShutterPositionFeedbackAddress(subGroup),
                LockControl: KnxAddressConfiguration.CreateShutterLockAddress(subGroup),
                LockFeedback: KnxAddressConfiguration.CreateShutterLockFeedbackAddress(subGroup),
                StopControl: KnxAddressConfiguration.CreateShutterStopAddress(subGroup),
                MovementStatusFeedback: KnxAddressConfiguration.CreateShutterMovementStatusFeedbackAddress(subGroup)
            );

            // Initialize with default state
            CurrentState = new ShutterState(
                Position: 0.0f,
                IsLocked: false,
                MovementState: ShutterMovementState.Unknown,
                LastUpdated: DateTime.Now
            );
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Start listening to feedback events
                StartListeningToFeedback();

                // Read initial state from KNX bus
                var position = await ReadPositionAsync();
                var isLocked = await ReadLockStateAsync();
                var movementState = await ReadMovementStateAsync();

                CurrentState = new ShutterState(
                    Position: position,
                    IsLocked: isLocked,
                    MovementState: movementState,
                    LastUpdated: DateTime.Now
                );

                Console.WriteLine($"Shutter {Id} ({Name}) initialized - Position: {position}%, Locked: {isLocked}, Movement: {movementState}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize shutter {Id}: {ex.Message}");
                throw;
            }
        }

        public async Task SaveCurrentStateAsync()
        {
            await RefreshCurrentStateAsync();
            SavedState = CurrentState;
            Console.WriteLine($"Shutter {Id} state saved - Position: {SavedState.Position}%, Locked: {SavedState.IsLocked}");
        }

        public async Task RestoreSavedStateAsync()
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

                // Restore position
                if (Math.Abs(CurrentState.Position - SavedState.Position) > 1.0)
                {
                    await SetPositionAsync(SavedState.Position);
                    await WaitForPositionAsync(SavedState.Position, tolerance: 3.0);
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

        public Task SetPositionAsync(float position, TimeSpan? timeout = null)
        {
            if (position < 0.0f || position > 100.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "Position must be between 0.0 and 100.0.");
            }
            
            var effectiveTimeout = timeout ?? _defaultTimeout;
            Console.WriteLine($"Setting shutter {Id} position to {position}%");

            _knxService.WriteGroupValue(Addresses.PositionControl, position);
            
            // Don't actively refresh - wait for feedback events
            return Task.CompletedTask;
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

        public Task StopAsync()
        {
            Console.WriteLine($"Stopping shutter {Id}");
            _knxService.WriteGroupValue(Addresses.StopControl, true);
            
            // Don't actively refresh - wait for feedback events
            return Task.CompletedTask;
        }

        public async Task SetLockAsync(bool locked)
        {
            Console.WriteLine($"{(locked ? "Locking" : "Unlocking")} shutter {Id}");
            _knxService.WriteGroupValue(Addresses.LockControl, locked);
            
            // Wait a moment for the command to be processed
            await Task.Delay(500);
            
            // Don't actively refresh - wait for feedback events
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

        public async Task<bool> ReadLockStateAsync()
        {
            try
            {
                return await _knxService.RequestGroupValue<bool>(Addresses.LockFeedback);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read lock state for shutter {Id}: {ex.Message}");
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
            Console.WriteLine($"Waiting for shutter {Id} to reach {targetPosition}% (tolerance: ±{tolerance:F1}%)");

            // Create a task that completes when target position is reached
            var waitTask = Task.Run(async () =>
            {
                while (true)
                {
                    // Check if we're close enough to target position using CurrentState (updated via feedback)
                    var difference = Math.Abs(CurrentState.Position - targetPosition);
                    if (difference <= tolerance)
                    {
                        Console.WriteLine($"Shutter {Id} reached target position: {CurrentState.Position}% (target: {targetPosition}%)");
                        await RefreshCurrentStateAsync(); // Final state update
                        return true;
                    }

                    await Task.Delay(200); // Check every 200ms
                }
            });

            // Create timeout task
            var timeoutTask = Task.Delay(effectiveTimeout);

            // Wait for either position to be reached or timeout
            var completedTask = await Task.WhenAny(waitTask, timeoutTask);

            if (completedTask == waitTask)
            {
                return await waitTask; // Position reached
            }
            else
            {
                // Timeout occurred
                Console.WriteLine($"⚠️ WARNING: Shutter {Id} position timeout - target {targetPosition}%, current {CurrentState.Position}%");
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
                    await Task.Delay(200); // Check internal state every 200ms
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
                
                // Optionally: throw exception to make the problem explicit
                // throw new TimeoutException($"Shutter {Id} feedback timeout - possible installation issue");
                
                return false;
            }
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

        private void StartListeningToFeedback()
        {
            if (_isListeningToFeedback)
                return;

            _isListeningToFeedback = true;
            
            // Subscribe to KNX group messages
            _knxService.GroupMessageReceived += OnKnxGroupMessageReceived;

            Console.WriteLine($"Started listening to feedback for shutter {Id}");
        }

        private void StopListeningToFeedback()
        {
            if (!_isListeningToFeedback)
                return;

            _isListeningToFeedback = false;

            // Unsubscribe from KNX group messages
            _knxService.GroupMessageReceived -= OnKnxGroupMessageReceived;

            Console.WriteLine($"Stopped listening to feedback for shutter {Id}");
        }

        private void OnKnxGroupMessageReceived(object? sender, KnxGroupEventArgs e)
        {
            try
            {
                // Check if this message is relevant to our shutter
                if (e.Destination == Addresses.PositionFeedback)
                {
                    OnPositionFeedback(e.Destination, e.Value);
                }
                else if (e.Destination == Addresses.LockFeedback)
                {
                    OnLockFeedback(e.Destination, e.Value);
                }
                else if (e.Destination == Addresses.MovementFeedback)
                {
                    OnMovementFeedback(e.Destination, e.Value);
                }
                else if (e.Destination == Addresses.MovementStatusFeedback)
                {
                    OnMovementStatusFeedback(e.Destination, e.Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing KNX message for shutter {Id}: {ex.Message}");
            }
        }

        private void OnPositionFeedback(string address, KnxValue value)
        {
            try
            {
                var positionPercent = value.AsPercentageValue();
                var updatedState = new ShutterState(
                    Position: positionPercent,
                    IsLocked: CurrentState.IsLocked,
                    MovementState: CurrentState.MovementState,
                    LastUpdated: DateTime.Now
                );
                CurrentState = updatedState;
                Console.WriteLine($"Shutter {Id} position updated via feedback: {positionPercent}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing position feedback for shutter {Id}: {ex.Message}");
            }
        }

        private void OnLockFeedback(string address, KnxValue value)
        {
            try
            {
                var isLocked = value.AsBoolean();
                var updatedState = new ShutterState(
                    Position: CurrentState.Position,
                    IsLocked: isLocked,
                    MovementState: CurrentState.MovementState,
                    LastUpdated: DateTime.Now
                );
                CurrentState = updatedState;
                Console.WriteLine($"Shutter {Id} lock state updated via feedback: {(isLocked ? "Locked" : "Unlocked")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing lock feedback for shutter {Id}: {ex.Message}");
            }
        }

        private void OnMovementFeedback(string address, KnxValue value)
        {
            try
            {
                var isMoving = value.AsBoolean();
                Console.WriteLine($"Shutter {Id} movement feedback: {(isMoving ? "Moving" : "Stopped")}");
                // Movement feedback can help us understand when movement starts/stops
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing movement feedback for shutter {Id}: {ex.Message}");
            }
        }

        private void OnMovementStatusFeedback(string address, KnxValue value)
        {
            try
            {
                var isActive = value.AsBoolean(); // DataType 1.011: Inactive/Active
                var movementState = isActive ? ShutterMovementState.MovingUp : ShutterMovementState.Stopped;
                // Note: We can't distinguish UP/DOWN from this feedback alone

                var updatedState = new ShutterState(
                    Position: CurrentState.Position,
                    IsLocked: CurrentState.IsLocked,
                    MovementState: movementState,
                    LastUpdated: DateTime.Now
                );
                CurrentState = updatedState;
                Console.WriteLine($"Shutter {Id} movement state updated via feedback: {(isActive ? "Active" : "Inactive")} -> {movementState}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing movement status feedback for shutter {Id}: {ex.Message}");
            }
        }

        public override string ToString()
        {
            return $"Shutter {Id} ({Name}) - Position: {CurrentState.Position}%, Locked: {CurrentState.IsLocked}, Movement: {CurrentState.MovementState}";
        }

        public void Dispose()
        {
            StopListeningToFeedback();
        }
    }
}
