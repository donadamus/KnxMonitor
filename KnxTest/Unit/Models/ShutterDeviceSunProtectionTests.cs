using FluentAssertions;
using KnxModel;
using KnxTest.Unit.Base;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using Xunit;

namespace KnxTest.Unit.Models
{
    public class ShutterDeviceSunProtectionTests : DeviceSunProtectionTests<ShutterDevice, ShutterAddresses>
    {
        private readonly ShutterDevice _device;
        private readonly ShutterAddresses _addresses;
        private readonly Mock<ILogger<ShutterDevice>> _mockLogger;
        private readonly SunProtectionDeviceTestHelper<ShutterDevice, ShutterAddresses> _sunProtectionHelper;

        protected override SunProtectionDeviceTestHelper<ShutterDevice, ShutterAddresses> _sunProtectionTestHelper => _sunProtectionHelper;

        public ShutterDeviceSunProtectionTests() : base()
        {
            _mockLogger = new Mock<ILogger<ShutterDevice>>();
            
            // Create device using the convenience constructor
            _device = new ShutterDevice("test-shutter-001", "Test Shutter", "1", _mockKnxService.Object, _mockLogger.Object, TimeSpan.FromSeconds(10));
            _addresses = _device.Addresses;

            // Create helper with lambda functions to get addresses from ShutterAddresses
            _sunProtectionHelper = new SunProtectionDeviceTestHelper<ShutterDevice, ShutterAddresses>(
                _device,
                _addresses,
                _mockKnxService,
                addresses => addresses.BrightnessThreshold1,
                addresses => addresses.BrightnessThreshold2,
                addresses => addresses.OutdoorTemperatureThreshold
            );
        }
    }
}
