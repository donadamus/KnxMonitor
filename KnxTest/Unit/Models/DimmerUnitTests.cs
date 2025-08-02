using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using Moq.Language.Flow;
using KnxModel;
using KnxService;

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
            // Arrange - Create expected addresses using configuration
            var subGroup = "1";
            var expectedSwitchControl = KnxAddressConfiguration.CreateDimmerSwitchControlAddress(subGroup);
            var expectedSwitchFeedback = KnxAddressConfiguration.CreateDimmerSwitchFeedbackAddress(subGroup);
            var expectedBrightnessControl = KnxAddressConfiguration.CreateDimmerBrightnessControlAddress(subGroup);
            var expectedBrightnessFeedback = KnxAddressConfiguration.CreateDimmerBrightnessFeedbackAddress(subGroup);
            var expectedLockControl = KnxAddressConfiguration.CreateDimmerLockAddress(subGroup);
            var expectedLockFeedback = KnxAddressConfiguration.CreateDimmerLockFeedbackAddress(subGroup);

            // Assert
            _dimmer.Addresses.SwitchControl.Should().Be(expectedSwitchControl);
            _dimmer.Addresses.SwitchFeedback.Should().Be(expectedSwitchFeedback);
            _dimmer.Addresses.BrightnessControl.Should().Be(expectedBrightnessControl);
            _dimmer.Addresses.BrightnessFeedback.Should().Be(expectedBrightnessFeedback);
            _dimmer.Addresses.LockControl.Should().Be(expectedLockControl);
            _dimmer.Addresses.LockFeedback.Should().Be(expectedLockFeedback);
        }

        #endregion

        #region Switch Control Tests

        [Fact]
        public async Task SetStateAsync_ShouldSendCorrectCommand()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.SwitchControl, true));

            // Act
            await _dimmer.SetStateAsync(true);

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue(_dimmer.Addresses.SwitchControl, true), Times.Once);
        }

        [Fact]
        public async Task TurnOnAsync_ShouldCallSetStateWithTrue()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.SwitchControl, true));

            // Act
            await _dimmer.TurnOnAsync();

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue(_dimmer.Addresses.SwitchControl, true), Times.Once);
        }

        [Fact]
        public async Task TurnOffAsync_ShouldCallSetStateWithFalse()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.SwitchControl, false));

            // Act
            await _dimmer.TurnOffAsync();

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue(_dimmer.Addresses.SwitchControl, false), Times.Once);
        }

        [Fact]
        public async Task ReadStateAsync_ShouldReturnCorrectValue()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_dimmer.Addresses.SwitchFeedback))
                          .ReturnsAsync(true);

            // Act
            var result = await _dimmer.ReadStateAsync();

            // Assert
            result.Should().BeTrue();
            _mockKnxService.Verify(s => s.RequestGroupValue<bool>(_dimmer.Addresses.SwitchFeedback), Times.Once);
        }

        [Fact]
        public async Task ToggleAsync_WhenOff_ShouldTurnOn()
        {
            // Arrange - dimmer starts OFF
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.SwitchControl, true));

            // Act
            await _dimmer.ToggleAsync();

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue(_dimmer.Addresses.SwitchControl, true), Times.Once);
        }

        #endregion

        #region Brightness Control Tests

        [Fact]
        public async Task SetBrightnessAsync_ShouldSendCorrectFloatValue()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.BrightnessControl, 50));

            // Act
            await _dimmer.SetBrightnessAsync(50, TimeSpan.Zero);

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue(_dimmer.Addresses.BrightnessControl, 50), Times.Once);
        }

        [Fact]
        public async Task SetBrightnessAsync_With0_ShouldSend0()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.BrightnessControl, 0.0f));

            // Act
            await _dimmer.SetBrightnessAsync(0);

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue(_dimmer.Addresses.BrightnessControl, 0.0f), Times.Once);
        }

        [Fact]
        public async Task SetBrightnessAsync_With100_ShouldSend1()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.BrightnessControl, 1.0f));

            // Act
            await _dimmer.SetBrightnessAsync(100);

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue(_dimmer.Addresses.BrightnessControl, 1.0f), Times.Once);
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
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_dimmer.Addresses.BrightnessFeedback))
                          .ReturnsAsync(75);

            // Act
            var result = await _dimmer.ReadBrightnessAsync();

            // Assert
            result.Should().Be(75);
            _mockKnxService.Verify(s => s.RequestGroupValue<float>(_dimmer.Addresses.BrightnessFeedback), Times.Once);
        }

        [Fact]
        public async Task ReadBrightnessAsync_WithZero_ShouldReturn0()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_dimmer.Addresses.BrightnessFeedback))
                          .ReturnsAsync(0);

            // Act
            var result = await _dimmer.ReadBrightnessAsync();

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task ReadBrightnessAsync_WithOne_ShouldReturn100()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_dimmer.Addresses.BrightnessFeedback))
                          .ReturnsAsync(100);

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
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.LockControl, true));

            // Act
            await _dimmer.LockAsync(TimeSpan.Zero);

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue(_dimmer.Addresses.LockControl, true), Times.Once);
        }

        [Fact]
        public async Task UnlockAsync_ShouldSendCorrectCommand()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.LockControl, false));

            // Act
            await _dimmer.UnlockAsync();

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue(_dimmer.Addresses.LockControl, false), Times.Once);
        }

        [Fact]
        public async Task ReadLockStateAsync_ShouldReturnCorrectValue()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_dimmer.Addresses.LockFeedback))
                          .ReturnsAsync(true);

            // Act
            var result = await _dimmer.ReadLockStateAsync();

            // Assert
            result.Should().BeTrue();
            _mockKnxService.Verify(s => s.RequestGroupValue<bool>(_dimmer.Addresses.LockFeedback), Times.Once);
        }

        #endregion

        #region State Management Tests

        [Fact]
        public void SaveCurrentStateAsync_ShouldReadAndSaveState()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_dimmer.Addresses.SwitchFeedback)).ReturnsAsync(true);
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_dimmer.Addresses.BrightnessFeedback)).ReturnsAsync(0.6f);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_dimmer.Addresses.LockFeedback)).ReturnsAsync(false);

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
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.BrightnessControl, 0.5f));

            // Manually set a saved state with 50% brightness
            var savedState = new DimmerState(true, 50, false, DateTime.Now);
            typeof(Dimmer).GetProperty(nameof(Dimmer.SavedState))?.SetValue(_dimmer, savedState);

            // Act
            await _dimmer.RestoreSavedStateAsync();

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue(_dimmer.Addresses.BrightnessControl, 0.5f), Times.Once);
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
