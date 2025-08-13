
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using KnxModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.RateLimiting;

namespace KnxService
{
    public class KnxService : IKnxService
    {

        private readonly KnxRateLimiterManager _knxRateLimiter;
        private readonly KnxBus _knxBus;
        public KnxService()
        {
            var parameters = new IpTunnelingConnectorParameters()
            {
                HostAddress = "192.168.20.2",
                AutoReconnect = true,
            };

            _knxBus = new KnxBus(parameters);
            _knxRateLimiter = new KnxRateLimiterManager();
            Console.WriteLine($"KnxService: Attempting to connect to {parameters.HostAddress}...");
            Connect();
        }


        private void Connect()
        {
            try
            {
                _knxBus.Connect();
                Console.WriteLine("KnxService: Connected successfully to KNX bus");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"KnxService: Failed to connect to KNX bus: {ex.Message}");
                throw;
            }

            _knxBus.GroupMessageReceived += (sender, args) =>
            {
                try
                {
                    // Extract information directly from Falcon's event args - no reflection needed!
                    var destination = args.DestinationAddress.ToString();
                    var source = args.SourceAddress.ToString();
                    var messageType = args.EventType.ToString();
                    var priority = args.MessagePriority.ToString();
                    
                    // Create KnxValue from Falcon's value
                    var knxValue = CreateKnxValue(args.Value, destination);

                    var knxGroupEventArgs = new KnxGroupEventArgs(
                        Destination: destination,
                        Value: knxValue,
                        Source: source,
                        Timestamp: DateTime.Now,
                        MessageType: messageType,
                        Priority: priority
                    );
                    
                    OnMessageReceived(knxGroupEventArgs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing KNX group message: {ex.Message}");
                    // Fallback to basic event
                    var basicValue = new KnxValue(args.Value?.ToString() ?? "Unknown");
                    var basicEvent = new KnxGroupEventArgs(
                        Destination: args.DestinationAddress.ToString(),
                        Value: basicValue,
                        Timestamp: DateTime.Now
                    );
                    OnMessageReceived(basicEvent);
                }
            };

        }

        private void OnMessageReceived(KnxGroupEventArgs e)
        {
            GroupMessageReceived?.Invoke(this, e);
        }

        private KnxValue CreateKnxValue(object? falconValue, string address)
        {
            try
            {
                // Cast to GroupValue directly - no reflection needed!
                if (falconValue is Knx.Falcon.GroupValue groupValue)
                {
                    var typedValue = groupValue.TypedValue;
                    var rawData = groupValue.Value; // This is byte[]
                    
                    var knxValue = new KnxValue(typedValue, rawData);
                    Console.WriteLine($"KNX {address} ({KnxAddressTypeConfig.GetAddressDescription(address)}): {knxValue.GetTypedValue(address)}");
                    return knxValue;
                }
                
                // Fallback for unknown types
                var fallbackValue = new KnxValue(falconValue ?? "Unknown");
                Console.WriteLine($"KNX {address} (fallback): {fallbackValue.GetTypedValue(address)}");
                return fallbackValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not extract enhanced value info for {address}: {ex.Message}");
                return new KnxValue(falconValue?.ToString() ?? "Unknown");
            }
        }

        public void WriteGroupValue(string address, bool value)
        {
            var groupAddress = new GroupAddress(address);
            var groupValue = new GroupValue(value);
            _knxRateLimiter.WaitAsync(KnxOperationType.WriteGroupValue).GetAwaiter().GetResult();
            _knxBus.WriteGroupValue(groupAddress, groupValue);
        }
        public async Task WriteGroupValueAsync(string address, bool value)
        {
            var groupAddress = new GroupAddress(address);
            var groupValue = new GroupValue(value);
            await _knxRateLimiter.WaitAsync(KnxOperationType.WriteGroupValue);
            await _knxBus.WriteGroupValueAsync(groupAddress, groupValue);
        }

        public async Task WriteGroupValueAsync(string address, byte[] data)
        {
            var groupAddress = new GroupAddress(address);
            var groupValue = new GroupValue(data);
            await _knxRateLimiter.WaitAsync(KnxOperationType.WriteGroupValue);
            await _knxBus.WriteGroupValueAsync(groupAddress, groupValue);
        }

