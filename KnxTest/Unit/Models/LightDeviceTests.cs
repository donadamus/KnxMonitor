using System;
using System.Threading.Tasks;
using FluentAssertions;
using KnxModel;
using Moq;
using Xunit;

namespace KnxTest.Unit.Models
{
    /// <summary>
    /// Unit tests for new LightDevice implementation
    /// Tests each interface functionality separately
    /// </summary>
    public class LightDeviceTests : BaseKnxDeviceUnitTests
    {
        private readonly LightDevice _lightDevice;

        public LightDeviceTests() : base()
        {
            // Initialize LightDevice with mock KNX service
            _lightDevice = new LightDevice("L_TEST", "Test Light", "1", _mockKnxService.Object);
        }

        #region IKnxDeviceBase Tests

        [Fact]
        public void Constructor_SetsBasicProperties()
        {
            // Assert
            _lightDevice.Id.Should().Be("L_TEST");
            _lightDevice.Name.Should().Be("Test Light");
            _lightDevice.SubGroup.Should().Be("1");
            _lightDevice.LastUpdated.Should().Be(DateTime.MinValue); // Not initialized yet
        }

        [Fact]
        public async Task InitializeAsync_UpdatesLastUpdated()
        {
            // Act
            await _lightDevice.InitializeAsync();

            // Assert
            _lightDevice.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task SaveAndRestoreState_WorksCorrectly()
        {
            // Arrange
            await _lightDevice.InitializeAsync();
            await _lightDevice.TurnOnAsync();
            await _lightDevice.LockAsync();

            // Act - Save state
            _lightDevice.SaveCurrentState();

            // Change state
            await _lightDevice.TurnOffAsync();
            await _lightDevice.UnlockAsync();

            // Restore state
            await _lightDevice.RestoreSavedStateAsync();

            // Assert
            _lightDevice.CurrentSwitchState.Should().Be(Switch.On);
            _lightDevice.CurrentLockState.Should().Be(Lock.On);
        }

        #endregion

        #region ISwitchable Tests

        [Fact]
        public async Task TurnOnAsync_UpdatesSwitchState()
        {
            // Act
            await _lightDevice.TurnOnAsync();

            // Assert
            _lightDevice.CurrentSwitchState.Should().Be(Switch.On);
            _lightDevice.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task TurnOffAsync_UpdatesSwitchState()
        {
            // Act
            await _lightDevice.TurnOffAsync();

            // Assert
            _lightDevice.CurrentSwitchState.Should().Be(Switch.Off);
            _lightDevice.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ToggleAsync_SwitchesFromOffToOn()
        {
            // Arrange
            await _lightDevice.TurnOffAsync();

            // Act
            await _lightDevice.ToggleAsync();

            // Assert
            _lightDevice.CurrentSwitchState.Should().Be(Switch.On);
        }

        [Fact]
        public async Task ToggleAsync_SwitchesFromOnToOff()
        {
            // Arrange
            await _lightDevice.TurnOnAsync();

            // Act
            await _lightDevice.ToggleAsync();

            // Assert
            _lightDevice.CurrentSwitchState.Should().Be(Switch.Off);
        }

        [Fact]
        public async Task WaitForSwitchStateAsync_ReturnsTrue_WhenStateMatches()
        {
            // Arrange
            await _lightDevice.TurnOnAsync();

            // Act
            var result = await _lightDevice.WaitForSwitchStateAsync(Switch.On, TimeSpan.FromSeconds(1));

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region ILockableDevice Tests

        [Fact]
        public async Task LockAsync_UpdatesLockState()
        {
            // Act
            await _lightDevice.LockAsync();

            // Assert
            _lightDevice.CurrentLockState.Should().Be(Lock.On);
            _lightDevice.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task UnlockAsync_UpdatesLockState()
        {
            // Act
            await _lightDevice.UnlockAsync();

            // Assert
            _lightDevice.CurrentLockState.Should().Be(Lock.Off);
            _lightDevice.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task WaitForLockStateAsync_ReturnsTrue_WhenStateMatches()
        {
            // Arrange
            await _lightDevice.LockAsync();

            // Act
            var result = await _lightDevice.WaitForLockStateAsync(Lock.On, TimeSpan.FromSeconds(1));

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region Interface Composition Tests

        [Fact]
        public void LightDevice_ImplementsAllRequiredInterfaces()
        {
            // Assert
            _lightDevice.Should().BeAssignableTo<IKnxDeviceBase>();
            _lightDevice.Should().BeAssignableTo<ISwitchable>();
            _lightDevice.Should().BeAssignableTo<ILockableDevice>();
            _lightDevice.Should().BeAssignableTo<ILightDevice>();
        }

        [Fact]
        public async Task CanUseAsISwitchable()
        {
            // Arrange
            ISwitchable switchable = _lightDevice;

            // Act
            await switchable.TurnOnAsync();

            // Assert
            switchable.CurrentSwitchState.Should().Be(Switch.On);
        }

        [Fact]
        public async Task CanUseAsILockableDevice()
        {
            // Arrange
            ILockableDevice lockable = _lightDevice;

            // Act
            await lockable.LockAsync();

            // Assert
            lockable.CurrentLockState.Should().Be(Lock.On);
        }

        #endregion

        #region Command Sending Tests

        [Fact]
        public async Task TurnOnAsync_ShouldSendCorrectTelegram()
        {
            // Arrange
            var address = _lightDevice.LightAddresses.Control;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, true))
                          .Returns(Task.CompletedTask);

            // Act
            await _lightDevice.TurnOnAsync(TimeSpan.Zero);
        }

        [Fact]
        public async Task TurnOffAsync_ShouldSendCorrectTelegramAsync()
        {
            // Arrange
            var address = _lightDevice.LightAddresses.Control;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, false))
                          .Returns(Task.CompletedTask);

            // Act
            await _lightDevice.TurnOffAsync(TimeSpan.Zero);
        }

        [Theory]
        [InlineData(Switch.Off, true)]  // Off -> On (should send true)
        [InlineData(Switch.On, false)]  // On -> Off (should send false)
        [InlineData(Switch.Unknown, true)] // Unknown -> On (default behavior)
        public async Task ToggleAsync_ShouldSendCorrectTelegram(Switch initialState, bool expectedValue)
        {
            // Arrange
            var address = _lightDevice.LightAddresses.Control;
            _lightDevice.SetSwitchStateForTest(initialState);
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, expectedValue))
                          .Returns(Task.CompletedTask);

            // Act
            await _lightDevice.ToggleAsync(TimeSpan.Zero);
        }

        [Fact]
        public async Task LockAsync_ShouldSendCorrectTelegram()
        {
            var address = _lightDevice.LightAddresses.LockControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, true))
                          .Returns(Task.CompletedTask);
            await _lightDevice.LockAsync(TimeSpan.Zero);
        }

