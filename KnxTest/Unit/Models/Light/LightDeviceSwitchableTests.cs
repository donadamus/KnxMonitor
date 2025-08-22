using KnxModel;
using KnxTest.Unit.Base;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace KnxTest.Unit.Models.Light
{
    public class LightDeviceSwitchableTests : DeviceSwitchableTests<LightDevice, LightAddresses>
    {
        protected override SwitchableDeviceTestHelper<LightDevice, LightAddresses> _switchableTestHelper { get; }
        public LightDeviceSwitchableTests()
        {
            // Initialize DimmerDevice with mock KNX service
            var logger = new Mock<ILogger<LightDevice>>().Object;
            var device = new LightDevice("D_TEST", "Test Dimmer", "1", _mockKnxService.Object, logger, TimeSpan.FromSeconds(1));
            _switchableTestHelper = new SwitchableDeviceTestHelper<LightDevice, LightAddresses>(
                device, device.Addresses, _mockKnxService);
        }

    }
}
