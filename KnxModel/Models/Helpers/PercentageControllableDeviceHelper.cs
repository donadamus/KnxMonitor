using Microsoft.Extensions.Logging;

namespace KnxModel.Models.Helpers
{
    public class PercentageControllableDeviceHelper<T> : DeviceHelperBase<T>
        where T : class
    {
        private readonly Func<IPercentageControllableAddress> _getAddresses;
        private readonly Action<float> _updatePercentage;
        private readonly Func<float> _getCurrentPercentage;
        private readonly ILogger<T> logger;

        public PercentageControllableDeviceHelper(
            IKnxService knxService,
            string deviceId,
            string deviceType,
            Func<IPercentageControllableAddress> getAddresses,
            Action<float> updatePercentage,
            Func<float> getCurrentPercentage,
            ILogger<T> logger) : base(knxService, deviceId, deviceType, logger)
        {
            _getAddresses = getAddresses ?? throw new ArgumentNullException(nameof(getAddresses));
            _updatePercentage = updatePercentage ?? throw new ArgumentNullException(nameof(updatePercentage));
            _getCurrentPercentage = getCurrentPercentage ?? throw new ArgumentNullException(nameof(getCurrentPercentage));
            this.logger = logger;
        }

        internal async Task AdjustPercentageAsync(float increment, TimeSpan? timeout)
        {
            var newPercentage = _getCurrentPercentage() + increment;
            newPercentage = Math.Max(0.0f, Math.Min(100.0f, newPercentage)); // Clamp to 0-100

            await SetPercentageAsync(newPercentage, timeout);
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
            if (percentage < 0.0f || percentage > 100.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between 0 and 100");
            }

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
            if (targetPercentage < 0.0f || targetPercentage > 100.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(targetPercentage), "Target percentage must be between 0 and 100");
            }

            return await WaitForConditionAsync(
                () => Math.Abs(_getCurrentPercentage() - targetPercentage) <= tolerance,
                timeout ?? _defaultTimeout,
                $"percentage {targetPercentage} ± {tolerance}"
            );
        }
    }
}
