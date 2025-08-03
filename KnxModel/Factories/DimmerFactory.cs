using System;
using System.Collections.Generic;

namespace KnxModel
{
    /// <summary>
    /// Factory for creating Light instances with predefined configurations
    /// </summary>
    public static class DimmerFactory
    {
        /// <summary>
        /// Creates a light instance by ID
        /// </summary>
        public static IDimmer CreateDimmer(string dimmerId, IKnxService knxService)
        {
            if (!DimmerConfigurations.TryGetValue(dimmerId, out var config))
            {
                throw new ArgumentException($"Unknown Dimmer ID: {dimmerId}");
            }

            return new Dimmer(dimmerId, config.Name, config.SubGroup, knxService);
        }

        /// <summary>
        /// Creates all lights defined in the configuration
        /// </summary>
        public static IEnumerable<IDimmer> CreateAllDimmers(IKnxService knxService)
        {
            foreach (var (id, config) in DimmerConfigurations)
            {
                yield return new Dimmer(id, config.Name, config.SubGroup, knxService);
            }
        }

        /// <summary>
        /// Gets all available light IDs
        /// </summary>
        public static IEnumerable<string> GetAvailableDimmerIds()
        {
            return DimmerConfigurations.Keys;
        }

        /// <summary>
        /// Configuration for a light
        /// </summary>
        public record LightConfig(string Name, string SubGroup);

        /// <summary>
        /// Predefined light configurations based on the KNX group addresses
        /// Maps from the test data in UnitTest1.cs
        /// </summary>
        public static readonly Dictionary<string, LightConfig> DimmerConfigurations = new()
        {
            ["D02.1"] = new("Kitchen LED, Dimmer 1", "1"),
            ["D02.2"] = new("Kitchen LED, Dimmer 2", "2"),
        };
    }
}
