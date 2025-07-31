using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Implementation of ILight that manages a KNX light device
    /// </summary>
    public class Light : ILight
    {
        private readonly IKnxService _knxService;
        private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5);
        private const int _pollingIntervalMs = 50; // Polling interval for wait operations
        private bool _isListeningToFeedback = false;

        public string Id { get; }
        public string Name { get; }
        public string SubGroup { get; }
        public LightAddresses Addresses { get; }
        public LightState CurrentState { get; private set; }
        public LightState? SavedState { get; private set; }

        /// <summary>
        /// Creates a new Light instance
        /// </summary>
        /// <param name="id">Unique identifier (e.g., "L1.1")</param>
        /// <param name="name">Human-readable name</param>
        /// <param name="subGroup">KNX sub-group number</param>
        /// <param name="knxService">KNX service for communication</param>
        public Light(string id, string name, string subGroup, IKnxService knxService)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SubGroup = subGroup ?? throw new ArgumentNullException(nameof(subGroup));
            _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));

            // Calculate KNX addresses based on sub-group
            // Feedback address has 100 offset (e.g., control "11" -> feedback "111")
            var feedbackSubGroup = (int.Parse(subGroup) + 100).ToString();
            Addresses = new LightAddresses(
                Control: KnxAddressConfiguration.CreateLightControlAddress(subGroup),
                Feedback: KnxAddressConfiguration.CreateLightFeedbackAddress(feedbackSubGroup)
            );

            // Initialize with default state
            CurrentState = new LightState(
                IsOn: false,
                LastUpdated: DateTime.Now
            );
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Start listening to feedback events
                StartListeningToFeedback();

                Console.WriteLine($"Light {Id} ({Name}) before initializing - State: {(CurrentState.IsOn ? "ON" : "OFF")}");

                // Read initial state from KNX bus
                var isOn = await ReadStateAsync();

                //CurrentState = new LightState(
                //    IsOn: isOn,
                //    LastUpdated: DateTime.Now
                //);

                Console.WriteLine($"Light {Id} ({Name}) initialized - State: {(isOn ? "ON" : "OFF")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize light {Id}: {ex.Message}");
                throw;
            }
        }

        public void SaveCurrentState()
        {
            SavedState = CurrentState;
            Console.WriteLine($"Light {Id} state saved - State: {(SavedState.IsOn ? "ON" : "OFF")}");
        }

        public async Task RestoreSavedStateAsync()
        {
            if (SavedState == null)
            {
                throw new InvalidOperationException($"No saved state available for light {Id}. Call SaveCurrentStateAsync() first.");
            }

            Console.WriteLine($"Restoring light {Id} to saved state - State: {(SavedState.IsOn ? "ON" : "OFF")}");

            try
            {
                // Restore state
                if (CurrentState.IsOn != SavedState.IsOn)
                {
                    await SetStateAsync(SavedState.IsOn);
                }

                Console.WriteLine($"Light {Id} successfully restored to saved state");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to restore light {Id} state: {ex.Message}");
                throw;
            }
        }

        public async Task SetStateAsync(bool isOn)
        {
            Console.WriteLine($"{(isOn ? "Turning ON" : "Turning OFF")} light {Id}");
            _knxService.WriteGroupValue(Addresses.Control, isOn);
            
            // Wait for state change to be confirmed via feedback
            var timeout = TimeSpan.FromSeconds(5);
            
            // Create a task that completes when target state is reached
            var waitTask = Task.Run(async () =>
            {
                while (true)
                {
                    if (CurrentState.IsOn == isOn)
                    {
                        Console.WriteLine($"✅ Light {Id} state confirmed: {(isOn ? "ON" : "OFF")}");
                        return true;
                    }
                    await Task.Delay(_pollingIntervalMs); // Check every 50ms
                }
            });

            // Create timeout task
            var timeoutTask = Task.Delay(timeout);

            // Wait for either state to be reached or timeout
            var completedTask = await Task.WhenAny(waitTask, timeoutTask);

            if (completedTask == waitTask)
            {
                await waitTask; // State reached
            }
            else
            {
                // Timeout occurred
                Console.WriteLine($"⚠️ WARNING: Light {Id} state change not confirmed within timeout");
            }
        }

        public async Task TurnOnAsync()
        {
            await SetStateAsync(true);
        }

        public async Task TurnOffAsync()
        {
            await SetStateAsync(false);
        }

        public async Task ToggleAsync()
        {
            var newState = !CurrentState.IsOn;
            Console.WriteLine($"Toggling light {Id} to {(newState ? "ON" : "OFF")}");
            await SetStateAsync(newState);
        }

        public async Task<bool> ReadStateAsync()
        {
            try
            {
                return await _knxService.RequestGroupValue<bool>(Addresses.Feedback);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read state for light {Id}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> WaitForStateAsync(bool targetState, TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? _defaultTimeout;
            Console.WriteLine($"Waiting for light {Id} to become: {(targetState ? "ON" : "OFF")}");

            // Create a task that completes when target state is reached
            var waitTask = Task.Run(async () =>
            {
                while (true)
                {
                    if (CurrentState.IsOn == targetState)
                    {
                        Console.WriteLine($"✅ Light {Id} state achieved: {(targetState ? "ON" : "OFF")}");
                        return true;
                    }

                    await Task.Delay(_pollingIntervalMs); // Check every 50ms
                }
            });

            // Create timeout task
            var timeoutTask = Task.Delay(effectiveTimeout);

            // Wait for either state to be reached or timeout
            var completedTask = await Task.WhenAny(waitTask, timeoutTask);

            if (completedTask == waitTask)
            {
                return await waitTask; // State reached
            }
            else
            {
                // Timeout occurred
                Console.WriteLine($"⚠️ WARNING: Light {Id} state timeout - expected {(targetState ? "ON" : "OFF")}, current {(CurrentState.IsOn ? "ON" : "OFF")}");
                Console.WriteLine($"This may indicate: missing feedback or hardware communication issue");
                return false;
            }
        }

        private async Task RefreshCurrentStateAsync()
        {
            try
            {
                var isOn = await ReadStateAsync();

                CurrentState = new LightState(
                    IsOn: isOn,
                    LastUpdated: DateTime.Now
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to refresh state for light {Id}: {ex.Message}");
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

            Console.WriteLine($"Started listening to feedback for light {Id}");
        }

        private void StopListeningToFeedback()
        {
            if (!_isListeningToFeedback)
                return;

            _isListeningToFeedback = false;

            // Unsubscribe from KNX group messages
            _knxService.GroupMessageReceived -= OnKnxGroupMessageReceived;

            Console.WriteLine($"Stopped listening to feedback for light {Id}");
        }

        private void OnKnxGroupMessageReceived(object? sender, KnxGroupEventArgs e)
        {
            try
            {
                // Check if this message is relevant to our light and update state accordingly
                if (e.Destination == Addresses.Feedback)
                {
                    var isOn = e.Value.AsBoolean();
                    CurrentState = CurrentState with { IsOn = isOn, LastUpdated = DateTime.Now };
                    Console.WriteLine($"Light {Id} state updated via feedback: {(isOn ? "ON" : "OFF")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing KNX message for light {Id}: {ex.Message}");
            }
        }

        public override string ToString()
        {
            return $"Light {Id} ({Name}) - State: {(CurrentState.IsOn ? "ON" : "OFF")}";
        }

        public void Dispose()
        {
            StopListeningToFeedback();
        }
    }
}
