using System;
using System.Threading.Tasks;

namespace KnxModel
{
    public interface IKnxService : IDisposable
    {
        event EventHandler<KnxGroupEventArgs> GroupMessageReceived;
        
        // New methods using Knx.Falcon.GroupAddress
        void WriteGroupValue(KnxGroupAddress address, bool value);
        void WriteGroupValue(string address, bool value);
        void WriteGroupValue(string address, int percentage);
        Task<T> RequestGroupValue<T>(string address);
    }

    public record KnxGroupEventArgs(
        string Destination, 
        KnxValue Value, 
        string? Source = null, 
        DateTime? Timestamp = null, 
        string? MessageType = null,
        string? Priority = null);
}
