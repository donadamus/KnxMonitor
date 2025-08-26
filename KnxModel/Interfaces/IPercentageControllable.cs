using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Interface for devices that can be controlled with percentage values (0-100%)
    /// Used by dimmers for brightness and shutters for position
    /// </summary>
    public interface IPercentageControllable : IIdentifable
    {

        float? SavedPercentage { get; internal set; }
        /// <summary>
        /// Current percentage value (0.0-100.0)
        /// </summary>
        float CurrentPercentage { get; internal set; }

        /// <summary>
        /// Set percentage value (0.0-100.0)
        /// </summary>
        /// <param name="percentage">Target percentage (0.0-100.0)</param>
        /// <param name="timeout">Maximum time to wait for operation</param>
        Task SetPercentageAsync(float percentage, TimeSpan? timeout = null);

        /// <summary>
        /// Read current percentage value from KNX bus
        /// </summary>
        Task<float> ReadPercentageAsync();

        /// <summary>
        /// Wait for specific percentage value
        /// </summary>
        /// <param name="targetPercentage">Target percentage to wait for</param>
        /// <param name="tolerance">Allowed deviation in percentage points</param>
        /// <param name="timeout">Maximum time to wait</param>
        Task<bool> WaitForPercentageAsync(float targetPercentage, double tolerance = 1.0, TimeSpan? timeout = null);

        /// <summary>
        /// Increase percentage by specified amount
        /// </summary>
        /// <param name="increment">Amount to increase (can be negative for decrease)</param>
        /// <param name="timeout">Maximum time to wait for operation</param>
        Task AdjustPercentageAsync(float increment, TimeSpan? timeout = null);
    }
}
