using System;
using System.Collections.Generic;

namespace KnxModel
{
    /// <summary>
    /// Factory for creating Shutter instances with predefined configurations
    /// </summary>
    public static class ShutterFactory
    {
        /// <summary>
        /// Creates a shutter instance by ID
        /// </summary>
        public static IShutter CreateShutter(string shutterId, IKnxService knxService)
        {
            if (!ShutterConfigurations.TryGetValue(shutterId, out var config))
            {
                throw new ArgumentException($"Unknown shutter ID: {shutterId}");
            }

            return new Shutter(shutterId, config.Name, config.SubGroup, knxService);
        }

        /// <summary>
        /// Creates all shutters defined in the configuration
        /// </summary>
        public static IEnumerable<IShutter> CreateAllShutters(IKnxService knxService)
        {
            foreach (var (id, config) in ShutterConfigurations)
            {
                yield return new Shutter(id, config.Name, config.SubGroup, knxService);
            }
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
        private record ShutterConfig(string Name, string SubGroup);

        /// <summary>
        /// Predefined shutter configurations based on the KNX group addresses
        /// </summary>
        private static readonly Dictionary<string, ShutterConfig> ShutterConfigurations = new()
        {
            // New shutters (4/0/1 to 4/0/12)
            { "R1.1", new("Bathroom", "1") },
            { "R2.1", new("Master Bathroom", "2") },
            { "R3.1", new("Master Bedroom", "3") },
            { "R3.2", new("Master Bedroom", "4") },
            { "R5.1", new("Guest Room", "5") },
            { "R6.1", new("Kinga's Room", "6") },
            { "R6.2", new("Kinga's Room", "7") },
            { "R6.3", new("Kinga's Room", "8") },
            { "R7.1", new("Rafal's Room", "9") },
            { "R7.2", new("Rafal's Room", "10") },
            { "R7.3", new("Rafal's Room", "11") },
            { "R8.1", new("Hall", "12") },

            // Existing shutters (4/0/13 to 4/0/18)
            { "R02.1", new("Kitchen", "13") },
            { "R02.2", new("Kitchen", "14") },
            { "R03.1", new("Dining Room", "15") },
            { "R04.1", new("Living Room", "16") },
            { "R04.2", new("Living Room", "17") },
            { "R05.1", new("Office", "18") }
        };
    }
}
