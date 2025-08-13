using Microsoft.Extensions.Logging;
using KnxModel.Models;

namespace KnxModel.Factories
{
    /// <summary>
    /// Factory for creating ThresholdSimulatorDevice instances
    /// Similar to ClockFactory but for threshold simulation
    /// </summary>
    public class ThresholdSimulatorFactory
    {
        /// <summary>
        /// Creates a ThresholdSimulatorDevice for integration testing
        /// </summary>
        /// <param name="id">Device ID</param>
        /// <param name="name">Device name</param>
        /// <param name="knxService">KNX service instance</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="defaultTimeout">Default timeout for operations</param>
        /// <returns>ThresholdSimulatorDevice instance</returns>
        public static ThresholdSimulatorDevice CreateThresholdSimulator(
            string id,
            string name,
            IKnxService knxService,
            ILogger<ThresholdSimulatorDevice> logger,
            TimeSpan defaultTimeout)
        {
            return new ThresholdSimulatorDevice(id, name, knxService, logger, defaultTimeout);
        }

        /// <summary>
        /// Creates a ThresholdSimulatorDevice with default testing configuration
        /// </summary>
        /// <param name="knxService">KNX service instance</param>
        /// <param name="logger">Logger instance</param>
        /// <returns>ThresholdSimulatorDevice instance with default settings</returns>
        public static ThresholdSimulatorDevice CreateDefaultTestingSimulator(
            IKnxService knxService,
            ILogger<ThresholdSimulatorDevice> logger)
        {
            return CreateThresholdSimulator(
                "threshold-simulator",
                "Testing Threshold Simulator",
                knxService,
                logger,
                TimeSpan.FromSeconds(5)
            );
        }

        /// <summary>
        /// Creates a ThresholdSimulatorDevice for sun protection testing
        /// </summary>
        /// <param name="knxService">KNX service instance</param>
        /// <param name="logger">Logger instance</param>
        /// <returns>ThresholdSimulatorDevice instance configured for sun protection testing</returns>
        public static ThresholdSimulatorDevice CreateSunProtectionSimulator(
            IKnxService knxService,
            ILogger<ThresholdSimulatorDevice> logger)
        {
            return CreateThresholdSimulator(
                "sun-protection-simulator",
                "Sun Protection Threshold Simulator",
                knxService,
                logger,
                TimeSpan.FromSeconds(3)
            );
        }
    }
}
