using FluentAssertions;
using KnxModel;
using KnxModel.Types;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Unit.Helpers
{
    public class SunProtectionDeviceTestHelper<TDevice, TAddresses>
        where TDevice : ISunProtectionThresholdCapableDevice, IKnxDeviceBase
        where TAddresses : ISunProtectionThresholdAddresses
    {
        private readonly TDevice _device;
        private readonly TAddresses _addresses;
        private readonly Mock<IKnxService> _mockKnxService;

        public SunProtectionDeviceTestHelper(
            TDevice device, 
            TAddresses addresses, 
            Mock<IKnxService> mockKnxService)
        {
            _device = device;
            _addresses = addresses;
            _mockKnxService = mockKnxService;
        }

        public void BrightnessThreshold1_WhenActivated_ShouldUpdateDeviceState()
        {
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, 
                _mockKnxService.Object, 
                new KnxGroupEventArgs(_addresses.BrightnessThreshold1, new KnxValue(true)));
            
            // Assert
            _device.BrightnessThreshold1Active.Should().BeTrue();
        }

        public void BrightnessThreshold1_WhenDeactivated_ShouldUpdateDeviceState()
        {
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, 
                _mockKnxService.Object, 
                new KnxGroupEventArgs(_addresses.BrightnessThreshold1, new KnxValue(false)));
            
            // Assert
            _device.BrightnessThreshold1Active.Should().BeFalse();
        }

        public void BrightnessThreshold2_WhenActivated_ShouldUpdateDeviceState()
        {
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, 
                _mockKnxService.Object, 
                new KnxGroupEventArgs(_addresses.BrightnessThreshold2, new KnxValue(true)));
            
            // Assert
            _device.BrightnessThreshold2Active.Should().BeTrue();
        }

        public void BrightnessThreshold2_WhenDeactivated_ShouldUpdateDeviceState()
        {
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, 
                _mockKnxService.Object, 
                new KnxGroupEventArgs(_addresses.BrightnessThreshold2, new KnxValue(false)));
            
            // Assert
            _device.BrightnessThreshold2Active.Should().BeFalse();
        }

        public void TemperatureThreshold_WhenActivated_ShouldUpdateDeviceState()
        {
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, 
                _mockKnxService.Object, 
                new KnxGroupEventArgs(_addresses.OutdoorTemperatureThreshold, new KnxValue(true)));
            
            // Assert
            _device.OutdoorTemperatureThresholdActive.Should().BeTrue();
        }

        public void TemperatureThreshold_WhenDeactivated_ShouldUpdateDeviceState()
        {
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, 
                _mockKnxService.Object, 
                new KnxGroupEventArgs(_addresses.OutdoorTemperatureThreshold, new KnxValue(false)));
            
            // Assert
            _device.OutdoorTemperatureThresholdActive.Should().BeFalse();
        }

        public async Task ReadBrightnessThreshold1StateAsync_ShouldRequestCorrectAddress()
        {
            // Arrange
            var expectedValue = true;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_addresses.BrightnessThreshold1))
                          .ReturnsAsync(expectedValue)
                          .Verifiable();

            // Act
            var result = await _device.ReadBrightnessThreshold1StateAsync();

            // Assert
            result.Should().Be(expectedValue);
        }

        public async Task ReadBrightnessThreshold2StateAsync_ShouldRequestCorrectAddress()
        {
            // Arrange
            var expectedValue = false;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_addresses.BrightnessThreshold2))
                          .ReturnsAsync(expectedValue)
                          .Verifiable();

            // Act
            var result = await _device.ReadBrightnessThreshold2StateAsync();

            // Assert
            result.Should().Be(expectedValue);
        }

        public async Task ReadTemperatureThresholdStateAsync_ShouldRequestCorrectAddress()
        {
            // Arrange
            var expectedValue = true;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_addresses.OutdoorTemperatureThreshold))
                          .ReturnsAsync(expectedValue)
                          .Verifiable();

            // Act
            var result = await _device.ReadOutdoorTemperatureThresholdStateAsync();

            // Assert
            result.Should().Be(expectedValue);
        }
    }
}
