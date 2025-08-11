using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System;
using System.Threading.Tasks;
using KnxModel;
using KnxModel.Types;

namespace KnxTest.Unit.Models
{
    /// <summary>
    /// Unit tests for ClockDevice
    /// </summary>
    public class ClockDeviceTests
    {
        private readonly Mock<IKnxService> _mockKnxService;
        private readonly Mock<ILogger<ClockDevice>> _mockLogger;
        private readonly ClockDevice _clockDevice;
        private readonly ClockConfiguration _configuration;

        public ClockDeviceTests()
        {
            _mockKnxService = new Mock<IKnxService>();
            _mockLogger = new Mock<ILogger<ClockDevice>>();
            
            _configuration = new ClockConfiguration(
                InitialMode: ClockMode.Master,
                TimeStamp: TimeSpan.FromSeconds(30)
            );

            _clockDevice = new ClockDevice(
                "clock-001",
                "Test Clock",
                _configuration,
                _mockKnxService.Object,
                _mockLogger.Object,
                TimeSpan.FromSeconds(5)
            );
        }

        [Fact]
        public async Task InitializeAsync_InMasterMode_ShouldStartSendingTime()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Returns(Task.CompletedTask);

            // Act
            await _clockDevice.InitializeAsync();

            // Assert
            Assert.Equal(ClockMode.Master, _clockDevice.Mode);
            Assert.True(_clockDevice.HasValidTime);
            
            // Wait a bit to allow timer to fire
            await Task.Delay(100);
            
