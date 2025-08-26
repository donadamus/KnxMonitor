using Microsoft.Extensions.Logging;

namespace KnxModel
{
    public abstract class KnxDeviceBase<TDevice, TAddressess> : IDisposable, IIdentifable
        where TDevice : IKnxDeviceBase
    {
        protected readonly IKnxService _knxService;
        protected readonly ILogger<TDevice> _logger;
        protected readonly KnxEventManager _eventManager;
        protected readonly TimeSpan _defaultTimeout;
        internal readonly TAddressess Addresses;
        public KnxDeviceBase(string id, string name, string subGroup, TAddressess addresses, IKnxService knxService, ILogger<TDevice> logger, TimeSpan defaultTimeout)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SubGroup = subGroup ?? throw new ArgumentNullException(nameof(subGroup));
            Addresses = addresses ?? throw new ArgumentNullException(nameof(addresses));
            _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventManager = new KnxEventManager(_knxService, Id, typeof(TDevice).Name);
            _eventManager.StartListening();
            _defaultTimeout = defaultTimeout;
        }

        public string Id { get; }
        public string Name { get; }
        public string SubGroup { get; }
        public DateTime LastUpdated { get; set; } = DateTime.MinValue;

        public virtual void Dispose() { _eventManager?.Dispose(); }
        
        public abstract Task InitializeAsync();

        public abstract void SaveCurrentState();

        public abstract Task RestoreSavedStateAsync(TimeSpan? timeout = null);

    }
}
