using Microsoft.Extensions.Logging;

namespace KnxModel.Models.Helpers
{
    public class DeviceHelperBase<TDevice, TAddress>
        where TDevice : IKnxDeviceBase
    {
        protected readonly TimeSpan _defaultTimeout;
        protected const int _pollingIntervalMs = 50; // Polling interval for wait operations
        protected readonly TDevice owner;
        protected readonly IKnxService _knxService;
        protected readonly string _deviceId;
        protected readonly string _deviceType;
        protected readonly ILogger<TDevice> _logger;
        protected TAddress addresses;

        public DeviceHelperBase(TDevice owner, TAddress address, IKnxService knxService, string deviceId, string deviceType, ILogger<TDevice> logger, TimeSpan defaultTimeout)
        {
            this.owner = owner;
            addresses = address ?? throw new ArgumentNullException(nameof(address));
            _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            _deviceType = deviceType ?? throw new ArgumentNullException(nameof(deviceType));
            _logger = logger;
            _defaultTimeout = defaultTimeout;
        }

        protected async Task<bool> WaitForConditionAsync(Func<bool> condition, TimeSpan? timeout = null, string description = "condition")
        {
            var effectiveTimeout = timeout ?? _defaultTimeout;
            _logger.LogInformation("Waiting for {DeviceType} {DeviceId} {Description}", _deviceType, _deviceId, description);
            if (condition())
                {
                _logger.LogInformation("✅ {DeviceType} {DeviceId} {Description} already met", _deviceType, _deviceId, description);
                return true; // Condition already met
            }

            // Create a task that completes when condition is met
            var waitTask = Task.Run(async () =>
            {
                while (!condition())
                {
                    await Task.Delay(_pollingIntervalMs);
                }
                return true;
            });
            //// Create a task that completes when condition is met
            //var logTask = Task.Run(async () =>
            //{
            //    while (!condition())
            //    {
            //        await Task.Delay(1000);
            //        _logger.LogInformation($"Device {_deviceType} {_deviceId} still waiting for {description}");
            //    }
            //    return true;
            //});

            // Create timeout task
            var timeoutTask = Task.Delay(effectiveTimeout);

            // Wait for either condition to be met or timeout
            var completedTask = await Task.WhenAny(waitTask, timeoutTask);

            if (completedTask == waitTask)
            {
                _logger.LogInformation("✅ {DeviceType} {DeviceId} {Description} achieved", _deviceType, _deviceId, description);
                return await waitTask;
            }
            else
            {
                _logger.LogWarning("⚠️ WARNING: {DeviceType} {DeviceId} {Description} timeout", _deviceType, _deviceId, description);
                return false;
            }
        }

        protected async Task SetBitFunctionAsync(string address, bool value, Func<bool> condition, TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? _defaultTimeout;
            _logger.LogInformation("Setting {DeviceType} {DeviceId} {Address} to {Value}", _deviceType, _deviceId, address, value);

            // Write the value to the KNX bus
            await _knxService.WriteGroupValueAsync(address, value);

            // Wait for the state to be updated
            await WaitForConditionAsync(
                condition: condition,
                timeout: effectiveTimeout,
                description: $"set {address} to {value}"
            );
        }

        protected async Task SetFloatFunctionAsync(string address, float value, Func<bool> condition, TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? _defaultTimeout;
            // Write the value to the KNX bus
            _knxService.WriteGroupValue(address, value);
            // Wait for the state to be updated

            await WaitForConditionAsync(
                condition: condition,
                timeout: effectiveTimeout,
                description: $"set {address} to {value}"
            );
        }

    }
}
