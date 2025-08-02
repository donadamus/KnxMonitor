using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using Moq.Language.Flow;
using KnxModel;

namespace KnxTest.Unit
{
    public class DimmerUnitTests : IDisposable
    {
        private readonly Mock<IKnxService> _mockKnxService;
        private readonly Dimmer _dimmer;

        public DimmerUnitTests()
        {
            _mockKnxService = new Mock<IKnxService>();
            _dimmer = new Dimmer("DIM1", "Test Dimmer", "1", _mockKnxService.Object);
        }

        public void Dispose()
        {
            _dimmer?.Dispose();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldCreateDimmerWithCorrectProperties()
        {
            // Assert
            _dimmer.Id.Should().Be("DIM1");
            _dimmer.Name.Should().Be("Test Dimmer");
            _dimmer.SubGroup.Should().Be("1");
            _dimmer.CurrentState.IsOn.Should().BeFalse();
            _dimmer.CurrentState.Brightness.Should().Be(0);
            _dimmer.CurrentState.IsLocked.Should().BeFalse();
        }

        [Fact]
        public void Constructor_ShouldCreateCorrectAddresses()
        {
            // Assert
            _dimmer.Addresses.SwitchControl.Should().Be("2/1/1");
            _dimmer.Addresses.SwitchFeedback.Should().Be("2/1/101");
            _dimmer.Addresses.BrightnessControl.Should().Be("2/2/1");
            _dimmer.Addresses.BrightnessFeedback.Should().Be("2/2/101");
            _dimmer.Addresses.LockControl.Should().Be("2/3/1");
            _dimmer.Addresses.LockFeedback.Should().Be("2/3/101");
        }

        #endregion

        #region Switch Control Tests

        [Fact]
        public async Task SetStateAsync_ShouldSendCorrectCommand()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue("2/1/1", true))
                          .Returns(Task.CompletedTask);

