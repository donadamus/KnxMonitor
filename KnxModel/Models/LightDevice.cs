using System;
using System.Threading.Tasks;

namespace KnxModel
{
    public class LightDevice : LightDeviceBase<LightAddresses>
    {
        public LightDevice(string id, string name, string subGroup, IKnxService knxService)
            : base(id, name, subGroup, KnxAddressConfiguration.CreateLightAddresses(subGroup), knxService)
        {
        }

        public override async Task InitializeAsync()
        {
            // Read initial states from KNX bus
            _currentSwitchState = await ReadSwitchStateAsync();
            _currentLockState = await ReadLockStateAsync();
            _lastUpdated = DateTime.Now;

            Console.WriteLine($"LightDevice {Id} initialized - Switch: {_currentSwitchState}, Lock: {_currentLockState}");
        }

    }
}
