using KnxModel.Models.Helpers;
using Microsoft.Extensions.Logging;
using System;

namespace KnxModel
{

    


    public class DimmerDevice : LightDeviceBase<DimmerDevice, DimmerAddresses>, IDimmerDevice, IPercentageLockableDevice
    {
        private readonly ILogger<DimmerDevice> _logger;
        internal float _currentPercentage = -1.0f; // 0% brightness
        private float? _savedPercentage;

        float? IPercentageControllable.SavedPercentage => _savedPercentage;

        private readonly PercentageControllableDeviceHelper<DimmerDevice, DimmerAddresses> _percentageControllableHelper;

        public DimmerDevice(string id, string name, string subGroup, IKnxService knxService, ILogger<DimmerDevice> logger, TimeSpan defaulTimeout)
            : base(id, name, subGroup, KnxAddressConfiguration.CreateDimmerAddresses(subGroup), knxService, logger, defaulTimeout)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Initialize(this);

            _percentageControllableHelper = new PercentageControllableDeviceHelper<DimmerDevice, DimmerAddresses>(this, this.Addresses,
                _knxService, Id, "DimmerDevice",
                logger, defaulTimeout);

            _eventManager.MessageReceived += OnKnxMessageReceived;
        }

        private void OnKnxMessageReceived(object? sender, KnxGroupEventArgs e)
        {
            // Process percentage control messages
            _percentageControllableHelper.ProcessSwitchMessage(e);

        }
        /// <summary>
        /// Waits for the required cooldown period between shutter commands.
        /// Physical shutters need a minimum 2-second delay between commands to prevent
        /// timing-based position tracking synchronization issues.
        /// </summary>


        public override async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing DimmerDevice {DeviceId} ({DeviceName})", Id, Name);
            
            // Read initial states from KNX bus
            _currentSwitchState = await ReadSwitchStateAsync();
            _currentLockState = await ReadLockStateAsync();
            _currentPercentage = await ReadPercentageAsync();
            _lastUpdated = DateTime.Now;

            _logger.LogInformation("DimmerDevice {DeviceId} initialized - Switch: {SwitchState}, Lock: {LockState}, Brightness: {Brightness}%", 
                Id, _currentSwitchState, _currentLockState, _currentPercentage);

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
                    await UnlockAsync(timeout ?? _defaulTimeout);
                }

                await SetPercentageAsync(_savedPercentage.Value, timeout ?? _defaulTimeout);
            }

            await base.RestoreSavedStateAsync(timeout ?? _defaulTimeout);
           Console.WriteLine($"DimmerDevice {Id} state restored - Brightness: {_currentPercentage}%");
        }



        #region IPercentageControllable Implementation

        public float CurrentPercentage => _currentPercentage;

        public float LockedPercentage => 0;

        public bool IsPercentageLockActive => true;

        public async Task SetPercentageAsync(float percentage, TimeSpan? timeout = null)
        {
            await _percentageControllableHelper.SetPercentageAsync(percentage, timeout);
        }

        public async Task<float> ReadPercentageAsync()
        {
            return await _percentageControllableHelper.ReadPercentageAsync();
        }

        public async Task<bool> WaitForPercentageAsync(float targetPercentage, double tolerance = 1.0, TimeSpan? timeout = null)
        {
            return await _percentageControllableHelper.WaitForPercentageAsync(targetPercentage, tolerance, timeout);
        }
        public async Task AdjustPercentageAsync(float increment, TimeSpan? timeout = null)
        {
            await _percentageControllableHelper.AdjustPercentageAsync(increment, timeout);
        }


        void IPercentageControllable.SetPercentageForTest(float currentPercentage)
        {
            _currentPercentage = currentPercentage;
            _lastUpdated = DateTime.Now;
        }
        void IPercentageControllable.SetSavedPercentageForTest(float currentPercentage)
        {
            _savedPercentage = currentPercentage;
        }

        #endregion


    }
}
