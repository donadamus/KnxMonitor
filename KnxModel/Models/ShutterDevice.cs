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
    public class ShutterDevice : LockableDeviceBase<ShutterDevice, ShutterAddresses>, IShutterDevice, IPercentageLockableDevice, ISunProtectionThresholdCapableDevice
    {
        private readonly PercentageControllableDeviceHelper<ShutterDevice> _shutterHelper;
        private readonly ShutterDeviceHelper<ShutterDevice> _shutterMovementHelper;
        private readonly ILogger<ShutterDevice> logger;
        private float _currentPercentage = 0.0f; // Start fully open
        private bool _isActive = false; // Movement status: true = moving, false = stopped
        private bool _isSunProtectionBlocked = false; // Sun protection block status
        
        // Sun protection threshold states
        private bool _brightnessThreshold1Active = false;
        private bool _brightnessThreshold2Active = false;
        private bool _outdoorTemperatureThresholdActive = false;
        
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
            
            // Process threshold feedback
            ProcessThresholdFeedback(e);
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

        /// <summary>
        /// Processes threshold feedback messages for sun protection
        /// </summary>
        private void ProcessThresholdFeedback(KnxGroupEventArgs e)
        {
            // Process brightness threshold 1 feedback
            if (e.Destination == Addresses.BrightnessThreshold1)
            {
                var thresholdActive = e.Value.AsBoolean();
                _brightnessThreshold1Active = thresholdActive;
                _lastUpdated = DateTime.Now;
                
                logger.LogInformation("ShutterDevice {DeviceId} brightness threshold 1: {State}", 
                    Id, thresholdActive ? "ACTIVE" : "INACTIVE");
                Console.WriteLine($"ShutterDevice {Id} brightness threshold 1: {(thresholdActive ? "ACTIVE" : "INACTIVE")}");
            }
            
            // Process brightness threshold 2 feedback
            if (e.Destination == Addresses.BrightnessThreshold2)
            {
                var thresholdActive = e.Value.AsBoolean();
                _brightnessThreshold2Active = thresholdActive;
                _lastUpdated = DateTime.Now;
                
                logger.LogInformation("ShutterDevice {DeviceId} brightness threshold 2: {State}", 
                    Id, thresholdActive ? "ACTIVE" : "INACTIVE");
                Console.WriteLine($"ShutterDevice {Id} brightness threshold 2: {(thresholdActive ? "ACTIVE" : "INACTIVE")}");
            }
            
            // Process outdoor temperature threshold feedback
            if (e.Destination == Addresses.OutdoorTemperatureThreshold)
            {
                var thresholdActive = e.Value.AsBoolean();
                _outdoorTemperatureThresholdActive = thresholdActive;
                _lastUpdated = DateTime.Now;
                
                logger.LogInformation("ShutterDevice {DeviceId} outdoor temperature threshold: {State}", 
                    Id, thresholdActive ? "ACTIVE" : "INACTIVE");
                Console.WriteLine($"ShutterDevice {Id} outdoor temperature threshold: {(thresholdActive ? "ACTIVE" : "INACTIVE")}");
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
            
            // Read initial threshold states
            _brightnessThreshold1Active = await ReadBrightnessThreshold1StateAsync();
            _brightnessThreshold2Active = await ReadBrightnessThreshold2StateAsync();
            _outdoorTemperatureThresholdActive = await ReadOutdoorTemperatureThresholdStateAsync();
            
            _lastUpdated = DateTime.Now;
            
            logger.LogInformation("ShutterDevice {DeviceId} initialized - Position: {Position}%, Lock: {LockState}, SunProtectionBlocked: {SunProtectionBlocked}, Thresholds: B1={BrightThreshold1}, B2={BrightThreshold2}, Temp={TempThreshold}", 
                Id, _currentPercentage, _currentLockState, _isSunProtectionBlocked, _brightnessThreshold1Active, _brightnessThreshold2Active, _outdoorTemperatureThresholdActive);
            
            Console.WriteLine($"ShutterDevice {Id} initialized - Position: {_currentPercentage}%, Lock: {_currentLockState}, SunProtectionBlocked: {_isSunProtectionBlocked}, Thresholds: B1={_brightnessThreshold1Active}, B2={_brightnessThreshold2Active}, Temp={_outdoorTemperatureThresholdActive}");
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

        #region ISunProtectionThresholdCapableDevice Implementation

        public bool BrightnessThreshold1Active => _brightnessThreshold1Active;
        public bool BrightnessThreshold2Active => _brightnessThreshold2Active;
        public bool OutdoorTemperatureThresholdActive => _outdoorTemperatureThresholdActive;

        public async Task<bool> ReadBrightnessThreshold1StateAsync()
        {
            logger.LogDebug("ShutterDevice {DeviceId} reading brightness threshold 1 state", Id);
            
            // Read actual state from KNX bus
            var thresholdState = await _knxService.RequestGroupValue<bool>(Addresses.BrightnessThreshold1);
            _brightnessThreshold1Active = thresholdState;
            _lastUpdated = DateTime.Now;
            
            logger.LogDebug("ShutterDevice {DeviceId} brightness threshold 1 state: {State}", Id, thresholdState);
            return thresholdState;
        }

        public async Task<bool> ReadBrightnessThreshold2StateAsync()
        {
            logger.LogDebug("ShutterDevice {DeviceId} reading brightness threshold 2 state", Id);
            
            // Read actual state from KNX bus
            var thresholdState = await _knxService.RequestGroupValue<bool>(Addresses.BrightnessThreshold2);
            _brightnessThreshold2Active = thresholdState;
            _lastUpdated = DateTime.Now;
            
            logger.LogDebug("ShutterDevice {DeviceId} brightness threshold 2 state: {State}", Id, thresholdState);
            return thresholdState;
        }

        public async Task<bool> ReadOutdoorTemperatureThresholdStateAsync()
        {
            logger.LogDebug("ShutterDevice {DeviceId} reading outdoor temperature threshold state", Id);
            
            // Read actual state from KNX bus
            var thresholdState = await _knxService.RequestGroupValue<bool>(Addresses.OutdoorTemperatureThreshold);
            _outdoorTemperatureThresholdActive = thresholdState;
            _lastUpdated = DateTime.Now;
            
            logger.LogDebug("ShutterDevice {DeviceId} outdoor temperature threshold state: {State}", Id, thresholdState);
            return thresholdState;
        }

        public async Task<bool> WaitForBrightnessThreshold1StateAsync(bool targetState, TimeSpan? timeout = null)
        {
            var actualTimeout = timeout ?? _defaulTimeout;
            var endTime = DateTime.Now + actualTimeout;
            
            logger.LogDebug("ShutterDevice {DeviceId} waiting for brightness threshold 1 state: {TargetState} (timeout: {Timeout})", 
                Id, targetState, actualTimeout);
            
            while (DateTime.Now < endTime)
            {
                var currentState = await ReadBrightnessThreshold1StateAsync();
                if (currentState == targetState)
                {
                    logger.LogDebug("ShutterDevice {DeviceId} reached target brightness threshold 1 state: {TargetState}", Id, targetState);
                    return true;
                }
                
                await Task.Delay(100); // Check every 100ms
            }
            
            logger.LogWarning("ShutterDevice {DeviceId} timeout waiting for brightness threshold 1 state: {TargetState}", Id, targetState);
            return false;
        }

        public async Task<bool> WaitForBrightnessThreshold2StateAsync(bool targetState, TimeSpan? timeout = null)
        {
            var actualTimeout = timeout ?? _defaulTimeout;
            var endTime = DateTime.Now + actualTimeout;
            
            logger.LogDebug("ShutterDevice {DeviceId} waiting for brightness threshold 2 state: {TargetState} (timeout: {Timeout})", 
                Id, targetState, actualTimeout);
            
            while (DateTime.Now < endTime)
            {
                var currentState = await ReadBrightnessThreshold2StateAsync();
                if (currentState == targetState)
                {
                    logger.LogDebug("ShutterDevice {DeviceId} reached target brightness threshold 2 state: {TargetState}", Id, targetState);
                    return true;
                }
                
                await Task.Delay(100); // Check every 100ms
            }
            
            logger.LogWarning("ShutterDevice {DeviceId} timeout waiting for brightness threshold 2 state: {TargetState}", Id, targetState);
            return false;
        }

        public async Task<bool> WaitForOutdoorTemperatureThresholdStateAsync(bool targetState, TimeSpan? timeout = null)
        {
            var actualTimeout = timeout ?? _defaulTimeout;
            var endTime = DateTime.Now + actualTimeout;
            
            logger.LogDebug("ShutterDevice {DeviceId} waiting for outdoor temperature threshold state: {TargetState} (timeout: {Timeout})", 
                Id, targetState, actualTimeout);
            
            while (DateTime.Now < endTime)
            {
                var currentState = await ReadOutdoorTemperatureThresholdStateAsync();
                if (currentState == targetState)
                {
                    logger.LogDebug("ShutterDevice {DeviceId} reached target outdoor temperature threshold state: {TargetState}", Id, targetState);
                    return true;
                }
                
                await Task.Delay(100); // Check every 100ms
            }
            
            logger.LogWarning("ShutterDevice {DeviceId} timeout waiting for outdoor temperature threshold state: {TargetState}", Id, targetState);
            return false;
        }

        #endregion
    }
}
