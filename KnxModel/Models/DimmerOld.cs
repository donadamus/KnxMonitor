using System;
using System.Threading;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Implementation of IDimmer that manages a KNX dimmer device
    /// Extends Light functionality with brightness control
    /// </summary>
    public class DimmerOld : LightOld, IDimmerOld
    {
        private static new readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);

        private DimmerAddresses _dimmerAddresses = null!; // Initialized in CreateAddresses() called by base constructor

        /// <summary>
        /// Gets the dimmer-specific addresses
        /// </summary>
        public new DimmerAddresses Addresses => _dimmerAddresses;

        /// <summary>
        /// Gets the current dimmer state
        /// </summary>
        public new DimmerState CurrentState 
        { 
            get => (DimmerState)base.CurrentState; 
            protected set => base.CurrentState = value; 
        }

        /// <summary>
        /// Gets the saved dimmer state
        /// </summary>
        public new DimmerState? SavedState 
        { 
            get => (DimmerState?)base.SavedState; 
            protected set => base.SavedState = value; 
        }

        /// <summary>
        /// Creates a new Dimmer instance
        /// </summary>
        /// <param name="id">Unique identifier (e.g., "DIM1")</param>
        /// <param name="name">Human-readable name</param>
        /// <param name="subGroup">KNX sub-group number</param>
        /// <param name="knxService">KNX service for communication</param>
        public DimmerOld(string id, string name, string subGroup, IKnxService knxService, TimeSpan? timeout = null)
            : base(id, name, subGroup, knxService, timeout == null ? _defaultTimeout : timeout)
        {
        }

        protected override LightAddresses CreateAddresses()
        {
            // Create and store dimmer addresses
            return _dimmerAddresses = new DimmerAddresses(
                SwitchControl: KnxAddressConfiguration.CreateDimmerSwitchControlAddress(SubGroup),
                SwitchFeedback: KnxAddressConfiguration.CreateDimmerSwitchFeedbackAddress(SubGroup),
                BrightnessControl: KnxAddressConfiguration.CreateDimmerBrightnessControlAddress(SubGroup),
                BrightnessFeedback: KnxAddressConfiguration.CreateDimmerBrightnessFeedbackAddress(SubGroup),
                LockControl: KnxAddressConfiguration.CreateDimmerLockAddress(SubGroup),
                LockFeedback: KnxAddressConfiguration.CreateDimmerLockFeedbackAddress(SubGroup)
            );
        }

        protected override LightState CreateDefaultState()
        {
            // Return DimmerState which inherits from LightState  
            return new DimmerState(
                Switch: Switch.Unknown,
                Brightness: 0,
                Lock: Lock.Unknown,
                LastUpdated: DateTime.Now
            );
        }

        protected override async Task<LightState> ReadCurrentStateAsync()
        {
            // Read the full dimmer state and return it (DimmerState inherits from LightState)
            return await ReadCurrentDimmerStateAsync();
        }

        private async Task<DimmerState> ReadCurrentDimmerStateAsync()
        {
            var switchState = await ReadStateAsync();
            var brightness = await ReadBrightnessAsync();
            var lockState = await ReadLockStateAsync();
            return new DimmerState(
                Switch: switchState,
                Brightness: brightness,
                Lock: lockState,
                LastUpdated: DateTime.Now
            );
        }

        protected override void ProcessDeviceSpecificMessage(KnxGroupEventArgs e)
        {
            // Handle dimmer-specific messages
            if (e.Destination == _dimmerAddresses.SwitchFeedback)
            {
                var switchState = e.Value.AsBoolean().ToSwitch();
                CurrentState = CurrentState with { 
                    Switch = switchState, 
                    LastUpdated = DateTime.Now 
                };
                Console.WriteLine($"Dimmer {Id} switch state updated via feedback: {switchState}");
            }
            else if (e.Destination == _dimmerAddresses.BrightnessFeedback)
            {
                var brightness = e.Value.AsPercentageValue();
                var isOn = brightness > 0;
                CurrentState = CurrentState with { 
                    Switch = isOn.ToSwitch(),
                    Brightness = brightness, 
                    LastUpdated = DateTime.Now 
                };
                Console.WriteLine($"Dimmer {Id} brightness updated via feedback: {brightness}%");
            }
            else
            {
                // Handle lock messages through base class
                base.ProcessDeviceSpecificMessage(e);
            }
        }

        protected override void SaveCurrentStateMessage()
        {
            Console.WriteLine($"Saved current state for dimmer {Id} - State: {CurrentState.Switch}, Brightness: {CurrentState.Brightness}%, Lock: {CurrentState.Lock}");
        }

        protected override void RestoreSavedStateMessage()
        {
            Console.WriteLine($"Restoring dimmer {Id} to saved state - State: {SavedState!.Switch}, Brightness: {SavedState.Brightness}%, Lock: {SavedState.Lock}");
        }

        protected override async Task PerformStateRestoration()
        {
            // Restore brightness first (this will also handle on/off state)
            if (CurrentState.Brightness != SavedState!.Brightness)
            {
                // Check if device is currently locked - if so, unlock it temporarily to allow state changes
                if (CurrentState.Lock == Lock.On)
                {
                    Console.WriteLine($"Dimmer {Id} is locked, temporarily unlocking to allow brightness restoration");
                    await SetLockAsync(Lock.Off);
                }

                await SetBrightnessAsync(SavedState.Brightness);
                Console.WriteLine($"Dimmer {Id} brightness restored to: {SavedState.Brightness}%");
            }
        }

        public override async Task RestoreSavedStateAsync()
        {
            if (SavedState == null)
            {
                throw new InvalidOperationException($"No saved state available for dimmer {Id}. Call SaveCurrentState() first.");
            }

            RestoreSavedStateMessage();

            try
            {
                // First restore dimmer-specific state (brightness)
                await PerformStateRestoration();
                
                // Then call base implementation to restore switch and lock states
                await base.RestoreSavedStateAsync();
                
                RestoreSuccessMessage();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to restore dimmer {Id} state: {ex.Message}");
                throw;
            }
        }

        protected override void RestoreSuccessMessage()
        {
            Console.WriteLine($"Dimmer {Id} successfully restored to saved state");
        }

        #region Brightness Control

        public async Task SetBrightnessAsync(float brightness, TimeSpan? timespan = null)
        {
            if (brightness < 0 || brightness > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(brightness), "Brightness must be between 0 and 100");
            }

            Console.WriteLine($"Setting dimmer {Id} brightness to {brightness}%");

            // Use brightness as float directly (0-100) - KnxService converts to KNX byte range
            await SetFloatFunctionAsync(
                _dimmerAddresses.BrightnessControl,
                (float)brightness,
                () => Math.Abs(CurrentState.Brightness - brightness) <= 1, // Allow 1% tolerance
                timespan
            );
        }

        public async Task<float> ReadBrightnessAsync()
        {
            try
            {
                // RequestGroupValue<int> will handle KNX byte (0-255) to percentage (0-100) conversion
                return await _knxService.RequestGroupValue<float>(_dimmerAddresses.BrightnessFeedback);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read brightness for dimmer {Id}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> WaitForBrightnessAsync(float targetBrightness, TimeSpan? timeout = null)
        {
            Console.WriteLine($"Waiting for dimmer {Id} brightness to become: {targetBrightness}%");
            
            return await WaitForConditionAsync(
                condition: () => Math.Abs(CurrentState.Brightness - targetBrightness) <= 1, // Allow 1% tolerance
                timeout: timeout,
                description: $"brightness {targetBrightness}%"
            );
        }

        public async Task FadeToAsync(float targetBrightness, TimeSpan duration)
        {
            if (targetBrightness < 0 || targetBrightness > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(targetBrightness), "Target brightness must be between 0 and 100");
            }

            Console.WriteLine($"Fading dimmer {Id} to {targetBrightness}% over {duration.TotalSeconds:F1} seconds");

            var startBrightness = CurrentState.Brightness;
            var stepCount = Math.Max(1, (int)(duration.TotalMilliseconds / 100)); // Step every 100ms
            var stepSize = (targetBrightness - startBrightness) / (float)stepCount;
            var stepDelay = duration.TotalMilliseconds / stepCount;

            for (int i = 1; i <= stepCount; i++)
            {
                var currentTarget = startBrightness + (int)(stepSize * i);
                await SetBrightnessAsync(currentTarget);
                
                if (i < stepCount) // Don't delay after the last step
                {
                    await Task.Delay((int)stepDelay);
                }
            }

            Console.WriteLine($"Fade completed for dimmer {Id}");
        }

        #endregion

        #region Lock Control (inherited from LockableKnxDeviceBase)

        protected override LockState? GetSavedLockState() => SavedState;
        protected override LockState GetCurrentLockState() => CurrentState;
        protected override LockableAddresses GetLockableAddresses() => _dimmerAddresses;

        protected override void UpdateCurrentStateLock(Lock lockState)
        {
            CurrentState = CurrentState with { Lock = lockState, LastUpdated = DateTime.Now };
        }

        #endregion

        #region State Management

        public new async Task RefreshStateAsync()
        {
            try
            {
                CurrentState = await ReadCurrentDimmerStateAsync();
                Console.WriteLine($"Refreshed state for dimmer {Id}: {ToString()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to refresh state for dimmer {Id}: {ex.Message}");
                // Don't throw - just keep the last known state
            }
        }

        #endregion

        public override string ToString()
        {
            return $"Dimmer {Id} ({Name}) - State: {CurrentState.Switch}, Brightness: {CurrentState.Brightness}%, Lock: {CurrentState.Lock}";
        }
    }
}
