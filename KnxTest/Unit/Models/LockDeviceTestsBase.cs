using FluentAssertions;
using KnxModel;
using KnxTest.Unit.Base;
using Microsoft.Extensions.Logging;
using Moq;

namespace KnxTest.Unit.Models
{
    /// <summary>
    /// Base unit tests for devices that implement ILockableDevice
    /// Tests lock-specific functionality
    /// </summary>
    public abstract class LockDeviceTestsBase<TDevice, TAddressess> : BaseKnxDeviceUnitTests
        where TDevice : LockableDeviceBase<TDevice, TAddressess>, ILockableDevice, IKnxDeviceBase
        where TAddressess : ILockableAddress
    {
        protected abstract TDevice _device { get; }

        protected abstract ILogger<TDevice> _logger { get; }

        public LockDeviceTestsBase() : base()
        {
        }



        #region Lock Feedback Processing Tests

        [Theory]
        [InlineData(Lock.On, true)]  // Lock.On -> true
        [InlineData(Lock.Off, false)] // Lock.Off -> false
        public void OnLockFeedback_ShouldUpdateState(Lock expectedLock, bool feedback)
        {
            // Test lock feedback: true->Lock.On, false->Lock.Off
            var feedbackAddress = _device.Addresses.LockFeedback;
            var feedbackArgs = new KnxGroupEventArgs(feedbackAddress, new KnxValue(feedback));
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
            // Assert
            _device.CurrentLockState.Should().Be(expectedLock);
        }

        #endregion

        #region Lock State Reading Tests

        [Fact]
        public async Task ReadLockStateAsync_ShouldRequestCorrectAddress()
        {
            // Test that ReadLockStateAsync calls RequestGroupValue with lock feedback address
            var address = _device.Addresses.LockFeedback;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(address))
                          .ReturnsAsync(true)
                          .Verifiable(); // Simulate lock on feedback
            var result = await _device.ReadLockStateAsync();
            result.Should().Be(Lock.On, "ReadLockStateAsync should return Lock.On for true feedback");
        }

        [Theory]
        [InlineData(true, Lock.On)]
        [InlineData(false, Lock.Off)]
        public async Task ReadLockStateAsync_ShouldReturnCorrectValue(bool value, Lock lockState)
        {
            // Test ReadLockStateAsync returns correct enum: true->Lock.On, false->Lock.Off
            var address = _device.Addresses.LockFeedback;
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(address))
                          .ReturnsAsync(value)
                          .Verifiable(); // Simulate lock feedback
            var result = await _device.ReadLockStateAsync();
            result.Should().Be(lockState, $"ReadLockStateAsync should return {lockState} for {value} feedback");
        }

        #endregion

        #region Lock Wait Methods Tests

        [Theory]
        [InlineData(Lock.On, 0, 0, 50)] // Wait for Lock.On
        [InlineData(Lock.On, 200, 0, 50)] // Wait for Lock.On with timeout
        [InlineData(Lock.Off, 0, 0, 50)] // Wait for Lock.Off
        [InlineData(Lock.Off, 200, 0, 50)] // Wait for Lock.Off with timeout
        [InlineData(Lock.Unknown, 0, 0, 50)] // Wait for Lock.Unknown
        [InlineData(Lock.Unknown, 200, 0, 50)] // Wait for Lock.Unknown with timeout
        public async Task WaitForLockAsync_ImmediateReturnTrueWhenAlreadyInState(Lock lockState, int waitingTime, int executionTimeMin, int executionTimeMax)
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

        [Theory]
        [InlineData(Lock.On, 200, Lock.Off, 50, Lock.On, false, 40, 100)] // Wait for Lock.Off with delay
        [InlineData(Lock.Off, 200, Lock.On, 50, Lock.Off, false, 40, 100)] // Wait for Lock.On with delay
        [InlineData(Lock.Unknown, 200, Lock.On, 50, Lock.Unknown, false, 40, 100)] // Wait for Lock.On from Unknown with delay
        [InlineData(Lock.Unknown, 200, Lock.Off, 50, Lock.Unknown, false, 40, 100)] // Wait for Lock.Off from Unknown with delay
        public async Task WaitForLockStateAsync_ShouldReturnCorrectly(Lock initialState, int delayInMs, Lock lockState, int waitingTime, Lock expectedState, bool expectedResult, int executionTimeMin, int executionTimeMax)
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
        [InlineData(Lock.On, 50, Lock.Off, 200, Lock.Off, 50, 150)] // Wait for Lock.Off with delay
        [InlineData(Lock.Off, 50, Lock.On, 200, Lock.On, 50, 150)] // Wait for Lock.On with delay
        [InlineData(Lock.Unknown, 0, Lock.On, 200, Lock.On, 0, 100)] // Wait for Lock.On from Unknown
        [InlineData(Lock.Unknown, 50, Lock.On, 200, Lock.On, 50, 150)] // Wait for Lock.On from Unknown with delay
        [InlineData(Lock.Unknown, 0, Lock.Off, 200, Lock.Off, 0, 100)] // Wait for Lock.Off from Unknown
        [InlineData(Lock.Unknown, 50, Lock.Off, 200, Lock.Off, 50, 150)] // Wait for Lock.Off from Unknown with delay
        public async Task WaitForLockStateAsync_WhenFeedbackReceived_ShouldReturnTrue(Lock initialState, int delayInMs, Lock lockState, int waitingTime, Lock expectedState, int executionTimeMin, int executionTimeMax)
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
