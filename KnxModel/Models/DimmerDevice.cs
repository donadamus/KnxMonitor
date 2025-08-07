using KnxModel.Models.Helpers;
using System;

namespace KnxModel
{
    public class DimmerDevice : LightDeviceBase<DimmerAddresses>, IDimmerDevice
    {

        private float _currentPercentage = -1.0f; // 0% brightness
        private float? _savedPercentage;
        private readonly PercentageControllableDeviceHelper _percentageControllableHelper;

        public DimmerDevice(string id, string name, string subGroup, IKnxService knxService)
            : base(id, name, subGroup, KnxAddressConfiguration.CreateDimmerAddresses(subGroup), knxService)
        {

            _percentageControllableHelper = new PercentageControllableDeviceHelper(
                _knxService, Id, "DimmerDevice",
                () => Addresses,
                percentage => { _currentPercentage = percentage; _lastUpdated = DateTime.Now; },
                () => _currentPercentage);

            _eventManager.MessageReceived += OnKnxMessageReceived;
        }

        private void OnKnxMessageReceived(object? sender, KnxGroupEventArgs e)
        {
            // Process percentage control messages
            _percentageControllableHelper.ProcessSwitchMessage(e);

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

            await _percentageControllableHelper.SetPercentageAsync(percentage, timeout);
        }

        public async Task<float> ReadPercentageAsync()
        {
            return await _percentageControllableHelper.ReadPercentageAsync();
        }

        public async Task<bool> WaitForPercentageAsync(float targetPercentage, double tolerance = 1.0, TimeSpan? timeout = null)
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

        public async Task FadeToAsync(float targetBrightness, TimeSpan duration)
        {
            if (targetBrightness < 0 || targetBrightness > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(targetBrightness), "Target brightness must be between 0 and 100");
            }

            Console.WriteLine($"Fading dimmer {Id} to {targetBrightness}% over {duration.TotalSeconds:F1} seconds");

            var startBrightness = _currentPercentage;
            var stepCount = Math.Max(1, (int)(duration.TotalMilliseconds / 100)); // Step every 100ms
            var stepSize = (targetBrightness - startBrightness) / (float)stepCount;
            var stepDelay = duration.TotalMilliseconds / stepCount;

            for (int i = 1; i <= stepCount; i++)
            {
                var currentTarget = startBrightness + (int)(stepSize * i);
                await SetPercentageAsync(currentTarget, TimeSpan.FromMilliseconds(stepDelay));

                if (i < stepCount) // Don't delay after the last step
                {
                    await Task.Delay((int)stepDelay);
                }
            }

            Console.WriteLine($"Fade completed for dimmer {Id}");
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
