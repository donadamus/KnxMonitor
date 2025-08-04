using System.Threading.Tasks;

namespace KnxTest.Integration.Interfaces
{
    /// <summary>
    /// Tests for devices that support lock functionality
    /// </summary>
    public interface ILockableDeviceTests
    {
        /// <summary>
        /// Tests basic lock and unlock functionality
        /// </summary>
        Task CanLockAndUnlock(string deviceId);

        /// <summary>
        /// Tests that locked device prevents state changes
        /// </summary>
        Task LockPreventsStateChanges(string deviceId);

        /// <summary>
        /// Tests reading lock state from device
        /// </summary>
        Task CanReadLockState(string deviceId);

        ///// <summary>
        ///// Tests that device automatically turns off when locked
        ///// </summary>
        //Task DeviceAutoOffWhenLocked(string deviceId);
    }
}
