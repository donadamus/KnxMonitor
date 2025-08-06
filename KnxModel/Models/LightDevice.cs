using KnxModel.Models.Helpers;
using System;
using System.Threading.Tasks;

namespace KnxModel
{





    public class DimmerDevice : LightDeviceBase<DimmerAddresses>, IDimmerDevice
    {

        private float _currentPercentage = 0.0f; // 0% brightness
        private float? _savedPercentage;

        public DimmerDevice(string id, string name, string subGroup, IKnxService knxService)
            : base(id, name, subGroup, KnxAddressConfiguration.CreateDimmerAddresses(subGroup), knxService)
        {
        }

        public override async Task InitializeAsync()
        {
            // Read initial states from KNX bus
            _currentSwitchState = await ReadSwitchStateAsync();
            _currentLockState = await ReadLockStateAsync();
            _currentPercentage = await ReadPercentageAsync();
            _lastUpdated = DateTime.Now;

            Console.WriteLine($"DimmerDevice {Id} initialized - Switch: {_currentSwitchState}, Lock: {_currentLockState}, Brightness: {_currentPercentage}%");
        }

        public override void SaveCurrentState()
        {
            base.SaveCurrentState();
            _savedPercentage = _currentPercentage; // Save current brightness percentage
            Console.WriteLine($"DimmerDevice {Id} state saved - Switch: {_currentSwitchState}, Lock: {_currentLockState}, Brightness: {_savedPercentage}%");
        }

        public override async Task RestoreSavedStateAsync(TimeSpan? timeout = null)
        {
            if (_savedPercentage.HasValue && _savedPercentage.Value != _currentPercentage)
            {
                // Unlock before changing switch state if necessary
                if (_currentLockState == Lock.On)
                {
                    await UnlockAsync(timeout);
                }

                await SetPercentageAsync(_savedPercentage.Value, timeout);
            }

            await base.RestoreSavedStateAsync(timeout);
           Console.WriteLine($"DimmerDevice {Id} state restored - Brightness: {_currentPercentage}%");
        }



        #region IPercentageControllable Implementation

        public float CurrentPercentage => _currentPercentage;

        public async Task SetPercentageAsync(float percentage, TimeSpan? timeout = null)
        {
            if (percentage < 0.0f || percentage > 100.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between 0 and 100");
            }

            // TODO: Send KNX command to set brightness
            await Task.Delay(75); // Simulate KNX communication (dimmers may be slower than switches)

            _currentPercentage = percentage;
            _lastUpdated = DateTime.Now;

            Console.WriteLine($"DimmerDevice {Id} brightness set to {percentage}%");
        }

        public async Task<float> ReadPercentageAsync()
        {
            // TODO: Read from KNX bus
            await Task.Delay(30); // Simulate KNX communication

            // For now, return current state (in real implementation, read from bus)
            _lastUpdated = DateTime.Now;
            return _currentPercentage;
        }

        public async Task<bool> WaitForPercentageAsync(float targetPercentage, double tolerance = 2.0, TimeSpan? timeout = null)
        {
            if (targetPercentage < 0.0f || targetPercentage > 100.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(targetPercentage), "Target percentage must be between 0 and 100");
            }

            var actualTimeout = timeout ?? TimeSpan.FromSeconds(10); // Default 10 seconds
            var endTime = DateTime.Now + actualTimeout;

            while (DateTime.Now < endTime)
            {
                var currentPercentage = await ReadPercentageAsync();
                if (Math.Abs(currentPercentage - targetPercentage) <= tolerance)
                {
                    return true;
                }

                await Task.Delay(100); // Check every 100ms
            }

            return false;
        }

        public async Task AdjustPercentageAsync(float increment, TimeSpan? timeout = null)
        {
            var newPercentage = _currentPercentage + increment;
            newPercentage = Math.Max(0.0f, Math.Min(100.0f, newPercentage)); // Clamp to 0-100

            await SetPercentageAsync(newPercentage, timeout);
        }

