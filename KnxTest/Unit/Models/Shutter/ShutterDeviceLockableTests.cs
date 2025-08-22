using KnxModel;
using KnxTest.Unit.Base;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace KnxTest.Unit.Models.Shutter
{
    public class ShutterDeviceLockableTests : DeviceLockableTests<ShutterDevice, ShutterAddresses>
    {
        protected override LockableDeviceTestHelper<ShutterDevice, ShutterAddresses> _lockableTestHelper { get; }
        public ShutterDeviceLockableTests()
        {
            // Initialize DimmerDevice with mock KNX service
            var logger = new Mock<ILogger<ShutterDevice>>().Object;
            var device = new ShutterDevice("D_TEST", "Test Dimmer", "1", _mockKnxService.Object, logger, TimeSpan.FromSeconds(1));
            _lockableTestHelper = new LockableDeviceTestHelper<ShutterDevice, ShutterAddresses>(
                device, device.Addresses, _mockKnxService);
        }

    }


}
