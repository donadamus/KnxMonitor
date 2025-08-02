using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Abstract base class for all KNX devices without generics
    /// </summary>
    public abstract class KnxDeviceBase : IKnxDevice
    {
        protected readonly IKnxService _knxService;
        protected readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5);
        protected const int _pollingIntervalMs = 50; // Polling interval for wait operations
        protected bool _isListeningToFeedback = false;

        public string Id { get; }
        public string Name { get; }
        public string SubGroup { get; }

        protected KnxDeviceBase(string id, string name, string subGroup, IKnxService knxService, TimeSpan? defaultTimeout = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SubGroup = subGroup ?? throw new ArgumentNullException(nameof(subGroup));
            _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
            if (defaultTimeout != null)
            {
                _defaultTimeout = defaultTimeout.Value;
            }
        }

        public abstract Task InitializeAsync();
        public abstract void SaveCurrentState();
        public abstract Task RestoreSavedStateAsync();
        
        protected abstract void ProcessKnxMessage(KnxGroupEventArgs e);

        /// <summary>
        /// Starts listening to KNX feedback messages
        /// </summary>
        protected virtual void StartListeningToFeedback()
        {
            if (_isListeningToFeedback)
                return;

            _isListeningToFeedback = true;
            
            // Subscribe to KNX group messages
            _knxService.GroupMessageReceived += OnKnxGroupMessageReceived;

            Console.WriteLine($"Started listening to feedback for {GetType().Name} {Id}");
        }

        /// <summary>
        /// Stops listening to KNX feedback messages
        /// </summary>
        protected virtual void StopListeningToFeedback()
        {
            if (!_isListeningToFeedback)
                return;

            _isListeningToFeedback = false;

            // Unsubscribe from KNX group messages
            _knxService.GroupMessageReceived -= OnKnxGroupMessageReceived;

            Console.WriteLine($"Stopped listening to feedback for {GetType().Name} {Id}");
        }

        /// <summary>
        /// Handles KNX group messages and delegates device-specific processing
        /// </summary>
        private void OnKnxGroupMessageReceived(object? sender, KnxGroupEventArgs e)
        {
            try
            {
                ProcessKnxMessage(e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing KNX message for {GetType().Name} {Id}: {ex.Message}");
            }
        }

        protected async Task<bool> WaitForConditionAsync(Func<bool> condition, TimeSpan? timeout = null, string description = "condition")
        {
            var effectiveTimeout = timeout ?? _defaultTimeout;
            Console.WriteLine($"Waiting for {GetType().Name} {Id} {description}");

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
                Console.WriteLine($"✅ {GetType().Name} {Id} {description} achieved");
                return await waitTask;
            }
            else
            {
                Console.WriteLine($"⚠️ WARNING: {GetType().Name} {Id} {description} timeout");
                return false;
            }
        }

        protected async Task SetBitFunctionAsync(string address, bool value, Func<bool> condition, TimeSpan? timeout = null)
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

        public virtual void Dispose()
        {
            // Override in derived classes if needed
        }
    }
}
