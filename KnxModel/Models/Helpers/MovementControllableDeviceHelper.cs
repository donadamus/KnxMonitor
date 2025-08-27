using Microsoft.Extensions.Logging;

namespace KnxModel.Models.Helpers
{
    /// <summary>
    /// Helper class for shutter-specific operations (Open, Close, Stop)
    /// Handles movement control commands and activity state management
    /// </summary>
    public class MovementControllableDeviceHelper<T, TAddress> : DeviceHelperBase<T, TAddress>
        where T : IActivityStatusReadable, ILockableDevice, IKnxDeviceBase, IMovementControllable
        where TAddress : IMovementControllableAddress
    {
        public MovementControllableDeviceHelper(T owner, TAddress addresses, IKnxService knxService, string deviceId, string deviceType,
            ILogger<T> logger, TimeSpan defaultTimeout) 
            : base(owner, addresses, knxService, deviceId, deviceType, logger, defaultTimeout)
        {
        }


        /// <summary>
        /// Opens the shutter using UP command (MovementControl = 0)
        /// Device will echo confirmation on MovementFeedback (offset +100)
        /// and send actual movement status on MovementStatusFeedback
        /// </summary>
        internal async Task OpenAsync(TimeSpan? timeout = null)
        {
            // Send UP command (0) to MovementControl
            // Device will echo on MovementFeedback and send status on MovementStatusFeedback
            _logger.LogInformation("{DeviceType} {DeviceId} sending UP command (0)", _deviceType, _deviceId);
            await _knxService.WriteGroupValueAsync(addresses.MovementControl, false);
            _logger.LogInformation("{DeviceType} {DeviceId} UP command sent", _deviceType, _deviceId);
        }

        /// <summary>
        /// Closes the shutter using DOWN command (MovementControl = 1)
        /// Device will echo confirmation on MovementFeedback (offset +100)
        /// and send actual movement status on MovementStatusFeedback
        /// </summary>
        internal async Task CloseAsync(TimeSpan? timeout = null)
        {
            // Send DOWN command (1) to MovementControl
            // Device will echo on MovementFeedback and send status on MovementStatusFeedback
            _logger.LogInformation("{DeviceType} {DeviceId} sending DOWN command (1)", _deviceType, _deviceId);
            await _knxService.WriteGroupValueAsync(addresses.MovementControl, true);
            
            _logger.LogInformation("{DeviceType} {DeviceId} DOWN command sent", _deviceType, _deviceId);
        }

        /// <summary>
        /// Stops the shutter movement using StopControl trigger
        /// Device will send status update on MovementStatusFeedback when stopped
        /// </summary>
        internal async Task StopAsync(TimeSpan? timeout = null)
        {
            // Send KNX stop trigger to StopControl address
            // Device will respond with movement status on MovementStatusFeedback
            _logger.LogInformation("{DeviceType} {DeviceId} sending STOP trigger", _deviceType, _deviceId);
            await _knxService.WriteGroupValueAsync(addresses.StopControl, true);
            
            _logger.LogInformation("{DeviceType} {DeviceId} STOP trigger sent", _deviceType, _deviceId);
        }

        /// <summary>
        /// Processes incoming KNX messages for movement feedback and status updates
        /// MovementFeedback: Echo from device confirming UP(1)/DOWN(0) command (offset +100)
        /// MovementStatusFeedback: Device status when starting/stopping movement
        /// </summary>
        internal void ProcessMovementMessage(KnxGroupEventArgs e)
        {
            // Handle movement control feedback - echo from device confirming UP/DOWN command (offset +100)
            if (e.Destination == addresses.MovementFeedback)
            {
                var movementDirection = e.Value.AsBoolean();
                // This is just confirmation echo, device will send actual status on MovementStatusFeedback

                // Update last updated through dynamic access
                owner.CurrentDirection = movementDirection;
                owner.LastUpdated = DateTime.Now;
                
                _logger.LogInformation("{DeviceType} {DeviceId} movement command confirmed: {Direction}", 
                    _deviceType, _deviceId, movementDirection ? "UP(1)" : "DOWN(0)");
            }
            
            // Handle movement status feedback - actual device status when starting/stopping
            else if (e.Destination == addresses.MovementStatusFeedback)
            {
                var isMoving = e.Value.AsBoolean();
                
                // Update activity state through dynamic access
                owner.IsActive = isMoving;
                owner.LastUpdated = DateTime.Now;
                
                _logger.LogInformation("{DeviceType} {DeviceId} movement status changed: {Status}", 
                    _deviceType, _deviceId, isMoving ? "STARTED" : "STOPPED");
            }
        }

        /// <summary>
        /// Waits for the shutter to become active (start moving)
        /// </summary>
        public async Task<bool> WaitForActiveAsync(TimeSpan? timeout = null)
        {
            return await WaitForConditionAsync(
                () => owner.IsActive,
                timeout ?? _defaultTimeout,
                "movement to start"
            );
        }

        /// <summary>
        /// Waits for the shutter to become inactive (stop moving)
        /// </summary>
        public async Task<bool> WaitForInactiveAsync(TimeSpan? timeout = null)
        {
            return await WaitForConditionAsync(
                () => !owner.IsActive,
                timeout ?? _defaultTimeout,
                "movement to stop"
            );
        }

        internal async Task<bool> ReadActivityStatusAsync()
        {
            return await _knxService.RequestGroupValue<bool>(addresses.MovementStatusFeedback);
        }
    }
}
