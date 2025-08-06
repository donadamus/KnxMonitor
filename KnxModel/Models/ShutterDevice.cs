using KnxModel.Models.Helpers;
using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Implementation of a Shutter device using new interface architecture
    /// Combines basic device functionality with percentage control (position), locking capabilities, and activity monitoring
    /// Position: 0% = fully open, 100% = fully closed
    /// IsActive: true = moving, false = stopped
    /// </summary>
    public class ShutterDevice : IShutterDevice
    {
        private readonly IKnxService _knxService;
        private readonly LockableDeviceHelper _lockableHelper;

        private float _currentPercentage = 0.0f; // Start fully open
        private Lock _currentLockState = Lock.Unknown;
        private bool _isActive = false; // Movement status: true = moving, false = stopped
        private DateTime _lastUpdated = DateTime.MinValue;
        
        // Saved state for testing
        private float? _savedPercentage;
        private Lock? _savedLockState;

        public ShutterDevice(string id, string name, string subGroup, ShutterAddresses addresses, IKnxService knxService)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SubGroup = subGroup ?? throw new ArgumentNullException(nameof(subGroup));
            ShutterAddresses = addresses ?? throw new ArgumentNullException(nameof(addresses));
            _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));


            _lockableHelper = new LockableDeviceHelper(
                _knxService, Id, "LightDevice",
                () => ShutterAddresses,
                state => { _currentLockState = state; _lastUpdated = DateTime.Now; },
                () => _currentLockState);
        }

        /// <summary>
        /// Convenience constructor that automatically creates addresses based on subGroup
        /// </summary>
        public ShutterDevice(string id, string name, string subGroup, IKnxService knxService)
            : this(id, name, subGroup, KnxAddressConfiguration.CreateShutterAddresses(subGroup), knxService)
        {
        }

        #region IKnxDeviceBase Implementation

        public string Id { get; }
        public string Name { get; }
        public string SubGroup { get; }
        public DateTime LastUpdated => _lastUpdated;

        #endregion

        #region ShutterDevice Addresses

        public ShutterAddresses ShutterAddresses { get; }
        public ShutterAddresses Addresses => ShutterAddresses; // Dla kompatybilności

        #endregion

        #region IKnxDeviceBase Methods

        public async Task InitializeAsync()
        {
            // Read initial states from KNX bus
            _currentPercentage = await ReadPercentageAsync();
            _currentLockState = await ReadLockStateAsync();
            _lastUpdated = DateTime.Now;
            
            Console.WriteLine($"ShutterDevice {Id} initialized - Position: {_currentPercentage}%, Lock: {_currentLockState}");
        }

        public void SaveCurrentState()
        {
            _savedPercentage = _currentPercentage;
            _savedLockState = _currentLockState;
            Console.WriteLine($"ShutterDevice {Id} state saved - Position: {_savedPercentage}%, Lock: {_savedLockState}");
        }

        public async Task RestoreSavedStateAsync()
        {
            if (_savedPercentage.HasValue)
            {
                await SetPercentageAsync(_savedPercentage.Value);
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

            Console.WriteLine($"ShutterDevice {Id} state restored");
        }

        public void Dispose()
        {
            // Cleanup resources if needed
            Console.WriteLine($"ShutterDevice {Id} disposed");
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

            // TODO: Send KNX command to set position
            Console.WriteLine($"ShutterDevice {Id} starting movement to {percentage}%");
            
            // Simulate movement - in real implementation this would trigger actual motor movement
            _isActive = true; // Movement started
            await Task.Delay(50); // Simulate KNX command send time
            
            // Simulate movement duration (longer movements take more time)
            var movementDuration = Math.Abs(_currentPercentage - percentage) * 10; // 10ms per 1% change
            await Task.Delay((int)movementDuration);
            
            _currentPercentage = percentage;
            _isActive = false; // Movement completed
            _lastUpdated = DateTime.Now;
            
            Console.WriteLine($"ShutterDevice {Id} position set to {percentage}% (movement completed)");
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

        public async Task AdjustPercentageAsync(float delta, TimeSpan? timeout = null)
        {
            var newPercentage = _currentPercentage + delta;
            newPercentage = Math.Max(0.0f, Math.Min(100.0f, newPercentage)); // Clamp to 0-100
            
            await SetPercentageAsync(newPercentage, timeout);
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
            
            Console.WriteLine($"ShutterDevice {Id} locked");
        }

        public async Task UnlockAsync(TimeSpan? timeout = null)
        {
            // TODO: Send KNX command to unlock
            await Task.Delay(50); // Simulate KNX communication
            
            _currentLockState = Lock.Off;
            _lastUpdated = DateTime.Now;
            
            Console.WriteLine($"ShutterDevice {Id} unlocked");
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

        #region IActivityStatusReadable Implementation

        public bool IsActive => _isActive;

        public async Task<bool> ReadActivityStatusAsync()
        {
            // TODO: Read from KNX bus - MovementStatusFeedback address
            await Task.Delay(30); // Simulate KNX communication
            
            // For now, return current state (in real implementation, read from bus)
            // var isMoving = await _knxService.RequestGroupValue<bool>(addresses.MovementStatusFeedback);
            // _isActive = isMoving;
            
            _lastUpdated = DateTime.Now;
            return _isActive;
        }

        public async Task<bool> WaitForInactiveAsync(TimeSpan? timeout = null)
        {
            var actualTimeout = timeout ?? TimeSpan.FromSeconds(30); // Default 30 seconds for movement
            var endTime = DateTime.Now + actualTimeout;
            
            Console.WriteLine($"ShutterDevice {Id} waiting for movement to stop (timeout: {actualTimeout.TotalSeconds}s)");
            
            while (DateTime.Now < endTime)
            {
                var isActive = await ReadActivityStatusAsync();
                if (!isActive)
                {
                    Console.WriteLine($"ShutterDevice {Id} movement stopped");
                    return true;
                }
                
                await Task.Delay(100); // Check every 100ms
            }
            
            Console.WriteLine($"ShutterDevice {Id} timeout waiting for movement to stop");
            return false;
        }

        public async Task<bool> WaitForActiveAsync(TimeSpan? timeout = null)
        {
            var actualTimeout = timeout ?? TimeSpan.FromSeconds(5); // Default 5 seconds to start moving
            var endTime = DateTime.Now + actualTimeout;
            
            Console.WriteLine($"ShutterDevice {Id} waiting for movement to start (timeout: {actualTimeout.TotalSeconds}s)");
            
            while (DateTime.Now < endTime)
            {
                var isActive = await ReadActivityStatusAsync();
                if (isActive)
                {
                    Console.WriteLine($"ShutterDevice {Id} movement started");
                    return true;
                }
                
                await Task.Delay(100); // Check every 100ms
            }
            
            Console.WriteLine($"ShutterDevice {Id} timeout waiting for movement to start");
            return false;
        }

        #endregion

        #region IShutterDevice Implementation (Convenience Methods)

        public async Task OpenAsync(TimeSpan? timeout = null)
        {
            await SetPercentageAsync(0.0f, timeout); // 0% = fully open
            Console.WriteLine($"ShutterDevice {Id} opened");
        }

        public async Task CloseAsync(TimeSpan? timeout = null)
        {
            await SetPercentageAsync(100.0f, timeout); // 100% = fully closed
            Console.WriteLine($"ShutterDevice {Id} closed");
        }

        public async Task StopAsync(TimeSpan? timeout = null)
        {
            // TODO: Send KNX stop command
            await Task.Delay(25); // Simulate KNX communication
            
            // In a real implementation, this would stop the motor at current position
            // and update the activity status
            _isActive = false; // Movement stopped
            _lastUpdated = DateTime.Now;
            
            Console.WriteLine($"ShutterDevice {Id} stopped at {_currentPercentage}%");
            
            // Wait for confirmation that movement actually stopped
            if (timeout.HasValue)
            {
                await WaitForInactiveAsync(timeout);
            }
        }

        #endregion
    }
}
