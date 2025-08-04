using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Implementation of ILight that manages a KNX light device
    /// </summary>
    public class LightOld : LockableKnxDeviceBase, ILightOld
    {
        private static new readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(2);

        private LightAddresses _addresses = null!; // Initialized in constructor
        protected LightState _currentState = null!; // Initialized in constructor
        protected LightState? _savedState;

        /// <summary>
        /// KNX addresses for light control and feedback
        /// </summary>
        public LightAddresses Addresses => _addresses;

        /// <summary>
        /// Current state of the light
        /// </summary>
        public LightState CurrentState
        {
            get => _currentState;
            protected set => _currentState = value;
        }

        /// <summary>
        /// Saved state for restoration after tests
        /// </summary>
        public LightState? SavedState
        {
            get => _savedState;
            protected set => _savedState = value;
        }

        /// <summary>
        /// Creates a new Light instance
        /// </summary>
        /// <param name="id">Unique identifier (e.g., "L1.1")</param>
        /// <param name="name">Human-readable name</param>
        /// <param name="subGroup">KNX sub-group number</param>
        /// <param name="knxService">KNX service for communication</param>
        public LightOld(string id, string name, string subGroup, IKnxService knxService, TimeSpan? timeout = null)
            : base(id, name, subGroup, knxService, timeout == null ? _defaultTimeout : timeout)
        {
            _addresses = CreateAddresses();
            _currentState = CreateDefaultState();
            StartListeningToFeedback();
        }

        protected virtual LightAddresses CreateAddresses()
        {
            return new LightAddresses(
                Control: KnxAddressConfiguration.CreateLightControlAddress(SubGroup),
                Feedback: KnxAddressConfiguration.CreateLightFeedbackAddress(SubGroup),
                LockControl: KnxAddressConfiguration.CreateLightLockAddress(SubGroup),
                LockFeedback: KnxAddressConfiguration.CreateLightLockFeedbackAddress(SubGroup)
            );
        }

        protected virtual LightState CreateDefaultState()
        {
            // Initialize with default state
            return new LightState(
                Switch: Switch.Unknown,
                Lock: Lock.Unknown,
                LastUpdated: DateTime.Now
            );
        }

        protected virtual async Task<LightState> ReadCurrentStateAsync()
        {
            var isOn = await ReadStateAsync();
            var isLocked = await ReadLockStateAsync();
            return new LightState(
                Switch: isOn,
                Lock: isLocked,
                LastUpdated: DateTime.Now
            );
        }

        protected override void ProcessDeviceSpecificMessage(KnxGroupEventArgs e)
        {
            // Handle light-specific (non-lock) messages
            if (e.Destination == Addresses.Feedback)
            {
                var switchState = e.Value.AsBoolean().ToSwitch();
                CurrentState = new LightState(
                    Switch: switchState, 
                    Lock: CurrentState.Lock, 
                    LastUpdated: DateTime.Now
                );
                Console.WriteLine($"Light {Id} state updated via feedback: {switchState}");
            }
        }

        #region Abstract implementations for LockableKnxDeviceBase

        protected override LockState? GetSavedLockState() => SavedState;
        protected override LockState GetCurrentLockState() => CurrentState;
        protected override LockableAddresses GetLockableAddresses() => Addresses;

        protected override void UpdateCurrentStateLock(Lock lockState)
        {
            CurrentState = CurrentState with { Lock = lockState, LastUpdated = DateTime.Now };
        }

        #endregion

        public override async Task InitializeAsync()
        {
            try
            {
                CurrentState = await ReadCurrentStateAsync();
                SaveCurrentState();
                Console.WriteLine($"Initialized light {Id}: {ToString()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize light {Id}, using default state: {ex.Message}");
                CurrentState = CreateDefaultState();
            }
        }

        public override void SaveCurrentState()
        {
            SavedState = CurrentState;
            SaveCurrentStateMessage();
        }

        protected virtual void SaveCurrentStateMessage()
        {
            Console.WriteLine($"Saved current state for light {Id} - State: {CurrentState.Switch}, Lock: {CurrentState.Lock}");
        }

        public override async Task RestoreSavedStateAsync()
        {
            if (SavedState == null)
            {
                return;
            }

            RestoreSavedStateMessage();

            try
            {
                await PerformStateRestoration();
                
                // Call base implementation to restore lock state
                await base.RestoreSavedStateAsync();
                
                RestoreSuccessMessage();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to restore light {Id} state: {ex.Message}");
                throw;
            }
        }

        protected virtual void RestoreSavedStateMessage()
        {
            Console.WriteLine($"Restoring light {Id} to saved state - State: {SavedState!.Switch}, Lock: {SavedState.Lock}");
        }

        protected virtual async Task PerformStateRestoration()
        {
            // Restore state
            if (CurrentState.Switch != SavedState!.Switch)
            {
                // Check if device is currently locked - if so, unlock it temporarily to allow state changes
                if (CurrentState.Lock == Lock.On)
                {
                    Console.WriteLine($"Light {Id} is locked, temporarily unlocking to allow switch state restoration");
                    await SetLockAsync(Lock.Off);
                }

                await SetStateAsync(SavedState.Switch);
            }
        }

        protected virtual void RestoreSuccessMessage()
        {
            Console.WriteLine($"Light {Id} successfully restored to saved state");
        }

        #region Lock Implementation

        public virtual LightState UpdateLockState(Lock lockState) => 
            CurrentState with { Lock = lockState, LastUpdated = DateTime.Now };

        #endregion

        public virtual async Task SetStateAsync(Switch switchState, TimeSpan? timeout = null)
        {
            Console.WriteLine($"{switchState} light {Id}");
            
            await SetBitFunctionAsync(
                Addresses.Control,
                switchState.ToBool(),
                () => CurrentState.Switch == switchState,
                timeout
            );
        }

        public virtual async Task TurnOnAsync(TimeSpan? timeout = null)
        {
            await SetStateAsync(Switch.On, timeout);
        }

        public virtual async Task TurnOffAsync(TimeSpan? timeout = null)
        {
            await SetStateAsync(Switch.Off, timeout);
        }

        public virtual async Task ToggleAsync(TimeSpan? timeout = null)
        {
            var newState = CurrentState.Switch.Opposite();
            Console.WriteLine($"Toggling light {Id} to {newState}");
            await SetStateAsync(newState, timeout);
        }

        public virtual async Task<Switch> ReadStateAsync()
        {
            try
            {
                var value = await _knxService.RequestGroupValue<bool>(Addresses.Feedback);
                return value.ToSwitch();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read state for light {Id}: {ex.Message}");
                throw;
            }
        }

        public virtual async Task<bool> WaitForStateAsync(Switch targetState, TimeSpan? timeout = null)
        {
            Console.WriteLine($"Waiting for light {Id} to become: {targetState}");
            
            return await WaitForConditionAsync(
                condition: () => CurrentState.Switch == targetState,
                timeout: timeout,
                description: $"state {targetState}"
            );
        }

        public async Task RefreshStateAsync()
        {
            try
            {
                CurrentState = await ReadCurrentStateAsync();
                Console.WriteLine($"Refreshed state for light {Id}: {ToString()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to refresh state for light {Id}: {ex.Message}");
                // Don't throw - just keep the last known state
            }
        }

        public override string ToString()
        {
            return $"Light {Id} ({Name}) - State: {CurrentState.Switch}, Lock: {CurrentState.Lock}";
        }

        public override void Dispose()
        {
            StopListeningToFeedback();
        }
    }
}
