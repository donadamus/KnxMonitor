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
    }
}
