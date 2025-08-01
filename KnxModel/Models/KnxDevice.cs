using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Abstract base class for all KNX devices with common functionality
    /// </summary>
    /// <typeparam name="TState">Type of device state</typeparam>
    /// <typeparam name="TAddresses">Type of device addresses</typeparam>
    public abstract class KnxDevice<TState, TAddresses> : IDisposable
        where TState : class
        where TAddresses : class
    {
        protected readonly IKnxService _knxService;
        protected readonly TimeSpan _defaultTimeout;
        protected const int _pollingIntervalMs = 50; // Polling interval for wait operations
        protected bool _isListeningToFeedback = false;

        public string Id { get; }
        public string Name { get; }
        public string SubGroup { get; }
        public TAddresses Addresses { get; set; }
        public TState CurrentState { get; set; }
        public TState? SavedState { get; protected set; }

        /// <summary>
        /// Creates a new KNX device instance
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <param name="name">Human-readable name</param>
        /// <param name="subGroup">KNX sub-group number</param>
        /// <param name="knxService">KNX service for communication</param>
        /// <param name="defaultTimeout">Default timeout for operations</param>
        protected KnxDevice(string id, string name, string subGroup, IKnxService knxService, TimeSpan defaultTimeout)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SubGroup = subGroup ?? throw new ArgumentNullException(nameof(subGroup));
            _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
            _defaultTimeout = defaultTimeout;
            
            Addresses = CreateAddresses();
            CurrentState = CreateDefaultState();
        }

        /// <summary>
        /// Creates device-specific addresses based on sub-group
        /// </summary>
        protected abstract TAddresses CreateAddresses();

        /// <summary>
        /// Creates default initial state for the device
        /// </summary>
        protected abstract TState CreateDefaultState();

        /// <summary>
        /// Reads the current state from KNX bus
        /// </summary>
        protected abstract Task<TState> ReadCurrentStateAsync();

        /// <summary>
        /// Processes KNX group messages specific to this device
        /// </summary>
        protected abstract void ProcessKnxMessage(KnxGroupEventArgs e);

        /// <summary>
        /// Initializes the device by reading state from KNX bus and starting feedback listening
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            try
            {
                

                Console.WriteLine($"{GetType().Name} {Id} ({Name}) initializing...");

                // Read initial state from KNX bus
                CurrentState = await ReadCurrentStateAsync();

                Console.WriteLine($"{GetType().Name} {Id} ({Name}) initialized successfully");
                
                // Start listening to feedback events
                StartListeningToFeedback();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize {GetType().Name} {Id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves the current state for later restoration
        /// </summary>
        public virtual void SaveCurrentState()
        {
            SavedState = CurrentState;
            Console.WriteLine($"{GetType().Name} {Id} state saved");
        }

        /// <summary>
        /// Restores the device to previously saved state
        /// </summary>
        public abstract Task RestoreSavedStateAsync();

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

        /// <summary>
        /// Sets the specified bit value on the KNX bus and waits for the state to reflect the change.
        /// </summary>
        /// <remarks>This method writes the specified value to the KNX bus and waits for the state to
        /// update  by repeatedly evaluating the <paramref name="stateSelector"/> function. If the state does not 
        /// update within the specified timeout, the operation will fail.</remarks>
        /// <param name="address">The KNX group address to which the value will be written.</param>
        /// <param name="targetValue">The desired boolean value to set on the KNX bus.</param>
        /// <param name="stateSelector">A function that evaluates the current state. The method waits until this function returns <see
        /// langword="true"/>  to confirm the state matches the desired value.</param>
        /// <param name="timeout">An optional timeout specifying the maximum duration to wait for the state to update.  If not provided, a
        /// default timeout is used.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected async Task SetBitFunctionAsync(string address, bool targetValue, Func<bool> stateSelector, TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? _defaultTimeout;
            // Write the value to the KNX bus
            _knxService.WriteGroupValue(address, targetValue);
            // Wait for the state to be updated

            await WaitForConditionAsync(
                condition: stateSelector,
                timeout: effectiveTimeout,
                description: $"set {address} to {targetValue}"
            );
        }

        /// <summary>
        /// Sets a float value on KNX bus and waits for state confirmation
        /// </summary>
        /// <param name="address">The KNX group address to write to</param>
        /// <param name="targetValue">The float value to set</param>
        /// <param name="stateSelector">Function that returns current state value</param>
        /// <param name="tolerance">Tolerance for float comparison (default: 0.01f)</param>
        /// <param name="timeout">Timeout for the operation. If null, 
        /// default timeout is used.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected async Task SetFloatFunctionAsync(string address, float targetValue, Func<float> stateSelector, float tolerance = 0.01f, TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? _defaultTimeout;
            // Write the value to the KNX bus
            _knxService.WriteGroupValue(address, targetValue);
            // Wait for the state to be updated

            await WaitForConditionAsync(
                condition: () => Math.Abs(stateSelector() - targetValue) <= tolerance,
                timeout: effectiveTimeout,
                description: $"set {address} to {targetValue}"
            );
        }

        /// <summary>
        /// Creates a wait task that completes when a condition is met
        /// </summary>
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

        public override string ToString()
        {
            return $"{GetType().Name} {Id} ({Name})";
        }

        public virtual void Dispose()
        {
            StopListeningToFeedback();
        }
    }
}
