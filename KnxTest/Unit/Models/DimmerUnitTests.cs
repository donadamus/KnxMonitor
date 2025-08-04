using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using Moq.Language.Flow;
using KnxModel;
using KnxService;

namespace KnxTest.Unit.Models
{
    public class DimmerUnitTests : BaseKnxDeviceUnitTests
    {
        private readonly DimmerOld _dimmer;

        public DimmerUnitTests()
        {
            _dimmer = new DimmerOld("DIM1", "Test Dimmer", "1", _mockKnxService.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldCreateDimmerWithCorrectProperties()
        {
            // Assert
            _dimmer.Id.Should().Be("DIM1");
            _dimmer.Name.Should().Be("Test Dimmer");
            _dimmer.SubGroup.Should().Be("1");
            _dimmer.CurrentState.Switch.Should().Be(Switch.Unknown);
            _dimmer.CurrentState.Brightness.Should().Be(0);
            _dimmer.CurrentState.Lock.Should().Be(Lock.Unknown);
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
            await _dimmer.SetStateAsync(Switch.On, TimeSpan.Zero);

            // Assert
        }

        [Fact]
        public async Task TurnOnAsync_ShouldCallSetStateWithTrue()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.SwitchControl, true));

            // Act
            await _dimmer.TurnOnAsync(TimeSpan.Zero);

            // Assert
        }

