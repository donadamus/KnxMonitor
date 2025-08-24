using FluentAssertions;
using KnxModel;
using KnxTest.Unit.Base;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using Xunit;

namespace KnxTest.Unit.Models.Shutter
{
    public class ShutterDeviceSunProtectionTests : DeviceSunProtectionTests<ShutterDevice, ShutterAddresses>
    {
        private readonly ShutterDevice _device;
        private readonly ShutterAddresses _addresses;
        private readonly Mock<ILogger<ShutterDevice>> _mockLogger;
        protected override SunProtectionDeviceTestHelper<ShutterDevice, ShutterAddresses> _sunProtectionTestHelper { get; }

        public ShutterDeviceSunProtectionTests() : base()
        {
            _mockLogger = new Mock<ILogger<ShutterDevice>>();
            
            // Create device using the convenience constructor
            _device = new ShutterDevice("test-shutter-001", "Test Shutter", "1", _mockKnxService.Object, _mockLogger.Object, TimeSpan.FromSeconds(10));
            _addresses = _device.Addresses;

            // Create helper with lambda functions to get addresses from ShutterAddresses
            _sunProtectionTestHelper = new SunProtectionDeviceTestHelper<ShutterDevice, ShutterAddresses>(
                _device,
                _addresses,
                _mockKnxService
            );
        }
    }
}
