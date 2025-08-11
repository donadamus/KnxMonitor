using System;

namespace KnxModel.Types
{
    /// <summary>
    /// Represents the position of the sun with azimuth and elevation angles
    /// </summary>
    public readonly struct SunPosition : IEquatable<SunPosition>
    {
        /// <summary>
        /// Azimuth angle in degrees (0-360°, 0° = North, 90° = East, 180° = South, 270° = West)
        /// </summary>
        public double Azimuth { get; }

        /// <summary>
        /// Elevation angle in degrees (-90° to +90°, 0° = horizon, +90° = zenith, negative = below horizon)
        /// </summary>
        public double Elevation { get; }

        /// <summary>
        /// Gets a value indicating whether the sun is above the horizon
        /// </summary>
        public bool IsSunAboveHorizon => Elevation > 0;

        /// <summary>
        /// Initializes a new instance of the SunPosition struct
        /// </summary>
        /// <param name="azimuth">Azimuth angle in degrees (0-360°)</param>
        /// <param name="elevation">Elevation angle in degrees (-90° to +90°)</param>
        public SunPosition(double azimuth, double elevation)
        {
            // Normalize azimuth to 0-360 range
            Azimuth = ((azimuth % 360) + 360) % 360;
            
            // Clamp elevation to valid range
            Elevation = Math.Max(-90, Math.Min(90, elevation));
        }

        public bool Equals(SunPosition other)
        {
            return Math.Abs(Azimuth - other.Azimuth) < 0.001 &&
                   Math.Abs(Elevation - other.Elevation) < 0.001;
        }

        public override bool Equals(object? obj)
        {
            return obj is SunPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Azimuth, Elevation);
        }

        public static bool operator ==(SunPosition left, SunPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SunPosition left, SunPosition right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"Azimuth: {Azimuth:F1}°, Elevation: {Elevation:F1}°";
        }
    }

    /// <summary>
    /// Represents sunrise and sunset times for a specific date
    /// </summary>
    public readonly struct SunTimes : IEquatable<SunTimes>
    {
        /// <summary>
        /// Sunrise time (local time), null if sun doesn't rise on this date
        /// </summary>
        public DateTime? Sunrise { get; }

        /// <summary>
        /// Sunset time (local time), null if sun doesn't set on this date
        /// </summary>
        public DateTime? Sunset { get; }

        /// <summary>
        /// Date for which these sun times are calculated
        /// </summary>
        public DateTime Date { get; }

        /// <summary>
        /// Gets a value indicating whether the sun rises on this date
        /// </summary>
        public bool HasSunrise => Sunrise.HasValue;

        /// <summary>
        /// Gets a value indicating whether the sun sets on this date
        /// </summary>
        public bool HasSunset => Sunset.HasValue;

        /// <summary>
        /// Gets the daylight duration, null if no complete sunrise/sunset cycle
        /// </summary>
        public TimeSpan? DaylightDuration
        {
            get
            {
                if (Sunrise.HasValue && Sunset.HasValue)
                    return Sunset.Value - Sunrise.Value;
                return null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the SunTimes struct
        /// </summary>
        /// <param name="date">Date for which sun times are calculated</param>
        /// <param name="sunrise">Sunrise time (null if sun doesn't rise)</param>
        /// <param name="sunset">Sunset time (null if sun doesn't set)</param>
        public SunTimes(DateTime date, DateTime? sunrise, DateTime? sunset)
        {
            Date = date.Date; // Ensure we only store the date part
            Sunrise = sunrise;
            Sunset = sunset;
        }

        public bool Equals(SunTimes other)
        {
            return Date.Equals(other.Date) &&
                   Sunrise.Equals(other.Sunrise) &&
                   Sunset.Equals(other.Sunset);
        }

        public override bool Equals(object? obj)
        {
            return obj is SunTimes other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Date, Sunrise, Sunset);
        }

        public static bool operator ==(SunTimes left, SunTimes right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SunTimes left, SunTimes right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            var sunriseStr = Sunrise?.ToString("HH:mm") ?? "No sunrise";
            var sunsetStr = Sunset?.ToString("HH:mm") ?? "No sunset";
            return $"Date: {Date:yyyy-MM-dd}, Sunrise: {sunriseStr}, Sunset: {sunsetStr}";
        }
    }
}
