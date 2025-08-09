using KnxModel;
using KnxTest.Integration.Interfaces;

namespace KnxTest.Integration.Base
{



    /// <summary>
    /// Integration tests for Light devices using new architecture
    /// Inherits from DeviceTestBase and implements ILockableDeviceTests interface
    /// </summary>
    [Collection("KnxService collection")]
    public abstract class LightIntegrationTestsBase<TDevice> : LockableIntegrationTestBase<TDevice>, ISwitchableDeviceTests
        where TDevice : ILightDevice
    {
        internal readonly SwitchTestHelper _switchTestHelper;

        public LightIntegrationTestsBase(KnxServiceFixture fixture) : base(fixture)
        {
            
            _switchTestHelper = new SwitchTestHelper();
        }

        #region ISwitchableDeviceTests Tests

        public abstract Task CanTurnOnAndTurnOff(string deviceId);
        public abstract Task CanToggleSwitch(string deviceId);
        public abstract Task CanReadSwitchState(string deviceId);

        #endregion
    }
}
