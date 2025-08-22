using KnxModel;
using KnxTest.Unit.Base;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace KnxTest.Unit.Models.Light
{
    public class LightDeviceLockableTests : DeviceLockableTests<LightDevice, LightAddresses>
    {
        protected override LockableDeviceTestHelper<LightDevice, LightAddresses> _lockableTestHelper { get; }
        public LightDeviceLockableTests()
        {
            // Initialize DimmerDevice with mock KNX service
            var logger = new Mock<ILogger<LightDevice>>().Object;
            var device = new LightDevice("D_TEST", "Test Dimmer", "1", _mockKnxService.Object, logger, TimeSpan.FromSeconds(1));
            _lockableTestHelper = new LockableDeviceTestHelper<LightDevice, LightAddresses>(
                device, device.Addresses, _mockKnxService);
        }

    }

}
