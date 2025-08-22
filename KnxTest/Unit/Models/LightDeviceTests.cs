using FluentAssertions;
using KnxModel;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using static KnxTest.Unit.Models.DimmerDeviceTests;

namespace KnxTest.Unit.Models
{
    public class LightDeviceTests : LightDeviceTestsBase<LightDevice, LightAddresses>
    {
        protected override LightDevice _device { get; }
        private readonly Mock<ILogger<LightDevice>> _mockLogger;

        protected override ILogger<LightDevice> _logger => _mockLogger.Object;

        // This class is just a placeholder to allow the test class to compile
        // Actual tests are defined in LightDeviceTests
        public LightDeviceTests()
        {
            _mockLogger = new Mock<ILogger<LightDevice>>();
            // Initialize LightDevice with mock KNX service
            _device = new LightDevice("L_TEST", "Test Light", "1", _mockKnxService.Object, _mockLogger.Object, TimeSpan.FromSeconds(1));
        }



    }
}