            // Act
            await _dimmer.SetStateAsync(true);

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("2/1/1", true), Times.Once);
        }

        [Fact]
        public async Task TurnOnAsync_ShouldCallSetStateWithTrue()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue("2/1/1", true))
                          .Returns(Task.CompletedTask);

            // Act
            await _dimmer.TurnOnAsync();

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("2/1/1", true), Times.Once);
        }

        [Fact]
        public async Task TurnOffAsync_ShouldCallSetStateWithFalse()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue("2/1/1", false))
                          .Returns(Task.CompletedTask);

            // Act
            await _dimmer.TurnOffAsync();

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("2/1/1", false), Times.Once);
        }

        [Fact]
        public async Task ReadStateAsync_ShouldReturnCorrectValue()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>("2/1/101"))
                          .ReturnsAsync(true);

            // Act
            var result = await _dimmer.ReadStateAsync();

            // Assert
            result.Should().BeTrue();
            _mockKnxService.Verify(s => s.RequestGroupValue<bool>("2/1/101"), Times.Once);
        }

        [Fact]
        public async Task ToggleAsync_WhenOff_ShouldTurnOn()
        {
            // Arrange - dimmer starts OFF
            _mockKnxService.Setup(s => s.WriteGroupValue("2/1/1", true))
                          .Returns(Task.CompletedTask);

            // Act
            await _dimmer.ToggleAsync();

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("2/1/1", true), Times.Once);
        }

        #endregion

        #region Brightness Control Tests

        [Fact]
        public async Task SetBrightnessAsync_ShouldSendCorrectFloatValue()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue("2/2/1", 0.5f))
                          .Returns(Task.CompletedTask);

            // Act
            await _dimmer.SetBrightnessAsync(50);

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("2/2/1", 0.5f), Times.Once);
        }

        [Fact]
        public async Task SetBrightnessAsync_With0_ShouldSend0()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue("2/2/1", 0.0f))
                          .Returns(Task.CompletedTask);

            // Act
            await _dimmer.SetBrightnessAsync(0);

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("2/2/1", 0.0f), Times.Once);
        }

        [Fact]
        public async Task SetBrightnessAsync_With100_ShouldSend1()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue("2/2/1", 1.0f))
                          .Returns(Task.CompletedTask);

            // Act
            await _dimmer.SetBrightnessAsync(100);

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("2/2/1", 1.0f), Times.Once);
        }

        [Fact]
        public async Task SetBrightnessAsync_WithNegativeValue_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _dimmer.SetBrightnessAsync(-1));
        }

        [Fact]
        public async Task SetBrightnessAsync_WithValueOver100_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _dimmer.SetBrightnessAsync(101));
        }

        [Fact]
        public async Task ReadBrightnessAsync_ShouldReturnCorrectPercentage()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<float>("2/2/101"))
                          .ReturnsAsync(0.75f);

            // Act
            var result = await _dimmer.ReadBrightnessAsync();

            // Assert
            result.Should().Be(75);
            _mockKnxService.Verify(s => s.RequestGroupValue<float>("2/2/101"), Times.Once);
        }

        [Fact]
        public async Task ReadBrightnessAsync_WithZero_ShouldReturn0()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<float>("2/2/101"))
                          .ReturnsAsync(0.0f);

            // Act
            var result = await _dimmer.ReadBrightnessAsync();

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task ReadBrightnessAsync_WithOne_ShouldReturn100()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<float>("2/2/101"))
                          .ReturnsAsync(1.0f);

            // Act
            var result = await _dimmer.ReadBrightnessAsync();

            // Assert
            result.Should().Be(100);
        }

        [Fact]
        public async Task FadeToAsync_WithNegativeTarget_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _dimmer.FadeToAsync(-1, TimeSpan.FromSeconds(1)));
        }

        [Fact]
        public async Task FadeToAsync_WithTargetOver100_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _dimmer.FadeToAsync(101, TimeSpan.FromSeconds(1)));
        }

        #endregion

        #region Lock Control Tests

        [Fact]
        public async Task LockAsync_ShouldSendCorrectCommand()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue("2/3/1", true))
                          .Returns(Task.CompletedTask);

            // Act
            await _dimmer.LockAsync();

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("2/3/1", true), Times.Once);
        }

        [Fact]
        public async Task UnlockAsync_ShouldSendCorrectCommand()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue("2/3/1", false))
                          .Returns(Task.CompletedTask);

            // Act
            await _dimmer.UnlockAsync();

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("2/3/1", false), Times.Once);
        }

        [Fact]
        public async Task ReadLockStateAsync_ShouldReturnCorrectValue()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>("2/3/101"))
                          .ReturnsAsync(true);

            // Act
            var result = await _dimmer.ReadLockStateAsync();

            // Assert
            result.Should().BeTrue();
            _mockKnxService.Verify(s => s.RequestGroupValue<bool>("2/3/101"), Times.Once);
        }

        #endregion

        #region Message Processing Tests

        [Fact]
        public void ProcessKnxMessage_WithSwitchFeedback_ShouldUpdateState()
        {
            // Arrange
            var mockValue = new Mock<IKnxValue>();
            mockValue.Setup(v => v.AsBoolean()).Returns(true);
            
            var args = new KnxGroupEventArgs("2/1/101", mockValue.Object);

            // Act
            _dimmer.ProcessKnxMessage(args);

            // Assert
            _dimmer.CurrentState.IsOn.Should().BeTrue();
        }

        [Fact]
        public void ProcessKnxMessage_WithBrightnessFeedback_ShouldUpdateBrightnessAndState()
        {
            // Arrange
            var mockValue = new Mock<IKnxValue>();
            mockValue.Setup(v => v.AsFloat()).Returns(0.8f);
            
            var args = new KnxGroupEventArgs("2/2/101", mockValue.Object);

            // Act
            _dimmer.ProcessKnxMessage(args);

            // Assert
            _dimmer.CurrentState.IsOn.Should().BeTrue();
            _dimmer.CurrentState.Brightness.Should().Be(80);
        }

        [Fact]
        public void ProcessKnxMessage_WithZeroBrightness_ShouldTurnOff()
        {
            // Arrange
            var mockValue = new Mock<IKnxValue>();
            mockValue.Setup(v => v.AsFloat()).Returns(0.0f);
            
            var args = new KnxGroupEventArgs("2/2/101", mockValue.Object);

            // Act
            _dimmer.ProcessKnxMessage(args);

            // Assert
            _dimmer.CurrentState.IsOn.Should().BeFalse();
            _dimmer.CurrentState.Brightness.Should().Be(0);
        }

        [Fact]
        public void ProcessKnxMessage_WithLockFeedback_ShouldUpdateLockState()
        {
            // Arrange
            var mockValue = new Mock<IKnxValue>();
            mockValue.Setup(v => v.AsBoolean()).Returns(true);
            
            var args = new KnxGroupEventArgs("2/3/101", mockValue.Object);

            // Act
            _dimmer.ProcessKnxMessage(args);

            // Assert
            _dimmer.CurrentState.IsLocked.Should().BeTrue();
        }

        #endregion

        #region State Management Tests

        [Fact]
        public async Task SaveCurrentStateAsync_ShouldReadAndSaveState()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>("2/1/101")).ReturnsAsync(true);
            _mockKnxService.Setup(s => s.RequestGroupValue<float>("2/2/101")).ReturnsAsync(0.6f);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>("2/3/101")).ReturnsAsync(false);

            // Act
            _dimmer.SaveCurrentState();

            // Assert
            _dimmer.SavedState.Should().NotBeNull();
            _dimmer.SavedState.IsOn.Should().BeTrue();
            _dimmer.SavedState.Brightness.Should().Be(60);
            _dimmer.SavedState.IsLocked.Should().BeFalse();
        }

        [Fact]
        public async Task RestoreSavedStateAsync_ShouldRestoreBrightness()
        {
            // Arrange
            _dimmer.SaveCurrentState(); // Save default state (0%)
            _mockKnxService.Setup(s => s.WriteGroupValue("2/2/1", 0.5f)).Returns(Task.CompletedTask);

            // Manually set a saved state with 50% brightness
            var savedState = new DimmerState(true, 50, false, DateTime.Now);
            typeof(Dimmer).GetProperty(nameof(Dimmer.SavedState))?.SetValue(_dimmer, savedState);

            // Act
            await _dimmer.RestoreSavedStateAsync();

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("2/2/1", 0.5f), Times.Once);
        }

        [Fact]
        public async Task RestoreSavedStateAsync_WithoutSavedState_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _dimmer.RestoreSavedStateAsync());
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ShouldReturnCorrectFormat()
        {
            // Act
            var result = _dimmer.ToString();

            // Assert
            result.Should().Be("Dimmer DIM1 (Test Dimmer) - State: OFF, Brightness: 0%, Lock: UNLOCKED");
        }

        #endregion
    }
}
