using FluentAssertions;
using KnxModel;
using Moq;

namespace KnxTest.Unit.Models
{
    /// <summary>
    /// Unit tests for new LightDevice implementation
    /// Tests each interface functionality separately
    /// </summary>
    public abstract class LightDeviceTestsBase<T, TAddressess> : BaseKnxDeviceUnitTests
        where T : LightDeviceBase<TAddressess>, ISwitchable, ILockableDevice, IKnxDeviceBase
        where TAddressess : ISwitchableAddress, ILockableAddress

    {

        // Replace this line:
        // private abstract readonly LightDevice _lightDevice;

        // With this property:
        protected abstract T _device { get; }
        public LightDeviceTestsBase() : base()
        {
            
        }

        #region IKnxDeviceBase Tests

        

        [Theory]
        [InlineData(Switch.On, Lock.On)]
        [InlineData(Switch.Off, Lock.Off)]
        [InlineData(Switch.On, Lock.Off)]
        [InlineData(Switch.Off, Lock.On)]
        public async Task InitializeAsync_UpdatesLastUpdatedAndStates(Switch switchState, Lock lockState)
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_device.Addresses.Feedback)).ReturnsAsync(switchState == Switch.On);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_device.Addresses.LockFeedback)).ReturnsAsync(lockState == Lock.On);

            // Act
            await _device.InitializeAsync();

            // Assert
            _device.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            _device.CurrentSwitchState.Should().Be(switchState);
            _device.CurrentLockState.Should().Be(lockState);
        }

        #endregion

        #region Interface Composition Tests

        [Fact]
        public void LightDevice_ImplementsAllRequiredInterfaces()
        {
            // Assert
            _device.Should().BeAssignableTo<IKnxDeviceBase>();
            _device.Should().BeAssignableTo<ISwitchable>();
            _device.Should().BeAssignableTo<ILockableDevice>();
            _device.Should().BeAssignableTo<ILightDevice>();
        }

        #endregion

        #region Command Sending Tests

        [Fact]
        public async Task TurnOnAsync_ShouldSendCorrectTelegram()
        {
            // Arrange
            var address = _device.Addresses.Control;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, true))
                          .Returns(Task.CompletedTask);

            // Act
            await _device.TurnOnAsync(TimeSpan.Zero);
        }

        [Fact]
        public async Task TurnOffAsync_ShouldSendCorrectTelegram()
        {
            // Arrange
            var address = _device.Addresses.Control;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, false))
                          .Returns(Task.CompletedTask);

            // Act
            await _device.TurnOffAsync(TimeSpan.Zero);
        }

        [Theory]
        [InlineData(Switch.Off, true)]  // Off -> On (should send true)
        [InlineData(Switch.On, false)]  // On -> Off (should send false)
        [InlineData(Switch.Unknown, true)] // Unknown -> On (default behavior)
        public async Task ToggleAsync_ShouldSendCorrectTelegram(Switch initialState, bool expectedValue)
        {
            // Arrange
            var address = _device.Addresses.Control;
            _device.SetSwitchStateForTest(initialState);
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, expectedValue))
                          .Returns(Task.CompletedTask);

            // Act
            await _device.ToggleAsync(TimeSpan.Zero);
        }

        [Fact]
        public async Task LockAsync_ShouldSendCorrectTelegram()
        {
            var address = _device.Addresses.LockControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, true))
                          .Returns(Task.CompletedTask);
            await _device.LockAsync(TimeSpan.Zero);
        }

        [Fact]
        public async Task UnlockAsync_ShouldSendCorrectTelegram()
        {
            var address = _device.Addresses.LockControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, false))
                          .Returns(Task.CompletedTask);
            await _device.UnlockAsync(TimeSpan.Zero);
        }

        [Theory]
        [InlineData(Lock.On, true)]  // Lock.On -> true
        [InlineData(Lock.Off, false)] // Lock.Off -> false
        public async Task SetLockAsync_ShouldSendCorrectTelegram(Lock lockState, bool expectedValue)
        {
            var address = _device.Addresses.LockControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, expectedValue))
                          .Returns(Task.CompletedTask);
            await _device.SetLockAsync(lockState, TimeSpan.Zero);
        }

        

        #endregion

        #region Feedback Processing Tests

        [Theory]
        [InlineData(Switch.On, true)]  // Switch.On -> true
        [InlineData(Switch.Off, false)] // Switch.Off -> false
        public void OnSwitchFeedback_ShouldUpdateState(Switch expectedSwitchState, bool feedback)
        {
            var feedbackAddress = _device.Addresses.Feedback;
            var feedbackArgs = new KnxGroupEventArgs(feedbackAddress, new KnxValue(feedback));
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);

            // Assert
            _device.CurrentSwitchState.Should().Be(expectedSwitchState);
        }

        [Theory]
        [InlineData(Lock.On, true)]  // Lock.On -> true
        [InlineData(Lock.Off, false)] // Lock.Off -> false
        public void OnLockFeedback_ShouldUpdateState(Lock expectedLock, bool feedback)
        {
            // TODO: Test lock feedback: true->Lock.On, false->Lock.Off
            var feedbackAddress = _device.Addresses.LockFeedback;
            var feedbackArgs = new KnxGroupEventArgs(feedbackAddress, new KnxValue(feedback));
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
            // Assert
            _device.CurrentLockState.Should().Be(expectedLock);
        }

        [Fact]
        public void OnSwitchFeedback_WhenLocked_ShouldStillUpdateState()
        {
            // TODO: Test that switch feedback updates state even when device is locked
            // This reflects real device state, integration tests will catch configuration issues
            _device.SetStateForTest(Switch.On, Lock.On);
            var feedbackAddress = _device.Addresses.Feedback;
            var feedbackArgs = new KnxGroupEventArgs(feedbackAddress, new KnxValue(false)); // Simulate switch off
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
            // Assert
            _device.CurrentSwitchState.Should().Be(Switch.Off, "Switch state should update even when locked");
        }

        [Fact]
        public void OnLockFeedback_WhenSwitchOn_ShouldUpdateLockOnly()
        {
            // TODO: Test that lock feedback only affects lock state, not switch state
            _device.SetStateForTest(Switch.On, Lock.Off);
            var feedbackAddress = _device.Addresses.LockFeedback;
            var feedbackArgs = new KnxGroupEventArgs(feedbackAddress, new KnxValue(true)); // Simulate lock on
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
            // Assert
            _device.CurrentLockState.Should().Be(Lock.On, "Lock state should update without affecting switch state");
            _device.CurrentSwitchState.Should().Be(Switch.On, "Switch state should remain unchanged when lock is updated");
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
            _device.SetStateForTest(currentSwitchState, currentLockState);
            var currentDate = _device.LastUpdated;
            var unknownAddress ="1/2/3";
            var feedbackArgsTrue = new KnxGroupEventArgs(unknownAddress, new KnxValue(true));
            var feedbackArgsFalse = new KnxGroupEventArgs(unknownAddress, new KnxValue(false));
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgsTrue);
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgsFalse);
            // Assert
            // Ensure no state change for unknown address
            _device.CurrentSwitchState.Should().Be(currentSwitchState);
            _device.CurrentLockState.Should().Be(currentLockState);
            _device.LastUpdated.Should().Be(currentDate, "LastUpdated should not change on unknown feedback");

        }

        #endregion

        #region State Reading Tests

        [Fact]
        public async Task ReadSwitchStateAsync_ShouldRequestCorrectAddress()
        {
            // TODO: Test that ReadSwitchStateAsync calls RequestGroupValue with switch feedback address
            var address = _device.Addresses.Feedback;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(address))
                          .ReturnsAsync(true); // Simulate switch on feedback
            var result = await _device.ReadSwitchStateAsync();
            result.Should().Be(Switch.On, "ReadSwitchStateAsync should return Switch.On for true feedback");
        }

        [Fact]
        public async Task ReadLockStateAsync_ShouldRequestCorrectAddress()
        {
            // TODO: Test that ReadLockStateAsync calls RequestGroupValue with lock feedback address
            var address = _device.Addresses.LockFeedback;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(address))
                          .ReturnsAsync(true); // Simulate lock on feedback
            var result = await _device.ReadLockStateAsync();
            result.Should().Be(Lock.On, "ReadLockStateAsync should return Lock.On for true feedback");
        }

        [Theory]
        [InlineData(true, Switch.On)]
        [InlineData(false, Switch.Off)]
        public async Task ReadSwitchStateAsync_ShouldReturnCorrectValue(bool value, Switch switchState)
        {
            // TODO: Test ReadSwitchStateAsync returns correct enum: true->Switch.On, false->Switch.Off
            var address = _device.Addresses.Feedback;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(address))
                          .ReturnsAsync(value); // Simulate switch feedback
            var result = await _device.ReadSwitchStateAsync();
            result.Should().Be(switchState, $"ReadSwitchStateAsync should return {switchState} for {value} feedback");
        }

        [Theory]
        [InlineData(true, Lock.On)]
        [InlineData(false, Lock.Off)]
        public async Task ReadLockStateAsync_ShouldReturnCorrectValue(bool value, Lock lockState)
        {
            // TODO: Test ReadLockStateAsync returns correct enum: true->Lock.On, false->Lock.Off
            var address = _device.Addresses.LockFeedback;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(address))
                          .ReturnsAsync(value); // Simulate lock feedback
            var result = await _device.ReadLockStateAsync();
            result.Should().Be(lockState, $"ReadLockStateAsync should return {lockState} for {value} feedback");
        }

        #endregion

        #region Wait Methods Tests

        [Theory]
        [InlineData(Switch.On, 0, 0, 50)] // Wait for Switch.On
        [InlineData(Switch.On, 200, 0, 50)] // Wait for Switch.On with timeout
        [InlineData(Switch.Off, 0, 0, 50)] // Wait for Switch.Off
        [InlineData(Switch.Off, 200, 0, 50)] // Wait for Switch.Off with timeout
        [InlineData(Switch.Unknown, 0, 0, 50)] // Wait for Switch.Unknown
        [InlineData(Switch.Unknown, 200, 0, 50)] // Wait for Switch.OfUnknownf with timeout
        public async Task WaitForSwitchStateAsync_ImmediateReturnTrueWhenAlreadyInState(Switch switchState, int waitingTime, int executionTimeMin, int executionTimeMax)
        {
            // TODO: Test WaitForSwitchStateAsync: immediate return when already in state, timeout when wrong state
            _device.SetSwitchStateForTest(switchState);

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            // Act
            var result = await _device.WaitForSwitchStateAsync(switchState, TimeSpan.FromMilliseconds(waitingTime));
            timer.Stop();

            // Assert
            result.Should().BeTrue($"WaitForSwitchStateAsync should return {true} when state matches expected");
            _device.CurrentSwitchState.Should().Be(switchState, "Current switch state should match expected after wait");
            timer.ElapsedMilliseconds.Should().BeInRange(executionTimeMin, executionTimeMax,
                $"Execution time should be between {executionTimeMin} and {executionTimeMax} ms");
        }

        [Theory]
        [InlineData(Switch.On, 200, Switch.Off, 50, Switch.On, false, 50, 100)] // Wait for Switch.Off with delay
        [InlineData(Switch.Off, 200, Switch.On, 50, Switch.Off, false, 50, 100)] // Wait for Switch.On with delay
        [InlineData(Switch.Unknown, 200, Switch.On, 50, Switch.Unknown,false, 50, 100)] // Wait for Switch.On from Unknown with delay
        [InlineData(Switch.Unknown, 200, Switch.Off, 50, Switch.Unknown,false, 50, 100)] // Wait for Switch.Off from Unknown with delay

        public async Task WaitForSwitchStateAsync_ShouldReturnCorrectly(Switch initialState, int delayInMs, Switch switchState, int waitingTime, Switch expectedState, bool expectedResult, int executionTimeMin, int executionTimeMax)
        {
            // TODO: Test WaitForSwitchStateAsync: immediate return when already in state, timeout when wrong state
            _device.SetSwitchStateForTest(initialState);

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            // Simulate delay before setting expected state
            _ = Task.Delay(delayInMs)
                    .ContinueWith(_ =>
                    {
                        _mockKnxService.Raise(
                            s => s.GroupMessageReceived += null,
                            _mockKnxService.Object,
                            new KnxGroupEventArgs(_device.Addresses.Feedback, new KnxValue(switchState == Switch.On)));
                    });

            // Act
            var result = await _device.WaitForSwitchStateAsync(switchState, TimeSpan.FromMilliseconds(waitingTime));
            timer.Stop();

            // Assert
            result.Should().Be(expectedResult, $"WaitForSwitchStateAsync should return {expectedResult} when state matches expected");
            _device.CurrentSwitchState.Should().Be(expectedState, "Current switch state should match expected after wait");
            timer.ElapsedMilliseconds.Should().BeInRange(executionTimeMin, executionTimeMax,
                $"Execution time should be between {executionTimeMin} and {executionTimeMax} ms");
        }

        [Theory]
        [InlineData(Lock.On, 0, 0, 50)] // Wait for Lock.On
        [InlineData(Lock.On, 200, 0, 50)] // Wait for Lock.On with timeout
        [InlineData(Lock.Off, 0, 0, 50)] // Wait for Lock.Off
        [InlineData(Lock.Off, 200, 0, 50)] // Wait for Lock.Off with timeout
        [InlineData(Lock.Unknown, 0, 0, 50)] // Wait for Lock.Unknown
        [InlineData(Lock.Unknown, 200, 0, 50)] // Wait for Lock.OfUnknownf with timeout
        public async Task WaitForLockAsync_ImmediateReturnTrueWhenAlreadyInState(Lock lockState, int waitingTime, int executionTimeMin, int executionTimeMax)
        {
            // TODO: Test WaitForLockStateAsync: immediate return when already in state, timeout when wrong state
            _device.SetLockStateForTest(lockState);

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            // Act
            var result = await _device.WaitForLockStateAsync(lockState, TimeSpan.FromMilliseconds(waitingTime));
            timer.Stop();

            // Assert
            result.Should().BeTrue($"WaitForLockStateAsync should return {true} when state matches expected");
            _device.CurrentLockState.Should().Be(lockState, "Current lock state should match expected after wait");
            timer.ElapsedMilliseconds.Should().BeInRange(executionTimeMin, executionTimeMax,
                $"Execution time should be between {executionTimeMin} and {executionTimeMax} ms");
        }


        [Theory]
        [InlineData(Lock.On, 200, Lock.Off, 50, Lock.On, false, 50, 100)] // Wait for Lock.Off with delay
        [InlineData(Lock.Off, 200, Lock.On, 50, Lock.Off, false, 50, 100)] // Wait for Lock.On with delay
        [InlineData(Lock.Unknown, 200, Lock.On, 50, Lock.Unknown, false, 50, 100)] // Wait for Lock.On from Unknown with delay
        [InlineData(Lock.Unknown, 200, Lock.Off, 50, Lock.Unknown, false, 50, 100)] // Wait for Lock.Off from Unknown with delay
        public async Task WaitForLockStateAsync_ShouldReturnCorrectly(Lock initialState, int delayInMs, Lock lockState, int waitingTime, Lock expectedState, bool expectedResult, int executionTimeMin, int executionTimeMax)
        {
            // TODO: Test WaitForLockStateAsync: immediate return when already in state, timeout when wrong state
            _device.SetLockStateForTest(initialState);
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            // Simulate delay before setting expected state
            _ = Task.Delay(delayInMs)
                    .ContinueWith(_ =>
                    {
                        _mockKnxService.Raise(
                            s => s.GroupMessageReceived += null,
                            _mockKnxService.Object,
                            new KnxGroupEventArgs(_device.Addresses.LockFeedback, new KnxValue(lockState == Lock.On)));
                    });

            // Act
            var result = await _device.WaitForLockStateAsync(lockState, TimeSpan.FromMilliseconds(waitingTime));
            timer.Stop();
            // Assert
            result.Should().Be(expectedResult, $"WaitForLockStateAsync should return {expectedResult} when state matches expected");
            _device.CurrentLockState.Should().Be(expectedState, "Current lock state should match expected after wait");
            timer.ElapsedMilliseconds.Should().BeInRange(executionTimeMin, executionTimeMax,
                $"Execution time should be between {executionTimeMin} and {executionTimeMax} ms");
        }

        [Theory]
        [InlineData(Switch.On, 50, Switch.Off, 200, Switch.Off,  50, 150)] // Wait for Switch.Off with delay
        [InlineData(Switch.Off, 50, Switch.On, 200, Switch.On, 50, 150)] // Wait for Switch.On with delay
        [InlineData(Switch.Unknown, 0, Switch.On, 200, Switch.On, 0, 100)] // Wait for Switch.On from Unknown
        [InlineData(Switch.Unknown, 50, Switch.On, 200, Switch.On, 50, 150)] // Wait for Switch.On from Unknown with delay
        [InlineData(Switch.Unknown, 0, Switch.Off, 200, Switch.Off, 0, 100)] // Wait for Switch.Off from Unknown
        [InlineData(Switch.Unknown, 50, Switch.Off, 200, Switch.Off, 50, 150)] // Wait for Switch.Off from Unknown with delay

        public async Task WaitForSwitchStateAsync_WhenFeedbackReceived_ShouldReturnTrue(Switch initialState, int delayInMs, Switch switchState, int waitingTime, Switch expectedState, int executionTimeMin, int executionTimeMax)
        {
            // TODO: Test that wait method returns true when feedback changes state to target
            _device.SetSwitchStateForTest(initialState);

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            // Simulate delay before setting expected state
            _ = Task.Delay(delayInMs)
                    .ContinueWith(_ =>
                    {
                        _mockKnxService.Raise(
                            s => s.GroupMessageReceived += null,
                            _mockKnxService.Object,
                            new KnxGroupEventArgs(_device.Addresses.Feedback, new KnxValue(switchState == Switch.On)));
                    });

            // Act
            var result = await _device.WaitForSwitchStateAsync(switchState, TimeSpan.FromMilliseconds(waitingTime));
            timer.Stop();

            // Assert
            result.Should().BeTrue($"WaitForSwitchStateAsync should return {true} when state matches expected");
            _device.CurrentSwitchState.Should().Be(expectedState, "Current switch state should match expected after wait");
            timer.ElapsedMilliseconds.Should().BeInRange(executionTimeMin, executionTimeMax,
                $"Execution time should be between {executionTimeMin} and {executionTimeMax} ms");
        }

        [Theory]
        [InlineData(Lock.On, 50, Lock.Off, 200, Lock.Off, 50, 150)] // Wait for Lock.Off with delay
        [InlineData(Lock.Off, 50, Lock.On, 200, Lock.On, 50, 150)] // Wait for Lock.On with delay
        [InlineData(Lock.Unknown, 0, Lock.On, 200, Lock.On, 0, 100)] // Wait for Lock.On from Unknown
        [InlineData(Lock.Unknown, 50, Lock.On, 200, Lock.On, 50, 150)] // Wait for Lock.On from Unknown with delay
        [InlineData(Lock.Unknown, 0, Lock.Off, 200, Lock.Off, 0, 100)] // Wait for Lock.Off from Unknown
        [InlineData(Lock.Unknown, 50, Lock.Off, 200, Lock.Off, 50, 150)] // Wait for Lock.Off from Unknown with delay

        public async Task WaitForLockStateAsync_WhenFeedbackReceived_ShouldReturnTrue(Lock initialState, int delayInMs, Lock lockState, int waitingTime, Lock expectedState, int executionTimeMin, int executionTimeMax)
        {
            // TODO: Test that wait method returns true when feedback changes state to target
            // TODO: Test WaitForLockStateAsync: immediate return when already in state, timeout when wrong state
            _device.SetLockStateForTest(initialState);
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            // Simulate delay before setting expected state
            _ = Task.Delay(delayInMs)
                    .ContinueWith(_ =>
                    {
                        _mockKnxService.Raise(
                            s => s.GroupMessageReceived += null,
                            _mockKnxService.Object,
                            new KnxGroupEventArgs(_device.Addresses.LockFeedback, new KnxValue(lockState == Lock.On)));
                    });

            // Act
            var result = await _device.WaitForLockStateAsync(lockState, TimeSpan.FromMilliseconds(waitingTime));
            timer.Stop();
            // Assert
            result.Should().BeTrue($"WaitForLockStateAsync should return {true} when state matches expected");
            _device.CurrentLockState.Should().Be(expectedState, "Current lock state should match expected after wait");
            timer.ElapsedMilliseconds.Should().BeInRange(executionTimeMin, executionTimeMax,
                $"Execution time should be between {executionTimeMin} and {executionTimeMax} ms");

        }

        #endregion

        #region State Management Tests

        [Theory]
        [InlineData(Switch.On, Lock.On)]
        [InlineData(Switch.On, Lock.Off)]
        [InlineData(Switch.On, Lock.Unknown)]
        [InlineData(Switch.Off, Lock.On)]
        [InlineData(Switch.Off, Lock.Off)]
        [InlineData(Switch.Off, Lock.Unknown)]
        [InlineData(Switch.Unknown, Lock.On)]
        [InlineData(Switch.Unknown, Lock.Off)]
        [InlineData(Switch.Unknown, Lock.Unknown)]

        public void SaveCurrentState_ShouldStoreCurrentValues(Switch switchState, Lock lockState)
        {
            // Arrange
            _device.SetStateForTest(switchState, lockState);

            // Act
            _device.SaveCurrentState();

            // Assert
            _device.SavedSwitchState.Should().Be(switchState, "Saved switch state should match current state");
            _device.SavedLockState.Should().Be(lockState, "Saved lock state should match current state");
        }

        [Theory]
        [InlineData(Switch.On, Lock.On, Switch.Off, Lock.Off)]
        [InlineData(Switch.Off, Lock.Off, Switch.On, Lock.On)]
        [InlineData(Switch.On, Lock.Off, Switch.Off, Lock.On)]
        [InlineData(Switch.Off, Lock.On, Switch.On, Lock.Off)]
        [InlineData(Switch.Unknown, Lock.Unknown, Switch.On, Lock.On)]
        [InlineData(Switch.Unknown, Lock.Unknown, Switch.Off, Lock.Off)]
        public async Task RestoreSavedStateAsync_ShouldSendCorrectTelegrams(Switch initialSwitchState, Lock initialLockState, Switch switchState, Lock lockState)
        {
            // TODO: Test that RestoreSavedStateAsync sends appropriate telegrams to restore state
            // Arrange
            _device.SetSavedStateForTest(initialSwitchState, initialLockState);
            _device.SetStateForTest(switchState, lockState);
            
            if (initialSwitchState != switchState && initialSwitchState != Switch.Unknown)
            {
                //unlock to allow switch change
                if (lockState == Lock.On)
                {
                    _mockKnxService.Setup(s => s.WriteGroupValueAsync(_device.Addresses.LockControl, false)).Returns(Task.CompletedTask);
                }
                _mockKnxService.Setup(s => s.WriteGroupValueAsync(_device.Addresses.Control, initialSwitchState == Switch.On)).Returns(Task.CompletedTask);
            }
            if (initialLockState != lockState && initialLockState != Lock.Unknown)
            {
                _mockKnxService.Setup(s => s.WriteGroupValueAsync(_device.Addresses.LockControl, initialLockState == Lock.On)).Returns(Task.CompletedTask);
            }
            
            // Act
            await _device.RestoreSavedStateAsync(TimeSpan.Zero);
        }

        [Theory]
        [InlineData(Switch.On, Lock.On, Switch.Off, Lock.Off, Switch.On, Lock.On)]
        [InlineData(Switch.Off, Lock.Off, Switch.On, Lock.On, Switch.Off, Lock.Off)]
        [InlineData(Switch.On, Lock.Off, Switch.Off, Lock.On, Switch.On, Lock.Off)]
        [InlineData(Switch.Off, Lock.On, Switch.On, Lock.Off, Switch.Off, Lock.On)]
        [InlineData(Switch.Unknown, Lock.Unknown, Switch.On, Lock.On,Switch.On, Lock.On)]
        [InlineData(Switch.Unknown, Lock.Unknown, Switch.Off, Lock.Off, Switch.Off, Lock.Off)]

        public async Task RestoreSavedStateAsync_ShouldRestoreCorrectState(Switch initialSwitchState, Lock initialLockState, Switch switchState, Lock lockState, Switch restoredSwitchState, Lock restoredLockState)
        {
            // TODO: Test that RestoreSavedStateAsync sends appropriate telegrams to restore state
            // Arrange
            _device.SetSavedStateForTest(initialSwitchState, initialLockState);
            _device.SetStateForTest(switchState, lockState);

            if (initialSwitchState != switchState && initialSwitchState != Switch.Unknown)
            {
                //unlock to allow switch change
                if (lockState == Lock.On)
                {
                    _mockKnxService.Setup(s => s.WriteGroupValueAsync(_device.Addresses.LockControl, false)).Returns(Task.CompletedTask).Callback(() =>
                    {
                        _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_device.Addresses.LockFeedback, new KnxValue(false)));
                    });
                }
                _mockKnxService.Setup(s => s.WriteGroupValueAsync(_device.Addresses.Control, initialSwitchState == Switch.On)).Returns(Task.CompletedTask).Callback(() =>
                {
                    _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_device.Addresses.Feedback, new KnxValue(initialSwitchState == Switch.On)));
                });
            }
            if (initialLockState != lockState && initialLockState != Lock.Unknown)
            {
                _mockKnxService.Setup(s => s.WriteGroupValueAsync(_device.Addresses.LockControl, initialLockState == Lock.On)).Returns(Task.CompletedTask).Callback(() =>
                {
                    _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_device.Addresses.LockFeedback, new KnxValue(initialLockState == Lock.On)));
                });
            }

            // Act
            await _device.RestoreSavedStateAsync(TimeSpan.Zero);
            // Assert
            _device.CurrentSwitchState.Should().Be(restoredSwitchState, "Current switch state should match restored state after restore");
            _device.CurrentLockState.Should().Be(restoredLockState, "Current lock state should match restored state after restore");
        }

        [Fact]
        public async Task RestoreSavedStateAsync_WhenNoSavedState_ShouldNotSendTelegrams()
        {
            // Arrange
            _device.SetStateForTest(Switch.On, Lock.On); // Set some initial state

            // Act
            await _device.RestoreSavedStateAsync(TimeSpan.Zero);
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public void MultipleCommandsInSequence_ShouldSendAllTelegrams()
        {
            // TODO: Test that multiple commands are all sent correctly
            // Arrange
            _device.SetStateForTest(Switch.Off, Lock.Off);
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(_device.Addresses.Control, true))
                          .Returns(Task.CompletedTask);
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(_device.Addresses.LockControl, true))
                            .Returns(Task.CompletedTask);
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(_device.Addresses.Control, false))
                            .Returns(Task.CompletedTask);
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(_device.Addresses.LockControl, false))
                            .Returns(Task.CompletedTask);
            // Act
            _ = _device.TurnOnAsync(TimeSpan.Zero);
            _ = _device.LockAsync(TimeSpan.Zero);
            _ = _device.TurnOffAsync(TimeSpan.Zero);
            _ = _device.UnlockAsync(TimeSpan.Zero);
        }

        [Theory]
        [InlineData(Switch.Off, Lock.Off)]
        [InlineData(Switch.On, Lock.On)]
        [InlineData(Switch.On, Lock.Off)]
        [InlineData(Switch.Off, Lock.On)]
        public void SimultaneousFeedbacks_ShouldProcessAllCorrectly(Switch switchState, Lock lockState)
        {
            // TODO: Test processing multiple feedbacks in quick succession
            // Arrange
            var switchFeedbackArgs = new KnxGroupEventArgs(_device.Addresses.Feedback, new KnxValue(switchState == Switch.On)); // Simulate switch
            var lockFeedbackArgs = new KnxGroupEventArgs(_device.Addresses.LockFeedback, new KnxValue(lockState == Lock.On)); // Simulate lock
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, switchFeedbackArgs);
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockFeedbackArgs);
            // Assert
            _device.CurrentSwitchState.Should().Be(switchState, "Switch state should match feedback");
            _device.CurrentLockState.Should().Be(lockState, "Lock state should match feedback");
        }

        [Fact]
        public void InvalidFeedbackAddress_ShouldBeIgnored()
        {
            // TODO: Test that feedback from unknown addresses is ignored
            // Arrange
            var invalidAddress = "invalid/address/format";
            var feedbackArgs = new KnxGroupEventArgs(invalidAddress, new KnxValue(true)); // Simulate invalid feedback
            var currentState = _device.CurrentSwitchState;
            var currentLockState = _device.CurrentLockState;

            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);

            // Assert
            _device.CurrentSwitchState.Should().Be(currentState, "Current switch state should remain unchanged on invalid address");
            _device.CurrentLockState.Should().Be(currentLockState, "Current lock state should remain unchanged on invalid address");
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _device?.Dispose();
            }
            base.Dispose(disposing); // WAŻNE: Wywołaj base.Dispose(disposing) aby zweryfikować mocki
        }
    }
}
