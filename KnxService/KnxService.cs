
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using KnxModel;
using System.ComponentModel.DataAnnotations;

namespace KnxService
{
    public class KnxService : IKnxService
    {

        private readonly KnxBus _knxBus;
        public KnxService()
        {
            var parameters = new IpTunnelingConnectorParameters()
            {
                HostAddress = "192.168.20.2",
                AutoReconnect = true,
            };

            _knxBus = new KnxBus(parameters);
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
                    // Extract basic information from Falcon's event args
                    var destination = args.DestinationAddress.ToString();
                    
                    // Create KnxValue from Falcon's value
                    var knxValue = CreateKnxValue(args.Value, destination);
                    
                    // Try to get additional properties if they exist
                    string? source = null;
                    string? messageType = null;
                    string? priority = null;
                    
                    // Use reflection to safely extract additional properties
                    var argsType = args.GetType();
                    
                    // Try to get Source/SourceAddress
                    var sourceProp = argsType.GetProperty("SourceAddress") ?? argsType.GetProperty("Source");
                    source = sourceProp?.GetValue(args)?.ToString();
                    
                    // Try to get MessageType
                    var typeProp = argsType.GetProperty("MessageType") ?? argsType.GetProperty("Type");
                    messageType = typeProp?.GetValue(args)?.ToString();
                    
                    // Try to get Priority
                    var priorityProp = argsType.GetProperty("Priority");
                    priority = priorityProp?.GetValue(args)?.ToString();

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
                // Extract raw data if possible
                byte[]? rawData = null;
                
                // Try to get raw bytes from Falcon's GroupValue
                if (falconValue != null)
                {
                    var valueType = falconValue.GetType();
                    
                    // Try to get Data property (common in KNX libraries)
                    var dataProp = valueType.GetProperty("Data") ?? valueType.GetProperty("RawData");
                    if (dataProp?.GetValue(falconValue) is byte[] data)
                    {
                        rawData = data;
                    }
                    
                    // Try to get TypedValue for better type handling
                    var typedValueProp = valueType.GetProperty("TypedValue");
                    if (typedValueProp != null)
                    {
                        var typedValue = typedValueProp.GetValue(falconValue);
                        if (typedValue != null)
                        {
                            var knxValue = new KnxValue(typedValue, rawData);
                            Console.WriteLine($"KNX {address} ({KnxAddressTypeConfig.GetAddressDescription(address)}): {knxValue.GetTypedValue(address)}");
                            return knxValue;
                        }
                    }
                }
                
                // Fallback to the original value
                var fallbackValue = new KnxValue(falconValue ?? "Unknown", rawData);
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
            _knxBus.WriteGroupValue(groupAddress, groupValue);
        }

        public void WriteGroupValue(string address, int percentage)
        {
            if (percentage < 0 || percentage > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between 0 and 100.");
            }
            
            var groupAddress = new GroupAddress(address);
            var knxRawValue = (byte)(percentage * 2.55); // Convert 0-100% to 0-255 KNX range
            var groupValue = new GroupValue(knxRawValue);
            _knxBus.WriteGroupValue(groupAddress, groupValue);
        }

        public void WriteGroupValue(KnxGroupAddress address, bool value)
        {
            WriteGroupValue(address.Address, value);
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
                var result = await _knxBus.ReadGroupValueAsync(groupAddress, TimeSpan.FromSeconds(2), MessagePriority.Low);

                if (result?.TypedValue is T typedValue)
                {
                    return typedValue;
                }
                if (result?.TypedValue is byte byteValue)
                {
                    if (typeof(T) == typeof(int))
                    {
                        // Convert KNX byte (0-255) to percentage (0-100)
                        var percentage = (int)(byteValue / 2.55);
                        return (T)(object)percentage;
                    }
                }
                
                // Log what we got instead
                Console.WriteLine($"RequestGroupValue<{typeof(T).Name}>({address}): result.TypedValue = {result?.TypedValue?.GetType().Name ?? "null"}, value = {result?.TypedValue}");
                
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
