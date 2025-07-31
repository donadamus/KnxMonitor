using System;
using System.Collections.Generic;

namespace KnxModel
{
    /// <summary>
    /// Factory for creating Light instances with predefined configurations
    /// </summary>
    public static class LightFactory
    {
        /// <summary>
        /// Creates a light instance by ID
        /// </summary>
        public static ILight CreateLight(string lightId, IKnxService knxService)
        {
            if (!LightConfigurations.TryGetValue(lightId, out var config))
            {
                throw new ArgumentException($"Unknown light ID: {lightId}");
            }

            return new Light(lightId, config.Name, config.SubGroup, knxService);
        }

        /// <summary>
        /// Creates all lights defined in the configuration
        /// </summary>
        public static IEnumerable<ILight> CreateAllLights(IKnxService knxService)
        {
            foreach (var (id, config) in LightConfigurations)
            {
                yield return new Light(id, config.Name, config.SubGroup, knxService);
            }
        }

        /// <summary>
        /// Gets all available light IDs
        /// </summary>
        public static IEnumerable<string> GetAvailableLightIds()
        {
            return LightConfigurations.Keys;
        }

        /// <summary>
        /// Configuration for a light
        /// </summary>
        private record LightConfig(string Name, string SubGroup);

        /// <summary>
        /// Predefined light configurations based on the KNX group addresses
        /// Maps from the test data in UnitTest1.cs
        /// </summary>
        private static readonly Dictionary<string, LightConfig> LightConfigurations = new()
        {
            // Based on test data: subGroups 11, 12, 13, 14, 15
            ["L11"] = new("Light 11", "11"),
            ["L12"] = new("Light 12", "12"),
            ["L13"] = new("Light 13", "13"),
            ["L14"] = new("Light 14", "14"),
            ["L15"] = new("Light 15", "15"),
            
            // Additional lights based on common KNX patterns
            ["L01"] = new("Kitchen Main", "01"),
            ["L02"] = new("Kitchen Counter", "02"),
            ["L03"] = new("Dining Room", "03"),
            ["L04"] = new("Living Room Main", "04"),
            ["L05"] = new("Living Room Accent", "05"),
            ["L06"] = new("Office", "06"),
            ["L07"] = new("Hall", "07"),
            ["L08"] = new("Master Bedroom", "08"),
            ["L09"] = new("Master Bathroom", "09"),
            ["L10"] = new("Guest Room", "10"),
            ["L16"] = new("Kinga's Room", "16"),
            ["L17"] = new("Rafal's Room", "17"),
            ["L18"] = new("Bathroom", "18"),
            ["L19"] = new("Garage", "19"),
            ["L20"] = new("Outdoor Front", "20")
        };
    }
}
