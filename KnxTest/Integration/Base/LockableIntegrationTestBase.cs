using KnxModel;
using KnxTest.Integration.Helpers;

namespace KnxTest.Integration.Base
{
    [Collection("KnxService collection")]
    public abstract class LockableIntegrationTestBase<TDevice> : IntegrationTestBase<TDevice>
        where TDevice : ILockableDevice, IKnxDeviceBase
    {
        internal readonly LockTestHelper _lockTestHelper;
        
        protected LockableIntegrationTestBase(KnxServiceFixture fixture) :base(fixture)
        {
            _lockTestHelper = new LockTestHelper();
        }

        internal abstract Task InitializeDevice(string deviceId, bool saveCurrentState = true);

        /// <summary>
        /// Initialize device and ensure it's unlocked for testing
        /// </summary>
        protected async Task InitializeDeviceAndEnsureUnlocked(string deviceId, bool saveCurrentState = true)
        {
            await InitializeDevice(deviceId, saveCurrentState);
            await _lockTestHelper.EnsureDeviceIsUnlockedBeforeTest(Device!);
        }

        #region ILockableDeviceTests Implementation

        public abstract Task CanLockAndUnlock(string deviceId);
        public abstract Task LockPreventsStateChanges(string deviceId);
        public abstract Task CanReadLockState(string deviceId);
        public abstract Task SwitchableDeviceTurnOffWhenLocked(string deviceId);

        #endregion


    }
}
