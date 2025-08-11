using KnxModel.Models.Helpers;
using KnxModel.Interfaces;
using KnxModel.Types;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CoordinateSharp;

namespace KnxModel
{
    /// <summary>
    /// Implementation of a Clock device for KNX time synchronization
    /// Supports Master, Slave, and Slave/Master modes with sun position calculation
    /// </summary>
    public class ClockDevice : IClockDevice, ISunPositionProvider, IDisposable
    {
        private readonly KnxEventManager _eventManager;
        private readonly IKnxService _knxService;
        private readonly ILogger<ClockDevice> _logger;
        private readonly ClockConfiguration _configuration;
        private readonly ClockAddresses _addresses;
        private readonly TimeSpan _defaultTimeout;

        // Device state
        private ClockMode _currentMode;
        private DateTime _currentDateTime;
        private DateTime? _lastTimeReceived;
        private DateTime _lastUpdated = DateTime.MinValue;
        private bool _hasValidTime = false;

        // Geographic location for sun position calculation
        private double _latitude = 51.1079; // Default: Wrocław, Poland
        private double _longitude = 17.0385;

        // Master mode timing
        private Timer? _timeTransmissionTimer;
        private Timer? _slaveMasterModeTimer;
        private Stopwatch? _masterModeStopwatch;
        private DateTime _masterModeStartTime;

        // Saved state for testing
        private ClockMode? _savedMode;
        private DateTime? _savedDateTime;
        private bool? _savedHasValidTime;

        public ClockDevice(string id, string name, ClockConfiguration configuration, IKnxService knxService, ILogger<ClockDevice> logger, TimeSpan defaultTimeout, double latitude = 51.1079, double longitude = 17.0385)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SubGroup = "1"; // Clock devices use fixed sub group
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultTimeout = defaultTimeout;

            // Set geographic location
            _latitude = latitude;
            _longitude = longitude;

            // Fixed address for clock synchronization
            _addresses = KnxAddressConfiguration.CreateClockAddresses();

            // Initialize mode and time
            _currentMode = configuration.InitialMode;
            _currentDateTime = DateTime.MinValue;

            // Initialize event manager
            _eventManager = new KnxEventManager(_knxService, Id, "ClockDevice");
            _eventManager.MessageReceived += OnKnxMessageReceived;
        }

        #region IKnxDeviceBase Implementation

        public string Id { get; }
        public string Name { get; }
        public string SubGroup { get; }
        public DateTime LastUpdated => _lastUpdated;

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing ClockDevice {DeviceId} ({DeviceName}) in {Mode} mode", Id, Name, _currentMode);

            _lastUpdated = DateTime.Now;

            switch (_currentMode)
            {
                case ClockMode.Master:
                    await SwitchToMasterModeAsync();
                    break;

                case ClockMode.Slave:
                    await SwitchToSlaveModeAsync();
                    break;

                case ClockMode.SlaveMaster:
                    await SwitchToSlaveModeAsync();
                    // Start timer to switch to Master if no time received
                    StartSlaveMasterModeTimer();
                    break;
            }

