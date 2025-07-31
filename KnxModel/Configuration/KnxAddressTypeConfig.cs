using System;
using System.Collections.Generic;

namespace KnxModel
{
    /// <summary>
    /// Configuration for providing human-readable descriptions of KNX group addresses
    /// Models themselves specify expected data types via RequestGroupValue<T>
    /// </summary>
    public static class KnxAddressTypeConfig
    {
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
