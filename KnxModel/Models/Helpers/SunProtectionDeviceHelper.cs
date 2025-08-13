using Microsoft.Extensions.Logging;

namespace KnxModel.Models.Helpers
{
    /// <summary>
    /// Helper class for sun protection operations and threshold monitoring
    /// Handles sun protection block state and threshold state waiting
    /// </summary>
    public class SunProtectionDeviceHelper<T, TAddress> : DeviceHelperBase<T, TAddress>
        where T : ISunProtectionBlockableDevice, ISunProtectionThresholdCapableDevice, IKnxDeviceBase
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
                () => owner.IsSunProtectionBlocked == targetState,
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
    }
}
