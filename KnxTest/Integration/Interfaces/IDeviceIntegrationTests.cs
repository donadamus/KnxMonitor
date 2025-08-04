using System.Threading.Tasks;

namespace KnxTest.Integration.Interfaces
{
    /// <summary>
    /// Core integration tests that EVERY KNX device must implement
    /// </summary>
    public interface IDeviceIntegrationTests
    {
        /// <summary>
        /// Tests device initialization and state reading
        /// </summary>
        Task OK_CanInitializeAndReadState(string deviceId);
        
        /// <summary>
        /// Tests basic switch functionality (ON/OFF)
        /// </summary>
        Task OK_CanTurnOnAndOff(string deviceId);
        
        /// <summary>
        /// Tests toggle functionality
        /// </summary>
        Task OK_CanToggle(string deviceId);
        
        /// <summary>
        /// Tests that lock prevents state changes
        /// </summary>
        Task OK_LockPreventsStateChanges(string deviceId);
        
        /// <summary>
        /// Tests state save and restore functionality
        /// </summary>
        Task OK_CanSaveAndRestoreState(string deviceId);
        
        /// <summary>
        /// Tests device address configuration is correct
        /// </summary>
        Task OK_HasCorrectAddressConfiguration(string deviceId);
        
        /// <summary>
        /// Tests reading device feedback updates current state
        /// </summary>
        Task OK_CanReadFeedbackAndCurrentStateIsUpdated(string deviceId);
    }
}
