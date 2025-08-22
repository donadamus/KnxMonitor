using KnxModel;
using KnxTest.Unit.Base;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace KnxTest.Unit.Models.Dimmer
{
    public class DimmerDevicePercentageControllableTests : DevicePercentageControllableTests<DimmerDevice, DimmerAddresses>
    {
        protected override PercentageControllableDeviceTestHelper<DimmerDevice, DimmerAddresses> _percentageTestHelper { get; }
        public DimmerDevicePercentageControllableTests()
        {
            // Initialize DimmerDevice with mock KNX service
            var logger = new Mock<ILogger<DimmerDevice>>().Object;
            var device = new DimmerDevice("D_TEST", "Test Dimmer", "1", _mockKnxService.Object, logger, TimeSpan.FromSeconds(1));
            _percentageTestHelper = new PercentageControllableDeviceTestHelper<DimmerDevice, DimmerAddresses>(
                device, device.Addresses, _mockKnxService);
        }

    }
}
