using KnxModel;
using Moq;

namespace KnxTest.Unit.Helpers
{
    public class LockableDeviceTestHelper<TDevice, TAddresses>
        where TDevice : ILockableDevice, IKnxDeviceBase
        where TAddresses : ILockableAddress

    {
        private readonly TDevice _device;
        private readonly TAddresses _addresses;
        private readonly Mock<IKnxService> _mockKnxService;

        // This class would contain methods to help with percentage control for dimmers
        // It would handle sending and receiving percentage-related messages
        public LockableDeviceTestHelper(TDevice device, TAddresses addresses, Mock<IKnxService> mockKnxService)
        {
            _device = device;
            _addresses = addresses;
            _mockKnxService = mockKnxService;
        }

        internal async Task LockAsync_ShouldSendCorrectTelegram()
        {
            var address = _addresses.LockControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, true))
                          .Returns(Task.CompletedTask)
                          .Verifiable();
            await _device.LockAsync(TimeSpan.Zero);

        }

        internal async Task SetLockAsync_ShouldSendCorrectTelegram(Lock lockState, bool expectedValue)
        {
            var address = _addresses.LockControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, expectedValue))
                          .Returns(Task.CompletedTask)
                          .Verifiable();
            await _device.SetLockAsync(lockState, TimeSpan.Zero);
        }

        internal async Task UnlockAsync_ShouldSendCorrectTelegram()
        {
            var address = _addresses.LockControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, false))
                          .Returns(Task.CompletedTask)
                          .Verifiable();
            await _device.UnlockAsync(TimeSpan.Zero);
        }
    }
}
