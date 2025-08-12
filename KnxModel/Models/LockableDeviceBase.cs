using KnxModel.Models.Helpers;
using Microsoft.Extensions.Logging;

namespace KnxModel
{

    

    public abstract class LockableDeviceBase<TDevice, TAddressess> : ILockableDevice, IDisposable, IIdentifable
        where TDevice : IKnxDeviceBase
        where TAddressess : ILockableAddress
    {
        internal readonly KnxEventManager _eventManager;
        internal readonly IKnxService _knxService;
        private readonly ILogger<TDevice> _logger;

        public string Id { get; }
        public string Name { get; }
        public string SubGroup { get; }
        public DateTime LastUpdated => _lastUpdated;
        internal Lock _currentLockState = Lock.Unknown;
        internal DateTime _lastUpdated = DateTime.MinValue;
        internal Lock? _savedLockState;
        internal TimeSpan _defaulTimeout;
        public TAddressess Addresses { get; }


        private LockableDeviceHelper<TDevice> _lockableHelper;

        public LockableDeviceBase(string id, string name, string subGroup, TAddressess addresses, IKnxService knxService, ILogger<TDevice> logger, TimeSpan defaulTimeout)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SubGroup = subGroup ?? throw new ArgumentNullException(nameof(subGroup));
            _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
            _logger = logger;
            Addresses = addresses ?? throw new ArgumentNullException(nameof(addresses));
            // Initialize event manager
            _eventManager = new KnxEventManager(_knxService, Id, "LockableDevice");
            _eventManager.MessageReceived += OnKnxMessageReceived;



            // Start listening to KNX events
            _eventManager.StartListening();
            _defaulTimeout = defaulTimeout;
        }

        internal virtual void Initialize(TDevice owner)
        {
            _lockableHelper = new LockableDeviceHelper<TDevice>(owner,
                                _knxService, Id, "LightDevice",
                                () => Addresses,
                                state => { _currentLockState = state; _lastUpdated = DateTime.Now; },
                                () => _currentLockState,
                                _logger, _defaulTimeout);
        }

        #region ILockableDevice Implementation

        public Lock CurrentLockState => _currentLockState;

        public async Task LockAsync(TimeSpan? timeout = null)
        {
            await _lockableHelper.LockAsync(timeout);
        }
        public async Task SetLockAsync(Lock lockState, TimeSpan? timeout = null)
        {
            await _lockableHelper.SetLockAsync(lockState, timeout);
        }
        public async Task UnlockAsync(TimeSpan? timeout = null)
        {
            await _lockableHelper.UnlockAsync(timeout);
        }

        public async Task<Lock> ReadLockStateAsync()
        {
            return await _lockableHelper.ReadLockStateAsync();
        }

        public async Task<bool> WaitForLockStateAsync(Lock targetState, TimeSpan timeout)
        {
            return await _lockableHelper.WaitForLockStateAsync(targetState, timeout);
        }

        #endregion

        #region Event Handling

        private void OnKnxMessageReceived(object? sender, KnxGroupEventArgs e)
        {
            // Process lockable messages (LockControl/LockFeedback) 
            _lockableHelper.ProcessLockMessage(e);
        }

        #endregion

        #region IKnxDeviceBase Implementation

        public abstract Task InitializeAsync();


        public virtual void SaveCurrentState()
        {
            _savedLockState = _currentLockState;
        }

        public virtual async Task RestoreSavedStateAsync(TimeSpan? timeout = null)
        {
            if (_savedLockState.HasValue && _savedLockState.Value != CurrentLockState)
            {
                switch (_savedLockState.Value)
                {
                    case Lock.On:
                        await LockAsync(timeout);
                        break;
                    case Lock.Off:
                        await UnlockAsync(timeout);
                        break;
                }
            }
        }

        #endregion




        #region IDisposable Implementation

        public void Dispose()
        {
            _eventManager?.Dispose();
            Console.WriteLine($"LightDevice {Id} disposed");
        }

        /// <summary>
        /// Internal method for setting only lock state in unit tests
        /// </summary>
        internal void SetLockStateForTest(Lock lockState)
        {
            _currentLockState = lockState;
            _lastUpdated = DateTime.Now;
        }

        #endregion
    }
}
