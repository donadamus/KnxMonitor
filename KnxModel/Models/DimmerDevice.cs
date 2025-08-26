using KnxModel.Models.Helpers;
using Microsoft.Extensions.Logging;
using System;

namespace KnxModel
{

    


    public class DimmerDevice : LightDeviceBase<DimmerDevice, DimmerAddresses>, IDimmerDevice, IPercentageLockableDevice
    {
        public float CurrentPercentage { get; private set; } = -1.0f;
        float IPercentageControllable.CurrentPercentage { get => CurrentPercentage; set => CurrentPercentage = value; }

        public float? SavedPercentage { get; private set; }
        float? IPercentageControllable.SavedPercentage { get => SavedPercentage; set => SavedPercentage = value; }


        private readonly PercentageControllableDeviceHelper<DimmerDevice, DimmerAddresses> _percentageControllableHelper;

        public DimmerDevice(string id, string name, string subGroup, IKnxService knxService, ILogger<DimmerDevice> logger, TimeSpan defaulTimeout)
            : base(id, name, subGroup, KnxAddressConfiguration.CreateDimmerAddresses(subGroup), knxService, logger, defaulTimeout)
        {
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

        public override async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing DimmerDevice {DeviceId} ({DeviceName})", Id, Name);
            await base.InitializeAsync();
            // Read initial states from KNX bus
            CurrentPercentage = await ReadPercentageAsync();
            LastUpdated = DateTime.Now;
            _logger.LogInformation("{type} {DeviceId} initialized - Brightness: {Brightness}%", typeof(DimmerDevice).Name, Id, CurrentPercentage);
        }

        public override void SaveCurrentState()
        {
            base.SaveCurrentState();
            SavedPercentage = CurrentPercentage; // Save current brightness percentage
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

            await base.RestoreSavedStateAsync(timeout ?? _defaultTimeout);
           Console.WriteLine($"DimmerDevice {Id} state restored - Brightness: {CurrentPercentage}%");
        }



        #region IPercentageControllable Implementation

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

        #endregion


    }
}
