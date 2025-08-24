using KnxModel.Models.Helpers;
using Microsoft.Extensions.Logging;

namespace KnxModel
{
    // Combines basic device functionality with switching and locking capabilities
    /// </summary>
    public abstract class LightDeviceBase<TDevice, TAddressess> : LockableDeviceBase<TDevice, TAddressess>, ILightDevice, ISwitchStateLockableDevice
        where TDevice : IKnxDeviceBase, ILightDevice, ILockableDevice
        where TAddressess : ISwitchableAddress, ILockableAddress
    {

        
        private SwitchableDeviceHelper<TDevice, TAddressess>? _switchableHelper;

        
        internal Switch _currentSwitchState = Switch.Unknown;

        
        // Saved state for testing
        private Switch? _savedSwitchState;
        private readonly ILogger<TDevice> _logger;

        public LightDeviceBase(string id, string name, string subGroup, TAddressess addresses, IKnxService knxService, ILogger<TDevice> logger, TimeSpan defaulTimeout)
            : base(id, name, subGroup, addresses,knxService, logger, defaulTimeout)
        {
            

            // Initialize event manager
            _eventManager.MessageReceived += OnKnxMessageReceived;
            _logger = logger;
        }


        internal override void Initialize(TDevice owner)
        {
            base.Initialize(owner);
            // Initialize helpers
            _switchableHelper = new SwitchableDeviceHelper<TDevice, TAddressess>(owner, 
                Addresses,
                _knxService, 
                _logger, 
                _defaulTimeout);
        }


        #region IKnxDeviceBase Implementation



        #endregion

        #region Event Handling

        private void OnKnxMessageReceived(object? sender, KnxGroupEventArgs e)
        {
            // Process switchable messages (Control/Feedback)
            _switchableHelper?.ProcessSwitchMessage(e);
        }

        #endregion



        #region IKnxDeviceBase Implementation


        public override void SaveCurrentState()
        {
            _savedSwitchState = _currentSwitchState;
            base.SaveCurrentState(); // Save lock state as well
        }

        public virtual async Task RestoreSavedStateAsync(TimeSpan? timeout = null)
        {
            if (_savedSwitchState.HasValue && _savedSwitchState.Value != CurrentSwitchState && _savedLockState != Lock.Unknown)
            {
                // Unlock before changing switch state if necessary
                if (_currentLockState == Lock.On)
                {
                    await UnlockAsync(timeout);
                }

                switch (_savedSwitchState.Value)
                {
                    case Switch.On:
                        await TurnOnAsync(timeout);
                        break;
                    case Switch.Off:
                        await TurnOffAsync(timeout);
                        break;
                }
            }

            if (_savedLockState.HasValue && _savedLockState.Value != CurrentLockState)
            {
                switch (_savedLockState.Value)
                {
                    case Lock.On:
                        await LockAsync(timeout);
                        break;
                    case Lock.Off:
                        await UnlockAsync(timeout);
                        break;
                }
            }

            Console.WriteLine($"LightDevice {Id} state restored");
        }

        #endregion

        #region ISwitchable Implementation

        public Switch CurrentSwitchState => _currentSwitchState;

        public async Task TurnOnAsync(TimeSpan? timeout = null)
        {
            await (_switchableHelper ?? throw new InvalidOperationException("Helper not initialized")).TurnOnAsync(timeout);
        }

        public async Task TurnOffAsync(TimeSpan? timeout = null)
        {
            await (_switchableHelper ?? throw new InvalidOperationException("Helper not initialized")).TurnOffAsync(timeout);
        }

        public async Task ToggleAsync(TimeSpan? timeout = null)
        {
            await (_switchableHelper ?? throw new InvalidOperationException("Helper not initialized")).ToggleAsync(timeout);
        }

        public async Task<Switch> ReadSwitchStateAsync()
        {
            return await (_switchableHelper ?? throw new InvalidOperationException("Helper not initialized")).ReadSwitchStateAsync();
        }

        public async Task<bool> WaitForSwitchStateAsync(Switch targetState, TimeSpan? timeout = null)
        {
            return await (_switchableHelper ?? throw new InvalidOperationException("Helper not initialized")).WaitForSwitchStateAsync(targetState, timeout);
        }

        #endregion



        #region Internal Test Helpers

        /// <summary>
        /// Internal method for setting device state in unit tests
        /// Bypasses KNX communication for testing scenarios
        /// </summary>
        internal void SetStateForTest(Switch switchState, Lock lockState)
        {
            _currentSwitchState = switchState;
            _currentLockState = lockState;
            _lastUpdated = DateTime.Now;
        }

        void ISwitchable.SetSavedSwitchForTest(Switch switchState)
        {
            _savedSwitchState = switchState;
        }


        /// <summary>
        /// Internal method for setting only switch state in unit tests
        /// </summary>
        void ISwitchable.SetSwitchForTest(Switch switchState)
        {
            _currentSwitchState = switchState;
            _lastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Internal property for accessing saved switch state in unit tests
        /// </summary>
        Switch? ISwitchable.SavedSwitchState => _savedSwitchState;

        /// <summary>
        /// Internal property for accessing saved lock state in unit tests
        /// </summary>
        internal Lock? SavedLockState => _savedLockState;

        public Switch LockedSwitchState => Switch.Off;

        public bool IsSwitchLockActive => true;

        #endregion


























    }
}
