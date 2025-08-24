using FluentAssertions;
using KnxModel;
using Moq;

namespace KnxTest.Unit.Helpers
{
    public class SwitchableDeviceTestHelper<TDevice, TAddresses>
        where TDevice : ISwitchable, IKnxDeviceBase
        where TAddresses : ISwitchableAddress

    {
        private readonly TDevice _device;
        private readonly TAddresses _addresses;
        private readonly Mock<IKnxService> _mockKnxService;

        // This class would contain methods to help with percentage control for dimmers
        // It would handle sending and receiving percentage-related messages
        public SwitchableDeviceTestHelper(TDevice device, TAddresses addresses, Mock<IKnxService> mockKnxService)
        {
            _device = device;
            _addresses = addresses;
            _mockKnxService = mockKnxService;
        }

        internal void Device_ImplementsAllRequiredInterfaces()
        {
            _device.Should().BeAssignableTo<IKnxDeviceBase>();
            _device.Should().BeAssignableTo<ISwitchable>();
        }

        internal async Task InitializeAsync_UpdatesLastUpdatedAndStates(Switch switchState)
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_addresses.Feedback))
                          .ReturnsAsync(switchState == Switch.On)
                          .Verifiable();
            // Act
            await _device.InitializeAsync();

            // Assert
            _device.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            _device.CurrentSwitchState.Should().Be(switchState);
        }

        internal void OnAnyFeedbackToUnknownAddress_ShouldProcessCorrectlyAndDoesNotChangeState(Switch currentSwitchState)
        {
            // Arrange
            _device.SetSwitchForTest(currentSwitchState);
            var currentDate = _device.LastUpdated;
            var unknownAddress = "9/9/9";
            var feedbackArgsTrue = new KnxGroupEventArgs(unknownAddress, new KnxValue(true));
            var feedbackArgsFalse = new KnxGroupEventArgs(unknownAddress, new KnxValue(false));
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgsTrue);
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgsFalse);
            // Assert
            // Ensure no state change for unknown address
            _device.CurrentSwitchState.Should().Be(currentSwitchState);
            _device.LastUpdated.Should().Be(currentDate, "LastUpdated should not change on unknown feedback");

        }

        internal void OnSwitchFeedback_ShouldUpdateState(Switch expectedSwitchState, bool feedback)
        {
            var feedbackAddress = _addresses.Feedback;
            var feedbackArgs = new KnxGroupEventArgs(feedbackAddress, new KnxValue(feedback));
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);

            // Assert
            _device.CurrentSwitchState.Should().Be(expectedSwitchState);
        }

        internal async Task ReadSwitchStateAsync_ShouldRequestCorrectAddress()
        {
            var address = _addresses.Feedback;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(address))
                          .ReturnsAsync(true); // Simulate switch on feedback
            var result = await _device.ReadSwitchStateAsync();
            result.Should().Be(Switch.On, "ReadSwitchStateAsync should return Switch.On for true feedback");

        }

        internal async Task ReadSwitchStateAsync_ShouldReturnCorrectValue(bool value, Switch switchState)
        {
            var address = _addresses.Feedback;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(address))
                          .ReturnsAsync(value)
                          .Verifiable(); // Simulate switch feedback
            var result = await _device.ReadSwitchStateAsync();
            result.Should().Be(switchState, $"ReadSwitchStateAsync should return {switchState} for {value} feedback");

        }

        internal async Task RestoreSavedStateAsync_ShouldSendCorrectTelegrams(Switch initialSwitchState, Switch switchState)
        {
            _device.SetSavedSwitchForTest(initialSwitchState);
            _device.SetSwitchForTest(switchState);

            if (initialSwitchState != switchState && initialSwitchState != Switch.Unknown)
            {
                _mockKnxService.Setup(s => s.WriteGroupValueAsync(_addresses.Control, initialSwitchState == Switch.On)).Returns(Task.CompletedTask).Verifiable();
            }

            // Act
            await _device.RestoreSavedStateAsync(TimeSpan.Zero);


        }

        internal void SaveCurrentState_ShouldStoreCurrentValues(Switch switchState)
        {
            // Arrange
            _device.SetSwitchForTest(switchState);

            // Act
            _device.SaveCurrentState();

            // Assert
            _device.SavedSwitchState.Should().Be(switchState, "Saved switch state should match current state");
            _device.CurrentSwitchState.Should().Be(switchState, "Current switch state should remain unchanged");

        }

        internal async Task ToggleAsync_ShouldSendCorrectTelegram(Switch initialState, bool expectedValue)
        {

            // Arrange
            var address = _addresses.Control;
            ((ISwitchable) _device).SetSwitchForTest(initialState);
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, expectedValue))
                          .Returns(Task.CompletedTask)
                          .Verifiable();

            // Act
            await _device.ToggleAsync(TimeSpan.Zero);

        }

        internal async Task TurnOffAsync_ShouldSendCorrectTelegram()
        {
            // Arrange
            var address = _addresses.Control;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, false))
                          .Returns(Task.CompletedTask)
                          .Verifiable();

            // Act
            await _device.TurnOffAsync(TimeSpan.Zero);

        }

        internal async Task TurnOnAsync_ShouldSendCorrectTelegram()
        {
            // Arrange
            var address = _addresses.Control;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, true))
                          .Returns(Task.CompletedTask)
                          .Verifiable();

            // Act
            await _device.TurnOnAsync(TimeSpan.Zero);
        }

        internal async Task WaitForSwitchStateAsync_ImmediateReturnTrueWhenAlreadyInState(Switch switchState, int waitingTime, int executionTimeMin, int executionTimeMax)
        {
            ((ISwitchable)_device).SetSwitchForTest(switchState);

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

        internal async Task WaitForSwitchStateAsync_ShouldReturnCorrectly(Switch initialState, int delayInMs, Switch switchState, int waitingTime, Switch expectedState, bool expectedResult, int executionTimeMin, int executionTimeMax)
        {
            ((ISwitchable)_device).SetSwitchForTest(initialState);

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            // Simulate delay before setting expected state
            _ = Task.Delay(delayInMs)
                    .ContinueWith(_ =>
                    {
                        _mockKnxService.Raise(
                            s => s.GroupMessageReceived += null,
                            _mockKnxService.Object,
                            new KnxGroupEventArgs(_addresses.Feedback, new KnxValue(switchState == Switch.On)));
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

        internal async Task WaitForSwitchStateAsync_WhenFeedbackReceived_ShouldReturnTrue(Switch initialState, int delayInMs, Switch switchState, int waitingTime, Switch expectedState, int executionTimeMin, int executionTimeMax)
        {
            ((ISwitchable)_device).SetSwitchForTest(initialState);

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            // Simulate delay before setting expected state
            _ = Task.Delay(delayInMs)
                    .ContinueWith(_ =>
                    {
                        _mockKnxService.Raise(
                            s => s.GroupMessageReceived += null,
                            _mockKnxService.Object,
                            new KnxGroupEventArgs(_addresses.Feedback, new KnxValue(switchState == Switch.On)));
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
    }
}
