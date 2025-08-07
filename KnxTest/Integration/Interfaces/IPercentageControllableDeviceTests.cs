using System.Threading.Tasks;

namespace KnxTest.Integration.Interfaces
{
    /// <summary>
    /// Tests for devices that support percentage control functionality (like dimmers and shutters)
    /// </summary>
    public interface IPercentageControllableDeviceTests
    {
        /// <summary>
        /// Tests basic percentage setting functionality
        /// </summary>
        Task CanSetPercentage(string deviceId);
        Task TestCanSetPercentage(string deviceId);

        /// <summary>
        /// Tests reading percentage value from device
        /// </summary>
        Task CanReadPercentage(string deviceId);
        Task TestCanReadPercentage(string deviceId);

        /// <summary>
        /// Tests percentage range validation (0-100%)
        /// </summary>
        Task PercentageRangeValidation(string deviceId);
        Task TestPercentageRangeValidation(string deviceId);

        /// <summary>
        /// Tests adjusting percentage by increments/decrements
        /// </summary>
        Task CanAdjustPercentage(string deviceId);
        Task TestCanAdjustPercentage(string deviceId);

        /// <summary>
        /// Tests setting percentage to minimum (0%)
        /// </summary>
        Task CanSetToMinimum(string deviceId);
        Task TestCanSetToMinimum(string deviceId);

        /// <summary>
        /// Tests setting percentage to maximum (100%)
        /// </summary>
        Task CanSetToMaximum(string deviceId);
        Task TestCanSetToMaximum(string deviceId);

        /// <summary>
        /// Tests waiting for percentage state to reach target value
        /// </summary>
        Task CanWaitForPercentageState(string deviceId);
        Task TestCanWaitForPercentageState(string deviceId);

        /// <summary>
        /// Tests setting specific percentage values (25%, 50%, 75%)
        /// </summary>
        Task CanSetSpecificPercentages(string deviceId);
        Task TestCanSetSpecificPercentages(string deviceId);
    }
}
