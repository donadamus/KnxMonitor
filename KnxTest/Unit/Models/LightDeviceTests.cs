using FluentAssertions;
using KnxModel;
using System;
using System.Threading.Tasks;
using Xunit;
using static KnxTest.Unit.Models.DimmerDeviceTests;

namespace KnxTest.Unit.Models
{
    public class LightDeviceTests : LightDeviceTestsBase<LightDevice, LightAddresses>
    {
        protected override LightDevice _device { get; }
        // This class is just a placeholder to allow the test class to compile
        // Actual tests are defined in LightDeviceTests
        public LightDeviceTests()
        {
            // Initialize LightDevice with mock KNX service
            _device = new LightDevice("L_TEST", "Test Light", "1", _mockKnxService.Object);
        }


        #region IKnxDeviceBase Tests

        [Fact]
        public override void Constructor_SetsBasicProperties()
        {
            // Assert
            _device.Id.Should().Be("L_TEST");
            _device.Name.Should().Be("Test Light");
            _device.SubGroup.Should().Be("1");
            _device.LastUpdated.Should().Be(DateTime.MinValue); // Not initialized yet
        }

        #endregion

    }
}
