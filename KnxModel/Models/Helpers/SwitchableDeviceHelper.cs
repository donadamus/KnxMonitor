using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace KnxModel.Models.Helpers
{
    /// <summary>
    /// Helper class for implementing switchable device functionality
    /// Handles switch state management and KNX communication for ISwitchable implementations
    /// </summary>
    public class SwitchableDeviceHelper<TDevice, TAddress> : DeviceHelperBase<TDevice, TAddress>
        where TDevice : IKnxDeviceBase, ILightDevice
        where TAddress : ISwitchableAddress
    {
        public SwitchableDeviceHelper(TDevice owner,
            TAddress address,
            IKnxService knxService,
            ILogger<TDevice> logger, 
            TimeSpan defaultTimeout) : base(owner, address, knxService, owner.Id, "SwitchableDevice", logger, defaultTimeout)
        {
        } 

        /// <summary>
        /// Processes KNX switch feedback messages
        /// </summary>
        public void ProcessSwitchMessage(KnxGroupEventArgs e)
        {
            if (e.Destination == addresses.Feedback)
            {
                var isOn = e.Value.AsBoolean();
                var switchState = isOn ? Switch.On : Switch.Off;
                
                // Update state through dynamic access to the device base
                var deviceBase = owner as dynamic;
                deviceBase._currentSwitchState = switchState;
                deviceBase._lastUpdated = DateTime.Now;
                
                _logger.LogInformation("{DeviceType} {DeviceId} switch state updated via feedback: {SwitchState}", _deviceType, _deviceId, switchState);
            }
        }

        /// <summary>
        /// Turns the device on
        /// </summary>
        public async Task TurnOnAsync(TimeSpan? timeout = null)
        {
            await SetSwitchStateAsync(Switch.On, timeout);
        }

        /// <summary>
        /// Turns the device off
        /// </summary>
        public async Task TurnOffAsync(TimeSpan? timeout = null)
        {
            await SetSwitchStateAsync(Switch.Off, timeout);
        }

        /// <summary>
        /// Toggles the device state
        /// </summary>
        public async Task ToggleAsync(TimeSpan? timeout = null)
        {
            var currentState = owner.CurrentSwitchState;
            var targetState = currentState switch
            {
                Switch.On => Switch.Off,
                Switch.Off => Switch.On,
                Switch.Unknown => Switch.On, // Default to On when unknown
                _ => Switch.On
            };
            await SetSwitchStateAsync(targetState, timeout);
        }

        /// <summary>
        /// Sets the switch state to the specified value
        /// </summary>
        public async Task SetSwitchStateAsync(Switch switchState, TimeSpan? timeout = null)
        {
            await SetBitFunctionAsync(
                address: addresses.Control,
                value: switchState == Switch.On,
                condition: () => owner.CurrentSwitchState == switchState,
                timeout: timeout ?? _defaultTimeout
            );
        }

        /// <summary>
        /// Reads the current switch state from KNX bus
        /// </summary>
        public async Task<Switch> ReadSwitchStateAsync()
        {
            try
            {
                var switchState = await _knxService.RequestGroupValue<bool>(addresses.Feedback);
                return switchState ? Switch.On : Switch.Off;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read switch state for {DeviceType} {DeviceId}: {Message}", _deviceType, _deviceId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Waits for the switch state to reach the specified value
        /// </summary>
        public async Task<bool> WaitForSwitchStateAsync(Switch switchState, TimeSpan? timeout = null)
        {
            return await WaitForConditionAsync(
                () => owner.CurrentSwitchState == switchState,
                timeout ?? _defaultTimeout,
                $"switch state {switchState}"
            );
        }
    }
}
