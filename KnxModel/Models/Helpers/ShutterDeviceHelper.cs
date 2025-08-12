using Microsoft.Extensions.Logging;

namespace KnxModel.Models.Helpers
{
    /// <summary>
    /// Helper class for shutter-specific operations (Open, Close, Stop)
    /// Handles movement control commands and activity state management
    /// </summary>
    public class ShutterDeviceHelper<T> : DeviceHelperBase<T>
        where T : IKnxDeviceBase
    {
        private readonly Func<ShutterAddresses> _getAddresses;
        private readonly Action<bool> _updateActivity;
        private readonly Func<DateTime> _getLastUpdated;
        private readonly Action _updateLastUpdated;
        private readonly Func<Lock> _getCurrentLockState;
        private readonly Func<Task> _unlockAsync;
        private readonly ILogger<T> logger;

        public ShutterDeviceHelper(T owner,
            IKnxService knxService,
            string deviceId,
            string deviceType,
            Func<ShutterAddresses> getAddresses,
            Action<bool> updateActivity,
            Func<DateTime> getLastUpdated,
            Action updateLastUpdated,
            Func<Lock> getCurrentLockState,
            Func<Task> unlockAsync,
            ILogger<T> logger,
            TimeSpan defaultTimeout) : base(owner, knxService, deviceId, deviceType, logger, defaultTimeout)
        {
            _getAddresses = getAddresses ?? throw new ArgumentNullException(nameof(getAddresses));
            _updateActivity = updateActivity ?? throw new ArgumentNullException(nameof(updateActivity));
            _getLastUpdated = getLastUpdated ?? throw new ArgumentNullException(nameof(getLastUpdated));
            _updateLastUpdated = updateLastUpdated ?? throw new ArgumentNullException(nameof(updateLastUpdated));
            _getCurrentLockState = getCurrentLockState ?? throw new ArgumentNullException(nameof(getCurrentLockState));
            _unlockAsync = unlockAsync ?? throw new ArgumentNullException(nameof(unlockAsync));
            this.logger = logger;
        }


        /// <summary>
        /// Opens the shutter using UP command (MovementControl = 1)
        /// Device will echo confirmation on MovementFeedback (offset +100)
        /// and send actual movement status on MovementStatusFeedback
        /// </summary>
        internal async Task OpenAsync(TimeSpan? timeout = null)
        {
            // Unlock before opening if necessary
            if (_getCurrentLockState() == Lock.On)
            {
                await _unlockAsync();
            }
            
            var addresses = _getAddresses();
            
            // Send UP command (1) to MovementControl
            // Device will echo on MovementFeedback and send status on MovementStatusFeedback
            logger.LogInformation("{DeviceType} {DeviceId} sending UP command (1)", _deviceType, _deviceId);
            await _knxService.WriteGroupValueAsync(addresses.MovementControl, true);
            
            _updateLastUpdated();
            
            logger.LogInformation("{DeviceType} {DeviceId} UP command sent", _deviceType, _deviceId);
            Console.WriteLine($"{_deviceType} {_deviceId} UP command sent");
        }

        /// <summary>
        /// Closes the shutter using DOWN command (MovementControl = 0)
        /// Device will echo confirmation on MovementFeedback (offset +100)
        /// and send actual movement status on MovementStatusFeedback
        /// </summary>
        internal async Task CloseAsync(TimeSpan? timeout = null)
        {
            // Unlock before closing if necessary
            if (_getCurrentLockState() == Lock.On)
            {
                await _unlockAsync();
            }
            
            var addresses = _getAddresses();
            
            // Send DOWN command (0) to MovementControl
            // Device will echo on MovementFeedback and send status on MovementStatusFeedback
            logger.LogInformation("{DeviceType} {DeviceId} sending DOWN command (0)", _deviceType, _deviceId);
            await _knxService.WriteGroupValueAsync(addresses.MovementControl, false);
            
            _updateLastUpdated();
            
            logger.LogInformation("{DeviceType} {DeviceId} DOWN command sent", _deviceType, _deviceId);
            Console.WriteLine($"{_deviceType} {_deviceId} DOWN command sent");
        }

        /// <summary>
        /// Stops the shutter movement using StopControl trigger
        /// Device will send status update on MovementStatusFeedback when stopped
        /// </summary>
        internal async Task StopAsync(TimeSpan? timeout = null)
        {
            var addresses = _getAddresses();
            
            // Send KNX stop trigger to StopControl address
            // Device will respond with movement status on MovementStatusFeedback
            logger.LogInformation("{DeviceType} {DeviceId} sending STOP trigger", _deviceType, _deviceId);
            await _knxService.WriteGroupValueAsync(addresses.StopControl, true);
            
            _updateLastUpdated();
            
            logger.LogInformation("{DeviceType} {DeviceId} STOP trigger sent", _deviceType, _deviceId);
            Console.WriteLine($"{_deviceType} {_deviceId} STOP trigger sent");
        }

        /// <summary>
        /// Processes incoming KNX messages for movement feedback and status updates
        /// MovementFeedback: Echo from device confirming UP(1)/DOWN(0) command (offset +100)
        /// MovementStatusFeedback: Device status when starting/stopping movement
        /// </summary>
        internal void ProcessMovementMessage(KnxGroupEventArgs e)
        {
            var addresses = _getAddresses();
            
            // Handle movement control feedback - echo from device confirming UP/DOWN command (offset +100)
            if (e.Destination == addresses.MovementFeedback)
            {
                var movementDirection = e.Value.AsBoolean();
                // This is just confirmation echo, device will send actual status on MovementStatusFeedback
                _updateLastUpdated();
                
                logger.LogInformation("{DeviceType} {DeviceId} movement command confirmed: {Direction}", 
                    _deviceType, _deviceId, movementDirection ? "UP(1)" : "DOWN(0)");
                Console.WriteLine($"{_deviceType} {_deviceId} movement command confirmed: {(movementDirection ? "UP(1)" : "DOWN(0)")}");
            }
            
            // Handle movement status feedback - actual device status when starting/stopping
            else if (e.Destination == addresses.MovementStatusFeedback)
            {
                var isMoving = e.Value.AsBoolean();
                _updateActivity(isMoving); // Update activity based on actual movement status
                _updateLastUpdated();
                
                logger.LogInformation("{DeviceType} {DeviceId} movement status changed: {Status}", 
                    _deviceType, _deviceId, isMoving ? "STARTED" : "STOPPED");
                Console.WriteLine($"{_deviceType} {_deviceId} movement status: {(isMoving ? "STARTED" : "STOPPED")}");
            }
        }

        /// <summary>
        /// Waits for the shutter to become active (start moving)
        /// </summary>
        public async Task<bool> WaitForActiveAsync(TimeSpan? timeout = null)
        {
            return await WaitForConditionAsync(
                () => ReadActivityStatus(),
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
                () => !ReadActivityStatus(),
                timeout ?? _defaultTimeout,
                "movement to stop"
            );
        }

        /// <summary>
        /// Reads current activity status (mock implementation for now)
        /// In real implementation this would read from KNX MovementStatusFeedback
        /// </summary>
        private bool ReadActivityStatus()
        {
            // TODO: Read from KNX bus - MovementStatusFeedback address
            // For now, return false as we don't have real feedback
            // var isMoving = await _knxService.RequestGroupValue<bool>(addresses.MovementStatusFeedback);
            
            // This is a mock - in reality we'd need to track the actual state
            // For now assume not moving
            return false;
        }
    }
}
