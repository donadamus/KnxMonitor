using System;

namespace KnxModel
{
    /// <summary>
    /// Represents a KNX value that can be automatically converted to appropriate types
    /// based on the context and data length
    /// </summary>
    public class KnxValue
    {
        public object RawValue { get; }
        public byte[] RawData { get; }
        public int DataLength => RawData.Length;
        public DateTime Timestamp { get; }

        public KnxValue(object rawValue, byte[]? rawData = null)
        {
            RawValue = rawValue ?? throw new ArgumentNullException(nameof(rawValue));
            RawData = rawData ?? ConvertToBytes(rawValue);
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Converts the value to a boolean (for 1-bit values like switches, locks)
        /// </summary>
        public bool AsBoolean()
        {
            return RawValue switch
            {
                bool b => b,
                string s => s == "1" || s.ToLowerInvariant() == "true",
                byte b => b != 0,
                int i => i != 0,
                _ => false
            };
        }

        /// <summary>
        /// Converts the value to a percentage (for 1-byte values like dimmer, shutter position)
        /// </summary>
        public Percent AsPercent()
        {
            return RawValue switch
            {
                Percent p => p,
                byte b => new Percent(b),
                int i when i >= 0 && i <= 255 => new Percent((byte)i),
                double d when d >= 0 && d <= 100 => Percent.FromPercantage(d),
                string s when double.TryParse(s, out var val) => Percent.FromPercantage(val),
                _ => new Percent(0)
            };
        }

        /// <summary>
        /// Converts the value to a percentage value as integer (0-100)
        /// </summary>
        public float AsPercentageValue()
        {
            return RawValue switch
            {
                Percent p => (int)p.Value,
                byte b => (int)(b / 2.55), // Convert KNX byte (0-255) to percentage (0-100)
                int i when i >= 0 && i <= 100 => (float)i, // Already percentage
                float f when f >= 0 && f <= 100 => f,
                double d when d >= 0 && d <= 100 => (float)d,
                string s when double.TryParse(s, out var val) && val >= 0 && val <= 100 => (int)val,
                _ => 0
            };
        }

        /// <summary>
        /// Converts the value to a byte (raw KNX 1-byte value)
        /// </summary>
        public byte AsByte()
        {
            return RawValue switch
            {
                byte b => b,
                bool b => (byte)(b ? 1 : 0),
                int i when i >= 0 && i <= 255 => (byte)i,
                string s when byte.TryParse(s, out var val) => val,
                _ => 0
            };
        }

        /// <summary>
        /// Converts the value to an integer (for 2-byte values)
        /// </summary>
        public int AsInt()
        {
            return RawValue switch
            {
                int i => i,
                byte b => b,
                bool b => b ? 1 : 0,
                string s when int.TryParse(s, out var val) => val,
                _ => 0
            };
        }

        /// <summary>
        /// Gets the raw string representation
        /// </summary>
        public string AsString()
        {
            return RawValue.ToString() ?? "Unknown";
        }

        /// <summary>
        /// Auto-detects the most appropriate type based on data length and context
        /// For shutter-related addresses
        /// </summary>
        public T AutoConvert<T>()
        {
            var targetType = typeof(T);

            if (targetType == typeof(bool))
                return (T)(object)AsBoolean();
            
            if (targetType == typeof(Percent))
                return (T)(object)AsPercent();
            
            if (targetType == typeof(byte))
                return (T)(object)AsByte();
            
            if (targetType == typeof(int))
                return (T)(object)AsInt();
            
            if (targetType == typeof(string))
                return (T)(object)AsString();

            // Try direct conversion
            if (RawValue is T directValue)
                return directValue;

            return default(T)!;
        }

        /// <summary>
        /// Determines the most likely type based on data length
        /// Models specify exact types via RequestGroupValue<T>
        /// </summary>
        public object GetTypedValue(string? address = null)
        {
            // Always use data length for automatic type detection
            // Models specify expected types explicitly via RequestGroupValue<T>
            return GetTypedValueByDataLength();
        }

        /// <summary>
        /// Fallback method to determine type based on data length only
        /// </summary>
        private object GetTypedValueByDataLength()
        {
            // For 1-bit values (switches, locks, movement commands)
            if (DataLength == 1 && RawData[0] <= 1)
            {
                return AsBoolean();
            }

            // For 1-byte values above 1 - assume percentage (dimmer, position)
            if (DataLength == 1)
            {
                return AsPercent();
            }

            // For 2-byte values
            if (DataLength == 2)
            {
                return AsInt();
            }

            // Default to raw value
            return RawValue;
        }

        private static byte[] ConvertToBytes(object value)
        {
            return value switch
            {
                bool b => new[] { (byte)(b ? 1 : 0) },
                byte b => new[] { b },
                Percent p => new[] { p.KnxRawValue },
                int i when i >= 0 && i <= 255 => new[] { (byte)i },
                int i => BitConverter.GetBytes(i),
                string s when byte.TryParse(s, out var b) => new[] { b },
                _ => new[] { (byte)0 }
            };
        }

        public override string ToString()
        {
            // Try to get a meaningful representation without address context
            var typedValue = GetTypedValue(); // Falls back to heuristics
            var typedDisplay = typedValue switch
            {
                Percent p => $"{p.Value:F1}%",
                bool b => b ? "ON" : "OFF",
                _ => typedValue.ToString()
            };
            return $"{typedDisplay} (Raw: {RawValue}, Length: {DataLength})";
        }
    }
}
