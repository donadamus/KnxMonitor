using FluentAssertions;
using KnxModel;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Unit.Helpers
{
    public class SunProtectionDeviceTestHelper<TDevice, TAddresses>
        where TDevice : ISunProtectionThresholdCapableDevice, IKnxDeviceBase
        where TAddresses : class
    {
        private readonly TDevice _device;
        private readonly TAddresses _addresses;
        private readonly Mock<IKnxService> _mockKnxService;
        private readonly Func<TAddresses, string> _getBrightnessThreshold1Address;
        private readonly Func<TAddresses, string> _getBrightnessThreshold2Address;
        private readonly Func<TAddresses, string> _getTemperatureThresholdAddress;

        public SunProtectionDeviceTestHelper(
            TDevice device, 
            TAddresses addresses, 
            Mock<IKnxService> mockKnxService,
            Func<TAddresses, string> getBrightnessThreshold1Address,
            Func<TAddresses, string> getBrightnessThreshold2Address,
            Func<TAddresses, string> getTemperatureThresholdAddress)
        {
            _device = device;
            _addresses = addresses;
            _mockKnxService = mockKnxService;
            _getBrightnessThreshold1Address = getBrightnessThreshold1Address;
            _getBrightnessThreshold2Address = getBrightnessThreshold2Address;
            _getTemperatureThresholdAddress = getTemperatureThresholdAddress;
        }

        public void BrightnessThreshold1_WhenActivated_ShouldUpdateDeviceState()
        {
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, 
                _mockKnxService.Object, 
                new KnxGroupEventArgs(_getBrightnessThreshold1Address(_addresses), new KnxValue(true)));
            
            // Assert
            _device.BrightnessThreshold1Active.Should().BeTrue();
        }

        public void BrightnessThreshold1_WhenDeactivated_ShouldUpdateDeviceState()
        {
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, 
                _mockKnxService.Object, 
                new KnxGroupEventArgs(_getBrightnessThreshold1Address(_addresses), new KnxValue(false)));
            
            // Assert
            _device.BrightnessThreshold1Active.Should().BeFalse();
        }

        public void BrightnessThreshold2_WhenActivated_ShouldUpdateDeviceState()
        {
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, 
                _mockKnxService.Object, 
                new KnxGroupEventArgs(_getBrightnessThreshold2Address(_addresses), new KnxValue(true)));
            
            // Assert
            _device.BrightnessThreshold2Active.Should().BeTrue();
        }

        public void BrightnessThreshold2_WhenDeactivated_ShouldUpdateDeviceState()
        {
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, 
                _mockKnxService.Object, 
                new KnxGroupEventArgs(_getBrightnessThreshold2Address(_addresses), new KnxValue(false)));
            
            // Assert
            _device.BrightnessThreshold2Active.Should().BeFalse();
        }

        public void TemperatureThreshold_WhenActivated_ShouldUpdateDeviceState()
        {
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, 
                _mockKnxService.Object, 
                new KnxGroupEventArgs(_getTemperatureThresholdAddress(_addresses), new KnxValue(true)));
            
            // Assert
            _device.OutdoorTemperatureThresholdActive.Should().BeTrue();
        }

        public void TemperatureThreshold_WhenDeactivated_ShouldUpdateDeviceState()
        {
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, 
                _mockKnxService.Object, 
                new KnxGroupEventArgs(_getTemperatureThresholdAddress(_addresses), new KnxValue(false)));
            
            // Assert
            _device.OutdoorTemperatureThresholdActive.Should().BeFalse();
        }

        public async Task ReadBrightnessThreshold1StateAsync_ShouldRequestCorrectAddress()
        {
            // Arrange
            var expectedValue = true;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_getBrightnessThreshold1Address(_addresses)))
                          .ReturnsAsync(expectedValue)
                          .Verifiable();

            // Act
            var result = await _device.ReadBrightnessThreshold1StateAsync();

            // Assert
            result.Should().Be(expectedValue);
            _mockKnxService.Verify(s => s.RequestGroupValue<bool>(_getBrightnessThreshold1Address(_addresses)), Times.Once);
        }

        public async Task ReadBrightnessThreshold2StateAsync_ShouldRequestCorrectAddress()
        {
            // Arrange
            var expectedValue = false;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_getBrightnessThreshold2Address(_addresses)))
                          .ReturnsAsync(expectedValue)
                          .Verifiable();

            // Act
            var result = await _device.ReadBrightnessThreshold2StateAsync();

            // Assert
            result.Should().Be(expectedValue);
            _mockKnxService.Verify(s => s.RequestGroupValue<bool>(_getBrightnessThreshold2Address(_addresses)), Times.Once);
        }

        public async Task ReadTemperatureThresholdStateAsync_ShouldRequestCorrectAddress()
        {
            // Arrange
            var expectedValue = true;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_getTemperatureThresholdAddress(_addresses)))
                          .ReturnsAsync(expectedValue)
                          .Verifiable();

            // Act
            var result = await _device.ReadOutdoorTemperatureThresholdStateAsync();

            // Assert
            result.Should().Be(expectedValue);
            _mockKnxService.Verify(s => s.RequestGroupValue<bool>(_getTemperatureThresholdAddress(_addresses)), Times.Once);
        }
    }
}
