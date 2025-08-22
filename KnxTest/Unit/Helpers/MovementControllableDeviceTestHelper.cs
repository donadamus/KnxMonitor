using KnxModel;
using Moq;

namespace KnxTest.Unit.Helpers
{
    public class MovementControllableDeviceTestHelper<TDevice, TAddresses>
        where TDevice : IMovementControllable, IKnxDeviceBase
        where TAddresses : IMovementControllableAddress

    {
        private readonly TDevice _device;
        private readonly TAddresses _addresses;
        private readonly Mock<IKnxService> _mockKnxService;

        // This class would contain methods to help with percentage control for dimmers
        // It would handle sending and receiving percentage-related messages
        public MovementControllableDeviceTestHelper(TDevice device, TAddresses addresses, Mock<IKnxService> mockKnxService)
        {
            _device = device;
            _addresses = addresses;
            _mockKnxService = mockKnxService;
            // Initialize helper with necessary parameters
        }

        internal async Task CloseAsync_ShouldSendCorrectTelegram()
        {
            var address = _addresses.MovementControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, false))
                          .Returns(Task.CompletedTask)
                          .Verifiable();
            // Act
            await _device.CloseAsync(TimeSpan.Zero);
        }

        internal async Task OpenAsync_ShouldSendCorrectTelegram()
        {
            var address = _addresses.MovementControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, true))
                          .Returns(Task.CompletedTask)
                          .Verifiable();
            // Act
            await _device.OpenAsync(TimeSpan.Zero);
        }

        internal async Task StopAsync_ShouldSendCorrectTelegram()
        {
            var address = _addresses.StopControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, true))
                          .Returns(Task.CompletedTask)
                          .Verifiable();
            // Act
            await _device.StopAsync(TimeSpan.Zero);
        }
    }
}
