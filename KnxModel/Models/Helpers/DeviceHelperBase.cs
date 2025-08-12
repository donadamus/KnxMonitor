using Microsoft.Extensions.Logging;

namespace KnxModel.Models.Helpers
{
    public class DeviceHelperBase<TDevice>
        where TDevice : IKnxDeviceBase
    {
        protected readonly TimeSpan _defaultTimeout;
        protected const int _pollingIntervalMs = 50; // Polling interval for wait operations
        protected readonly TDevice owner;
        protected readonly IKnxService _knxService;
        protected readonly string _deviceId;
        protected readonly string _deviceType;
        protected readonly ILogger<TDevice> _logger;

        public DeviceHelperBase(TDevice owner, IKnxService knxService, string deviceId, string deviceType, ILogger<TDevice> logger, TimeSpan defaultTimeout)
        {
            this.owner = owner;
            _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            _deviceType = deviceType ?? throw new ArgumentNullException(nameof(deviceType));
            _logger = logger;
            _defaultTimeout = defaultTimeout;
        }

        protected async Task<bool> WaitForConditionAsync(Func<bool> condition, TimeSpan? timeout = null, string description = "condition")
        {
            var effectiveTimeout = timeout ?? _defaultTimeout;
            Console.WriteLine($"Waiting for {_deviceType} {_deviceId} {description}");
            if (condition())
                {
                Console.WriteLine($"✅ {_deviceType} {_deviceId} {description} already met");
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
                Console.WriteLine($"✅ {_deviceType} {_deviceId} {description} achieved");
                return await waitTask;
            }
            else
            {
                Console.WriteLine($"⚠️ WARNING: {_deviceType} {_deviceId} {description} timeout");
                return false;
            }
        }

        protected async Task SetBitFunctionAsync(string address, bool value, Func<bool> condition, TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? _defaultTimeout;
            Console.WriteLine($"Setting {_deviceType} {_deviceId} {address} to {value}");

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