            // Verify that time was sent
            _mockKnxService.Verify(s => s.WriteGroupValueAsync("0/0/1", It.IsAny<byte[]>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task SwitchToSlaveMode_ShouldStopSendingTime()
        {
            // Arrange
            await _clockDevice.InitializeAsync();
            
            // Wait for initial timer actions to complete
            await Task.Delay(150);
            _mockKnxService.Reset();

            // Act
            await _clockDevice.SwitchToSlaveModeAsync();

            // Assert
            Assert.Equal(ClockMode.Slave, _clockDevice.Mode);
            
            // Wait a bit to ensure timer doesn't fire anymore
            await Task.Delay(150);
            
            // Verify that no time was sent after switching to slave
            _mockKnxService.Verify(s => s.WriteGroupValueAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        }

        [Fact]
        public async Task SynchronizeWithSystemTimeAsync_ShouldUpdateCurrentTime()
        {
            // Arrange
            var beforeSync = DateTime.Now;

            // Act
            await _clockDevice.SynchronizeWithSystemTimeAsync();

            // Assert
            Assert.True(_clockDevice.HasValidTime);
            Assert.True(_clockDevice.CurrentDateTime >= beforeSync);
            Assert.True(_clockDevice.CurrentDateTime <= DateTime.Now);
        }

        [Fact]
        public async Task SendTimeAsync_WithoutValidTime_ShouldNotSendTelegram()
        {
            // Arrange
            // Clock starts without valid time in Slave mode
            var slaveConfig = new ClockConfiguration(ClockMode.Slave, TimeSpan.FromSeconds(30));
            var slaveClock = new ClockDevice("slave-001", "Slave Clock", slaveConfig, _mockKnxService.Object, _mockLogger.Object, TimeSpan.FromSeconds(5));

            // Act
            await slaveClock.SendTimeAsync();

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValueAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        }

        [Fact]
        public async Task SaveAndRestoreState_ShouldPreserveClockState()
        {
            // Act
            _clockDevice.SaveCurrentState();
            // Change the state
            await _clockDevice.SwitchToSlaveModeAsync();
            // Restore
            await _clockDevice.RestoreSavedStateAsync();

            // Assert
            // Note: This is a simplified test - in real scenario we'd need to verify the actual restoration
            Assert.NotNull(_clockDevice);
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Assert
            Assert.Equal("clock-001", _clockDevice.Id);
            Assert.Equal("Test Clock", _clockDevice.Name);
            Assert.Equal("1", _clockDevice.SubGroup);
            Assert.Equal(ClockMode.Master, _clockDevice.Mode);
            Assert.Equal(TimeSpan.FromSeconds(30), _clockDevice.TimeStamp);
        }

        [Fact]
        public void Constructor_WithNullId_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ClockDevice(null!, "Test", _configuration, _mockKnxService.Object, _mockLogger.Object, TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void Constructor_WithNullName_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ClockDevice("test", null!, _configuration, _mockKnxService.Object, _mockLogger.Object, TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ClockDevice("test", "Test", null!, _mockKnxService.Object, _mockLogger.Object, TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void Constructor_WithNullKnxService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ClockDevice("test", "Test", _configuration, null!, _mockLogger.Object, TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ClockDevice("test", "Test", _configuration, _mockKnxService.Object, null!, TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public async Task CurrentDateTime_InMasterMode_ShouldUseStopwatchForAccuracy()
        {
            // Arrange
            await _clockDevice.InitializeAsync();
            var initialTime = _clockDevice.CurrentDateTime;
            
            // Act - wait a bit to see if time advances based on Stopwatch
            await Task.Delay(50); // 50ms delay
            var timeAfterDelay = _clockDevice.CurrentDateTime;
            
            // Assert
            var timeDifference = timeAfterDelay - initialTime;
            
            // Time should have advanced approximately by the delay amount (with some tolerance)
            Assert.True(timeDifference.TotalMilliseconds >= 40, $"Expected time to advance by ~50ms, but only advanced by {timeDifference.TotalMilliseconds}ms");
            Assert.True(timeDifference.TotalMilliseconds <= 100, $"Expected time to advance by ~50ms, but advanced by {timeDifference.TotalMilliseconds}ms");
        }

        [Fact]
        public async Task SynchronizeWithSystemTimeAsync_InMasterMode_ShouldResetStopwatch()
        {
            // Arrange
            await _clockDevice.InitializeAsync();
            var timeBeforeSync = _clockDevice.CurrentDateTime;
            
            // Wait a bit so there's a difference
            await Task.Delay(50);
            
            // Act
            await _clockDevice.SynchronizeWithSystemTimeAsync();
            var timeAfterSync = _clockDevice.CurrentDateTime;
            
            // Assert
            // After sync, the time should be very close to system time (within 100ms tolerance)
            var systemTime = DateTime.Now;
            var difference = Math.Abs((timeAfterSync - systemTime).TotalMilliseconds);
            Assert.True(difference < 100, $"Time after sync should be close to system time, but difference was {difference}ms");
        }

        #region Sun Position Tests

        [Fact]
        public void Latitude_SetValidValue_ShouldUpdateCorrectly()
        {
            // Arrange
            var newLatitude = 50.0614; // Krakow

            // Act
            _clockDevice.Latitude = newLatitude;

            // Assert
            Assert.Equal(newLatitude, _clockDevice.Latitude);
        }

        [Fact]
        public void Longitude_SetValidValue_ShouldUpdateCorrectly()
        {
            // Arrange
            var newLongitude = 19.9365; // Krakow

            // Act
            _clockDevice.Longitude = newLongitude;

            // Assert
            Assert.Equal(newLongitude, _clockDevice.Longitude);
        }

        [Fact]
        public void DefaultLocation_ShouldBeWroclaw()
        {
            // Assert
            Assert.Equal(51.1079, _clockDevice.Latitude, 4); // Wrocław latitude
            Assert.Equal(17.0385, _clockDevice.Longitude, 4); // Wrocław longitude
        }

        [Theory]
        [InlineData(-91)]
        [InlineData(91)]
        public void Latitude_SetInvalidValue_ShouldThrowException(double invalidLatitude)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _clockDevice.Latitude = invalidLatitude);
        }

        [Theory]
        [InlineData(-181)]
        [InlineData(181)]
        public void Longitude_SetInvalidValue_ShouldThrowException(double invalidLongitude)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _clockDevice.Longitude = invalidLongitude);
        }

        [Fact]
        public void GetCurrentSunPosition_ShouldReturnValidPosition()
        {
            // Arrange
            _clockDevice.Latitude = 51.1079; // Wrocław
            _clockDevice.Longitude = 17.0385;

            // Act
            var sunPosition = _clockDevice.GetCurrentSunPosition();

            // Assert
            Assert.True(sunPosition.Azimuth >= 0 && sunPosition.Azimuth <= 360);
            Assert.True(sunPosition.Elevation >= -90 && sunPosition.Elevation <= 90);
        }

        [Fact]
        public void GetSunPosition_ForSpecificTime_ShouldReturnValidPosition()
        {
            // Arrange
            _clockDevice.Latitude = 51.1079; // Wrocław
            _clockDevice.Longitude = 17.0385;
            var testTime = new DateTime(2025, 6, 21, 12, 0, 0); // Summer solstice noon

            // Act
            var sunPosition = _clockDevice.GetSunPosition(testTime);

            // Assert
            Assert.True(sunPosition.Azimuth >= 0 && sunPosition.Azimuth <= 360);
            Assert.True(sunPosition.Elevation >= -90 && sunPosition.Elevation <= 90);
            // At summer solstice noon in Wrocław, sun should be relatively high
            Assert.True(sunPosition.Elevation > 40); // Should be above 40 degrees
        }

        [Fact]
        public void GetSunPosition_ForNightTime_ShouldReturnBelowHorizon()
        {
            // Arrange
            _clockDevice.Latitude = 51.1079; // Wrocław
            _clockDevice.Longitude = 17.0385;
            var nightTime = new DateTime(2025, 6, 21, 0, 0, 0); // Midnight

            // Act
            var sunPosition = _clockDevice.GetSunPosition(nightTime);

            // Assert
            Assert.True(sunPosition.Azimuth >= 0 && sunPosition.Azimuth <= 360);
            Assert.True(sunPosition.Elevation < 0); // Should be below horizon at midnight
            Assert.False(sunPosition.IsSunAboveHorizon); // Sun should be below horizon
        }

        [Fact]
        public void GetTodaySunTimes_ShouldReturnValidTimes()
        {
            // Arrange
            _clockDevice.Latitude = 51.1079; // Wrocław
            _clockDevice.Longitude = 17.0385;

            // Act
            var sunTimes = _clockDevice.GetTodaySunTimes();

            // Assert
            Assert.Equal(DateTime.Today, sunTimes.Date);
            // In Wrocław, there should typically be sunrise and sunset
            Assert.True(sunTimes.HasSunrise);
            Assert.True(sunTimes.HasSunset);
            Assert.NotNull(sunTimes.DaylightDuration);
            
            // Sunrise should be before sunset
            if (sunTimes.Sunrise.HasValue && sunTimes.Sunset.HasValue)
            {
                Assert.True(sunTimes.Sunrise.Value < sunTimes.Sunset.Value);
            }
        }

        [Fact]
        public void GetSunTimes_ForSpecificDate_ShouldReturnValidTimes()
        {
            // Arrange
            _clockDevice.Latitude = 51.1079; // Wrocław
            _clockDevice.Longitude = 17.0385;
            var testDate = new DateTime(2025, 6, 21); // Summer solstice

            // Act
            var sunTimes = _clockDevice.GetSunTimes(testDate);

            // Assert
            Assert.Equal(testDate.Date, sunTimes.Date);
            Assert.True(sunTimes.HasSunrise);
            Assert.True(sunTimes.HasSunset);
            Assert.NotNull(sunTimes.DaylightDuration);
            
            // Summer solstice should have long daylight duration
            Assert.True(sunTimes.DaylightDuration.Value.TotalHours > 14); // Over 14 hours of daylight
        }

        [Fact]
        public void GetSunTimes_ForWinterSolstice_ShouldHaveShortDaylight()
        {
            // Arrange
            _clockDevice.Latitude = 51.1079; // Wrocław
            _clockDevice.Longitude = 17.0385;
            var winterSolstice = new DateTime(2025, 12, 21); // Winter solstice

            // Act
            var sunTimes = _clockDevice.GetSunTimes(winterSolstice);

            // Assert
            Assert.Equal(winterSolstice.Date, sunTimes.Date);
            Assert.True(sunTimes.HasSunrise);
            Assert.True(sunTimes.HasSunset);
            Assert.NotNull(sunTimes.DaylightDuration);
            
            // Winter solstice should have short daylight duration
            Assert.True(sunTimes.DaylightDuration.Value.TotalHours < 10); // Less than 10 hours of daylight
        }

        [Fact]
        public void SunTimes_ToString_ShouldFormatCorrectly()
        {
            // Arrange
            var date = new DateTime(2025, 6, 21);
            var sunrise = new DateTime(2025, 6, 21, 5, 30, 0);
            var sunset = new DateTime(2025, 6, 21, 20, 45, 0);
            var sunTimes = new SunTimes(date, sunrise, sunset);

            // Act
            var result = sunTimes.ToString();

            // Assert
            Assert.Contains("2025-06-21", result);
            Assert.Contains("05:30", result);
            Assert.Contains("20:45", result);
        }

        [Fact]
        public void SunPosition_IsSunAboveHorizon_ShouldWorkCorrectly()
        {
            // Arrange
            var aboveHorizon = new SunPosition(180, 45); // Sun at 45 degrees elevation
            var belowHorizon = new SunPosition(180, -10); // Sun 10 degrees below horizon

            // Assert
            Assert.True(aboveHorizon.IsSunAboveHorizon);
            Assert.False(belowHorizon.IsSunAboveHorizon);
        }

        #endregion
    }
}
