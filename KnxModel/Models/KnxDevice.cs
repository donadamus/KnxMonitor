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
        public TAddresses Addresses { get; protected set; }
        public TState CurrentState { get; protected set; }
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
                // Start listening to feedback events
                StartListeningToFeedback();

                Console.WriteLine($"{GetType().Name} {Id} ({Name}) initializing...");

                // Read initial state from KNX bus
                CurrentState = await ReadCurrentStateAsync();

                Console.WriteLine($"{GetType().Name} {Id} ({Name}) initialized successfully");
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
