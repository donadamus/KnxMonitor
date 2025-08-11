using KnxModel.Types;

namespace KnxModel.Interfaces
{
    /// <summary>
    /// Interface for devices that can provide sun position calculations
    /// </summary>
    public interface ISunPositionProvider
    {
        /// <summary>
        /// Gets the current position of the sun based on device location and current time
        /// </summary>
        /// <returns>Current sun position with azimuth and elevation angles</returns>
        SunPosition GetCurrentSunPosition();

        /// <summary>
        /// Gets the sun position for a specific date and time
        /// </summary>
        /// <param name="dateTime">The date and time for which to calculate sun position</param>
        /// <returns>Sun position at the specified time</returns>
        SunPosition GetSunPosition(DateTime dateTime);

        /// <summary>
        /// Gets sunrise and sunset times for today
        /// </summary>
        /// <returns>Sun times for today</returns>
        SunTimes GetTodaySunTimes();

        /// <summary>
        /// Gets sunrise and sunset times for a specific date
        /// </summary>
        /// <param name="date">The date for which to calculate sun times</param>
        /// <returns>Sun times for the specified date</returns>
        SunTimes GetSunTimes(DateTime date);

        /// <summary>
        /// Gets or sets the latitude of the device location in degrees
        /// </summary>
        double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude of the device location in degrees
        /// </summary>
        double Longitude { get; set; }
    }
}
