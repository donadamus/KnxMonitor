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
        private readonly PercentageControllableDeviceHelper<ShutterDevice, ShutterAddresses> _shutterHelper;
        private readonly MovementControllableDeviceHelper<ShutterDevice, ShutterAddresses> _shutterMovementHelper;
        private readonly SunProtectionDeviceHelper<ShutterDevice, ShutterAddresses> _sunProtectionHelper;
        internal float _currentPercentage = 0.0f; // Start fully open
        private TimeSpan _cooldown = TimeSpan.FromSeconds(2);
        private bool _isActive = false; // Movement status: true = moving, false = stopped
        private bool _isSunProtectionBlocked = false; // Sun protection block status
        
        // Sun protection threshold states
        private bool _brightnessThreshold1Active = false;
        private bool _brightnessThreshold2Active = false;
        private bool _outdoorTemperatureThresholdActive = false;
        
        // Saved state for testing
        private float? _savedPercentage;
        private bool? _savedSunProtectionBlocked;
        float? IPercentageControllable.SavedPercentage => _savedPercentage;

        /// <summary>
        /// Convenience constructor that automatically creates addresses based on subGroup
        /// </summary>
        public ShutterDevice(string id, string name, string subGroup, IKnxService knxService, ILogger<ShutterDevice> logger, TimeSpan defaulTimeout, TimeSpan? cooldown = null)
            : base(id, name, subGroup, KnxAddressConfiguration.CreateShutterAddresses(subGroup), knxService, logger, defaulTimeout)
        {
            _shutterHelper = new PercentageControllableDeviceHelper<ShutterDevice, ShutterAddresses>(this, this.Addresses,
                            _knxService, Id, "ShutterDevice",
                            logger, defaulTimeout
                            );

            _shutterMovementHelper = new MovementControllableDeviceHelper<ShutterDevice, ShutterAddresses>(this, this.Addresses,
                            _knxService, Id, "ShutterDevice",
                            logger, defaulTimeout
                            );

            _sunProtectionHelper = new SunProtectionDeviceHelper<ShutterDevice, ShutterAddresses>(this, this.Addresses,
                            _knxService, Id, "ShutterDevice",
                            logger, defaulTimeout
                            );

            _eventManager.MessageReceived += OnKnxMessageReceived;
            _cooldown = cooldown ?? _cooldown;

            Initialize(this);
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
                LastUpdated = DateTime.Now;
                
                _logger.LogInformation("ShutterDevice {DeviceId} sun protection block feedback: {BlockState}", 
                    Id, blockState ? "BLOCKED" : "UNBLOCKED");
                Console.WriteLine($"ShutterDevice {Id} sun protection block: {(blockState ? "BLOCKED" : "UNBLOCKED")}");
            }
            
            // Process sun protection status feedback (offset +100)
            if (e.Destination == Addresses.SunProtectionStatus)
            {
                var isActive = e.Value.AsBoolean();
                // This is the actual sun protection state (1=Active), but for now we use block state
               _logger.LogInformation("ShutterDevice {DeviceId} sun protection status: {Status}", 
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
                LastUpdated = DateTime.Now;
                
                _logger.LogInformation("ShutterDevice {DeviceId} brightness threshold 1: {State}", 
                    Id, thresholdActive ? "ACTIVE" : "INACTIVE");
                Console.WriteLine($"ShutterDevice {Id} brightness threshold 1: {(thresholdActive ? "ACTIVE" : "INACTIVE")}");
            }
            
            // Process brightness threshold 2 feedback
            if (e.Destination == Addresses.BrightnessThreshold2)
            {
                var thresholdActive = e.Value.AsBoolean();
                _brightnessThreshold2Active = thresholdActive;
                LastUpdated = DateTime.Now;
                
                _logger.LogInformation("ShutterDevice {DeviceId} brightness threshold 2: {State}", 
                    Id, thresholdActive ? "ACTIVE" : "INACTIVE");
                Console.WriteLine($"ShutterDevice {Id} brightness threshold 2: {(thresholdActive ? "ACTIVE" : "INACTIVE")}");
            }
            
            // Process outdoor temperature threshold feedback
            if (e.Destination == Addresses.OutdoorTemperatureThreshold)
            {
                var thresholdActive = e.Value.AsBoolean();
                _outdoorTemperatureThresholdActive = thresholdActive;
                LastUpdated = DateTime.Now;
                
                _logger.LogInformation("ShutterDevice {DeviceId} outdoor temperature threshold: {State}", 
                    Id, thresholdActive ? "ACTIVE" : "INACTIVE");
                Console.WriteLine($"ShutterDevice {Id} outdoor temperature threshold: {(thresholdActive ? "ACTIVE" : "INACTIVE")}");
            }
        }

        private async Task WaitForCooldownAsync()
        {
            var elapsed = DateTime.Now - LastUpdated;

            if (elapsed < _cooldown && elapsed > TimeSpan.Zero)
            {
                await Task.Delay(_cooldown - elapsed);
            }
        }


        #region IKnxDeviceBase Methods

        public override async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing ShutterDevice {DeviceId} ({DeviceName})", Id, Name);
            
            await base.InitializeAsync();
            // Read initial states from KNX bus
            _currentPercentage = await ReadPercentageAsync();
            _isSunProtectionBlocked = await ReadSunProtectionBlockStateAsync();
            
            // Read initial threshold states
            _brightnessThreshold1Active = await ReadBrightnessThreshold1StateAsync();
            _brightnessThreshold2Active = await ReadBrightnessThreshold2StateAsync();
            _outdoorTemperatureThresholdActive = await ReadOutdoorTemperatureThresholdStateAsync();

            _isActive = await ReadActivityStatusAsync();

            LastUpdated = DateTime.Now;
            
            _logger.LogInformation("ShutterDevice {DeviceId} initialized - Position: {Position}%, SunProtectionBlocked: {SunProtectionBlocked}, Thresholds: B1={BrightThreshold1}, B2={BrightThreshold2}, Temp={TempThreshold}", 
                Id, _currentPercentage, _isSunProtectionBlocked, _brightnessThreshold1Active, _brightnessThreshold2Active, _outdoorTemperatureThresholdActive);
            
        }

        public override void SaveCurrentState()
        {
            base.SaveCurrentState();
            _savedPercentage = _currentPercentage;
            _savedSunProtectionBlocked = _isSunProtectionBlocked;
            Console.WriteLine($"ShutterDevice {Id} state saved - Position: {_savedPercentage}%, SunProtectionBlocked: {_savedSunProtectionBlocked}");
        }

        public override async Task RestoreSavedStateAsync(TimeSpan? timeout = null)
        {
            if (_savedPercentage.HasValue && _savedPercentage.Value != _currentPercentage)
            {
                // Unlock before changing switch state if necessary
                if (CurrentLockState == Lock.On)
                {
                    await UnlockAsync(timeout ?? _defaultTimeout);
                }

                await SetPercentageAsync(_savedPercentage.Value, timeout ?? _defaultTimeout);
            }

            // Restore sun protection block state if it was saved and is different
            if (_savedSunProtectionBlocked.HasValue && _savedSunProtectionBlocked.Value != _isSunProtectionBlocked)
            {
                await SetSunProtectionBlockStateAsync(_savedSunProtectionBlocked.Value, timeout ?? _defaultTimeout);
            }

            await base.RestoreSavedStateAsync(timeout ?? _defaultTimeout);
            _logger.LogInformation("ShutterDevice {DeviceId} initialized - Position: {Position}%, Lock: {LockState}, SunProtectionBlocked: {SunProtectionBlocked}",
    Id, _currentPercentage, CurrentLockState, _isSunProtectionBlocked);
            Console.WriteLine($"ShutterDevice {Id} state restored - Position: {_savedPercentage}%, SunProtectionBlocked: {_savedSunProtectionBlocked}");
        }

        #endregion

        #region IPercentageControllable Implementation

        public float CurrentPercentage => _currentPercentage;

        public async Task SetPercentageAsync(float percentage, TimeSpan? timeout = null)
        {
            await WaitForCooldownAsync();
            _logger.LogInformation($"ShutterDevice {Id} set percentage to {percentage}%");
            await _shutterHelper.SetPercentageAsync(percentage, timeout);
            _logger.LogInformation($"ShutterDevice {Id} current percentage is now {_currentPercentage}%");
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
            _logger.LogInformation($"ShutterDevice {Id} adjusting percentage by {delta}%, timeout: {timeout?.TotalSeconds ?? 0}s");
            await _shutterHelper.AdjustPercentageAsync(delta, timeout);
            _logger.LogInformation($"ShutterDevice {Id} adjusted percentage by {delta}%, new value: {_currentPercentage}%");
        }

        #endregion

        #region IActivityStatusReadable Implementation

        public bool IsActive => _isActive;

        public bool IsSunProtectionBlocked => _isSunProtectionBlocked;

        public float LockedPercentage => 100;

        public bool IsPercentageLockActive => true;

        public async Task<bool> ReadActivityStatusAsync()
        {
            return await _shutterMovementHelper.ReadActivityStatusAsync();
        }

        public async Task<bool> WaitForInactiveAsync(TimeSpan? timeout = null)
        {
            return await _shutterMovementHelper.WaitForInactiveAsync(timeout);
        }

        public async Task<bool> WaitForActiveAsync(TimeSpan? timeout = null)
        {
            return await _shutterMovementHelper.WaitForActiveAsync(timeout);
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
            _logger.LogInformation("ShutterDevice {DeviceId} blocking sun protection", Id);

            await _sunProtectionHelper.BlockSunProtectionAsync(timeout);
        }

        public async Task UnblockSunProtectionAsync(TimeSpan? timeout = null)
        {
            _logger.LogInformation("ShutterDevice {DeviceId} unblocking sun protection", Id);
            await _sunProtectionHelper.UnblockSunProtectionAsync(timeout);
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
            _logger.LogDebug("ShutterDevice {DeviceId} reading sun protection block state", Id);
            
            // Read actual state from KNX bus
            var blockState = await _knxService.RequestGroupValue<bool>(Addresses.SunProtectionBlockFeedback);
            
            return blockState;
        }

        public async Task<bool> WaitForSunProtectionBlockStateAsync(bool targetState, TimeSpan? timeout = null)
        {
            return await _sunProtectionHelper.WaitForSunProtectionBlockStateAsync(targetState, timeout);
        }

        #endregion

        #region ISunProtectionThresholdCapableDevice Implementation

        public bool BrightnessThreshold1Active => _brightnessThreshold1Active;
        public bool BrightnessThreshold2Active => _brightnessThreshold2Active;
        public bool OutdoorTemperatureThresholdActive => _outdoorTemperatureThresholdActive;

        public bool SunProtectionActive => throw new NotImplementedException();

        public async Task<bool> ReadBrightnessThreshold1StateAsync()
        {
            _logger.LogDebug("ShutterDevice {DeviceId} reading brightness threshold 1 state", Id);
            
            // Read actual state from KNX bus
            var thresholdState = await _knxService.RequestGroupValue<bool>(Addresses.BrightnessThreshold1);
            
            return thresholdState;
        }

        public async Task<bool> ReadBrightnessThreshold2StateAsync()
        {
            _logger.LogDebug("ShutterDevice {DeviceId} reading brightness threshold 2 state", Id);
            
            // Read actual state from KNX bus
            var thresholdState = await _knxService.RequestGroupValue<bool>(Addresses.BrightnessThreshold2);
            
            return thresholdState;
        }

        public async Task<bool> ReadOutdoorTemperatureThresholdStateAsync()
        {
            _logger.LogDebug("ShutterDevice {DeviceId} reading outdoor temperature threshold state", Id);
            
            // Read actual state from KNX bus
            var thresholdState = await _knxService.RequestGroupValue<bool>(Addresses.OutdoorTemperatureThreshold);
            
            return thresholdState;
        }

        public async Task<bool> WaitForBrightnessThreshold1StateAsync(bool targetState, TimeSpan? timeout = null)
        {
            return await _sunProtectionHelper.WaitForBrightnessThreshold1StateAsync(targetState, timeout);
        }

        public async Task<bool> WaitForBrightnessThreshold2StateAsync(bool targetState, TimeSpan? timeout = null)
        {
            return await _sunProtectionHelper.WaitForBrightnessThreshold2StateAsync(targetState, timeout);
        }

        public async Task<bool> WaitForOutdoorTemperatureThresholdStateAsync(bool targetState, TimeSpan? timeout = null)
        {
            return await _sunProtectionHelper.WaitForOutdoorTemperatureThresholdStateAsync(targetState, timeout);
        }

        void IPercentageControllable.SetPercentageForTest(float currentPercentage)
        {
            _currentPercentage = currentPercentage;
            LastUpdated = DateTime.Now;
        }
        void IPercentageControllable.SetSavedPercentageForTest(float currentPercentage)
        {
            _savedPercentage = currentPercentage;
        }

        public async Task<bool> ReadSunProtectionStateAsync()
        {
            return await _sunProtectionHelper.ReadSunProtectionStateAsync();
        }

        public async Task<bool> WaitForSunProtectionStateAsync(bool targetState, TimeSpan? timeout = null)
        {
            return await _sunProtectionHelper.WaitForSunProtectionStateAsync(targetState, timeout);
        }
        #endregion
    }
}
