using KnxModel;
using KnxTest.Unit.Base;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace KnxTest.Unit.Models.Dimmer
{
    public class DimmerDeviceLockableTests : DeviceLockableTests<DimmerDevice, DimmerAddresses>
    {
        protected override LockableDeviceTestHelper<DimmerDevice, DimmerAddresses> _lockableTestHelper { get; }
        public DimmerDeviceLockableTests()
        {
            // Initialize DimmerDevice with mock KNX service
            var logger = new Mock<ILogger<DimmerDevice>>().Object;
            var device = new DimmerDevice("D_TEST", "Test Dimmer", "1", _mockKnxService.Object, logger, TimeSpan.FromSeconds(1));
            _lockableTestHelper = new LockableDeviceTestHelper<DimmerDevice, DimmerAddresses>(
                device, device.Addresses, _mockKnxService);
        }

    }

}
