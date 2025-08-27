using KnxModel.Types;
using Microsoft.Extensions.Logging;

namespace KnxModel.Models.Helpers
{
    /// <summary>
    /// Helper class for sun protection operations and threshold monitoring
    /// Handles sun protection block state and threshold state waiting
    /// </summary>
    public class SunProtectionDeviceHelper<T, TAddress> : DeviceHelperBase<T, TAddress>
        where T : ISunProtectionBlockableDevice, ISunProtectionThresholdCapableDevice, IKnxDeviceBase
        where TAddress : ISunProtectionThresholdAddresses, ISunProtectionBlockableAddresses
    {
        public SunProtectionDeviceHelper(T owner, TAddress addresses, IKnxService knxService, string deviceId, string deviceType,
            ILogger<T> logger, TimeSpan defaultTimeout) 
            : base(owner, addresses, knxService, deviceId, deviceType, logger, defaultTimeout)
        {
        }

        /// <summary>
        /// Waits for sun protection block state to reach the specified value
        /// </summary>
        public async Task<bool> WaitForSunProtectionBlockStateAsync(bool targetState, TimeSpan? timeout = null)
        {
            return await WaitForConditionAsync(
                () => owner.SunProtectionBlocked == targetState,
                timeout ?? _defaultTimeout,
                $"sun protection block state {targetState}"
            );
        }

        /// <summary>
        /// Waits for brightness threshold 1 state to reach the specified value
        /// </summary>
        public async Task<bool> WaitForBrightnessThreshold1StateAsync(bool targetState, TimeSpan? timeout = null)
        {
            return await WaitForConditionAsync(
                () => owner.BrightnessThreshold1Active == targetState,
                timeout ?? _defaultTimeout,
                $"brightness threshold 1 state {targetState}"
            );
        }

        /// <summary>
        /// Waits for brightness threshold 2 state to reach the specified value
        /// </summary>
        public async Task<bool> WaitForBrightnessThreshold2StateAsync(bool targetState, TimeSpan? timeout = null)
        {
            return await WaitForConditionAsync(
                () => owner.BrightnessThreshold2Active == targetState,
                timeout ?? _defaultTimeout,
                $"brightness threshold 2 state {targetState}"
            );
        }

        /// <summary>
        /// Waits for outdoor temperature threshold state to reach the specified value
        /// </summary>
        public async Task<bool> WaitForOutdoorTemperatureThresholdStateAsync(bool targetState, TimeSpan? timeout = null)
        {
            return await WaitForConditionAsync(
                () => owner.OutdoorTemperatureThresholdActive == targetState,
                timeout ?? _defaultTimeout,
                $"outdoor temperature threshold state {targetState}"
            );
        }

        internal async Task<bool> ReadSunProtectionStateAsync()
        {
            var thresholdState = await _knxService.RequestGroupValue<bool>(addresses.SunProtectionActive);

            return thresholdState;
        }

        internal async Task<bool> WaitForSunProtectionStateAsync(bool targetState, TimeSpan? timeout)
        {
            return await WaitForConditionAsync(
               () => owner.SunProtectionActive == targetState,
               timeout ?? _defaultTimeout,
               $"sun pritection 1 state {targetState}"
           );
        }

        internal async Task BlockSunProtectionAsync(TimeSpan? timeout)
        {
            await SetBlockSunProtectionAsync(true, timeout);
        }

        internal async Task UnblockSunProtectionAsync(TimeSpan? timeout)
        {
            await SetBlockSunProtectionAsync(false, timeout);
        }

        private async Task SetBlockSunProtectionAsync(bool value, TimeSpan? timeout)
        {
            await SetBitFunctionAsync(
                    address: addresses.SunProtectionBlockControl,
                    value: value,
                    condition: () => owner.SunProtectionBlocked == value,
                    timeout: timeout ?? _defaultTimeout
                    );
        }

        internal void ProcessSunProtectionMessage(KnxGroupEventArgs e)
        {
            // Process sun protection block feedback (same address as control)
            if (e.Destination == addresses.SunProtectionBlockFeedback)
            {
                var blockState = e.Value.AsBoolean();
                owner.SunProtectionBlocked = blockState;
                owner.LastUpdated = DateTime.Now;

                _logger.LogInformation("ShutterDevice {DeviceId} sun protection block feedback: {BlockState}",
                    _deviceId, blockState ? "BLOCKED" : "UNBLOCKED");
                Console.WriteLine($"ShutterDevice {_deviceId} sun protection block: {(blockState ? "BLOCKED" : "UNBLOCKED")}");
            }

        }

        internal void ProcessThresholdMessage(KnxGroupEventArgs e)
        {
            // Process brightness threshold 1 feedback
            if (e.Destination == addresses.BrightnessThreshold1)
            {
                var thresholdActive = e.Value.AsBoolean();
                owner.BrightnessThreshold1Active = thresholdActive;
                owner.LastUpdated = DateTime.Now;

                _logger.LogInformation("ShutterDevice {DeviceId} brightness threshold 1: {State}",
                    _deviceId, thresholdActive ? "ACTIVE" : "INACTIVE");
            }

            // Process brightness threshold 2 feedback
            if (e.Destination == addresses.BrightnessThreshold2)
            {
                var thresholdActive = e.Value.AsBoolean();
                owner.BrightnessThreshold2Active = thresholdActive;
                owner.LastUpdated = DateTime.Now;

                _logger.LogInformation("ShutterDevice {DeviceId} brightness threshold 2: {State}",
                    _deviceId, thresholdActive ? "ACTIVE" : "INACTIVE");
            }

            // Process outdoor temperature threshold feedback
            if (e.Destination == addresses.OutdoorTemperatureThreshold)
            {
                var thresholdActive = e.Value.AsBoolean();
                owner.OutdoorTemperatureThresholdActive = thresholdActive;
                owner.LastUpdated = DateTime.Now;

                _logger.LogInformation("ShutterDevice {DeviceId} outdoor temperature threshold: {State}",
                    _deviceId, thresholdActive ? "ACTIVE" : "INACTIVE");
            }

            // Process sun protection status feedback (offset +100)
            if (e.Destination == addresses.SunProtectionActive)
            {
                var isActive = e.Value.AsBoolean();
                owner.SunProtectionActive = isActive;
                owner.LastUpdated = DateTime.Now;
                _logger.LogInformation("ShutterDevice {DeviceId} sun protection status: {Status}",
                     _deviceId, isActive ? "ACTIVE" : "INACTIVE");
            }

        }
    }
}
