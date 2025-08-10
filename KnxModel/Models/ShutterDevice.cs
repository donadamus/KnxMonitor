using KnxModel.Models.Helpers;
using Microsoft.Extensions.Logging;
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
    public class ShutterDevice : LockableDeviceBase<ShutterDevice, ShutterAddresses>, IShutterDevice
    {
        private readonly PercentageControllableDeviceHelper<ShutterDevice> _shutterHelper;
        private readonly ShutterDeviceHelper<ShutterDevice> _shutterMovementHelper;
        private readonly ILogger<ShutterDevice> logger;
        private float _currentPercentage = 0.0f; // Start fully open
        private bool _isActive = false; // Movement status: true = moving, false = stopped
        
        // Saved state for testing
        private float? _savedPercentage;

        /// <summary>
        /// Convenience constructor that automatically creates addresses based on subGroup
        /// </summary>
        public ShutterDevice(string id, string name, string subGroup, IKnxService knxService, ILogger<ShutterDevice> logger, TimeSpan defaulTimeout)
            : base(id, name, subGroup, KnxAddressConfiguration.CreateShutterAddresses(subGroup), knxService, logger, defaulTimeout)
        {
            _shutterHelper = new PercentageControllableDeviceHelper<ShutterDevice>(
                            _knxService, Id, "ShutterDevice",
                            () => Addresses,
                            state => { _currentPercentage = state; _lastUpdated = DateTime.Now; },
                            () => _currentPercentage,
                            logger: logger
                            );

            _shutterMovementHelper = new ShutterDeviceHelper<ShutterDevice>(
                            _knxService, Id, "ShutterDevice",
                            () => Addresses,
                            active => { _isActive = active; },
                            () => _lastUpdated,
                            () => { _lastUpdated = DateTime.Now; },
                            () => _currentLockState,
                            () => UnlockAsync(),
                            logger: logger
                            );

            _eventManager.MessageReceived += OnKnxMessageReceived;
            this.logger = logger;
        }

        private void OnKnxMessageReceived(object? sender, KnxGroupEventArgs e)
        {
            // Process percentage control messages
            _shutterHelper.ProcessSwitchMessage(e);
            
            // Process movement feedback messages
            _shutterMovementHelper.ProcessMovementMessage(e);
        }

        private async Task WaitForCooldownAsync()
        {
            var elapsed = DateTime.Now - LastUpdated;

            if (elapsed < TimeSpan.FromSeconds(2) && elapsed > TimeSpan.Zero)
            {
                await Task.Delay(TimeSpan.FromSeconds(2) - elapsed);
            }
        }


        #region IKnxDeviceBase Methods

        public override async Task InitializeAsync()
        {
            logger.LogInformation("Initializing ShutterDevice {DeviceId} ({DeviceName})", Id, Name);
            
            // Read initial states from KNX bus
            _currentPercentage = await ReadPercentageAsync();
            _currentLockState = await ReadLockStateAsync();
            _lastUpdated = DateTime.Now;
            
            logger.LogInformation("ShutterDevice {DeviceId} initialized - Position: {Position}%, Lock: {LockState}", 
                Id, _currentPercentage, _currentLockState);
            
            Console.WriteLine($"ShutterDevice {Id} initialized - Position: {_currentPercentage}%, Lock: {_currentLockState}");
        }

        public override void SaveCurrentState()
        {
            base.SaveCurrentState();
            _savedPercentage = _currentPercentage;
            Console.WriteLine($"ShutterDevice {Id} state saved - Position: {_savedPercentage}%, Lock: {_savedLockState}");
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

                await SetPercentageAsync(_savedPercentage.Value, timeout ?? _defaulTimeout);
            }

            await base.RestoreSavedStateAsync(timeout ?? _defaulTimeout);

            Console.WriteLine($"ShutterDevice {Id} state restored");
        }

        #endregion

        #region IPercentageControllable Implementation

        public float CurrentPercentage => _currentPercentage;

        public async Task SetPercentageAsync(float percentage, TimeSpan? timeout = null)
        {
            await WaitForCooldownAsync();
            logger.LogInformation($"ShutterDevice {Id} set percentage to {percentage}%");
            await _shutterHelper.SetPercentageAsync(percentage, timeout);
            logger.LogInformation($"ShutterDevice {Id} current percentage is now {_currentPercentage}%");
        }

        public async Task<float> ReadPercentageAsync()
        {
            return await _shutterHelper.ReadPercentageAsync();
        }

        public async Task<bool> WaitForPercentageAsync(float targetPercentage, double tolerance = 2.0, TimeSpan? timeout = null)
        {
            return await _shutterHelper.WaitForPercentageAsync(targetPercentage, tolerance, timeout);

        }

        public async Task AdjustPercentageAsync(float delta, TimeSpan? timeout = null)
        {
            await WaitForCooldownAsync();
            logger.LogInformation($"ShutterDevice {Id} adjusting percentage by {delta}%, timeout: {timeout?.TotalSeconds ?? 0}s");
            await _shutterHelper.AdjustPercentageAsync(delta, timeout);
            logger.LogInformation($"ShutterDevice {Id} adjusted percentage by {delta}%, new value: {_currentPercentage}%");
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
            await WaitForCooldownAsync();
            await _shutterMovementHelper.OpenAsync(timeout);
        }

        public async Task CloseAsync(TimeSpan? timeout = null)
        {
            await WaitForCooldownAsync();
            await _shutterMovementHelper.CloseAsync(timeout);
        }

        public async Task StopAsync(TimeSpan? timeout = null)
        {
            await _shutterMovementHelper.StopAsync(timeout);
            
            // Wait for confirmation that movement actually stopped
            if (timeout.HasValue)
            {
                await WaitForInactiveAsync(timeout);
            }
        }

        #endregion
    }
}
