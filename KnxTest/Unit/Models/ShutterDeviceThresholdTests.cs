using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using KnxModel;

namespace KnxTest.Unit.Models
{
    public class ShutterDeviceThresholdTests
    {
        private readonly Mock<IKnxService> _mockKnxService;
        private readonly Mock<ILogger<ShutterDevice>> _mockLogger;
        private readonly ShutterDevice _shutterDevice;
        private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);

        public ShutterDeviceThresholdTests()
        {
            _mockKnxService = new Mock<IKnxService>();
            _mockLogger = new Mock<ILogger<ShutterDevice>>();
            
            _shutterDevice = new ShutterDevice(
                "test-shutter-001", 
                "Test Shutter", 
                "1",
                _mockKnxService.Object, 
                _mockLogger.Object, 
                _defaultTimeout
            );
        }

        [Fact]
        public async Task InitializeAsync_ShouldReadAllThresholdStates()
        {
            // Arrange
            _mockKnxService.Setup(x => x.RequestGroupValue<float>(It.IsAny<string>())).ReturnsAsync(50.0f);
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>("4/3/101")).ReturnsAsync(false); // Lock feedback
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>("4/4/1")).ReturnsAsync(false); // Sun protection block
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>("0/2/3")).ReturnsAsync(true); // Brightness threshold 1
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>("0/2/4")).ReturnsAsync(false); // Brightness threshold 2
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>("0/2/7")).ReturnsAsync(true); // Outdoor temperature threshold

            // Act
            await _shutterDevice.InitializeAsync();

            // Assert
            Assert.True(_shutterDevice.BrightnessThreshold1Active);
            Assert.False(_shutterDevice.BrightnessThreshold2Active);
            Assert.True(_shutterDevice.OutdoorTemperatureThresholdActive);
            
            _mockKnxService.Verify(x => x.RequestGroupValue<bool>("0/2/3"), Times.Once);
            _mockKnxService.Verify(x => x.RequestGroupValue<bool>("0/2/4"), Times.Once);
            _mockKnxService.Verify(x => x.RequestGroupValue<bool>("0/2/7"), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadBrightnessThreshold1StateAsync_ShouldReturnCorrectValue(bool expectedState)
        {
            // Arrange
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>("0/2/3")).ReturnsAsync(expectedState);

            // Act
            var result = await _shutterDevice.ReadBrightnessThreshold1StateAsync();

            // Assert
            Assert.Equal(expectedState, result);
            Assert.Equal(expectedState, _shutterDevice.BrightnessThreshold1Active);
            _mockKnxService.Verify(x => x.RequestGroupValue<bool>("0/2/3"), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadBrightnessThreshold2StateAsync_ShouldReturnCorrectValue(bool expectedState)
        {
            // Arrange
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>("0/2/4")).ReturnsAsync(expectedState);

            // Act
            var result = await _shutterDevice.ReadBrightnessThreshold2StateAsync();

            // Assert
            Assert.Equal(expectedState, result);
            Assert.Equal(expectedState, _shutterDevice.BrightnessThreshold2Active);
            _mockKnxService.Verify(x => x.RequestGroupValue<bool>("0/2/4"), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadOutdoorTemperatureThresholdStateAsync_ShouldReturnCorrectValue(bool expectedState)
        {
            // Arrange
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>("0/2/7")).ReturnsAsync(expectedState);

            // Act
            var result = await _shutterDevice.ReadOutdoorTemperatureThresholdStateAsync();

            // Assert
            Assert.Equal(expectedState, result);
            Assert.Equal(expectedState, _shutterDevice.OutdoorTemperatureThresholdActive);
            _mockKnxService.Verify(x => x.RequestGroupValue<bool>("0/2/7"), Times.Once);
        }

        [Fact]
        public async Task WaitForBrightnessThreshold1StateAsync_WhenTargetStateReached_ShouldReturnTrue()
        {
            // Arrange
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>("0/2/3")).ReturnsAsync(true);

            // Act
            var result = await _shutterDevice.WaitForBrightnessThreshold1StateAsync(true, TimeSpan.FromSeconds(1));

            // Assert
            Assert.True(result);
            Assert.True(_shutterDevice.BrightnessThreshold1Active);
        }

        [Fact]
        public async Task WaitForBrightnessThreshold2StateAsync_WhenTargetStateReached_ShouldReturnTrue()
        {
            // Arrange
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>("0/2/4")).ReturnsAsync(false);

            // Act
            var result = await _shutterDevice.WaitForBrightnessThreshold2StateAsync(false, TimeSpan.FromSeconds(1));

            // Assert
            Assert.True(result);
            Assert.False(_shutterDevice.BrightnessThreshold2Active);
        }

        [Fact]
        public async Task WaitForOutdoorTemperatureThresholdStateAsync_WhenTargetStateReached_ShouldReturnTrue()
        {
            // Arrange
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>("0/2/7")).ReturnsAsync(true);

            // Act
            var result = await _shutterDevice.WaitForOutdoorTemperatureThresholdStateAsync(true, TimeSpan.FromSeconds(1));

            // Assert
            Assert.True(result);
            Assert.True(_shutterDevice.OutdoorTemperatureThresholdActive);
        }

        [Fact]
        public void ProcessThresholdFeedback_BrightnessThreshold1_ShouldUpdateState()
        {
            // Arrange
            var eventArgs = new KnxGroupEventArgs("0/2/3", new KnxValue(true));

            // Act
            _shutterDevice.GetType()
                .GetMethod("OnKnxMessageReceived", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(_shutterDevice, new object[] { _shutterDevice, eventArgs });

            // Assert
            Assert.True(_shutterDevice.BrightnessThreshold1Active);
        }

        [Fact]
        public void ProcessThresholdFeedback_BrightnessThreshold2_ShouldUpdateState()
        {
            // Arrange
            var eventArgs = new KnxGroupEventArgs("0/2/4", new KnxValue(false));

            // Act
            _shutterDevice.GetType()
                .GetMethod("OnKnxMessageReceived", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(_shutterDevice, new object[] { _shutterDevice, eventArgs });

            // Assert
            Assert.False(_shutterDevice.BrightnessThreshold2Active);
        }

        [Fact]
        public void ProcessThresholdFeedback_OutdoorTemperatureThreshold_ShouldUpdateState()
        {
            // Arrange
            var eventArgs = new KnxGroupEventArgs("0/2/7", new KnxValue(true));

            // Act
            _shutterDevice.GetType()
                .GetMethod("OnKnxMessageReceived", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(_shutterDevice, new object[] { _shutterDevice, eventArgs });

            // Assert
            Assert.True(_shutterDevice.OutdoorTemperatureThresholdActive);
        }

        [Fact]
        public void ThresholdAddresses_ShouldBeCorrectlyConfigured()
        {
            // Assert
            Assert.Equal("0/2/3", _shutterDevice.Addresses.BrightnessThreshold1);
            Assert.Equal("0/2/4", _shutterDevice.Addresses.BrightnessThreshold2);
            Assert.Equal("0/2/7", _shutterDevice.Addresses.OutdoorTemperatureThreshold);
        }
    }
}
