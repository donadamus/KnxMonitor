using KnxModel.Models.Helpers;
using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Implementation of a Dimmer device using new interface architecture
    /// Combines light functionality (switching + locking) with brightness control
    /// Brightness: 0% = minimum brightness (but still ON), 100% = maximum brightness
    /// </summary>
    public class DimmerDevice : IDimmerDevice
    {
        
        private readonly IKnxService _knxService;
        private readonly LockableDeviceHelper _lockableHelper;
        private Switch _currentSwitchState = Switch.Unknown;
        private Lock _currentLockState = Lock.Unknown;
        private float _currentPercentage = 0.0f; // 0% brightness
        private DateTime _lastUpdated = DateTime.MinValue;
        
        // Saved state for testing
        private Switch? _savedSwitchState;
        private Lock? _savedLockState;
        private float? _savedPercentage;

        public DimmerDevice(string id, string name, string subGroup, DimmerAddresses addresses, IKnxService knxService)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SubGroup = subGroup ?? throw new ArgumentNullException(nameof(subGroup));
            DimmerAddresses = addresses ?? throw new ArgumentNullException(nameof(addresses));
            _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));


            _lockableHelper = new LockableDeviceHelper(
                _knxService, Id, "LightDevice",
                () => DimmerAddresses,
                state => { _currentLockState = state; _lastUpdated = DateTime.Now; },
                () => _currentLockState);
        }

        /// <summary>
        /// Convenience constructor that automatically creates addresses based on subGroup
        /// </summary>
        public DimmerDevice(string id, string name, string subGroup, IKnxService knxService)
            : this(id, name, subGroup, KnxAddressConfiguration.CreateDimmerAddresses(subGroup), knxService)
        {
        }

        #region IKnxDeviceBase Implementation

        public string Id { get; }
        public string Name { get; }
        public string SubGroup { get; }
        public DateTime LastUpdated => _lastUpdated;

        #endregion

        #region DimmerDevice Addresses

        public DimmerAddresses DimmerAddresses { get; }
        public DimmerAddresses Addresses => DimmerAddresses; // Dla kompatybilności

        #endregion

        #region IKnxDeviceBase Methods

        public async Task InitializeAsync()
        {
            // Read initial states from KNX bus
            _currentSwitchState = await ReadSwitchStateAsync();
            _currentLockState = await ReadLockStateAsync();
            _currentPercentage = await ReadPercentageAsync();
            _lastUpdated = DateTime.Now;
            
            Console.WriteLine($"DimmerDevice {Id} initialized - Switch: {_currentSwitchState}, Lock: {_currentLockState}, Brightness: {_currentPercentage}%");
        }

        public void SaveCurrentState()
        {
            _savedSwitchState = _currentSwitchState;
            _savedLockState = _currentLockState;
            _savedPercentage = _currentPercentage;
            Console.WriteLine($"DimmerDevice {Id} state saved - Switch: {_savedSwitchState}, Lock: {_savedLockState}, Brightness: {_savedPercentage}%");
        }

        public async Task RestoreSavedStateAsync(TimeSpan? timeout = null)
        {
            if (_savedSwitchState.HasValue)
            {
                switch (_savedSwitchState.Value)
                {
                    case Switch.On:
                        await TurnOnAsync();
                        break;
                    case Switch.Off:
                        await TurnOffAsync();
                        break;
                }
            }

            if (_savedLockState.HasValue)
            {
                switch (_savedLockState.Value)
                {
                    case Lock.On:
                        await LockAsync();
                        break;
                    case Lock.Off:
                        await UnlockAsync();
                        break;
                }
            }

            if (_savedPercentage.HasValue)
            {
                await SetPercentageAsync(_savedPercentage.Value);
            }

            Console.WriteLine($"DimmerDevice {Id} state restored");
        }

        public void Dispose()
        {
            // Cleanup resources if needed
            Console.WriteLine($"DimmerDevice {Id} disposed");
        }

        #endregion

        #region ISwitchable Implementation

        public Switch CurrentSwitchState => _currentSwitchState;

        public async Task TurnOnAsync(TimeSpan? timeout = null)
        {
            // TODO: Send KNX command to turn on
            await Task.Delay(50); // Simulate KNX communication
            
            _currentSwitchState = Switch.On;
            _lastUpdated = DateTime.Now;
            
            Console.WriteLine($"DimmerDevice {Id} turned ON");
        }

        public async Task TurnOffAsync(TimeSpan? timeout = null)
        {
            // TODO: Send KNX command to turn off
            await Task.Delay(50); // Simulate KNX communication
            
            _currentSwitchState = Switch.Off;
            _lastUpdated = DateTime.Now;
            
            Console.WriteLine($"DimmerDevice {Id} turned OFF");
        }

        public async Task ToggleAsync(TimeSpan? timeout = null)
        {
            switch (_currentSwitchState)
            {
                case Switch.On:
                    await TurnOffAsync(timeout);
                    break;
                case Switch.Off:
                    await TurnOnAsync(timeout);
                    break;
                default:
                    // If unknown, default to turning on
                    await TurnOnAsync(timeout);
                    break;
            }
        }

        public async Task<Switch> ReadSwitchStateAsync()
        {
            // TODO: Read from KNX bus
            await Task.Delay(30); // Simulate KNX communication
            
            // For now, return current state (in real implementation, read from bus)
            _lastUpdated = DateTime.Now;
            return _currentSwitchState;
        }

        public async Task<bool> WaitForSwitchStateAsync(Switch targetState, TimeSpan? timeout = null)
        {
            var endTime = DateTime.Now + timeout;
            
            while (DateTime.Now < endTime)
            {
                var currentState = await ReadSwitchStateAsync();
                if (currentState == targetState)
                {
                    return true;
                }
                
                await Task.Delay(100); // Check every 100ms
            }
            
            return false;
        }

        #endregion

        #region ILockableDevice Implementation

        public Lock CurrentLockState => _currentLockState;

        public async Task SetLockAsync(Lock lockState, TimeSpan? timeout = null)
        {
            await _lockableHelper.SetLockAsync(lockState);
        }

        public async Task LockAsync(TimeSpan? timeout = null)
        {
            // TODO: Send KNX command to lock
            await Task.Delay(50); // Simulate KNX communication
            
            _currentLockState = Lock.On;
            _lastUpdated = DateTime.Now;
            
            Console.WriteLine($"DimmerDevice {Id} locked");
        }

        public async Task UnlockAsync(TimeSpan? timeout = null)
        {
            // TODO: Send KNX command to unlock
            await Task.Delay(50); // Simulate KNX communication
            
            _currentLockState = Lock.Off;
            _lastUpdated = DateTime.Now;
            
            Console.WriteLine($"DimmerDevice {Id} unlocked");
        }

        public async Task<Lock> ReadLockStateAsync()
        {
            // TODO: Read from KNX bus
            await Task.Delay(30); // Simulate KNX communication
            
            // For now, return current state (in real implementation, read from bus)
            _lastUpdated = DateTime.Now;
            return _currentLockState;
        }

        public async Task<bool> WaitForLockStateAsync(Lock targetState, TimeSpan timeout)
        {
            var endTime = DateTime.Now + timeout;
            
            while (DateTime.Now < endTime)
            {
                var currentState = await ReadLockStateAsync();
                if (currentState == targetState)
                {
                    return true;
                }
                
                await Task.Delay(100); // Check every 100ms
            }
            
            return false;
        }

        #endregion

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
}
