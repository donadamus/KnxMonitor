using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace KnxModel.Models.Helpers
{
    /// <summary>
    /// Helper class for implementing switchable device functionality
    /// Handles switch state management and KNX communication for ISwitchable implementations
    /// </summary>
    public class SwitchableDeviceHelper<TDevice> : DeviceHelperBase<TDevice>
    {
        private readonly Func<ISwitchableAddress> _getAddresses;
        private readonly Action<Switch> _updateSwitchState;
        private readonly Func<Switch> _getCurrentSwitchState;

        public SwitchableDeviceHelper(
            IKnxService knxService,
            string deviceId,
            string deviceType,
            Func<ISwitchableAddress> getAddresses,
            Action<Switch> updateSwitchState,
            Func<Switch> getCurrentSwitchState,
            ILogger<TDevice> logger, TimeSpan defaultTimeout) : base(knxService, deviceId, deviceType, logger, defaultTimeout)
        {
            _getAddresses = getAddresses ?? throw new ArgumentNullException(nameof(getAddresses));
            _updateSwitchState = updateSwitchState ?? throw new ArgumentNullException(nameof(updateSwitchState));
            _getCurrentSwitchState = getCurrentSwitchState ?? throw new ArgumentNullException(nameof(getCurrentSwitchState));
        } 

        /// <summary>
        /// Processes KNX switch feedback messages
        /// </summary>
        public void ProcessSwitchMessage(KnxGroupEventArgs e)
        {
            var addresses = _getAddresses();
            if (e.Destination == addresses.Feedback)
            {
                var isOn = e.Value.AsBoolean();
                var switchState = isOn ? Switch.On : Switch.Off;
                _updateSwitchState(switchState);
                Console.WriteLine($"{_deviceType} {_deviceId} switch state updated via feedback: {switchState}");
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
            var currentState = _getCurrentSwitchState();
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
                address: _getAddresses().Control,
                value: switchState == Switch.On,
                condition: () => _getCurrentSwitchState() == switchState,
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
                var addresses = _getAddresses();
                var switchState = await _knxService.RequestGroupValue<bool>(addresses.Feedback);
                return switchState ? Switch.On : Switch.Off;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read switch state for {_deviceType} {_deviceId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Waits for the switch state to reach the specified value
        /// </summary>
        public async Task<bool> WaitForSwitchStateAsync(Switch switchState, TimeSpan? timeout = null)
        {
            return await WaitForConditionAsync(
                () => _getCurrentSwitchState() == switchState,
                timeout ?? _defaultTimeout,
                $"switch state {switchState}"
            );
        }
    }
}
