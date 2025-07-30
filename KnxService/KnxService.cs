
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


        public void WriteGroupValue(string mainGroup, string middleGroup, string subGroup, bool value)
        {
            var address = new KnxGroupAddress(mainGroup, middleGroup, subGroup);
            WriteGroupValue(address, value);
        }
        public void WriteGroupValue(string address, bool value)
        {
            var groupAddress = new GroupAddress(address);
            var groupValue = new GroupValue(value);
            _knxBus.WriteGroupValue(groupAddress, groupValue);
        }

        public void WriteGroupValue(string address, Percent value)
        {
            var groupAddress = new GroupAddress(address);
            var groupValue = new GroupValue(value.KnxRawValue);
            _knxBus.WriteGroupValue(groupAddress, groupValue);
        }

        //public void ReceiveGroupAddress(string mainGroup, string middleGroup, string subGroup)
        //{
        //    var address = new KnxGroupAddress(mainGroup, middleGroup, subGroup);
        //    Receive(address);
        //}

        public void WriteGroupValue(KnxGroupAddress address, bool value)
        {
            WriteGroupValue(address.Address, value);
        }

        public event EventHandler<KnxGroupEventArgs> GroupMessageReceived;


        //public void Receive(KnxGroupAddress address)
        //{
        //    if (!_isConnected)
        //    {
        //        Connect();
        //    }
        //    // Here you would implement the logic to receive messages from the specified group address.
        //    // This is a placeholder for demonstration purposes.
        //    Console.WriteLine($"Receiving messages from {address.MainGroup}/{address.MiddleGroup}/{address.SubGroup}");

            
        //}

        public void Dispose()
        {
            _knxBus.Dispose();
        }

        public async Task<string> RequestGroupValue(string mainGroup, string middleGroup, string subGroup)
        {
            var address = new KnxGroupAddress(mainGroup, middleGroup, subGroup);
            return await RequestGroupValue(address);
        }

        public async Task<string> RequestGroupValue(KnxGroupAddress address)
        {
            return await RequestGroupValue(address.Address);
        }
        public async Task<string> RequestGroupValue(string address)
        {
            var groupAddress = new GroupAddress(address);
            var result = await _knxBus.ReadGroupValueAsync(groupAddress, TimeSpan.FromSeconds(2), MessagePriority.Alarm);

            return result.TypedValue.ToString() == "1" ? "1" : "0";
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
                    if (typeof(T) == typeof(Percent))
                    {
                        return (T)(object)new Percent(byteValue);
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