        public void WriteGroupValue(string address, float percentage)
        {
            if (percentage < 0.0f || percentage > 100.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between 0.0 and 100.0.");
            }
            
            var groupAddress = new GroupAddress(address);
            // Use 1-byte percentage like int version - most KNX devices expect this format
            var knxRawValue = (byte)(percentage * 2.55f); // Convert 0.0-100.0% to 0-255 KNX range
            var groupValue = new GroupValue(knxRawValue);
            _knxRateLimiter.WaitAsync(KnxOperationType.WriteGroupValue).GetAwaiter().GetResult();
            _knxBus.WriteGroupValue(groupAddress, groupValue);
        }
        public async Task WriteGroupValueAsync(string address, float percentage)
        {
            if (percentage < 0.0f || percentage > 100.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between 0.0 and 100.0.");
            }
            
            var groupAddress = new GroupAddress(address);
            // Use 1-byte percentage like int version - most KNX devices expect this format
            var knxRawValue = (byte)(percentage * 2.55f); // Convert 0.0-100.0% to 0-255 KNX range
            var groupValue = new GroupValue(knxRawValue);
            await _knxRateLimiter.WaitAsync(KnxOperationType.WriteGroupValue);
            await _knxBus.WriteGroupValueAsync(groupAddress, groupValue);
        }

        public event EventHandler<KnxGroupEventArgs> GroupMessageReceived;

        public void Dispose()
        {
            _knxBus.Dispose();
        }

        public async Task<T> RequestGroupValue<T>(string address)
        {
            var groupAddress = new GroupAddress(address);
            try
            {
                _knxRateLimiter.WaitAsync(KnxOperationType.ReadGroupValueAsync).GetAwaiter().GetResult();
                var result = await _knxBus.ReadGroupValueAsync(groupAddress, TimeSpan.FromSeconds(2) , MessagePriority.Low);

                Console.WriteLine($"RequestGroupValue<{typeof(T).Name}>({address}): {result?.TypedValue?.GetType().Name} = {result?.TypedValue}");

                // First, try direct cast to requested type
                if (result?.TypedValue is T directValue)
                {
                    return directValue;
                }

                // Handle specific type conversions
                if (result?.TypedValue != null)
                {
                    var targetType = typeof(T);

                    // Boolean conversions
                    if (targetType == typeof(bool))
                    {
                        if (result.TypedValue is bool boolVal)
                            return (T)(object)boolVal;
                        if (result.TypedValue is byte byteVal)
                            return (T)(object)(byteVal != 0); // 0 = false, anything else = true
                        if (result.TypedValue is int intVal)
                            return (T)(object)(intVal != 0);
                    }

                    // Float/percentage conversions
                    if (targetType == typeof(float))
                    {
                        if (result.TypedValue is byte byteVal)
                            return (T)(object)(byteVal / 2.55f); // Convert KNX byte (0-255) to percentage (0-100)
                        if (result.TypedValue is ushort ushortVal)
                            return (T)(object)(ushortVal / 655.35f); // Convert 2-byte (0-65535) to percentage (0-100)
                        if (result.TypedValue is int intVal)
                            return (T)(object)(float)intVal;
                        if (result.TypedValue is double doubleVal)
                            return (T)(object)(float)doubleVal;
                    }

                    // Int conversions
                    if (targetType == typeof(int))
                    {
                        if (result.TypedValue is byte byteVal)
                            return (T)(object)(int)(byteVal / 2.55f); // Convert KNX byte (0-255) to percentage (0-100)
                        if (result.TypedValue is ushort ushortVal)
                            return (T)(object)(int)ushortVal;
                        if (result.TypedValue is float floatVal)
                            return (T)(object)(int)floatVal;
                    }
                }

                // Log failure and return default
                Console.WriteLine($"FAILED to convert {result?.TypedValue?.GetType().Name ?? "null"} to {typeof(T).Name}");
                return default(T)!;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading group value {address}: {ex.Message}");
                throw;
            }
        }
    }
}
