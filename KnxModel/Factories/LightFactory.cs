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
        public record LightConfig(string Name, string SubGroup);

        /// <summary>
        /// Predefined light configurations based on the KNX group addresses
        /// Maps from the test data in UnitTest1.cs
        /// </summary>
        public static readonly Dictionary<string, LightConfig> LightConfigurations = new()
        {
            ["L01.1"] = new("Hall, Celling Light", "1"),
            ["L02.1"] = new("Kitchen, Celling Light", "2"),
            ["L02.2"] = new("Kitchen, Bar Light", "3"),
            ["L03.1"] = new("Dining Room, Celling Light", "4"),
            ["L03.2"] = new("Dining Room Hall, Celling Light", "5"),
            ["L04.1"] = new("Living Room, Celling Light 1", "6"),
            ["L04.2"] = new("Living Room, Celling Light 2", "7"),
            ["L05.1"] = new("Office, Celling Light", "8"),
            ["L06.1"] = new("Pantry, Celling Light", "9"),
            ["L07.1"] = new("Bathroom, Celling Light", "10"),
            ["L07.2"] = new("Bathroom, Mirror Light", "11"),
           // ["L08"] = new("Master Bedroom", "08"),
           // ["L09"] = new("Master Bathroom", "09"),
           // ["L10"] = new("Guest Room", "10"),
           // ["L16"] = new("Kinga's Room", "16"),
           // ["L17"] = new("Rafal's Room", "17"),
           // ["L18"] = new("Bathroom", "18"),
           // ["L19"] = new("Garage", "19"),
           // ["L20"] = new("Outdoor Front", "20")
           //["L11"] = new("Light 11", "11"),
           // ["L12"] = new("Light 12", "12"),
           // ["L13"] = new("Light 13", "13"),
           // ["L14"] = new("Light 14", "14"),
           // ["L15"] = new("Light 15", "15"),
           // ["L25"] = new("Light 25", "25"),

        };
    }
}
