using Microsoft.Extensions.Logging;

namespace KnxModel.Models.Helpers
{
    /// <summary>
    /// Helper class for sun protection operations and threshold monitoring
    /// Handles sun protection block state and threshold state waiting
    /// </summary>
    public class SunProtectionDeviceHelper<T> : DeviceHelperBase<T>
        where T : class
    {
        private readonly Func<ShutterAddresses> _getAddresses;
        private readonly Func<bool> _getCurrentSunProtectionBlockState;
        private readonly Func<bool> _getCurrentBrightnessThreshold1State;
        private readonly Func<bool> _getCurrentBrightnessThreshold2State;
        private readonly Func<bool> _getCurrentOutdoorTemperatureThresholdState;
        private readonly new ILogger<T> _logger;

        public SunProtectionDeviceHelper(
            IKnxService knxService,
            string deviceId,
            string deviceType,
            Func<ShutterAddresses> getAddresses,
            Func<bool> getCurrentSunProtectionBlockState,
            Func<bool> getCurrentBrightnessThreshold1State,
            Func<bool> getCurrentBrightnessThreshold2State,
            Func<bool> getCurrentOutdoorTemperatureThresholdState,
            ILogger<T> logger,
            TimeSpan defaultTimeout) : base(knxService, deviceId, deviceType, logger, defaultTimeout)
        {
            _getAddresses = getAddresses ?? throw new ArgumentNullException(nameof(getAddresses));
            _getCurrentSunProtectionBlockState = getCurrentSunProtectionBlockState ?? throw new ArgumentNullException(nameof(getCurrentSunProtectionBlockState));
            _getCurrentBrightnessThreshold1State = getCurrentBrightnessThreshold1State ?? throw new ArgumentNullException(nameof(getCurrentBrightnessThreshold1State));
            _getCurrentBrightnessThreshold2State = getCurrentBrightnessThreshold2State ?? throw new ArgumentNullException(nameof(getCurrentBrightnessThreshold2State));
            _getCurrentOutdoorTemperatureThresholdState = getCurrentOutdoorTemperatureThresholdState ?? throw new ArgumentNullException(nameof(getCurrentOutdoorTemperatureThresholdState));
            _logger = logger;
        }

        /// <summary>
        /// Waits for sun protection block state to reach the specified value
        /// </summary>
        public async Task<bool> WaitForSunProtectionBlockStateAsync(bool targetState, TimeSpan? timeout = null)
        {
            return await WaitForConditionAsync(
                () => _getCurrentSunProtectionBlockState() == targetState,
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
                () => _getCurrentBrightnessThreshold1State() == targetState,
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
                () => _getCurrentBrightnessThreshold2State() == targetState,
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
                () => _getCurrentOutdoorTemperatureThresholdState() == targetState,
                timeout ?? _defaultTimeout,
                $"outdoor temperature threshold state {targetState}"
            );
        }
    }
}
