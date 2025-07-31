using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Implementation of ILight that manages a KNX light device
    /// </summary>
    public class Light : KnxDevice<LightState, LightAddresses>, ILight
    {

        /// <summary>
        /// Creates a new Light instance
        /// </summary>
        /// <param name="id">Unique identifier (e.g., "L1.1")</param>
        /// <param name="name">Human-readable name</param>
        /// <param name="subGroup">KNX sub-group number</param>
        /// <param name="knxService">KNX service for communication</param>
        public Light(string id, string name, string subGroup, IKnxService knxService)
            : base(id, name, subGroup, knxService, TimeSpan.FromSeconds(5))
        {
        }

        protected override LightAddresses CreateAddresses()
        {
            // Calculate KNX addresses based on sub-group
            // Feedback address has 100 offset (e.g., control "11" -> feedback "111")
            var feedbackSubGroup = (int.Parse(SubGroup) + 100).ToString();
            return new LightAddresses(
                Control: KnxAddressConfiguration.CreateLightControlAddress(SubGroup),
                Feedback: KnxAddressConfiguration.CreateLightFeedbackAddress(feedbackSubGroup)
            );
        }

        protected override LightState CreateDefaultState()
        {
            // Initialize with default state
            return new LightState(
                IsOn: false,
                LastUpdated: DateTime.Now
            );
        }

        protected override async Task<LightState> ReadCurrentStateAsync()
        {
            var isOn = await ReadStateAsync();
            return new LightState(
                IsOn: isOn,
                LastUpdated: DateTime.Now
            );
        }

        protected override void ProcessKnxMessage(KnxGroupEventArgs e)
        {
            // Check if this message is relevant to our light and update state accordingly
            if (e.Destination == Addresses.Feedback)
            {
                var isOn = e.Value.AsBoolean();
                CurrentState = new LightState(IsOn: isOn, LastUpdated: DateTime.Now);
                Console.WriteLine($"Light {Id} state updated via feedback: {(isOn ? "ON" : "OFF")}");
            }
        }

        public override async Task RestoreSavedStateAsync()
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

        // Note: StartListeningToFeedback and StopListeningToFeedback are now handled by base class
        // Note: OnKnxGroupMessageReceived is now handled by ProcessKnxMessage in base class

        public override string ToString()
        {
            return $"Light {Id} ({Name}) - State: {(CurrentState.IsOn ? "ON" : "OFF")}";
        }

        // Note: Dispose is now handled by base class
    }
}
