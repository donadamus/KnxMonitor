using System;
using System.Collections.Generic;

namespace KnxModel
{
    /// <summary>
    /// Factory for creating Light instances with predefined configurations
    /// </summary>
    public static class DimmerFactory
    {


        public static DimmerDevice CreateDimmer(string dimmerId, IKnxService knxService)
        {
            if (DimmerConfigurations.TryGetValue(dimmerId, out var config))
            {
                return new DimmerDevice(dimmerId, config.Name, config.SubGroup, knxService);
            }
            //else if (DimmerFactory.DimmerConfigurations.TryGetValue(lightId, out var dimmerConfig))
            //{
            //    return new DimmerOld(lightId, dimmerConfig.Name, dimmerConfig.SubGroup, knxService);
            //}

            throw new ArgumentException($"Unknown light ID: {dimmerId}");
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
