namespace KnxModel.Models.Helpers
{
    public class PercentageControllableDeviceHelper : DeviceHelperBase
    {
        private readonly Func<IPercentageControllableAddress> _getAddresses;
        private readonly Action<float> _updatePercentage;
        private readonly Func<float> _getCurrentPercentage;
        public PercentageControllableDeviceHelper(
            IKnxService knxService,
            string deviceId,
            string deviceType,
            Func<IPercentageControllableAddress> getAddresses,
            Action<float> updatePercentage,
            Func<float> getCurrentPercentage) : base(knxService, deviceId, deviceType)
        {
            _getAddresses = getAddresses ?? throw new ArgumentNullException(nameof(getAddresses));
            _updatePercentage = updatePercentage ?? throw new ArgumentNullException(nameof(updatePercentage));
            _getCurrentPercentage = getCurrentPercentage ?? throw new ArgumentNullException(nameof(getCurrentPercentage));
        }

        internal void ProcessSwitchMessage(KnxGroupEventArgs e)
        {
            var addresses = _getAddresses();
            if (e.Destination == addresses.PercentageFeedback)
            {
                var brightness = e.Value.AsPercentageValue();
                _updatePercentage(brightness);
                Console.WriteLine($"{_deviceType} {_deviceId} brightness updated via feedback: {brightness}%");
            }
        }

        internal async Task<float> ReadPercentageAsync()
        {
            try
            {
                var addresses = _getAddresses();
                return await _knxService.RequestGroupValue<float>(addresses.PercentageFeedback);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read brightness for {_deviceType} {_deviceId}: {ex.Message}");
                throw;
            }
        }

        internal async Task SetPercentageAsync(float percentage, TimeSpan? timeout)
        {
            // Use brightness as float directly (0-100) - KnxService converts to KNX byte range
            await SetFloatFunctionAsync(
                _getAddresses().PercentageControl,
                percentage,
                () => Math.Abs(_getCurrentPercentage() - percentage) <= 1, // Allow 1% tolerance
                timeout
            );
        }

        internal async Task<bool> WaitForPercentageAsync(float targetPercentage, double tolerance, TimeSpan? timeout)
        {
            return await WaitForConditionAsync(
                () => Math.Abs(_getCurrentPercentage() - targetPercentage) <= tolerance,
                timeout ?? _defaultTimeout,
                $"percentage {targetPercentage} ± {tolerance}"
            );
        }
    }
}
