using FluentAssertions;
using KnxModel;
using KnxTest.Unit.Helpers;

namespace KnxTest.Unit.Base
{
    public abstract class DeviceLockableTests<TDevice, TAddresses> : BaseKnxDeviceUnitTests
        where TDevice : ILockableDevice, IKnxDeviceBase
        where TAddresses : ILockableAddress

    {
        protected abstract LockableDeviceTestHelper<TDevice, TAddresses> _lockableTestHelper { get; }

        public DeviceLockableTests() : base()
        {
        }


        #region Lock Command Sending Tests

        [Fact]
        public async Task LockAsync_ShouldSendCorrectTelegram()
        {
            await _lockableTestHelper.LockAsync_ShouldSendCorrectTelegram();
        }

        [Fact]
        public async Task UnlockAsync_ShouldSendCorrectTelegram()
        {
            await _lockableTestHelper.UnlockAsync_ShouldSendCorrectTelegram();
            
        }

        [Theory]
        [InlineData(Lock.On, true)]  // Lock.On -> true
        [InlineData(Lock.Off, false)] // Lock.Off -> false
        public async Task SetLockAsync_ShouldSendCorrectTelegram(Lock lockState, bool expectedValue)
        {
            await _lockableTestHelper.SetLockAsync_ShouldSendCorrectTelegram(lockState, expectedValue);
            
        }

        #endregion


        #region Lock Feedback Processing Tests

        [Theory]
        [InlineData(Lock.On, true)]  // Lock.On -> true
        [InlineData(Lock.Off, false)] // Lock.Off -> false
        public void OnLockFeedback_ShouldUpdateState(Lock expectedLock, bool feedback)
        {
            _lockableTestHelper.OnLockFeedback_ShouldUpdateState(expectedLock, feedback);
            
        }

        #endregion

        #region Lock State Reading Tests

        [Fact]
        public async Task ReadLockStateAsync_ShouldRequestCorrectAddress()
        {
            await _lockableTestHelper.ReadLockStateAsync_ShouldRequestCorrectAddress();
        }

        [Theory]
        [InlineData(true, Lock.On)]
        [InlineData(false, Lock.Off)]
        public async Task ReadLockStateAsync_ShouldReturnCorrectValue(bool value, Lock lockState)
        {
            await _lockableTestHelper.ReadLockStateAsync_ShouldReturnCorrectValue(value, lockState);
            
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
            await _lockableTestHelper.WaitForLockAsync_ImmediateReturnTrueWhenAlreadyInState(lockState, waitingTime, executionTimeMin, executionTimeMax);
            
        }

        [Theory]
        [InlineData(Lock.On, 200, Lock.Off, 50, Lock.On, false, 40, 100)] // Wait for Lock.Off with delay
        [InlineData(Lock.Off, 200, Lock.On, 50, Lock.Off, false, 40, 100)] // Wait for Lock.On with delay
        [InlineData(Lock.Unknown, 200, Lock.On, 50, Lock.Unknown, false, 40, 100)] // Wait for Lock.On from Unknown with delay
        [InlineData(Lock.Unknown, 200, Lock.Off, 50, Lock.Unknown, false, 40, 100)] // Wait for Lock.Off from Unknown with delay
        public async Task WaitForLockStateAsync_ShouldReturnCorrectly(Lock initialState, int delayInMs, Lock lockState, int waitingTime, Lock expectedState, bool expectedResult, int executionTimeMin, int executionTimeMax)
        {
            await _lockableTestHelper.WaitForLockStateAsync_ShouldReturnCorrectly(initialState, delayInMs, lockState, waitingTime, expectedState, expectedResult, executionTimeMin, executionTimeMax);
            
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
            await _lockableTestHelper.WaitForLockStateAsync_WhenFeedbackReceived_ShouldReturnTrue(initialState, delayInMs, lockState, waitingTime, expectedState, executionTimeMin, executionTimeMax);
            
        }


        #endregion

        [Theory]
        [InlineData(Lock.Off)]
        [InlineData(Lock.On)]
        [InlineData(Lock.Unknown)]
        public void OnAnyFeedbackToUnknownAddress_ShouldProcessCorrectlyAndDoesNotChangeState(Lock lockState)
        {
            _lockableTestHelper.OnAnyFeedbackToUnknownAddress_ShouldProcessCorrectlyAndDoesNotChangeState(lockState);
        }

        [Theory]
        [InlineData(Lock.Off)]
        [InlineData(Lock.On)]
        [InlineData(Lock.Unknown)]

        public void SaveCurrentState_ShouldStoreCurrentValues(Lock lockState)
        {
            _lockableTestHelper.SaveCurrentState_ShouldStoreCurrentValues(lockState);
        }

        [Fact]
        public void Device_ImplementsAllRequiredInterfaces()
        {
            _lockableTestHelper.Device_ImplementsAllRequiredInterfaces();
        }

        [Theory]
        [InlineData(Lock.On, Lock.Off)]
        [InlineData(Lock.Off, Lock.On)]
        [InlineData(Lock.On, Lock.On)]
        [InlineData(Lock.Off, Lock.Off)]
        [InlineData(Lock.Unknown, Lock.On)]
        [InlineData(Lock.Unknown, Lock.Off)]
        public async Task RestoreSavedStateAsync_ShouldSendCorrectTelegrams(Lock initialLockState, Lock lockState)
        {
            await _lockableTestHelper.RestoreSavedStateAsync_ShouldSendCorrectTelegrams(initialLockState, lockState);
        }


        [Theory]
        [InlineData(Lock.On)]
        [InlineData(Lock.Off)]
        public async Task InitializeAsync_UpdatesLastUpdatedAndStates(Lock lockState)
        {
            await _lockableTestHelper.InitializeAsync_UpdatesLastUpdatedAndStates(lockState);
        }

    }
}
