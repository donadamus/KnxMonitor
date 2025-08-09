using Microsoft.Extensions.Logging;

namespace KnxModel.Models.Helpers
{
    /// <summary>
    /// Helper class for shutter-specific operations (Open, Close, Stop)
    /// Handles movement control commands and activity state management
    /// </summary>
    public class ShutterDeviceHelper<T> : DeviceHelperBase<T>
        where T : class
    {
        private readonly Func<ShutterAddresses> _getAddresses;
        private readonly Action<bool> _updateActivity;
        private readonly Func<DateTime> _getLastUpdated;
        private readonly Action _updateLastUpdated;
        private readonly Func<Lock> _getCurrentLockState;
        private readonly Func<Task> _unlockAsync;
        private readonly ILogger<T> logger;

        public ShutterDeviceHelper(
            IKnxService knxService,
            string deviceId,
            string deviceType,
            Func<ShutterAddresses> getAddresses,
            Action<bool> updateActivity,
            Func<DateTime> getLastUpdated,
            Action updateLastUpdated,
            Func<Lock> getCurrentLockState,
            Func<Task> unlockAsync,
            ILogger<T> logger) : base(knxService, deviceId, deviceType, logger)
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
        /// Waits for the required cooldown period between shutter commands.
        /// Physical shutters need a minimum 2-second delay between commands to prevent
        /// timing-based position tracking synchronization issues.
        /// </summary>
        private async Task WaitForCooldownAsync()
        {
            var elapsed = DateTime.Now - _getLastUpdated();

            if (elapsed < TimeSpan.FromSeconds(2) && elapsed > TimeSpan.Zero)
            {
                await Task.Delay(TimeSpan.FromSeconds(2) - elapsed);
            }
        }

        /// <summary>
        /// Opens the shutter using UP command (MovementControl = true)
        /// More reliable than percentage control for physical shutters
        /// </summary>
        internal async Task OpenAsync(TimeSpan? timeout = null)
        {
            await WaitForCooldownAsync();
            
            // Unlock before opening if necessary
            if (_getCurrentLockState() == Lock.On)
            {
                await _unlockAsync();
            }
            
            var addresses = _getAddresses();
            
            // Send UP command (true) to MovementControl instead of percentage 0%
            // This is more reliable for physical shutters that use timing-based positioning
            logger.LogInformation("{DeviceType} {DeviceId} sending UP command (open)", _deviceType, _deviceId);
            await _knxService.WriteGroupValueAsync(addresses.MovementControl, true);
            
            // Update internal state - opening means moving towards 0%
            _updateActivity(true);
            _updateLastUpdated();
            
            logger.LogInformation("{DeviceType} {DeviceId} opened with UP command", _deviceType, _deviceId);
            Console.WriteLine($"{_deviceType} {_deviceId} opened with UP command");
        }

        /// <summary>
        /// Closes the shutter using DOWN command (MovementControl = false)
        /// More reliable than percentage control for physical shutters
        /// </summary>
        internal async Task CloseAsync(TimeSpan? timeout = null)
        {
            await WaitForCooldownAsync();
            
            // Unlock before closing if necessary
            if (_getCurrentLockState() == Lock.On)
            {
                await _unlockAsync();
            }
            
            var addresses = _getAddresses();
            
            // Send DOWN command (false) to MovementControl instead of percentage 100%
            // This is more reliable for physical shutters that use timing-based positioning
            logger.LogInformation("{DeviceType} {DeviceId} sending DOWN command (close)", _deviceType, _deviceId);
            await _knxService.WriteGroupValueAsync(addresses.MovementControl, false);
            
            // Update internal state - closing means moving towards 100%
            _updateActivity(true);
            _updateLastUpdated();
            
            logger.LogInformation("{DeviceType} {DeviceId} closed with DOWN command", _deviceType, _deviceId);
            Console.WriteLine($"{_deviceType} {_deviceId} closed with DOWN command");
        }

        /// <summary>
        /// Stops the shutter movement using StopControl command
        /// </summary>
        internal async Task StopAsync(TimeSpan? timeout = null)
        {
            var addresses = _getAddresses();
            
            // Send KNX stop command to StopControl address
            logger.LogInformation("{DeviceType} {DeviceId} sending STOP command", _deviceType, _deviceId);
            await _knxService.WriteGroupValueAsync(addresses.StopControl, true);
            
            // Update internal state - movement stopped
            _updateActivity(false);
            _updateLastUpdated();
            
            logger.LogInformation("{DeviceType} {DeviceId} stopped", _deviceType, _deviceId);
            Console.WriteLine($"{_deviceType} {_deviceId} stopped");
        }

        /// <summary>
        /// Processes incoming KNX messages for movement feedback and status updates
        /// </summary>
        internal void ProcessMovementMessage(KnxGroupEventArgs e)
        {
            var addresses = _getAddresses();
            
            // Handle movement control feedback (UP/DOWN commands feedback)
            if (e.Destination == addresses.MovementFeedback)
            {
                var movementDirection = e.Value.AsBoolean();
                _updateActivity(true); // Movement started
                _updateLastUpdated();
                
                logger.LogInformation("{DeviceType} {DeviceId} movement feedback received: {Direction}", 
                    _deviceType, _deviceId, movementDirection ? "UP" : "DOWN");
                Console.WriteLine($"{_deviceType} {_deviceId} movement feedback: {(movementDirection ? "UP" : "DOWN")}");
            }
            
            // Handle movement status feedback (is device currently moving?)
            else if (e.Destination == addresses.MovementStatusFeedback)
            {
                var isMoving = e.Value.AsBoolean();
                _updateActivity(isMoving);
                _updateLastUpdated();
                
                logger.LogInformation("{DeviceType} {DeviceId} movement status feedback: {Status}", 
                    _deviceType, _deviceId, isMoving ? "MOVING" : "STOPPED");
                Console.WriteLine($"{_deviceType} {_deviceId} movement status: {(isMoving ? "MOVING" : "STOPPED")}");
            }
        }
    }
}
