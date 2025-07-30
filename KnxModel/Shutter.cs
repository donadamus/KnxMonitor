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
        private readonly TimeSpan _defaultMoveTimeout = TimeSpan.FromSeconds(10);
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

            // Calculate KNX addresses based on sub-group
            var feedbackSubGroup = (int.Parse(subGroup) + 100).ToString();
            Addresses = new ShutterAddresses(
                MovementControl: $"4/0/{subGroup}",
                MovementFeedback: $"4/0/{feedbackSubGroup}",
                PositionControl: $"4/2/{subGroup}",
                PositionFeedback: $"4/2/{feedbackSubGroup}",
                LockControl: $"4/3/{subGroup}",
                LockFeedback: $"4/3/{feedbackSubGroup}",
                StopControl: $"4/1/{subGroup}",
                MovementStatusFeedback: $"4/1/{feedbackSubGroup}"
            );

            // Initialize with default state
            CurrentState = new ShutterState(
                Position: Percent.FromPercantage(0),
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

                Console.WriteLine($"Shutter {Id} ({Name}) initialized - Position: {position.Value:F1}%, Locked: {isLocked}, Movement: {movementState}");
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
            Console.WriteLine($"Shutter {Id} state saved - Position: {SavedState.Position.Value:F1}%, Locked: {SavedState.IsLocked}");
        }

        public async Task RestoreSavedStateAsync()
        {
            if (SavedState == null)
            {
                throw new InvalidOperationException($"No saved state available for shutter {Id}. Call SaveCurrentStateAsync() first.");
            }

            Console.WriteLine($"Restoring shutter {Id} to saved state - Position: {SavedState.Position.Value:F1}%, Locked: {SavedState.IsLocked}");

            try
            {
                // First unlock if currently locked
                if (CurrentState.IsLocked && !SavedState.IsLocked)
                {
                    await SetLockAsync(false);
                    await Task.Delay(1000); // Wait for unlock
                }

                // Restore position
                if (Math.Abs(CurrentState.Position.Value - SavedState.Position.Value) > 1.0)
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

        public Task SetPositionAsync(Percent position, TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? _defaultTimeout;
            Console.WriteLine($"Setting shutter {Id} position to {position.Value:F1}%");

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
                await Task.Delay(duration.Value);
                await StopAsync();
            }

            // Don't actively refresh - wait for feedback events
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

        public async Task<Percent> ReadPositionAsync()
        {
            try
            {
                return await _knxService.RequestGroupValue<Percent>(Addresses.PositionFeedback);
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
                var result = await _knxService.RequestGroupValue(Addresses.LockFeedback);
                return result == "1";
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
                var result = await _knxService.RequestGroupValue(Addresses.MovementStatusFeedback);
                return result switch
                {
                    "0" => ShutterMovementState.Stopped,
                    "1" => ShutterMovementState.MovingUp, // Assuming 1 = moving up
                    "2" => ShutterMovementState.MovingDown, // Assuming 2 = moving down
                    _ => ShutterMovementState.Unknown
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read movement state for shutter {Id}: {ex.Message}");
                return ShutterMovementState.Unknown;
            }
        }

        public async Task<bool> WaitForPositionAsync(Percent targetPosition, double tolerance = 2.0, TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? _defaultTimeout;
            var startTime = DateTime.Now;
            var targetValue = targetPosition.Value;

            Console.WriteLine($"Waiting for shutter {Id} to reach {targetValue:F1}% (tolerance: ±{tolerance:F1}%)");

            while (DateTime.Now - startTime < effectiveTimeout)
            {
                try
                {
                    var currentPosition = await ReadPositionAsync();
                    var difference = Math.Abs(currentPosition.Value - targetValue);

                    if (difference <= tolerance)
                    {
                        Console.WriteLine($"Shutter {Id} reached target position: {currentPosition.Value:F1}%");
                        await RefreshCurrentStateAsync();
                        return true;
                    }

                    await Task.Delay(1000); // Check every second
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while waiting for position: {ex.Message}");
                    await Task.Delay(1000);
                }
            }

            Console.WriteLine($"Timeout waiting for shutter {Id} to reach position {targetValue:F1}%");
            return false;
        }

        public async Task<bool> WaitForMovementStopAsync(TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? _defaultMoveTimeout;
            var startTime = DateTime.Now;

            Console.WriteLine($"Waiting for shutter {Id} movement to stop");

            while (DateTime.Now - startTime < effectiveTimeout)
            {
                try
                {
                    var movementState = await ReadMovementStateAsync();
                    if (movementState == ShutterMovementState.Stopped)
                    {
                        Console.WriteLine($"Shutter {Id} movement stopped");
                        await RefreshCurrentStateAsync();
                        return true;
                    }

                    await Task.Delay(500); // Check every half second
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while waiting for movement stop: {ex.Message}");
                    await Task.Delay(500);
                }
            }

            Console.WriteLine($"Timeout waiting for shutter {Id} movement to stop");
            return false;
        }

        public async Task<bool> TestLockFunctionalityAsync(TimeSpan? testDuration = null)
        {
            var duration = testDuration ?? TimeSpan.FromSeconds(5);
            Console.WriteLine($"Testing lock functionality for shutter {Id}");

            // Save current state
            var initialPosition = await ReadPositionAsync();
            var initialLockState = await ReadLockStateAsync();

            try
            {
                // Ensure shutter is locked
                if (!initialLockState)
                {
                    await SetLockAsync(true);
                }

                // Try to move the shutter
                Console.WriteLine($"Attempting to move locked shutter {Id}");
                await MoveAsync(ShutterDirection.Up, duration);
                
                // Check if position changed
                var positionAfterMove = await ReadPositionAsync();
                var positionDifference = Math.Abs(positionAfterMove.Value - initialPosition.Value);

                var lockEffective = positionDifference < 1.0; // Less than 1% movement means lock is effective

                if (lockEffective)
                {
                    Console.WriteLine($"✓ Lock test passed for shutter {Id} - no movement detected ({positionDifference:F1}% change)");
                }
                else
                {
                    Console.WriteLine($"✗ Lock test failed for shutter {Id} - movement detected ({positionDifference:F1}% change)");
                }

                return lockEffective;
            }
            finally
            {
                // Restore original lock state
                await SetLockAsync(initialLockState);
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
                var percent = value.AsPercent();
                var updatedState = new ShutterState(
                    Position: percent,
                    IsLocked: CurrentState.IsLocked,
                    MovementState: CurrentState.MovementState,
                    LastUpdated: DateTime.Now
                );
                CurrentState = updatedState;
                Console.WriteLine($"Shutter {Id} position updated via feedback: {percent.Value:F1}%");
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
                var rawValue = value.AsByte();
                var movementState = rawValue switch
                {
                    0 => ShutterMovementState.Stopped,
                    1 => ShutterMovementState.MovingUp,
                    2 => ShutterMovementState.MovingDown,
                    _ => ShutterMovementState.Unknown
                };

                var updatedState = new ShutterState(
                    Position: CurrentState.Position,
                    IsLocked: CurrentState.IsLocked,
                    MovementState: movementState,
                    LastUpdated: DateTime.Now
                );
                CurrentState = updatedState;
                Console.WriteLine($"Shutter {Id} movement state updated via feedback: {movementState}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing movement status feedback for shutter {Id}: {ex.Message}");
            }
        }

        public override string ToString()
        {
            return $"Shutter {Id} ({Name}) - Position: {CurrentState.Position.Value:F1}%, Locked: {CurrentState.IsLocked}, Movement: {CurrentState.MovementState}";
        }

        public void Dispose()
        {
            StopListeningToFeedback();
        }
    }
}
