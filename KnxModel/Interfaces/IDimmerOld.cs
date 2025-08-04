using System;
using System.Threading.Tasks;

namespace KnxModel
{
    /// <summary>
    /// Interface for KNX dimmer devices that support both switch (on/off) and brightness control
    /// Extends ILight with additional brightness functionality
    /// </summary>
    public interface IDimmerOld : ILightOld
    {
        /// <summary>
        /// Gets the dimmer-specific addresses (overrides ILight.Addresses)
        /// </summary>
        new DimmerAddresses Addresses { get; }

        /// <summary>
        /// Gets the current dimmer state (overrides ILight.CurrentState)
        /// </summary>
        new DimmerState CurrentState { get; }

        /// <summary>
        /// Gets the saved dimmer state (overrides ILight.SavedState)
        /// </summary>
        new DimmerState? SavedState { get; }

        // Switch Control methods (SetStateAsync, TurnOnAsync, TurnOffAsync, ToggleAsync, 
        // ReadStateAsync, WaitForStateAsync) are inherited from ILight
        
        #region Brightness Control
        
        /// <summary>
        /// Sets the dimmer brightness level
        /// </summary>
        /// <param name="brightness">Brightness level (0-100%)</param>
        Task SetBrightnessAsync(float brightness, TimeSpan? timespan = null);

        /// <summary>
        /// Reads the current brightness level of the dimmer
        /// </summary>
        /// <returns>Current brightness level (0-100%)</returns>
        Task<float> ReadBrightnessAsync();

        /// <summary>
        /// Waits for the dimmer to reach the target brightness level
        /// </summary>
        /// <param name="targetBrightness">The target brightness level to wait for</param>
        /// <param name="timeout">Optional timeout for the operation</param>
        /// <returns>True if the target brightness was reached within the timeout</returns>
        Task<bool> WaitForBrightnessAsync(float targetBrightness, TimeSpan? timeout = null);

        /// <summary>
        /// Gradually dims the light to a target brightness level
        /// </summary>
        /// <param name="targetBrightness">Target brightness level (0-100%)</param>
        /// <param name="duration">Duration of the dimming process</param>
        Task FadeToAsync(float targetBrightness, TimeSpan duration);
        
        #endregion
    }
}
