using System;
using System.Threading.Tasks;

namespace KnxModel
{
    public interface IKnxService : IDisposable
    {
        event EventHandler<KnxGroupEventArgs> GroupMessageReceived;
        
        void WriteGroupValue(string address, bool value);
        void WriteGroupValue(string address, float percentage);
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
