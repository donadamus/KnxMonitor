
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
            Connect();
        }


        private void Connect()
        {
            _knxBus.Connect();

            _knxBus.GroupMessageReceived += (sender, args) =>
            {
                var knxGroupEventArgs = new KnxGroupEventArgs(args.DestinationAddress.ToString(), args.Value.ToString());
                OnMessageReceived(knxGroupEventArgs);


            };

        }

        private void OnMessageReceived(KnxGroupEventArgs e)
        {
            GroupMessageReceived?.Invoke(this, e);
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
            var result = await _knxBus.ReadGroupValueAsync(groupAddress, TimeSpan.FromSeconds(2), MessagePriority.Low);

            if (result.TypedValue is T typedValue)
            {
                return typedValue;
            }
            if (result.TypedValue is byte byteValue)
            {
                if (typeof(T) == typeof(Percent))
                {
                    return  (T)(object) new Percent(byteValue);
                }
            }
            return default(T);
        }


    }


    public record KnxGroupEventArgs(string Destination, string Value);
}
