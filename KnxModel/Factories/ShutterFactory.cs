using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace KnxModel
{
    /// <summary>
    /// Factory for creating Shutter instances with predefined configurations
    /// </summary>
    public static class ShutterFactory
    {
        public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        public static ShutterDevice CreateShutter(string shutterId, IKnxService knxService, ILogger<ShutterDevice> logger)
        {
            if (ShutterConfigurations.TryGetValue(shutterId, out var config))
            {
                return new ShutterDevice(shutterId, config.Name, config.SubGroup, knxService, logger, DefaultTimeout);
            }
            
            throw new ArgumentException($"Unknown light ID: {shutterId}");
        }


        /// <summary>
        /// Gets all available shutter IDs
        /// </summary>
        public static IEnumerable<string> GetAvailableShutterIds()
        {
            return ShutterConfigurations.Keys;
        }

        /// <summary>
        /// Configuration for a shutter
        /// </summary>
        public record ShutterConfig(string Name, string SubGroup);

        /// <summary>
        /// Predefined shutter configurations based on the KNX group addresses
        /// </summary>
        public static readonly Dictionary<string, ShutterConfig> ShutterConfigurations = new()
        {
            //{ "R1.1", new("Bathroom", "1") },
            //{ "R2.1", new("Master Bathroom", "2") },
            //{ "R3.1", new("Master Bedroom", "3") },
            //{ "R3.2", new("Master Bedroom", "4") },
            //{ "R5.1", new("Guest Room", "5") },
            //{ "R6.1", new("Kinga's Room", "6") },
            //{ "R6.2", new("Kinga's Room", "7") },
            //{ "R6.3", new("Kinga's Room", "8") },
            //{ "R7.1", new("Rafal's Room", "9") },
            //{ "R7.2", new("Rafal's Room", "10") },
            //{ "R7.3", new("Rafal's Room", "11") },
            //{ "R8.1", new("Hall", "12") },

            //{ "R02.1", new("Kitchen", "13") },
            //{ "R02.2", new("Kitchen", "14") },
            //{ "R03.1", new("Dining Room", "15") },
            //{ "R04.1", new("Living Room", "16") },
            //{ "R04.2", new("Living Room", "17") },
            { "R05.1", new("Office", "18") }
        };
    }
}
