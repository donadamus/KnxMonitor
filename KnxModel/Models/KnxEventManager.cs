using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Helper class for managing KNX event subscriptions for devices
    /// Provides centralized event handling to avoid code duplication
    /// </summary>
    public class KnxEventManager : IDisposable
    {
        private static int _activeSubscriptions = 0;
        
        private readonly IKnxService _knxService;
        private readonly string _deviceId;
        private readonly string _deviceType;
        private bool _isListening = false;
        private bool _disposed = false;

        public KnxEventManager(IKnxService knxService, string deviceId, string deviceType)
        {
            _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            _deviceType = deviceType ?? throw new ArgumentNullException(nameof(deviceType));
        }

        /// <summary>
        /// Event fired when a KNX group message is received
        /// </summary>
        public event EventHandler<KnxGroupEventArgs>? MessageReceived;

        /// <summary>
        /// Starts listening to KNX feedback messages
        /// </summary>
        public void StartListening()
        {
            if (_isListening || _disposed)
                return;

            _isListening = true;
            _knxService.GroupMessageReceived += OnKnxGroupMessageReceived;
            _activeSubscriptions++;
            Console.WriteLine($"📡 Started listening to feedback for {_deviceType} {_deviceId} (Active subscriptions: {_activeSubscriptions})");
        }

        /// <summary>
        /// Stops listening to KNX feedback messages
        /// </summary>
        public void StopListening()
        {
            if (!_isListening || _disposed)
                return;

            _isListening = false;
            _knxService.GroupMessageReceived -= OnKnxGroupMessageReceived;
            _activeSubscriptions--;
            Console.WriteLine($"📴 Stopped listening to feedback for {_deviceType} {_deviceId} (Active subscriptions: {_activeSubscriptions})");
        }

        private void OnKnxGroupMessageReceived(object? sender, KnxGroupEventArgs e)
        {
            try
            {
                MessageReceived?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing KNX message for {_deviceType} {_deviceId}: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                Console.WriteLine($"⚠️ KnxEventManager for {_deviceType} {_deviceId} already disposed");
                return;
            }

            Console.WriteLine($"🗑️ Disposing KnxEventManager for {_deviceType} {_deviceId}");
            StopListening();
            _disposed = true;
        }
    }
}
