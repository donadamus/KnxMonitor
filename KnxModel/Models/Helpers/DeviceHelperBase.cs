namespace KnxModel.Models.Helpers
{
    public class DeviceHelperBase
    {
        protected readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5);
        protected const int _pollingIntervalMs = 50; // Polling interval for wait operations

        protected readonly IKnxService _knxService;
        protected readonly string _deviceId;
        protected readonly string _deviceType;
        public DeviceHelperBase(IKnxService knxService, string deviceId, string deviceType)
        {
            _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            _deviceType = deviceType ?? throw new ArgumentNullException(nameof(deviceType));
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
    }
}