        [Fact]
        public async Task UnlockAsync_ShouldSendCorrectTelegram()
        {
            var address = _lightDevice.LightAddresses.LockControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, false))
                          .Returns(Task.CompletedTask);
            await _lightDevice.UnlockAsync(TimeSpan.Zero);
        }

        [Theory]
        [InlineData(Lock.On, true)]  // Lock.On -> true
        [InlineData(Lock.Off, false)] // Lock.Off -> false
        public async Task SetLockAsync_ShouldSendCorrectTelegram(Lock lockState, bool expectedValue)
        {
            var address = _lightDevice.LightAddresses.LockControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, expectedValue))
                          .Returns(Task.CompletedTask);
            await _lightDevice.SetLockAsync(lockState, TimeSpan.Zero);
        }

        

        #endregion

        #region Feedback Processing Tests

        [Theory]
        [InlineData(Switch.On, true)]  // Switch.On -> true
        [InlineData(Switch.Off, false)] // Switch.Off -> false
        public void OnSwitchFeedback_ShouldUpdateState(Switch expectedSwitchState, bool feedback)
        {
            var feedbackAddress = _lightDevice.LightAddresses.Feedback;
            var feedbackArgs = new KnxGroupEventArgs(feedbackAddress, new KnxValue(feedback));
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);

            // Assert
            _lightDevice.CurrentSwitchState.Should().Be(expectedSwitchState);
        }

        [Theory]
        [InlineData(Lock.On, true)]  // Lock.On -> true
        [InlineData(Lock.Off, false)] // Lock.Off -> false
        public void OnLockFeedback_ShouldUpdateState(Lock expectedLock, bool feedback)
        {
            // TODO: Test lock feedback: true->Lock.On, false->Lock.Off
            var feedbackAddress = _lightDevice.LightAddresses.LockFeedback;
            var feedbackArgs = new KnxGroupEventArgs(feedbackAddress, new KnxValue(feedback));
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
            // Assert
            _lightDevice.CurrentLockState.Should().Be(expectedLock);
        }

        [Fact]
        public void OnSwitchFeedback_WhenLocked_ShouldStillUpdateState()
        {
            // TODO: Test that switch feedback updates state even when device is locked
            // This reflects real device state, integration tests will catch configuration issues
            _lightDevice.SetStateForTest(Switch.On, Lock.On);
            var feedbackAddress = _lightDevice.LightAddresses.Feedback;
            var feedbackArgs = new KnxGroupEventArgs(feedbackAddress, new KnxValue(false)); // Simulate switch off
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
            // Assert
            _lightDevice.CurrentSwitchState.Should().Be(Switch.Off, "Switch state should update even when locked");
        }

        [Fact]
        public void OnLockFeedback_WhenSwitchOn_ShouldUpdateLockOnly()
        {
            // TODO: Test that lock feedback only affects lock state, not switch state
            _lightDevice.SetStateForTest(Switch.On, Lock.Off);
            var feedbackAddress = _lightDevice.LightAddresses.LockFeedback;
            var feedbackArgs = new KnxGroupEventArgs(feedbackAddress, new KnxValue(true)); // Simulate lock on
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
            // Assert
            _lightDevice.CurrentLockState.Should().Be(Lock.On, "Lock state should update without affecting switch state");
            _lightDevice.CurrentSwitchState.Should().Be(Switch.On, "Switch state should remain unchanged when lock is updated");
        }

        [Theory]
        [InlineData(Switch.On, Lock.Off)]  // Switch.On, Lock.Off
        [InlineData(Switch.On, Lock.On)]  // Switch.On, Lock.On
        [InlineData(Switch.On, Lock.Unknown)] // Switch.On, Lock.Unknown
        [InlineData(Switch.Off, Lock.On)]  // Switch.Off, Lock.On
        [InlineData(Switch.Off, Lock.Off)] // Switch.Off, Lock.Off
        [InlineData(Switch.Off, Lock.Unknown)] // Switch.Off, Lock.Unknown
        [InlineData(Switch.Unknown, Lock.On)] // Unknown switch, known lock
        [InlineData(Switch.Unknown, Lock.Off)] // Unknown switch, known lock
        [InlineData(Switch.Unknown, Lock.Unknown)] // Unknown states
        public void OnAnyFeedback_ShouldProcessCorrectly(Switch currentSwitchState, Lock currentLockState)
        {
            // TODO: Test device processes only relevant feedback addresses (switch, lock, ignore unknown)
            // Arrange
            _lightDevice.SetStateForTest(currentSwitchState, currentLockState);
            var currentDate = _lightDevice.LastUpdated;
            var unknownAddress ="1/2/3";
            var feedbackArgsTrue = new KnxGroupEventArgs(unknownAddress, new KnxValue(true));
            var feedbackArgsFalse = new KnxGroupEventArgs(unknownAddress, new KnxValue(false));
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgsTrue);
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgsFalse);
            // Assert
            // Ensure no state change for unknown address
            _lightDevice.CurrentSwitchState.Should().Be(currentSwitchState);
            _lightDevice.CurrentLockState.Should().Be(currentLockState);
            _lightDevice.LastUpdated.Should().Be(currentDate, "LastUpdated should not change on unknown feedback");



        }

        #endregion

        #region State Reading Tests

        [Fact]
        public async Task ReadSwitchStateAsync_ShouldRequestCorrectAddress()
        {
            // TODO: Test that ReadSwitchStateAsync calls RequestGroupValue with switch feedback address
            var address = _lightDevice.LightAddresses.Feedback;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(address))
                          .ReturnsAsync(true); // Simulate switch on feedback
            var result = await _lightDevice.ReadSwitchStateAsync();
            result.Should().Be(Switch.On, "ReadSwitchStateAsync should return Switch.On for true feedback");
        }

        [Fact]
        public async Task ReadLockStateAsync_ShouldRequestCorrectAddress()
        {
            // TODO: Test that ReadLockStateAsync calls RequestGroupValue with lock feedback address
            var address = _lightDevice.LightAddresses.LockFeedback;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(address))
                          .ReturnsAsync(true); // Simulate lock on feedback
            var result = await _lightDevice.ReadLockStateAsync();
            result.Should().Be(Lock.On, "ReadLockStateAsync should return Lock.On for true feedback");
        }

        [Theory]
        [InlineData(true, Switch.On)]
        [InlineData(false, Switch.Off)]
        public async Task ReadSwitchStateAsync_ShouldReturnCorrectValue(bool value, Switch switchState)
        {
            // TODO: Test ReadSwitchStateAsync returns correct enum: true->Switch.On, false->Switch.Off
            var address = _lightDevice.LightAddresses.Feedback;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(address))
                          .ReturnsAsync(value); // Simulate switch feedback
            var result = await _lightDevice.ReadSwitchStateAsync();
            result.Should().Be(switchState, $"ReadSwitchStateAsync should return {switchState} for {value} feedback");
        }

        [Theory]
        [InlineData(true, Lock.On)]
        [InlineData(false, Lock.Off)]
        public async Task ReadLockStateAsync_ShouldReturnCorrectValue(bool value, Lock lockState)
        {
            // TODO: Test ReadLockStateAsync returns correct enum: true->Lock.On, false->Lock.Off
            var address = _lightDevice.LightAddresses.LockFeedback;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(address))
                          .ReturnsAsync(value); // Simulate lock feedback
            var result = await _lightDevice.ReadLockStateAsync();
            result.Should().Be(lockState, $"ReadLockStateAsync should return {lockState} for {value} feedback");
        }

        #endregion

        #region Wait Methods Tests

        [Theory]

        [InlineData(Switch.On, 0, Switch.On, 100, Switch.On, true, 0, 10)] // Wait for Switch.On
        [InlineData(Switch.On, 50, Switch.Off,100, Switch.Off, true, 50, 60)] // Wait for Switch.Off with delay
        [InlineData(Switch.Off, 0, Switch.Off,100, Switch.Off, true, 0, 10)] // Wait for Switch.Off
        [InlineData(Switch.Off, 50, Switch.On, 100, Switch.On, true, 50, 60)] // Wait for Switch.On with delay
        [InlineData(Switch.Unknown, 0, Switch.On, 100, Switch.On, true, 0, 10)] // Wait for Switch.On from Unknown
        [InlineData(Switch.Unknown, 50, Switch.On, 100, Switch.On, true, 50, 60)] // Wait for Switch.On from Unknown with delay
        [InlineData(Switch.Unknown, 0, Switch.Off, 100, Switch.Off, true, 0, 10)] // Wait for Switch.Off from Unknown
        [InlineData(Switch.Unknown, 50, Switch.Off, 100, Switch.Off, true, 50, 60)] // Wait for Switch.Off from Unknown with delay
        [InlineData(Switch.On, 100, Switch.On, 0, Switch.On, true, 0, 10)] // Wait for Switch.On
        [InlineData(Switch.On, 100, Switch.Off, 50, Switch.On, false, 50, 60)] // Wait for Switch.Off with delay
        [InlineData(Switch.Off, 100, Switch.Off, 0, Switch.Off, true, 0, 10)] // Wait for Switch.Off
        [InlineData(Switch.Off, 100, Switch.On, 50, Switch.Off, false, 50, 60)] // Wait for Switch.On with delay
        [InlineData(Switch.Unknown, 100, Switch.On, 0, Switch.Unknown,false, 0, 10)] // Wait for Switch.On from Unknown
        [InlineData(Switch.Unknown, 100, Switch.On, 50, Switch.Unknown,false, 50, 60)] // Wait for Switch.On from Unknown with delay
        [InlineData(Switch.Unknown, 100, Switch.Off, 0, Switch.Unknown,false, 0, 10)] // Wait for Switch.Off from Unknown
        [InlineData(Switch.Unknown, 100, Switch.Off, 50, Switch.Unknown,false, 50, 60)] // Wait for Switch.Off from Unknown with delay

        public async Task WaitForSwitchStateAsync_ShouldReturnCorrectly(Switch initialState, int delayInMs, Switch switchState, int waitingTime, Switch expectedState, bool expectedResult, int executionTimeMin, int executionTimeMax)
        {
            // TODO: Test WaitForSwitchStateAsync: immediate return when already in state, timeout when wrong state
            _lightDevice.SetSwitchStateForTest(initialState);

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            // Simulate delay before setting expected state
            _ = Task.Delay(delayInMs)
                    .ContinueWith(_ =>
                    {
                        _lightDevice.SetSwitchStateForTest(switchState);
                    });

            // Act
            var result = await _lightDevice.WaitForSwitchStateAsync(switchState, TimeSpan.FromMilliseconds(waitingTime));
            timer.Stop();

            // Assert
            result.Should().Be(expectedResult, $"WaitForSwitchStateAsync should return {expectedResult} when state matches expected");
            _lightDevice.CurrentSwitchState.Should().Be(expectedState, "Current switch state should match expected after wait");
            timer.ElapsedMilliseconds.Should().BeInRange(executionTimeMin, executionTimeMax,
                $"Execution time should be between {executionTimeMin} and {executionTimeMax} ms");
        }

        [Fact]
        public void WaitForLockStateAsync_ShouldReturnCorrectly()
        {
            // TODO: Test WaitForLockStateAsync: immediate return when already in state, timeout when wrong state
        }

        [Fact]
        public void WaitForSwitchStateAsync_WhenFeedbackReceived_ShouldReturnTrue()
        {
            // TODO: Test that wait method returns true when feedback changes state to target
        }

        [Fact]
        public void WaitForLockStateAsync_WhenFeedbackReceived_ShouldReturnTrue()
        {
            // TODO: Test that wait method returns true when feedback changes state to target
        }

        #endregion

        #region State Management Tests

        [Fact]
        public void SaveCurrentState_ShouldStoreCurrentValues()
        {
            // TODO: Test that SaveCurrentState captures current switch and lock states
        }

        [Fact]
        public void RestoreSavedStateAsync_ShouldSendCorrectTelegrams()
        {
            // TODO: Test that RestoreSavedStateAsync sends appropriate telegrams to restore state
        }

        [Fact]
        public void RestoreSavedStateAsync_ShouldRestoreCorrectState()
        {
            // TODO: Test state restoration with different saved state combinations
        }

        [Fact]
        public void RestoreSavedStateAsync_WhenNoSavedState_ShouldNotSendTelegrams()
        {
            // TODO: Test that restoration without saved state doesn't send any telegrams
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public void MultipleCommandsInSequence_ShouldSendAllTelegrams()
        {
            // TODO: Test that multiple commands are all sent correctly
        }

        [Fact]
        public void SimultaneousFeedbacks_ShouldProcessAllCorrectly()
        {
            // TODO: Test processing multiple feedbacks in quick succession
        }

        [Fact]
        public void InvalidFeedbackAddress_ShouldBeIgnored()
        {
            // TODO: Test that feedback from unknown addresses is ignored
        }

        [Fact]
        public void InvalidAddressFormat_ShouldBeHandledGracefully()
        {
            // TODO: Test handling of invalid address formats in feedback (null, empty, invalid)
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _lightDevice?.Dispose();
            }
            base.Dispose(disposing); // WAŻNE: Wywołaj base.Dispose(disposing) aby zweryfikować mocki
        }
    }
}
