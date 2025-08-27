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

        public float CurrentPercentage { get; private set; } = -1.0f;
        float IPercentageControllable.CurrentPercentage { get => CurrentPercentage; set => CurrentPercentage = value; }
        public float? SavedPercentage { get; private set; }
        float? IPercentageControllable.SavedPercentage { get => SavedPercentage; set => SavedPercentage = value; }

        public bool SunProtectionBlocked { get; private set; }
        bool ISunProtectionBlockableDevice.SunProtectionBlocked { get => SunProtectionBlocked; set => SunProtectionBlocked = value; }
        public bool? SavedSunProtectionBlocked { get; private set; }
        bool? ISunProtectionBlockableDevice.SavedSunProtectionBlocked { get => SavedSunProtectionBlocked; set => SavedSunProtectionBlocked = value; }
        
        private readonly PercentageControllableDeviceHelper<ShutterDevice, ShutterAddresses> _percentageHelper;
        private readonly MovementControllableDeviceHelper<ShutterDevice, ShutterAddresses> _movementHelper;
        private readonly SunProtectionDeviceHelper<ShutterDevice, ShutterAddresses> _sunProtectionHelper;
        private TimeSpan _cooldown = TimeSpan.FromSeconds(2);

        public bool CurrentMovementOrientation { get; private set; }
        bool IMovementControllable.CurrentDirection { get => CurrentMovementOrientation; set => CurrentMovementOrientation = value; }
        // Sun protection threshold states

        public bool BrightnessThreshold1Active { get; private set; }
        bool ISunProtectionThresholdCapableDevice.BrightnessThreshold1Active { get => BrightnessThreshold1Active; set => BrightnessThreshold1Active = value; }

        public bool BrightnessThreshold2Active { get; private set; }
        bool ISunProtectionThresholdCapableDevice.BrightnessThreshold2Active { get => BrightnessThreshold2Active; set => BrightnessThreshold2Active = value; }

        public bool OutdoorTemperatureThresholdActive { get; private set; }
        bool ISunProtectionThresholdCapableDevice.OutdoorTemperatureThresholdActive { get => OutdoorTemperatureThresholdActive; set => OutdoorTemperatureThresholdActive = value; }

        public bool SunProtectionActive { get; private set; }
        bool ISunProtectionThresholdCapableDevice.SunProtectionActive { get => SunProtectionActive; set => SunProtectionActive = value; }


        public bool IsActive { get; private set; }
        bool IActivityStatusReadable.IsActive { get => IsActive; set => IsActive = value; }



        /// <summary>
        /// Convenience constructor that automatically creates addresses based on subGroup
        /// </summary>
        public ShutterDevice(string id, string name, string subGroup, IKnxService knxService, ILogger<ShutterDevice> logger, TimeSpan defaulTimeout, TimeSpan? cooldown = null)
            : base(id, name, subGroup, KnxAddressConfiguration.CreateShutterAddresses(subGroup), knxService, logger, defaulTimeout)
        {
            _percentageHelper = new PercentageControllableDeviceHelper<ShutterDevice, ShutterAddresses>(this, this.Addresses,
                            _knxService, Id, "ShutterDevice",
                            logger, defaulTimeout
                            );

            _movementHelper = new MovementControllableDeviceHelper<ShutterDevice, ShutterAddresses>(this, this.Addresses,
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
            _percentageHelper.ProcessSwitchMessage(e);
            
            // Process movement feedback messages
            _movementHelper.ProcessMovementMessage(e);

            // Process sun protection block feedback
            _sunProtectionHelper.ProcessSunProtectionMessage(e);

            // Process threshold feedback
            _sunProtectionHelper.ProcessThresholdMessage(e);
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
            CurrentPercentage = await ReadPercentageAsync();
            SunProtectionBlocked = await ReadSunProtectionBlockStateAsync();
            
            // Read initial threshold states
            BrightnessThreshold1Active = await ReadBrightnessThreshold1StateAsync();
            BrightnessThreshold2Active = await ReadBrightnessThreshold2StateAsync();
            OutdoorTemperatureThresholdActive = await ReadOutdoorTemperatureThresholdStateAsync();

            IsActive = await ReadActivityStatusAsync();

            LastUpdated = DateTime.Now;
            
            _logger.LogInformation("ShutterDevice {DeviceId} initialized - Position: {Position}%, SunProtectionBlocked: {SunProtectionBlocked}, Thresholds: B1={BrightThreshold1}, B2={BrightThreshold2}, Temp={TempThreshold}", 
                Id, CurrentPercentage, SunProtectionBlocked, BrightnessThreshold1Active, BrightnessThreshold2Active, OutdoorTemperatureThresholdActive);
            
        }

        public override void SaveCurrentState()
        {
            base.SaveCurrentState();
            SavedPercentage = CurrentPercentage;
            SavedSunProtectionBlocked = SunProtectionBlocked;
            Console.WriteLine($"ShutterDevice {Id} state saved - Position: {SavedPercentage}%, SunProtectionBlocked: {SavedSunProtectionBlocked}");
        }

        public override async Task RestoreSavedStateAsync(TimeSpan? timeout = null)
        {
            if (SavedPercentage.HasValue && SavedPercentage.Value != CurrentPercentage)
            {
                // Unlock before changing switch state if necessary
                if (CurrentLockState == Lock.On)
                {
                    await UnlockAsync(timeout ?? _defaultTimeout);
                }

                await SetPercentageAsync(SavedPercentage.Value, timeout ?? _defaultTimeout);
            }

            // Restore sun protection block state if it was saved and is different
            if (SavedSunProtectionBlocked.HasValue && SavedSunProtectionBlocked.Value != SunProtectionBlocked)
            {
                await SetSunProtectionBlockStateAsync(SavedSunProtectionBlocked.Value, timeout ?? _defaultTimeout);
            }

            await base.RestoreSavedStateAsync(timeout ?? _defaultTimeout);
            _logger.LogInformation("ShutterDevice {DeviceId} initialized - Position: {Position}%, Lock: {LockState}, SunProtectionBlocked: {SunProtectionBlocked}",
    Id, CurrentPercentage, CurrentLockState, SunProtectionBlocked);
            Console.WriteLine($"ShutterDevice {Id} state restored - Position: {SavedPercentage}%, SunProtectionBlocked: {SavedSunProtectionBlocked}");
        }

        #endregion

        #region IPercentageControllable Implementation


        public async Task SetPercentageAsync(float percentage, TimeSpan? timeout = null)
        {
            await WaitForCooldownAsync();
            await _percentageHelper.SetPercentageAsync(percentage, timeout);
            _logger.LogInformation($"ShutterDevice {Id} current percentage is now {CurrentPercentage}%");
        }

        public async Task<float> ReadPercentageAsync()
        {
            return await _percentageHelper.ReadPercentageAsync();
        }

        public async Task<bool> WaitForPercentageAsync(float targetPercentage, double tolerance = 2.0, TimeSpan? timeout = null)
        {
            return await _percentageHelper.WaitForPercentageAsync(targetPercentage, tolerance, timeout);

        }

        public async Task AdjustPercentageAsync(float delta, TimeSpan? timeout = null)
        {
            await WaitForCooldownAsync();
            await _percentageHelper.AdjustPercentageAsync(delta, timeout);
            _logger.LogInformation($"ShutterDevice {Id} adjusted percentage by {delta}%, new value: {CurrentPercentage}%");
        }

        #endregion

        #region IActivityStatusReadable Implementation


        
        public float LockedPercentage => 100;

        public bool IsPercentageLockActive => true;


        public async Task<bool> ReadActivityStatusAsync()
        {
            return await _movementHelper.ReadActivityStatusAsync();
        }

        public async Task<bool> WaitForInactiveAsync(TimeSpan? timeout = null)
        {
            return await _movementHelper.WaitForInactiveAsync(timeout);
        }

        public async Task<bool> WaitForActiveAsync(TimeSpan? timeout = null)
        {
            return await _movementHelper.WaitForActiveAsync(timeout);
        }

        #endregion

        #region IShutterDevice Implementation (Convenience Methods)

        public async Task OpenAsync(TimeSpan? timeout = null)
        {
            await WaitForCooldownAsync();
            await _movementHelper.OpenAsync(timeout);
        }

        public async Task CloseAsync(TimeSpan? timeout = null)
        {
            await WaitForCooldownAsync();
            await _movementHelper.CloseAsync(timeout);
        }

        public async Task StopAsync(TimeSpan? timeout = null)
        {
            await _movementHelper.StopAsync(timeout);
            
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
