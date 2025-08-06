using FluentAssertions;
using KnxModel;

namespace KnxTest.Unit.Models
{
    public class DimmerDeviceTests : LightDeviceTestsBase<DimmerDevice, DimmerAddresses>
    {
        protected override DimmerDevice _device { get; }
        // This class is just a placeholder to allow the test class to compile
        // Actual tests are defined in DimmerDeviceTests
        public DimmerDeviceTests()
        {
            // Initialize DimmerDevice with mock KNX service
            _device = new DimmerDevice("D_TEST", "Test Dimmer", "1", _mockKnxService.Object);
        }

        public override void Constructor_SetsBasicProperties()
        {
            // Assert
            _device.Id.Should().Be("D_TEST");
            _device.Name.Should().Be("Test Dimmer");
            _device.SubGroup.Should().Be("1");
        }
    }
}
