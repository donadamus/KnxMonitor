using KnxModel;
using KnxTest.Unit.Base;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace KnxTest.Unit.Models.Shutter
{
    public class ShutterDevicePercentageControllableTests : DevicePercentageControllableTests<ShutterDevice, ShutterAddresses>
    {
        protected override PercentageControllableDeviceTestHelper<ShutterDevice, ShutterAddresses> _percentageTestHelper { get; }
        public ShutterDevicePercentageControllableTests()
        {
            // Initialize DimmerDevice with mock KNX service
            var logger = new Mock<ILogger<ShutterDevice>>().Object;
            var device = new ShutterDevice("D_TEST", "Test Dimmer", "1", _mockKnxService.Object, logger, TimeSpan.FromSeconds(1));
            _percentageTestHelper = new PercentageControllableDeviceTestHelper<ShutterDevice, ShutterAddresses>(
                device, device.Addresses, _mockKnxService);
        }

    }

}
