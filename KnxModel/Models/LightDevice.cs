using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KnxModel
{
    public class LightDevice : LightDeviceBase<LightDevice, LightAddresses>
    {
        private readonly ILogger<LightDevice> _logger;

        public LightDevice(string id, string name, string subGroup, IKnxService knxService, ILogger<LightDevice> logger, TimeSpan defaultTimeout)
            : base(id, name, subGroup, KnxAddressConfiguration.CreateLightAddresses(subGroup), knxService, logger, defaultTimeout)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Initialize(this);
        }

        public override async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing LightDevice {DeviceId} ({DeviceName})", Id, Name);
            
            // Read initial states from KNX bus
            _currentSwitchState = await ReadSwitchStateAsync();
            _currentLockState = await ReadLockStateAsync();
            _lastUpdated = DateTime.Now;

            _logger.LogInformation("LightDevice {DeviceId} initialized - Switch: {SwitchState}, Lock: {LockState}", 
                Id, _currentSwitchState, _currentLockState);
            
            Console.WriteLine($"LightDevice {Id} initialized - Switch: {_currentSwitchState}, Lock: {_currentLockState}");
        }

    }
}
