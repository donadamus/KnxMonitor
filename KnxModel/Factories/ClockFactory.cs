using Microsoft.Extensions.Logging;
using System;

namespace KnxModel.Factories
{
    /// <summary>
    /// Factory for creating ClockDevice instances
    /// </summary>
    public class ClockFactory
    {
        /// <summary>
        /// Creates a ClockDevice with the specified parameters
        /// </summary>
        /// <param name="id">Device ID</param>
        /// <param name="name">Device name</param>
        /// <param name="configuration">Clock configuration</param>
        /// <param name="knxService">KNX service instance</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="defaultTimeout">Default timeout for operations</param>
        /// <param name="latitude">Latitude for sun position calculation (default: Wrocław, Poland)</param>
        /// <param name="longitude">Longitude for sun position calculation (default: Wrocław, Poland)</param>
        /// <returns>ClockDevice instance</returns>
        public static ClockDevice CreateClockDevice(
            string id,
            string name,
            ClockConfiguration configuration,
            IKnxService knxService,
            ILogger<ClockDevice> logger,
            TimeSpan defaultTimeout,
            double latitude = 51.1079,
            double longitude = 17.0385)
        {
            return new ClockDevice(id, name, configuration, knxService, logger, defaultTimeout, latitude, longitude);
        }

        /// <summary>
        /// Creates a ClockDevice with default configuration
        /// </summary>
        /// <param name="id">Device ID</param>
        /// <param name="name">Device name</param>
        /// <param name="initialMode">Initial clock mode</param>
        /// <param name="knxService">KNX service instance</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="defaultTimeout">Default timeout for operations</param>
        /// <returns>ClockDevice instance</returns>
        public static ClockDevice CreateClockDevice(
            string id,
            string name,
            ClockMode initialMode,
            IKnxService knxService,
            ILogger<ClockDevice> logger,
            TimeSpan defaultTimeout)
        {
            var configuration = new ClockConfiguration(
                InitialMode: initialMode,
                TimeStamp: TimeSpan.FromSeconds(30) // Default 30 seconds
            );

            return CreateClockDevice(id, name, configuration, knxService, logger, defaultTimeout);
        }

        /// <summary>
        /// Creates a Master mode ClockDevice
        /// </summary>
        /// <param name="id">Device ID</param>
        /// <param name="name">Device name</param>
        /// <param name="knxService">KNX service instance</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="defaultTimeout">Default timeout for operations</param>
        /// <returns>ClockDevice instance in Master mode</returns>
        public static ClockDevice CreateMasterClockDevice(
            string id,
            string name,
            IKnxService knxService,
            ILogger<ClockDevice> logger,
            TimeSpan defaultTimeout)
        {
            return CreateClockDevice(id, name, ClockMode.Master, knxService, logger, defaultTimeout);
        }

        /// <summary>
        /// Creates a Slave mode ClockDevice
        /// </summary>
        /// <param name="id">Device ID</param>
        /// <param name="name">Device name</param>
        /// <param name="knxService">KNX service instance</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="defaultTimeout">Default timeout for operations</param>
        /// <returns>ClockDevice instance in Slave mode</returns>
        public static ClockDevice CreateSlaveClockDevice(
            string id,
            string name,
            IKnxService knxService,
            ILogger<ClockDevice> logger,
            TimeSpan defaultTimeout)
        {
            return CreateClockDevice(id, name, ClockMode.Slave, knxService, logger, defaultTimeout);
        }

        /// <summary>
        /// Creates a Slave/Master mode ClockDevice (adaptive mode)
        /// </summary>
        /// <param name="id">Device ID</param>
        /// <param name="name">Device name</param>
        /// <param name="knxService">KNX service instance</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="defaultTimeout">Default timeout for operations</param>
        /// <returns>ClockDevice instance in Slave/Master mode</returns>
        public static ClockDevice CreateSlaveMasterClockDevice(
            string id,
            string name,
            IKnxService knxService,
            ILogger<ClockDevice> logger,
            TimeSpan defaultTimeout)
        {
            return CreateClockDevice(id, name, ClockMode.SlaveMaster, knxService, logger, defaultTimeout);
        }
    }
}
