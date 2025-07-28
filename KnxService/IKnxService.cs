
using KnxModel;

namespace KnxService
{
    public interface IKnxService :IDisposable
    {
        event EventHandler<KnxGroupEventArgs> GroupMessageReceived;

        //void Connect();
        //void Disconnect();
        void WriteGroupValue(string mainGroup, string middleGroup, string subGroup, bool value);
        Task<string> RequestGroupValue(string mainGroup, string middleGroup, string subGroup);
        Task<string> RequestGroupValue(string address);
        //void ReceiveGroupAddress(string mainGroup, string middleGroup, string subGroup);

        // New methods using Knx.Falcon.GroupAddress
        void WriteGroupValue(KnxGroupAddress address, bool value);
        void WriteGroupValue(string address, bool value);
        Task<string> RequestGroupValue(KnxGroupAddress address);
        //void Receive(KnxGroupAddress address);
        Task<T> RequestGroupValue<T>(string address);
    }
}
