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

            ["L08.1"] = new("Utility Room, Celling Light", "12"),
            ["L09.1"] = new("Boiler Room, Celling Light", "13"),
            ["L00.1"] = new("Garage, Celling Light", "14"),
            ["L00.2"] = new("Garage, Celling Light", "15"),
            ["L1.1"] = new("Bathroom, Celling Light", "16"),
            ["L1.2"] = new("Bathroom, Mirror Light", "17"),
            ["L2.1"] = new("Master Bathroom", "18"),
            ["L2.2"] = new("Master Bathroom", "19"),
            ["L3.1"] = new("Master Bedroom, Celling Light", "20"),
            ["L3.2"] = new("Master Bedroom, Bed Light Left", "21"),
            ["L3.3"] = new("Master Bedroom, Bed Light Right", "22"),
            ["L4.1"] = new("Wardrobe, Celling Light", "23"),
            ["L5.1"] = new("Guest Room, Celling Light", "24"),
            ["L6.1"] = new("Kinga, Celling Light", "25"),
            ["L7.1"] = new("Rafał, Celling Light", "26"),
            ["L8.1"] = new("Hall, Celling Light", "27"),

            ["L01.2"] = new("Hall, Bell", "28"),

        };
    }
}
