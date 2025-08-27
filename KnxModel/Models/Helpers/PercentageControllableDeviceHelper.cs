using Microsoft.Extensions.Logging;
using System;

namespace KnxModel.Models.Helpers
{
    public class PercentageControllableDeviceHelper<T, TAddress> : DeviceHelperBase<T, TAddress>
        where T : IPercentageControllable, IKnxDeviceBase
        where TAddress : IPercentageControllableAddress
    {
        public PercentageControllableDeviceHelper(T owner, TAddress addresses, IKnxService knxService, string deviceId, string deviceType,
            ILogger<T> logger, TimeSpan defaultTimeout) 
            : base(owner, addresses, knxService, deviceId, deviceType, logger, defaultTimeout)
        {
        }

        internal async Task AdjustPercentageAsync(float increment, TimeSpan? timeout)
        {
            var newPercentage = owner.CurrentPercentage + increment;
            newPercentage = Math.Max(0.0f, Math.Min(100.0f, newPercentage)); // Clamp to 0-100

            await SetPercentageAsync(newPercentage, timeout);
        }

        internal void ProcessSwitchMessage(KnxGroupEventArgs e)
        {
            if (e.Destination == addresses.PercentageFeedback)
            {
                var brightness = e.Value.AsPercentageValue();
                
                // Update state through dynamic access to the device base
                owner.CurrentPercentage = brightness;
                owner.LastUpdated = DateTime.Now;
                
                _logger.LogInformation("{DeviceType} {DeviceId} brightness updated via feedback: {Brightness}%", _deviceType, _deviceId, brightness);
            }
        }

        internal async Task<float> ReadPercentageAsync()
        {
            try
            {
                return await _knxService.RequestGroupValue<float>(addresses.PercentageFeedback);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read brightness for {DeviceType} {DeviceId}: {Message}", _deviceType, _deviceId, ex.Message);
                throw;
            }
        }

        internal async Task SetPercentageAsync(float percentage, TimeSpan? timeout)
        {
            _logger.LogInformation("{DeviceType} {DeviceId} percentage: {percentage}%", _deviceType, _deviceId, percentage);
            if (percentage < 0.0f || percentage > 100.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between 0 and 100");
            }

            // Use brightness as float directly (0-100) - KnxService converts to KNX byte range
            await SetFloatFunctionAsync(
                addresses.PercentageControl,
                percentage,
                () => Math.Abs(owner.CurrentPercentage - percentage) <= 1, // Allow 1% tolerance
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
                () => Math.Abs(owner.CurrentPercentage - targetPercentage) <= tolerance,
                timeout ?? _defaultTimeout,
                $"percentage {targetPercentage} ± {tolerance}"
            );
        }
    }
}
