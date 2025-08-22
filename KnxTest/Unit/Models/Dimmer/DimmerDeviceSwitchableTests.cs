using KnxModel;
using KnxTest.Unit.Base;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace KnxTest.Unit.Models.Dimmer
{
    public class DimmerDeviceSwitchableTests : DeviceSwitchableTests<DimmerDevice, DimmerAddresses>
    {
        protected override SwitchableDeviceTestHelper<DimmerDevice, DimmerAddresses> _switchableTestHelper { get; }
        public DimmerDeviceSwitchableTests()
        {
            // Initialize DimmerDevice with mock KNX service
            var logger = new Mock<ILogger<DimmerDevice>>().Object;
            var device = new DimmerDevice("D_TEST", "Test Dimmer", "1", _mockKnxService.Object, logger, TimeSpan.FromSeconds(1));
            _switchableTestHelper = new SwitchableDeviceTestHelper<DimmerDevice, DimmerAddresses>(
                device, device.Addresses, _mockKnxService);
        }

    }
}
