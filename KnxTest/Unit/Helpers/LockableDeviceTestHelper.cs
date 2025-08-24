using FluentAssertions;
using KnxModel;
using Moq;

namespace KnxTest.Unit.Helpers
{
    public class LockableDeviceTestHelper<TDevice, TAddresses>
        where TDevice : ILockableDevice, IKnxDeviceBase
        where TAddresses : ILockableAddress

    {
        private readonly TDevice _device;
        private readonly TAddresses _addresses;
        private readonly Mock<IKnxService> _mockKnxService;

        // This class would contain methods to help with percentage control for dimmers
        // It would handle sending and receiving percentage-related messages
        public LockableDeviceTestHelper(TDevice device, TAddresses addresses, Mock<IKnxService> mockKnxService)
        {
            _device = device;
            _addresses = addresses;
            _mockKnxService = mockKnxService;
        }

        internal void Device_ImplementsAllRequiredInterfaces()
        {
            _device.Should().BeAssignableTo<IKnxDeviceBase>();
            _device.Should().BeAssignableTo<ILockableDevice>();
        }

        internal async Task InitializeAsync_UpdatesLastUpdatedAndStates(Lock lockState)
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_addresses.LockFeedback))
                          .ReturnsAsync(lockState == Lock.On)
                          .Verifiable();

            // Act
            await _device.InitializeAsync();

            // Assert
            _device.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            _device.CurrentLockState.Should().Be(lockState);

        }

        internal async Task LockAsync_ShouldSendCorrectTelegram()
        {
            var address = _addresses.LockControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, true))
                          .Returns(Task.CompletedTask)
                          .Verifiable();
            await _device.LockAsync(TimeSpan.Zero);

        }

        internal void OnAnyFeedbackToUnknownAddress_ShouldProcessCorrectlyAndDoesNotChangeState(Lock lockState)
        {
            // Arrange
            _device.SetLockForTest(lockState);
            var currentDate = _device.LastUpdated;
            var unknownAddress = "9/9/9";
            var feedbackArgsTrue = new KnxGroupEventArgs(unknownAddress, new KnxValue(true));
            var feedbackArgsFalse = new KnxGroupEventArgs(unknownAddress, new KnxValue(false));
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgsTrue);
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgsFalse);
            // Assert
            // Ensure no state change for unknown address
            _device.CurrentLockState.Should().Be(lockState);
            _device.LastUpdated.Should().Be(currentDate, "LastUpdated should not change on unknown feedback");

        }

        internal void OnLockFeedback_ShouldUpdateState(Lock expectedLock, bool feedback)
        {
            var feedbackAddress = _addresses.LockFeedback;
            var feedbackArgs = new KnxGroupEventArgs(feedbackAddress, new KnxValue(feedback));
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
            // Assert
            _device.CurrentLockState.Should().Be(expectedLock);
        }

        internal async Task ReadLockStateAsync_ShouldRequestCorrectAddress()
        {
            var address = _addresses.LockFeedback;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(address))
                          .ReturnsAsync(true)
                          .Verifiable(); // Simulate lock on feedback
            var result = await _device.ReadLockStateAsync();
            result.Should().Be(Lock.On, "ReadLockStateAsync should return Lock.On for true feedback");

        }

        internal async Task ReadLockStateAsync_ShouldReturnCorrectValue(bool value, Lock lockState)
        {
            // Test ReadLockStateAsync returns correct enum: true->Lock.On, false->Lock.Off
            var address = _addresses.LockFeedback;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(address))
                          .ReturnsAsync(value)
                          .Verifiable(); // Simulate lock feedback
            var result = await _device.ReadLockStateAsync();
            result.Should().Be(lockState, $"ReadLockStateAsync should return {lockState} for {value} feedback");
        }

        internal async Task RestoreSavedStateAsync_ShouldSendCorrectTelegrams(Lock initialLockState, Lock lockState)
        {
            _device.SetSavedLockForTest(initialLockState);
            _device.SetLockForTest(lockState);
            
            if (initialLockState != lockState && initialLockState != Lock.Unknown)
            {
                _mockKnxService.Setup(s => s.WriteGroupValueAsync(_addresses.LockControl, initialLockState == Lock.On)).Returns(Task.CompletedTask).Verifiable();
            }

            // Act
            await _device.RestoreSavedStateAsync(TimeSpan.Zero);

        }

        internal void SaveCurrentState_ShouldStoreCurrentValues(Lock lockState)
        {
            // Arrange
            _device.SetLockForTest(lockState);

            // Act
            _device.SaveCurrentState();

            // Assert
            _device.SavedLockState.Should().Be(lockState, "Saved lock state should match current state");
            _device.CurrentLockState.Should().Be(lockState, "Current lock state should remain unchanged");


        }

        internal async Task SetLockAsync_ShouldSendCorrectTelegram(Lock lockState, bool expectedValue)
        {
            var address = _addresses.LockControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, expectedValue))
                          .Returns(Task.CompletedTask)
                          .Verifiable();
            await _device.SetLockAsync(lockState, TimeSpan.Zero);
        }

        internal async Task UnlockAsync_ShouldSendCorrectTelegram()
        {
            var address = _addresses.LockControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, false))
                          .Returns(Task.CompletedTask)
                          .Verifiable();
            await _device.UnlockAsync(TimeSpan.Zero);
        }

        internal async Task WaitForLockAsync_ImmediateReturnTrueWhenAlreadyInState(Lock lockState, int waitingTime, int executionTimeMin, int executionTimeMax)
        {
            // Test WaitForLockStateAsync: immediate return when already in state, timeout when wrong state
            ((ILockableDevice)_device).SetLockForTest(lockState);

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

        internal async Task WaitForLockStateAsync_ShouldReturnCorrectly(Lock initialState, int delayInMs, Lock lockState, int waitingTime, Lock expectedState, bool expectedResult, int executionTimeMin, int executionTimeMax)
        {
            // Test WaitForLockStateAsync: immediate return when already in state, timeout when wrong state
            _device.SetLockForTest(initialState);
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            // Simulate delay before setting expected state
            _ = Task.Delay(delayInMs)
                    .ContinueWith(_ =>
                    {
                        _mockKnxService.Raise(
                            s => s.GroupMessageReceived += null,
                            _mockKnxService.Object,
                            new KnxGroupEventArgs(_addresses.LockFeedback, new KnxValue(lockState == Lock.On)));
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

        internal async Task WaitForLockStateAsync_WhenFeedbackReceived_ShouldReturnTrue(Lock initialState, int delayInMs, Lock lockState, int waitingTime, Lock expectedState, int executionTimeMin, int executionTimeMax)
        {
            // Test that wait method returns true when feedback changes state to target
            _device.SetLockForTest(initialState);
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            // Simulate delay before setting expected state
            _ = Task.Delay(delayInMs)
                    .ContinueWith(_ =>
                    {
                        _mockKnxService.Raise(
                            s => s.GroupMessageReceived += null,
                            _mockKnxService.Object,
                            new KnxGroupEventArgs(_addresses.LockFeedback, new KnxValue(lockState == Lock.On)));
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
    }
}