        [Fact]
        public async Task TurnOffAsync_ShouldCallSetStateWithFalse()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.SwitchControl, false))
                .Callback<string, bool>((address, value) =>
                {
                    // Simulate feedback response for switch state
                    var feedbackArgs = new KnxGroupEventArgs(_dimmer.Addresses.SwitchFeedback, new KnxValue(value));
                    _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                });

            // Act
            await _dimmer.TurnOffAsync();

            // Assert
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
            result.Should().Be(Switch.On);
        }

        [Fact]
        public async Task ToggleAsync_WhenOff_ShouldTurnOn()
        {
            // Arrange - dimmer starts OFF
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.SwitchControl, It.IsAny<bool>()))
                .Callback<string, bool>((address, value) =>
                {
                    // Simulate feedback response for switch state
                    var feedbackArgs = new KnxGroupEventArgs(_dimmer.Addresses.SwitchFeedback, new KnxValue(value));
                    _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                });

            // Act
            await _dimmer.TurnOffAsync();
            _dimmer.CurrentState.Switch.Should().Be(Switch.Off);

            await _dimmer.ToggleAsync();

            _dimmer.CurrentState.Switch.Should().Be(Switch.On);
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
        }

        [Fact]
        public async Task SetBrightnessAsync_With0_ShouldSend0()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.BrightnessControl, 0));

            // Act
            await _dimmer.SetBrightnessAsync(0);
        }

        [Fact]
        public async Task SetBrightnessAsync_With100_ShouldSend1()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.BrightnessControl, 100));

            // Act
            await _dimmer.SetBrightnessAsync(100, TimeSpan.Zero);
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
        }

        [Fact]
        public async Task UnlockAsync_ShouldSendCorrectCommand()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.LockControl, false));

            // Act
            await _dimmer.UnlockAsync(TimeSpan.Zero);
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
            result.Should().Be(Lock.On);
        }

        #endregion

        #region State Management Tests

        [Fact]
        public async Task SaveCurrentState_ShouldSaveCurrentState()
        {
            // Arrange - setup mock responses for RefreshStateAsync
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_dimmer.Addresses.SwitchFeedback)).ReturnsAsync(true);
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_dimmer.Addresses.BrightnessFeedback)).ReturnsAsync(60f);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_dimmer.Addresses.LockFeedback)).ReturnsAsync(false);

            // Refresh state from mocked KNX responses
            await _dimmer.RefreshStateAsync();

            // Act
            _dimmer.SaveCurrentState();

            // Assert
            _dimmer.SavedState.Should().NotBeNull();
            _dimmer.SavedState.Switch.Should().Be(Switch.On);
            _dimmer.SavedState.Brightness.Should().Be(60);
            _dimmer.SavedState.Lock.Should().Be(Lock.Off);
        }

        [Fact]
        public async Task RestoreSavedStateAsync_ShouldRestoreBrightness()
        {
            // Arrange - simulate dimmer being at 50% brightness
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_dimmer.Addresses.SwitchFeedback))
                          .ReturnsAsync(true);
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_dimmer.Addresses.BrightnessFeedback))
                          .ReturnsAsync(50);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_dimmer.Addresses.LockFeedback))
                          .ReturnsAsync(false);
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.BrightnessControl, It.IsAny<float>()))
                .Callback<string, float>((address, positionValue) =>
                {
                    // Simulate KNX feedback response with the same value that was written
                    // Convert float to double for KnxValue since AsPercentageValue() supports double but not float
                    var feedbackArgs = new KnxGroupEventArgs(_dimmer.Addresses.BrightnessFeedback, new KnxValue(positionValue));
                    _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                });


            await _dimmer.InitializeAsync();
            _dimmer.SaveCurrentState();

            _dimmer.CurrentState.Brightness.Should().Be(50);

            // Change current state to 0% (simulate dimmer being turned off)
            await _dimmer.SetBrightnessAsync(10);

            _dimmer.CurrentState.Brightness.Should().Be(10);

            // Act
            await _dimmer.RestoreSavedStateAsync();
            _dimmer.CurrentState.Brightness.Should().Be(50);

        }

        [Fact]
        public async Task RestoreSavedStateAsync_WithoutSavedState_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _dimmer.RestoreSavedStateAsync());
        }

        [Fact]
        public async Task RestoreSavedStateAsync_WhenLocked_ShouldTemporarilyUnlockAndRestore()
        {
            // Arrange - set up initial state: dimmer at 60%, unlocked
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_dimmer.Addresses.SwitchFeedback)).ReturnsAsync(true);
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_dimmer.Addresses.BrightnessFeedback)).ReturnsAsync(60f);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_dimmer.Addresses.LockFeedback)).ReturnsAsync(false);

            await _dimmer.InitializeAsync();
            _dimmer.SaveCurrentState(); // Save state: 60%, unlocked

            // Change brightness to 30% and lock the dimmer
            var brightnessArgs = new KnxGroupEventArgs(_dimmer.Addresses.BrightnessFeedback, new KnxValue(30));
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, brightnessArgs);
            
            var lockArgs = new KnxGroupEventArgs(_dimmer.Addresses.LockFeedback, new KnxValue(true)); // locked
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockArgs);

            // Verify current state is locked at 30%
            _dimmer.CurrentState.Brightness.Should().Be(30);
            _dimmer.CurrentState.Lock.Should().Be(Lock.On);

            // Setup mocks for restoration process
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.LockControl, false)) // unlock
                .Callback<string, bool>((addr, value) =>
                {
                    var unlockFeedback = new KnxGroupEventArgs(_dimmer.Addresses.LockFeedback, new KnxValue(false));
                    _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, unlockFeedback);
                });

            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.BrightnessControl, 60f)) // restore brightness
                .Callback<string, float>((addr, value) =>
                {
                    var brightnessFeedback = new KnxGroupEventArgs(_dimmer.Addresses.BrightnessFeedback, new KnxValue(value));
                    _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, brightnessFeedback);
                });

            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.LockControl, false)) // restore lock (Off)
                .Callback<string, bool>((addr, value) =>
                {
                    var lockFeedback = new KnxGroupEventArgs(_dimmer.Addresses.LockFeedback, new KnxValue(value));
                    _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockFeedback);
                });

            // Act
            await _dimmer.RestoreSavedStateAsync();

            // Assert
            _dimmer.CurrentState.Brightness.Should().Be(60); // brightness restored
            _dimmer.CurrentState.Lock.Should().Be(Lock.Off); // lock restored

            // Verify that unlock was called before brightness restoration
            _mockKnxService.Verify(s => s.WriteGroupValue(_dimmer.Addresses.LockControl, false), Times.AtLeastOnce());
            _mockKnxService.Verify(s => s.WriteGroupValue(_dimmer.Addresses.BrightnessControl, 60f), Times.Once());
        }

        [Fact]
        public async Task RestoreSavedStateAsync_ShouldRestoreLockState()
        {
            // Arrange - set up initial state: dimmer at 50%, locked
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_dimmer.Addresses.SwitchFeedback)).ReturnsAsync(true);
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_dimmer.Addresses.BrightnessFeedback)).ReturnsAsync(50f);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_dimmer.Addresses.LockFeedback)).ReturnsAsync(true); // locked

            await _dimmer.InitializeAsync();
            _dimmer.SaveCurrentState(); // Save state: 50%, locked

            // Change to unlocked state
            var lockArgs = new KnxGroupEventArgs(_dimmer.Addresses.LockFeedback, new KnxValue(false)); // unlocked
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockArgs);

            _dimmer.CurrentState.Lock.Should().Be(Lock.Off); // now unlocked

            // Setup mock for lock restoration
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.LockControl, true)) // restore lock (On)
                .Callback<string, bool>((addr, value) =>
                {
                    var lockFeedback = new KnxGroupEventArgs(_dimmer.Addresses.LockFeedback, new KnxValue(value));
                    _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockFeedback);
                });

            // Act
            await _dimmer.RestoreSavedStateAsync();

            // Assert
            _dimmer.CurrentState.Lock.Should().Be(Lock.On); // lock restored
            _mockKnxService.Verify(s => s.WriteGroupValue(_dimmer.Addresses.LockControl, true), Times.Once());
        }

        #endregion

        #region Wait Methods Tests

        [Fact]
        public async Task WaitForBrightnessAsync_WhenBrightnessMatches_ShouldReturnTrue()
        {
            // Arrange - set current brightness to 50%
            var feedbackArgs = new KnxGroupEventArgs(_dimmer.Addresses.BrightnessFeedback, new KnxValue(50));
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);

            // Act
            var result = await _dimmer.WaitForBrightnessAsync(50, TimeSpan.FromMilliseconds(100));

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region Additional Toggle Tests

        [Fact]
        public async Task ToggleAsync_WhenOn_ShouldTurnOff()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_dimmer.Addresses.SwitchControl, It.IsAny<bool>()))
                .Callback<string, bool>((address, value) =>
                {
                    // Simulate feedback response for switch state
                    var feedbackArgs = new KnxGroupEventArgs(_dimmer.Addresses.SwitchFeedback, new KnxValue(value));
                    _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                });

            // Act - first turn ON, then toggle to OFF
            await _dimmer.TurnOnAsync();
            _dimmer.CurrentState.Switch.Should().Be(Switch.On);

            await _dimmer.ToggleAsync();

            // Assert
            _dimmer.CurrentState.Switch.Should().Be(Switch.Off);
        }

        #endregion

        #region Initialization Tests

        [Fact]
        public async Task InitializeAsync_ShouldReadCurrentStateFromKnx()
        {
            // Arrange
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_dimmer.Addresses.SwitchFeedback))
                          .ReturnsAsync(true);
            _mockKnxService.Setup(x => x.RequestGroupValue<float>(_dimmer.Addresses.BrightnessFeedback))
                          .ReturnsAsync(75);
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_dimmer.Addresses.LockFeedback))
                          .ReturnsAsync(false);

            // Act
            await _dimmer.InitializeAsync();

            // Assert
            _dimmer.CurrentState.Switch.Should().Be(Switch.On);
            _dimmer.CurrentState.Brightness.Should().Be(75);
            _dimmer.CurrentState.Lock.Should().Be(Lock.Off);
        }

        [Fact]
        public async Task RefreshStateAsync_ShouldUpdateCurrentState()
        {
            // Arrange
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_dimmer.Addresses.SwitchFeedback))
                          .ReturnsAsync(false);
            _mockKnxService.Setup(x => x.RequestGroupValue<float>(_dimmer.Addresses.BrightnessFeedback))
                          .ReturnsAsync(25);
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_dimmer.Addresses.LockFeedback))
                          .ReturnsAsync(true);

            // Act
            await _dimmer.RefreshStateAsync();

            // Assert
            _dimmer.CurrentState.Switch.Should().Be(Switch.Off);
            _dimmer.CurrentState.Brightness.Should().Be(25);
            _dimmer.CurrentState.Lock.Should().Be(Lock.On);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ShouldReturnCorrectFormat()
        {
            // Act
            var result = _dimmer.ToString();

            // Assert
            result.Should().Be("Dimmer DIM1 (Test Dimmer) - State: Unknown, Brightness: 0%, Lock: Unknown");
        }

        #endregion
    }
}
