using KnxModel;
using KnxTest.Unit.Base;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace KnxTest.Unit.Models.Shutter
{
    public class ShutterDeviceMovementControllableTests : DeviceMovementControllableTests<ShutterDevice, ShutterAddresses>
    {
        protected override MovementControllableDeviceTestHelper<ShutterDevice, ShutterAddresses> _movementTestHelper { get; }
        public ShutterDeviceMovementControllableTests()
        {
            // Initialize DimmerDevice with mock KNX service
            var logger = new Mock<ILogger<ShutterDevice>>().Object;
            var device = new ShutterDevice("D_TEST", "Test Dimmer", "1", _mockKnxService.Object, logger, TimeSpan.FromSeconds(1));
            _movementTestHelper = new MovementControllableDeviceTestHelper<ShutterDevice, ShutterAddresses>(
                device, device.Addresses, _mockKnxService);
        }

    }

}
