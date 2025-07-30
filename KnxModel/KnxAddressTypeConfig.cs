using System;
using System.Collections.Generic;

namespace KnxModel
{
    /// <summary>
    /// Defines the expected data type for KNX group addresses
    /// </summary>
    public enum KnxDataType
    {
        Boolean,        // 1-bit: switches, locks, movement commands
        Percent,        // 1-byte: position, dimmer level
        Temperature,    // 2-byte: temperature values
        Byte,          // 1-byte: raw values
        TwoByteUnsigned, // 2-byte: counters, raw values
        DateTime,       // Date/time values
        Text,          // String values
        Unknown        // Unknown or unsupported type
    }

    /// <summary>
    /// Configuration for determining KNX data types based on group addresses
    /// </summary>
    public static class KnxAddressTypeConfig
    {
        /// <summary>
        /// Default configuration for common KNX address patterns
        /// Key: "MainGroup/MiddleGroup" or "MainGroup" for main group only
        /// Value: Expected data type
        /// </summary>
        private static readonly Dictionary<string, KnxDataType> _addressTypeMap = new()
        {
            // Lighting (Group 1)
            ["1"] = KnxDataType.Boolean,
            ["1/1"] = KnxDataType.Boolean,      // Light switches
            ["1/2"] = KnxDataType.Percent,      // Dimmer levels
            ["1/3"] = KnxDataType.Boolean,      // Scene control

            // Blinds/Shutters (Group 4)
            ["4"] = KnxDataType.Boolean,
            ["4/0"] = KnxDataType.Boolean,      // Movement control (up/down)
            ["4/1"] = KnxDataType.Boolean,      // Stop commands
            ["4/2"] = KnxDataType.Percent,      // Position control/feedback
            ["4/3"] = KnxDataType.Boolean,      // Lock control/feedback

            // HVAC (Group 2)
            ["2"] = KnxDataType.Temperature,
            ["2/1"] = KnxDataType.Temperature,  // Temperature sensors
            ["2/2"] = KnxDataType.Boolean,      // HVAC on/off
            ["2/3"] = KnxDataType.Percent,      // HVAC level/speed

            // Security (Group 3)
            ["3"] = KnxDataType.Boolean,
            ["3/1"] = KnxDataType.Boolean,      // Alarm states
            ["3/2"] = KnxDataType.Boolean,      // Door locks
            ["3/3"] = KnxDataType.Boolean,      // Motion sensors

            // Counters/Meters (Group 5)
            ["5"] = KnxDataType.TwoByteUnsigned,
            ["5/1"] = KnxDataType.TwoByteUnsigned, // Energy counters
            ["5/2"] = KnxDataType.TwoByteUnsigned, // Water meters

            // Time/Date (Group 6)
            ["6"] = KnxDataType.DateTime,
            ["6/1"] = KnxDataType.DateTime,     // Time
            ["6/2"] = KnxDataType.DateTime,     // Date

            // General purpose (Group 0)
            ["0"] = KnxDataType.Unknown,
        };

        /// <summary>
        /// Custom address type mappings for specific installations
        /// </summary>
        private static readonly Dictionary<string, KnxDataType> _customAddressTypes = new();

        /// <summary>
        /// Gets the expected data type for a KNX group address
        /// </summary>
        /// <param name="address">Full KNX address (e.g., "4/2/17")</param>
        /// <returns>Expected data type</returns>
        public static KnxDataType GetExpectedType(string address)
        {
            if (string.IsNullOrEmpty(address))
                return KnxDataType.Unknown;

            var parts = address.Split('/');
            if (parts.Length < 2)
                return KnxDataType.Unknown;

            var mainGroup = parts[0];
            var middleGroup = parts[1];
            var mainMiddle = $"{mainGroup}/{middleGroup}";

            // Check custom mappings first
            if (_customAddressTypes.TryGetValue(address, out var customType))
                return customType;

            if (_customAddressTypes.TryGetValue(mainMiddle, out customType))
                return customType;

            if (_customAddressTypes.TryGetValue(mainGroup, out customType))
                return customType;

            // Check default mappings
            if (_addressTypeMap.TryGetValue(mainMiddle, out var type))
                return type;

            if (_addressTypeMap.TryGetValue(mainGroup, out type))
                return type;

            return KnxDataType.Unknown;
        }

        /// <summary>
        /// Adds or updates a custom address type mapping
        /// </summary>
        /// <param name="addressPattern">Address pattern (e.g., "4/2", "4/2/17", "4")</param>
        /// <param name="dataType">Expected data type</param>
        public static void SetCustomType(string addressPattern, KnxDataType dataType)
        {
            _customAddressTypes[addressPattern] = dataType;
        }

        /// <summary>
        /// Gets all configured address patterns and their types
        /// </summary>
        public static IReadOnlyDictionary<string, KnxDataType> GetAllMappings()
        {
            var combined = new Dictionary<string, KnxDataType>(_addressTypeMap);
            foreach (var kvp in _customAddressTypes)
            {
                combined[kvp.Key] = kvp.Value;
            }
            return combined;
        }

        /// <summary>
        /// Checks if an address pattern matches a specific data type
        /// </summary>
        public static bool IsAddressOfType(string address, KnxDataType expectedType)
        {
            return GetExpectedType(address) == expectedType;
        }

        /// <summary>
        /// Gets a human-readable description of what the address typically represents
        /// </summary>
        public static string GetAddressDescription(string address)
        {
            if (string.IsNullOrEmpty(address))
                return "Unknown";

            var parts = address.Split('/');
            if (parts.Length < 2)
                return "Invalid address";

            var mainGroup = parts[0];
            var middleGroup = parts[1];

            return $"{mainGroup}/{middleGroup}" switch
            {
                "1/1" => "Light Switch",
                "1/2" => "Dimmer Level",
                "4/0" => "Shutter Movement",
                "4/1" => "Shutter Stop",
                "4/2" => "Shutter Position",
                "4/3" => "Shutter Lock",
                "2/1" => "Temperature",
                "2/2" => "HVAC Control",
                "3/1" => "Security Alarm",
                "3/2" => "Door Lock",
                _ => $"Group {mainGroup} Function {middleGroup}"
            };
        }
    }
}