        #endregion


    }


    public class LightDevice : LightDeviceBase<LightAddresses>
    {
        public LightDevice(string id, string name, string subGroup, IKnxService knxService)
            : base(id, name, subGroup, KnxAddressConfiguration.CreateLightAddresses(subGroup), knxService)
        {
        }

        public override async Task InitializeAsync()
        {
            // Read initial states from KNX bus
            _currentSwitchState = await ReadSwitchStateAsync();
            _currentLockState = await ReadLockStateAsync();
            _lastUpdated = DateTime.Now;

            Console.WriteLine($"LightDevice {Id} initialized - Switch: {_currentSwitchState}, Lock: {_currentLockState}");
        }

    }
    // Combines basic device functionality with switching and locking capabilities
    /// </summary>
    public abstract class LightDeviceBase<TAddressess> : ILightDevice, IDisposable
        where TAddressess : ISwitchableAddress, ILockableAddress
    {
        


        private readonly IKnxService _knxService;
        private readonly KnxEventManager _eventManager;
        private readonly SwitchableDeviceHelper _switchableHelper;
        private readonly LockableDeviceHelper _lockableHelper;
        
        internal Switch _currentSwitchState = Switch.Unknown;
        internal Lock _currentLockState = Lock.Unknown;
        internal DateTime _lastUpdated = DateTime.MinValue;
        
        // Saved state for testing
        private Switch? _savedSwitchState;
        private Lock? _savedLockState;

        public LightDeviceBase(string id, string name, string subGroup, TAddressess addresses, IKnxService knxService)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SubGroup = subGroup ?? throw new ArgumentNullException(nameof(subGroup));
            Addresses = addresses ?? throw new ArgumentNullException(nameof(addresses));
            _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));

            // Initialize event manager
            _eventManager = new KnxEventManager(_knxService, Id, "LightDevice");
            _eventManager.MessageReceived += OnKnxMessageReceived;

            // Initialize helpers
            _switchableHelper = new SwitchableDeviceHelper(
                _knxService, Id, "LightDevice",
                () => Addresses,
                state => { _currentSwitchState = state; _lastUpdated = DateTime.Now; },
                () => _currentSwitchState);

            _lockableHelper = new LockableDeviceHelper(
                _knxService, Id, "LightDevice", 
                () => Addresses,
                state => { _currentLockState = state; _lastUpdated = DateTime.Now; },
                () => _currentLockState);

            // Start listening to KNX events
            _eventManager.StartListening();
        }

        #region IKnxDeviceBase Implementation

        public string Id { get; }
        public string Name { get; }
        public string SubGroup { get; }
        public DateTime LastUpdated => _lastUpdated;

        #endregion

        #region Event Handling

        private void OnKnxMessageReceived(object? sender, KnxGroupEventArgs e)
        {
            // Process switchable messages (Control/Feedback)
            _switchableHelper.ProcessSwitchMessage(e);
            
            // Process lockable messages (LockControl/LockFeedback) 
            _lockableHelper.ProcessLockMessage(e);
        }

        #endregion

        #region LightDevice Addresses

        public TAddressess Addresses { get; }
        //public TAddressess Addresses => LightAddresses; // Dla kompatybilności

        #endregion

        #region IKnxDeviceBase Implementation

        public abstract Task InitializeAsync();
        

        public virtual void SaveCurrentState()
        {
            _savedSwitchState = _currentSwitchState;
            _savedLockState = _currentLockState;
            Console.WriteLine($"LightDevice {Id} state saved - Switch: {_savedSwitchState}, Lock: {_savedLockState}");
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
            await _switchableHelper.TurnOnAsync(timeout);
        }

        public async Task TurnOffAsync(TimeSpan? timeout = null)
        {
            await _switchableHelper.TurnOffAsync(timeout);
        }

        public async Task ToggleAsync(TimeSpan? timeout = null)
        {
            await _switchableHelper.ToggleAsync(timeout);
        }

        public async Task<Switch> ReadSwitchStateAsync()
        {
            return await _switchableHelper.ReadSwitchStateAsync();
        }

        public async Task<bool> WaitForSwitchStateAsync(Switch targetState, TimeSpan? timeout = null)
        {
            return await _switchableHelper.WaitForSwitchStateAsync(targetState, timeout);
        }

        #endregion

        #region ILockableDevice Implementation

        public Lock CurrentLockState => _currentLockState;

        public async Task LockAsync(TimeSpan? timeout = null)
        {
            await _lockableHelper.LockAsync(timeout);
        }
        public async Task SetLockAsync(Lock lockState, TimeSpan? timeout = null)
        {
            await _lockableHelper.SetLockAsync(lockState, timeout);
        }
        public async Task UnlockAsync(TimeSpan? timeout = null)
        {
            await _lockableHelper.UnlockAsync(timeout);
        }

        public async Task<Lock> ReadLockStateAsync()
        {
            return await _lockableHelper.ReadLockStateAsync();
        }

        public async Task<bool> WaitForLockStateAsync(Lock targetState, TimeSpan timeout)
        {
            return await _lockableHelper.WaitForLockStateAsync(targetState, timeout);
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

        /// <summary>
        /// Internal method for setting device state in unit tests
        /// Bypasses KNX communication for testing scenarios
        /// </summary>
        internal void SetSavedStateForTest(Switch switchState, Lock lockState)
        {
            _savedSwitchState = switchState;
            _savedLockState = lockState;
        }

        /// <summary>
        /// Internal method for setting only switch state in unit tests
        /// </summary>
        internal void SetSwitchStateForTest(Switch switchState)
        {
            _currentSwitchState = switchState;
            _lastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Internal method for setting only lock state in unit tests
        /// </summary>
        internal void SetLockStateForTest(Lock lockState)
        {
            _currentLockState = lockState;
            _lastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Internal property for accessing saved switch state in unit tests
        /// </summary>
        internal Switch? SavedSwitchState => _savedSwitchState;

        /// <summary>
        /// Internal property for accessing saved lock state in unit tests
        /// </summary>
        internal Lock? SavedLockState => _savedLockState;

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            _eventManager?.Dispose();
            Console.WriteLine($"LightDevice {Id} disposed");
        }

        #endregion
























    }
}