            _logger.LogInformation("ClockDevice {DeviceId} initialized in {Mode} mode", Id, _currentMode);
            Console.WriteLine($"ClockDevice {Id} initialized in {_currentMode} mode");
        }

        public void SaveCurrentState()
        {
            _savedMode = _currentMode;
            _savedDateTime = _currentDateTime;
            _savedHasValidTime = _hasValidTime;

            _logger.LogDebug("ClockDevice {DeviceId} state saved - Mode: {Mode}, Time: {Time}, HasValidTime: {HasValidTime}", 
                Id, _savedMode, _savedDateTime, _savedHasValidTime);
        }

        public async Task RestoreSavedStateAsync(TimeSpan? timeout = null)
        {
            if (_savedMode.HasValue && _savedMode.Value != _currentMode)
            {
                _currentMode = _savedMode.Value;
                switch (_currentMode)
                {
                    case ClockMode.Master:
                        await SwitchToMasterModeAsync();
                        break;
                    case ClockMode.Slave:
                        await SwitchToSlaveModeAsync();
                        break;
                    case ClockMode.SlaveMaster:
                        await SwitchToSlaveModeAsync();
                        StartSlaveMasterModeTimer();
                        break;
                }
            }

            if (_savedDateTime.HasValue)
            {
                _currentDateTime = _savedDateTime.Value;
            }

            if (_savedHasValidTime.HasValue)
            {
                _hasValidTime = _savedHasValidTime.Value;
            }

            _lastUpdated = DateTime.Now;
            _logger.LogDebug("ClockDevice {DeviceId} state restored - Mode: {Mode}, Time: {Time}, HasValidTime: {HasValidTime}", 
                Id, _currentMode, _currentDateTime, _hasValidTime);
        }

        #endregion

        #region IClockDevice Implementation

        public ClockMode Mode => _currentMode;
        public DateTime CurrentDateTime 
        { 
            get
            {
                // In Master mode, calculate current time based on elapsed time
                if (_currentMode == ClockMode.Master && _masterModeStopwatch != null && _hasValidTime)
                {
                    var elapsedTime = _masterModeStopwatch.Elapsed;
                    return _masterModeStartTime.Add(elapsedTime);
                }
                return _currentDateTime;
            }
        }
        public TimeSpan TimeStamp => _configuration.TimeStamp;
        public DateTime? LastTimeReceived => _lastTimeReceived;
        public bool HasValidTime => _hasValidTime;

        public async Task SendTimeAsync()
        {
            if (!_hasValidTime)
            {
                _logger.LogWarning("ClockDevice {DeviceId} cannot send time - no valid time available", Id);
                return;
            }

            _logger.LogDebug("ClockDevice {DeviceId} sending time: {Time}", Id, _currentDateTime);

            // Convert DateTime to KNX datetime format (8 bytes)
            var timeBytes = ConvertDateTimeToKnxBytes(_currentDateTime);
            await _knxService.WriteGroupValueAsync(_addresses.TimeControl, timeBytes);

            _logger.LogInformation("ClockDevice {DeviceId} time sent: {Time}", Id, _currentDateTime);
            Console.WriteLine($"ClockDevice {Id} time sent: {_currentDateTime:yyyy-MM-dd HH:mm:ss}");
        }

        public async Task SynchronizeWithSystemTimeAsync()
        {
            _currentDateTime = DateTime.Now;
            _hasValidTime = true;
            _lastUpdated = DateTime.Now;

            // If in Master mode, reset the stopwatch to restart timing from this point
            if (_currentMode == ClockMode.Master)
            {
                _masterModeStartTime = _currentDateTime;
                _masterModeStopwatch?.Restart();
            }

            _logger.LogInformation("ClockDevice {DeviceId} synchronized with system time: {Time}", Id, _currentDateTime);
            Console.WriteLine($"ClockDevice {Id} synchronized with system time: {_currentDateTime:yyyy-MM-dd HH:mm:ss}");

            await Task.CompletedTask;
        }

        public async Task SwitchToMasterModeAsync()
        {
            StopAllTimers();

            _currentMode = ClockMode.Master;

            // If no valid time, get from system
            if (!_hasValidTime)
            {
                await SynchronizeWithSystemTimeAsync();
            }

            // Initialize timing for Master mode
            _masterModeStartTime = _currentDateTime;
            _masterModeStopwatch = Stopwatch.StartNew();

            // Start time transmission timer
            _timeTransmissionTimer = new Timer(OnTimeTransmissionTimer, null, TimeSpan.Zero, _configuration.TimeStamp);

            _logger.LogInformation("ClockDevice {DeviceId} switched to Master mode", Id);
            Console.WriteLine($"ClockDevice {Id} switched to Master mode");
        }

        public async Task SwitchToSlaveModeAsync()
        {
            StopAllTimers();

            _currentMode = ClockMode.Slave;
            _lastTimeReceived = null;

            _logger.LogInformation("ClockDevice {DeviceId} switched to Slave mode", Id);
            Console.WriteLine($"ClockDevice {Id} switched to Slave mode");

            await Task.CompletedTask;
        }

        #endregion

        #region Event Handling

        private void OnKnxMessageReceived(object? sender, KnxGroupEventArgs e)
        {
            if (e.Destination == _addresses.TimeControl)
            {
                ProcessTimeMessage(e);
            }
        }

        private void ProcessTimeMessage(KnxGroupEventArgs e)
        {
            try
            {
                // Convert KNX datetime format (8 bytes) to DateTime
                var receivedTime = ConvertKnxBytesToDateTime(e.Value.RawData);
                
                _currentDateTime = receivedTime;
                _lastTimeReceived = DateTime.Now;
                _hasValidTime = true;
                _lastUpdated = DateTime.Now;

                _logger.LogInformation("ClockDevice {DeviceId} received time: {Time}", Id, receivedTime);
                Console.WriteLine($"ClockDevice {Id} received time: {receivedTime:yyyy-MM-dd HH:mm:ss}");

                // If in SlaveMaster mode and other device sent time, switch to Slave
                if (_currentMode == ClockMode.SlaveMaster)
                {
                    _logger.LogInformation("ClockDevice {DeviceId} received time from other device, staying in Slave mode", Id);
                    StartSlaveMasterModeTimer(); // Restart the timer
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClockDevice {DeviceId} error processing time message", Id);
            }
        }

        #endregion

        #region Private Methods

        private void OnTimeTransmissionTimer(object? state)
        {
            if (_currentMode == ClockMode.Master && _masterModeStopwatch != null)
            {
                // Calculate current time based on elapsed time since Master mode started
                var elapsedTime = _masterModeStopwatch.Elapsed;
                _currentDateTime = _masterModeStartTime.Add(elapsedTime);
                
                // Send time telegram
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SendTimeAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ClockDevice {DeviceId} error sending time", Id);
                    }
                });
            }
        }

        private void StartSlaveMasterModeTimer()
        {
            StopSlaveMasterModeTimer();

            var timeout = TimeSpan.FromMilliseconds(_configuration.TimeStamp.TotalMilliseconds * 2); // 2x TimeStamp
            _slaveMasterModeTimer = new Timer(OnSlaveMasterModeTimeout, null, timeout, Timeout.InfiniteTimeSpan);

            _logger.LogDebug("ClockDevice {DeviceId} SlaveMaster timer started with timeout: {Timeout}", Id, timeout);
        }

        private void OnSlaveMasterModeTimeout(object? state)
        {
            if (_currentMode == ClockMode.SlaveMaster)
            {
                _logger.LogInformation("ClockDevice {DeviceId} no time received in SlaveMaster mode, switching to Master", Id);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SwitchToMasterModeAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ClockDevice {DeviceId} error switching to Master mode", Id);
                    }
                });
            }
        }

        private void StopAllTimers()
        {
            StopTimeTransmissionTimer();
            StopSlaveMasterModeTimer();
            StopMasterModeStopwatch();
        }

        private void StopTimeTransmissionTimer()
        {
            _timeTransmissionTimer?.Dispose();
            _timeTransmissionTimer = null;
        }

        private void StopSlaveMasterModeTimer()
        {
            _slaveMasterModeTimer?.Dispose();
            _slaveMasterModeTimer = null;
        }

        private void StopMasterModeStopwatch()
        {
            _masterModeStopwatch?.Stop();
            _masterModeStopwatch = null;
        }

        private static byte[] ConvertDateTimeToKnxBytes(DateTime dateTime)
        {
            // Simplified KNX datetime conversion (8 bytes)
            // In real implementation, this would follow KNX DPT 19.001 format
            var bytes = new byte[8];
            
            // Year (2 bytes)
            var year = (ushort)dateTime.Year;
            bytes[0] = (byte)(year >> 8);
            bytes[1] = (byte)(year & 0xFF);
            
            // Month, Day, Hour, Minute, Second, DayOfWeek
            bytes[2] = (byte)dateTime.Month;
            bytes[3] = (byte)dateTime.Day;
            bytes[4] = (byte)dateTime.Hour;
            bytes[5] = (byte)dateTime.Minute;
            bytes[6] = (byte)dateTime.Second;
            bytes[7] = (byte)dateTime.DayOfWeek;

            return bytes;
        }

        private static DateTime ConvertKnxBytesToDateTime(byte[] bytes)
        {
            // Simplified KNX datetime conversion from 8 bytes
            // In real implementation, this would follow KNX DPT 19.001 format
            if (bytes.Length < 8)
                throw new ArgumentException("Invalid KNX datetime format - expected 8 bytes");

            var year = (bytes[0] << 8) | bytes[1];
            var month = bytes[2];
            var day = bytes[3];
            var hour = bytes[4];
            var minute = bytes[5];
            var second = bytes[6];
            // bytes[7] is day of week

            return new DateTime(year, month, day, hour, minute, second);
        }

        #endregion

        #region ISunPositionProvider Implementation

        /// <summary>
        /// Gets or sets the latitude of the device location in degrees
        /// </summary>
        public double Latitude
        {
            get => _latitude;
            set
            {
                if (value < -90 || value > 90)
                    throw new ArgumentOutOfRangeException(nameof(value), "Latitude must be between -90 and 90 degrees");
                _latitude = value;
            }
        }

        /// <summary>
        /// Gets or sets the longitude of the device location in degrees
        /// </summary>
        public double Longitude
        {
            get => _longitude;
            set
            {
                if (value < -180 || value > 180)
                    throw new ArgumentOutOfRangeException(nameof(value), "Longitude must be between -180 and 180 degrees");
                _longitude = value;
            }
        }

        /// <summary>
        /// Gets the current position of the sun based on device location and current time
        /// </summary>
        /// <returns>Current sun position with azimuth and elevation angles</returns>
        public SunPosition GetCurrentSunPosition()
        {
            return GetSunPosition(CurrentDateTime);
        }

        /// <summary>
        /// Gets the sun position for a specific date and time
        /// </summary>
        /// <param name="dateTime">The date and time for which to calculate sun position</param>
        /// <returns>Sun position at the specified time</returns>
        public SunPosition GetSunPosition(DateTime dateTime)
        {
            try
            {
                // Use CoordinateSharp to compute sun position
                var coordinate = new Coordinate(_latitude, _longitude, dateTime);
                
                // Get azimuth and elevation from CoordinateSharp
                var azimuth = coordinate.CelestialInfo.SunAzimuth;
                var elevation = coordinate.CelestialInfo.SunAltitude;

                return new SunPosition(azimuth, elevation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating sun position for ClockDevice {DeviceId} at {DateTime}, Lat: {Latitude}, Lng: {Longitude}", 
                    Id, dateTime, _latitude, _longitude);
                
                // Return default position (sun at horizon facing south) in case of error
                return new SunPosition(180, 0);
            }
        }

        /// <summary>
        /// Gets sunrise and sunset times for today
        /// </summary>
        /// <returns>Sun times for today</returns>
        public SunTimes GetTodaySunTimes()
        {
            return GetSunTimes(DateTime.Today);
        }

        /// <summary>
        /// Gets sunrise and sunset times for a specific date
        /// </summary>
        /// <param name="date">The date for which to calculate sun times</param>
        /// <returns>Sun times for the specified date</returns>
        public SunTimes GetSunTimes(DateTime date)
        {
            try
            {
                // Use CoordinateSharp to compute sunrise/sunset
                var coordinate = new Coordinate(_latitude, _longitude, date.Date);
                
                // Get sunrise and sunset from CoordinateSharp
                var sunriseTime = coordinate.CelestialInfo.SunRise;
                var sunsetTime = coordinate.CelestialInfo.SunSet;

                return new SunTimes(date.Date, sunriseTime, sunsetTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating sun times for ClockDevice {DeviceId} on {Date}, Lat: {Latitude}, Lng: {Longitude}", 
                    Id, date.Date, _latitude, _longitude);
                
                // Return default times (no sunrise/sunset) in case of error
                return new SunTimes(date.Date, null, null);
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            StopAllTimers();
            _eventManager?.Dispose();

            _logger.LogInformation("ClockDevice {DeviceId} disposed", Id);
            Console.WriteLine($"ClockDevice {Id} disposed");
        }

        #endregion
    }
}
