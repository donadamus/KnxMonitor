using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Implementation of ILight that manages a KNX light device
    /// </summary>
    public class Light : LockableKnxDevice<LightState, LightAddresses>, ILight
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
            return new LightAddresses(
                Control: KnxAddressConfiguration.CreateLightControlAddress(SubGroup),
                Feedback: KnxAddressConfiguration.CreateLightFeedbackAddress(SubGroup),
                LockControl: KnxAddressConfiguration.CreateLightLockAddress(SubGroup),
                LockFeedback: KnxAddressConfiguration.CreateLightLockFeedbackAddress(SubGroup)
            );
        }

        protected override LightState CreateDefaultState()
        {
            // Initialize with default state
            return new LightState(
                IsOn: false,
                IsLocked: false,
                LastUpdated: DateTime.Now
            );
        }

        protected override async Task<LightState> ReadCurrentStateAsync()
        {
            var isOn = await ReadStateAsync();
            var isLocked = await ReadLockStateAsync();
            return new LightState(
                IsOn: isOn,
                IsLocked: isLocked,
                LastUpdated: DateTime.Now
            );
        }

        protected override void ProcessDeviceSpecificMessage(KnxGroupEventArgs e)
        {
            // Handle light-specific (non-lock) messages
            if (e.Destination == Addresses.Feedback)
            {
                var isOn = e.Value.AsBoolean();
                CurrentState = new LightState(
                    IsOn: isOn, 
                    IsLocked: CurrentState.IsLocked, 
                    LastUpdated: DateTime.Now
                );
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

        #region LockableKnxDevice Implementation

        public override LightState UpdateLockState(bool isLocked) => 
            CurrentState with { IsLocked = isLocked, LastUpdated = DateTime.Now };

        #endregion

        public async Task SetStateAsync(bool isOn)
        {
            Console.WriteLine($"{(isOn ? "Turning ON" : "Turning OFF")} light {Id}");
            
            await SetBitFunctionAsync(
                Addresses.Control,
                isOn,
                () => CurrentState.IsOn == isOn
            );
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
            Console.WriteLine($"Waiting for light {Id} to become: {(targetState ? "ON" : "OFF")}");
            
            return await WaitForConditionAsync(
                condition: () => CurrentState.IsOn == targetState,
                timeout: timeout,
                description: $"state {(targetState ? "ON" : "OFF")}"
            );
        }

        private async Task RefreshCurrentStateAsync()
        {
            try
            {
                var isOn = await ReadStateAsync();
                var isLocked = await ReadLockStateAsync();

                CurrentState = new LightState(
                    IsOn: isOn,
                    IsLocked: isLocked,
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
            return $"Light {Id} ({Name}) - State: {(CurrentState.IsOn ? "ON" : "OFF")}, Lock: {(CurrentState.IsLocked ? "LOCKED" : "UNLOCKED")}";
        }

        // Note: Dispose is now handled by base class
    }
}
