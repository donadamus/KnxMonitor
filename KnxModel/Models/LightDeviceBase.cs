using KnxModel.Models.Helpers;
using Microsoft.Extensions.Logging;

namespace KnxModel
{
    // Combines basic device functionality with switching and locking capabilities
    /// </summary>
    public abstract class LightDeviceBase<TDevice, TAddressess> : LockableDeviceBase<TDevice, TAddressess>, ILightDevice, ISwitchStateLockableDevice
        where TDevice : ILightDevice
        where TAddressess : ISwitchableAddress, ILockableAddress
    {
        public Switch CurrentSwitchState { get; private set; } = Switch.Unknown;
        Switch ISwitchable.CurrentSwitchState { get => CurrentSwitchState; set => CurrentSwitchState = value; }
        public Switch? SavedSwitchState { get; private set; }
        Switch? ISwitchable.SavedSwitchState { get => SavedSwitchState; set => SavedSwitchState = value; }

        private SwitchableDeviceHelper<TDevice, TAddressess>? _switchableHelper;

        public LightDeviceBase(string id, string name, string subGroup, TAddressess addresses, IKnxService knxService, ILogger<TDevice> logger, TimeSpan defaulTimeout)
            : base(id, name, subGroup, addresses, knxService, logger, defaulTimeout)
        {
            _eventManager.MessageReceived += OnKnxMessageReceived;
        }

        internal override void Initialize(TDevice owner)
        {
            base.Initialize(owner);
            // Initialize helpers
            _switchableHelper = new SwitchableDeviceHelper<TDevice, TAddressess>(owner, 
                Addresses,
                _knxService, 
                _logger, 
                _defaultTimeout);
        }


        public override async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing DimmerDevice {DeviceId} ({DeviceName})", Id, Name);
            await base.InitializeAsync();
            // Read initial states from KNX bus
            CurrentSwitchState = await ReadSwitchStateAsync();
            LastUpdated = DateTime.Now;

            _logger.LogInformation("{type} {DeviceId} initialized - Switch: {SwitchState}", typeof(TDevice).Name, Id, CurrentSwitchState);
        }


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
            SavedSwitchState = CurrentSwitchState;
            base.SaveCurrentState(); // Save lock state as well
        }

        public override async Task RestoreSavedStateAsync(TimeSpan? timeout = null)
        {
            if (SavedSwitchState.HasValue && SavedSwitchState.Value != CurrentSwitchState)
            {
                // Unlock before changing switch state if necessary
                if (CurrentLockState == Lock.On)
                {
                    await UnlockAsync(timeout ?? _defaultTimeout);
                }

                switch (SavedSwitchState.Value)
                {
                    case Switch.On:
                        await TurnOnAsync(timeout ?? _defaultTimeout);
                        break;
                    case Switch.Off:
                        await TurnOffAsync(timeout ?? _defaultTimeout);
                        break;
                }
            }

            await base.RestoreSavedStateAsync(timeout ?? _defaultTimeout); // Restore lock state as well

            Console.WriteLine($"LightDevice {Id} state restored");
        }

        #endregion

        #region ISwitchable Implementation



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
            return await _switchableHelper!.WaitForSwitchStateAsync(targetState, timeout);
        }

        #endregion



        #region Internal Test Helpers


        /// <summary>
        /// Internal property for accessing saved lock state in unit tests
        /// </summary>

        public Switch LockedSwitchState => Switch.Off;

        public bool IsSwitchLockActive => true;

        #endregion

























    }
}
