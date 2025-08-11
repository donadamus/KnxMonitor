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
    public class ShutterDevice : LockableDeviceBase<ShutterDevice, ShutterAddresses>, IShutterDevice, IPercentageLockableDevice
    {
        private readonly PercentageControllableDeviceHelper<ShutterDevice> _shutterHelper;
        private readonly ShutterDeviceHelper<ShutterDevice> _shutterMovementHelper;
        private readonly ILogger<ShutterDevice> logger;
        private float _currentPercentage = 0.0f; // Start fully open
        private bool _isActive = false; // Movement status: true = moving, false = stopped
        private bool _isSunProtectionBlocked = false; // Sun protection block status
        
        // Saved state for testing
        private float? _savedPercentage;
        private bool? _savedSunProtectionBlocked;

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
                            logger: logger,
                            defaulTimeout
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
                            , defaulTimeout
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
            
            // Process sun protection block feedback
            ProcessSunProtectionFeedback(e);
        }

        /// <summary>
        /// Processes sun protection block feedback messages
        /// </summary>
        private void ProcessSunProtectionFeedback(KnxGroupEventArgs e)
        {
            // Process sun protection block feedback (same address as control)
            if (e.Destination == Addresses.SunProtectionBlockFeedback)
            {
                var blockState = e.Value.AsBoolean();
                _isSunProtectionBlocked = blockState;
                _lastUpdated = DateTime.Now;
                
                logger.LogInformation("ShutterDevice {DeviceId} sun protection block feedback: {BlockState}", 
                    Id, blockState ? "BLOCKED" : "UNBLOCKED");
                Console.WriteLine($"ShutterDevice {Id} sun protection block: {(blockState ? "BLOCKED" : "UNBLOCKED")}");
            }
            
            // Process sun protection status feedback (offset +100)
            if (e.Destination == Addresses.SunProtectionStatus)
            {
                var isActive = e.Value.AsBoolean();
                // This is the actual sun protection state (1=Active), but for now we use block state
                logger.LogInformation("ShutterDevice {DeviceId} sun protection status: {Status}", 
                    Id, isActive ? "ACTIVE" : "INACTIVE");
                Console.WriteLine($"ShutterDevice {Id} sun protection status: {(isActive ? "ACTIVE" : "INACTIVE")}");
            }
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
            _isSunProtectionBlocked = await ReadSunProtectionBlockStateAsync();
            _lastUpdated = DateTime.Now;
            
            logger.LogInformation("ShutterDevice {DeviceId} initialized - Position: {Position}%, Lock: {LockState}, SunProtectionBlocked: {SunProtectionBlocked}", 
                Id, _currentPercentage, _currentLockState, _isSunProtectionBlocked);
            
            Console.WriteLine($"ShutterDevice {Id} initialized - Position: {_currentPercentage}%, Lock: {_currentLockState}, SunProtectionBlocked: {_isSunProtectionBlocked}");
        }

        public override void SaveCurrentState()
        {
            base.SaveCurrentState();
            _savedPercentage = _currentPercentage;
            _savedSunProtectionBlocked = _isSunProtectionBlocked;
            Console.WriteLine($"ShutterDevice {Id} state saved - Position: {_savedPercentage}%, Lock: {_savedLockState}, SunProtectionBlocked: {_savedSunProtectionBlocked}");
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

            // Restore sun protection block state if it was saved and is different
            if (_savedSunProtectionBlocked.HasValue && _savedSunProtectionBlocked.Value != _isSunProtectionBlocked)
            {
                await SetSunProtectionBlockStateAsync(_savedSunProtectionBlocked.Value, timeout ?? _defaulTimeout);
            }

            await base.RestoreSavedStateAsync(timeout ?? _defaulTimeout);
            logger.LogInformation("ShutterDevice {DeviceId} initialized - Position: {Position}%, Lock: {LockState}, SunProtectionBlocked: {SunProtectionBlocked}",
    Id, _currentPercentage, _currentLockState, _isSunProtectionBlocked);
            Console.WriteLine($"ShutterDevice {Id} state restored - Position: {_savedPercentage}%, Lock: {_savedLockState}, SunProtectionBlocked: {_savedSunProtectionBlocked}");
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

        public bool IsSunProtectionBlocked => _isSunProtectionBlocked;

        public float LockedPercentage => 100;

        public bool IsPercentageLockActive => true;

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

        public async Task BlockSunProtectionAsync(TimeSpan? timeout = null)
        {
            logger.LogInformation("ShutterDevice {DeviceId} blocking sun protection", Id);
            await _knxService.WriteGroupValueAsync(Addresses.SunProtectionBlockControl, true);
            _isSunProtectionBlocked = true;
            _lastUpdated = DateTime.Now;
            
            logger.LogInformation("ShutterDevice {DeviceId} sun protection blocked", Id);
            Console.WriteLine($"ShutterDevice {Id} sun protection blocked");
        }

        public async Task UnblockSunProtectionAsync(TimeSpan? timeout = null)
        {
            logger.LogInformation("ShutterDevice {DeviceId} unblocking sun protection", Id);
            await _knxService.WriteGroupValueAsync(Addresses.SunProtectionBlockControl, false);
            _isSunProtectionBlocked = false;
            _lastUpdated = DateTime.Now;
            
            logger.LogInformation("ShutterDevice {DeviceId} sun protection unblocked", Id);
            Console.WriteLine($"ShutterDevice {Id} sun protection unblocked");
        }

        public async Task SetSunProtectionBlockStateAsync(bool blocked, TimeSpan? timeout = null)
        {
            if (blocked)
            {
                await BlockSunProtectionAsync(timeout);
            }
            else
            {
                await UnblockSunProtectionAsync(timeout);
            }
        }

        public async Task<bool> ReadSunProtectionBlockStateAsync()
        {
            logger.LogDebug("ShutterDevice {DeviceId} reading sun protection block state", Id);
            
            // Read actual state from KNX bus
            var blockState = await _knxService.RequestGroupValue<bool>(Addresses.SunProtectionBlockFeedback);
            _isSunProtectionBlocked = blockState;
            _lastUpdated = DateTime.Now;
            
            logger.LogDebug("ShutterDevice {DeviceId} sun protection block state: {BlockState}", Id, blockState);
            return blockState;
        }

        public async Task<bool> WaitForSunProtectionBlockStateAsync(bool targetState, TimeSpan? timeout = null)
        {
            var actualTimeout = timeout ?? _defaulTimeout;
            var endTime = DateTime.Now + actualTimeout;
            
            logger.LogDebug("ShutterDevice {DeviceId} waiting for sun protection block state: {TargetState} (timeout: {Timeout})", 
                Id, targetState, actualTimeout);
            
            while (DateTime.Now < endTime)
            {
                var currentState = await ReadSunProtectionBlockStateAsync();
                if (currentState == targetState)
                {
                    logger.LogDebug("ShutterDevice {DeviceId} reached target sun protection block state: {TargetState}", Id, targetState);
                    return true;
                }
                
                await Task.Delay(100); // Check every 100ms
            }
            
            logger.LogWarning("ShutterDevice {DeviceId} timeout waiting for sun protection block state: {TargetState}", Id, targetState);
            return false;
        }

        #endregion
    }
}
