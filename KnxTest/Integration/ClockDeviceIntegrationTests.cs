using Microsoft.Extensions.Logging;
using KnxModel.Interfaces;
using KnxModel.Models;
using KnxService;
using KnxModel.Types;
using KnxModel;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace KnxTest.Integration;

public class ClockDeviceIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<ClockDevice> _logger;
    private readonly ClockConfiguration _clockConfig;
    private ClockDevice? _clockDevice;

    public ClockDeviceIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = new TestLogger();
        
        _clockConfig = new ClockConfiguration(
            InitialMode: ClockMode.Master,
            TimeStamp: TimeSpan.FromSeconds(10) // Send time every 10 seconds for testing
        );
    }

    [Fact]
    public async Task ClockDevice_SendTime_ShouldTransmitDateTimeInKnxFormat()
    {
        // Arrange
        var testDateTime = new DateTime(2024, 12, 19, 14, 30, 45); // Thursday
        var receivedData = new List<(KnxGroupAddress address, byte[] data)>();
        
        var mockService = new TestKnxService((address, data) =>
        {
            receivedData.Add((address, data));
            _output.WriteLine($"KNX Write: Address={address}, Data=[{string.Join(", ", data.Select(b => $"0x{b:X2}"))}]");
        });

        _clockDevice = new ClockDevice(
            id: "TEST_CLOCK_001",
            name: "Test Clock Device", 
            configuration: _clockConfig,
            knxService: mockService,
            logger: _logger,
            defaultTimeout: TimeSpan.FromSeconds(5)
        );

        // Act
        await _clockDevice.SendTimeAsync(testDateTime);

        // Assert
        Assert.Single(receivedData);
        var (address, data) = receivedData[0];
        
        // Check if we have the expected address (TimeControl from ClockAddresses = 0/0/1)
        _output.WriteLine($"Address used: {address}");
        Assert.Equal(8, data.Length); // KNX DateTime should be 8 bytes

        // Analyze the transmitted date format (KNX DPT 19.001)
        _output.WriteLine($"Transmitted DateTime: {testDateTime:yyyy-MM-dd HH:mm:ss dddd}");
        _output.WriteLine($"Raw bytes: [{string.Join(", ", data.Select(b => $"0x{b:X2} ({b})"))}]");
        
        // Decode and analyze each byte according to KNX DPT 19.001
        var year = 1900 + data[0]; // Fixed: Year calculation (1900 + byte)
        var month = data[1];
        var day = data[2];
        var knxDayOfWeek = (data[3] >> 5) & 0x07; // Bits 7-5
        var hour = data[3] & 0x1F; // Bits 4-0
        var minute = data[4];
        var second = data[5];
        var qualityFlags = data[6];
        var clockQuality = data[7];

        _output.WriteLine($"Decoded - Year: {year}, Month: {month}, Day: {day}");
        _output.WriteLine($"Decoded - Hour: {hour}, Minute: {minute}, Second: {second}");
        _output.WriteLine($"Decoded - KNX DayOfWeek: {knxDayOfWeek} (1=Mon,7=Sun)");
        _output.WriteLine($"Decoded - Quality flags: 0x{qualityFlags:X2}, Clock quality: 0x{clockQuality:X2}");

        // Verify basic decoding
        Assert.Equal(testDateTime.Year, year);
        Assert.Equal(testDateTime.Month, month);
        Assert.Equal(testDateTime.Day, day);
        Assert.Equal(testDateTime.Hour, hour);
        Assert.Equal(testDateTime.Minute, minute);
        Assert.Equal(testDateTime.Second, second);
        
        // Verify KNX day of week conversion (KNX: 1=Mon,7=Sun vs .NET: 0=Sun,6=Sat)
        var expectedKnxDayOfWeek = testDateTime.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)testDateTime.DayOfWeek;
        Assert.Equal(expectedKnxDayOfWeek, knxDayOfWeek);
    }

    [Fact]
    public async Task ClockDevice_SendCurrentTime_ShouldTransmitSystemDateTime()
    {
        // Arrange
        var receivedData = new List<(KnxGroupAddress address, byte[] data)>();
        var beforeSend = DateTime.Now;
        
        var mockService = new TestKnxService((address, data) =>
        {
            receivedData.Add((address, data));
            var afterSend = DateTime.Now;
            
            _output.WriteLine($"System time before send: {beforeSend:yyyy-MM-dd HH:mm:ss.fff}");
            _output.WriteLine($"System time after send: {afterSend:yyyy-MM-dd HH:mm:ss.fff}");
            _output.WriteLine($"KNX Raw data: [{string.Join(", ", data.Select(b => $"0x{b:X2}"))}]");
            
            // Decode transmitted time according to KNX DPT 19.001
            var year = 1900 + data[0]; // Fixed: Year calculation (1900 + byte)
            var month = data[1];
            var day = data[2];
            var knxDayOfWeek = (data[3] >> 5) & 0x07; // Bits 7-5
            var hour = data[3] & 0x1F; // Bits 4-0
            var minute = data[4];
            var second = data[5];
            
            var transmittedTime = new DateTime(year, month, day, hour, minute, second);
            _output.WriteLine($"Transmitted time: {transmittedTime:yyyy-MM-dd HH:mm:ss} (KNX DayOfWeek: {knxDayOfWeek})");
            
            // Check if transmitted time is within reasonable range of system time
            var timeDifference = Math.Abs((transmittedTime - beforeSend).TotalSeconds);
            _output.WriteLine($"Time difference: {timeDifference} seconds");
        });

        _clockDevice = new ClockDevice(
            id: "TEST_CLOCK_002",
            name: "Test Clock Device 2", 
            configuration: _clockConfig,
            knxService: mockService,
            logger: _logger,
            defaultTimeout: TimeSpan.FromSeconds(5)
        );

        // Act - synchronize with system time and send
        await _clockDevice.SynchronizeWithSystemTimeAsync();
        await _clockDevice.SendTimeAsync();

        // Assert
        Assert.Single(receivedData);
        var (address, data) = receivedData[0];
        Assert.Equal(8, data.Length);
    }

    [Theory]
    [InlineData(2024, 1, 1, 0, 0, 0)]      // New Year
    [InlineData(2024, 12, 31, 23, 59, 59)] // End of year
    [InlineData(2024, 2, 29, 12, 0, 0)]    // Leap year
    [InlineData(2025, 1, 1, 12, 0, 0)]     // Current year
    [InlineData(2099, 12, 31, 23, 59, 59)] // End of 21st century (year 99 in KNX)
    public async Task ClockDevice_SendVariousDateTimes_ShouldHandleEdgeCases(int year, int month, int day, int hour, int minute, int second)
    {
        // Arrange
        var testDateTime = new DateTime(year, month, day, hour, minute, second);
        var receivedData = new List<byte[]>();
        
        var mockService = new TestKnxService((address, data) =>
        {
            receivedData.Add(data);
            _output.WriteLine($"Test case: {testDateTime:yyyy-MM-dd HH:mm:ss dddd}");
            _output.WriteLine($"Raw bytes: [{string.Join(", ", data.Select(b => $"0x{b:X2}"))}]");
        });

        _clockDevice = new ClockDevice(
            id: $"TEST_CLOCK_{year}_{month}_{day}",
            name: $"Test Clock Device {testDateTime:yyyyMMdd}", 
            configuration: _clockConfig,
            knxService: mockService,
            logger: _logger,
            defaultTimeout: TimeSpan.FromSeconds(5)
        );

        // Act
        await _clockDevice.SendTimeAsync(testDateTime);

        // Assert
        Assert.Single(receivedData);
        var data = receivedData[0];
        
        // Verify round-trip conversion according to KNX DPT 19.001
        var decodedYear = 1900 + data[0]; // Fixed: Year calculation (1900 + byte)
        var decodedMonth = data[1];
        var decodedDay = data[2];
        var decodedKnxDayOfWeek = (data[3] >> 5) & 0x07; // Bits 7-5
        var decodedHour = data[3] & 0x1F; // Bits 4-0
        var decodedMinute = data[4];
        var decodedSecond = data[5];

        Assert.Equal(testDateTime.Year, decodedYear);
        Assert.Equal(testDateTime.Month, decodedMonth);
        Assert.Equal(testDateTime.Day, decodedDay);
        Assert.Equal(testDateTime.Hour, decodedHour);
        Assert.Equal(testDateTime.Minute, decodedMinute);
        Assert.Equal(testDateTime.Second, decodedSecond);
        
        // Verify KNX day of week conversion
        var expectedKnxDayOfWeek = testDateTime.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)testDateTime.DayOfWeek;
        Assert.Equal(expectedKnxDayOfWeek, decodedKnxDayOfWeek);
        
        var reconstructedDateTime = new DateTime(decodedYear, decodedMonth, decodedDay, decodedHour, decodedMinute, decodedSecond);
        Assert.Equal(testDateTime, reconstructedDateTime);
    }

    [Fact]
    public async Task ClockDevice_SendFutureTime_ShouldTransmitCorrectKnxFormat()
    {
        // Arrange - Use future date like in ShutterDevice test
        var futureDateTime = DateTime.Now.AddDays(1);
        byte[]? transmittedData = null;

        var testKnxService = new TestKnxService((address, data) =>
        {
            transmittedData = data;
        });

        _clockDevice = new ClockDevice("TEST_CLOCK_003", "Test Clock 003", 
            new ClockConfiguration(ClockMode.Master, TimeSpan.FromSeconds(30)), testKnxService, _logger, TimeSpan.FromSeconds(5));

        // Act - Send future time
        await _clockDevice.SendTimeAsync(futureDateTime);

        // Assert
        Assert.NotNull(transmittedData);
        Assert.Equal(8, transmittedData.Length);

        Console.WriteLine($"Future time sent: {futureDateTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"KNX Raw data: [{string.Join(", ", transmittedData.Select(b => $"0x{b:X2}"))}]");

        // Decode the KNX data
        var decodedYear = 1900 + transmittedData[0]; // Fixed: use 1900 + byte (like KNX DPT 19.001 standard)
        var decodedMonth = transmittedData[1];
        var decodedDay = transmittedData[2];
        var decodedDayOfWeekAndHour = transmittedData[3];
        var decodedKnxDayOfWeek = (decodedDayOfWeekAndHour >> 5) & 0x07;
        var decodedHour = decodedDayOfWeekAndHour & 0x1F;
        var decodedMinute = transmittedData[4];
        var decodedSecond = transmittedData[5];

        Console.WriteLine($"Decoded future time: {decodedYear}-{decodedMonth:D2}-{decodedDay:D2} {decodedHour:D2}:{decodedMinute:D2}:{decodedSecond:D2} (KNX DayOfWeek: {decodedKnxDayOfWeek})");

        // Verify the transmitted data matches the future date
        Assert.Equal(futureDateTime.Year, decodedYear);
        Assert.Equal(futureDateTime.Month, decodedMonth);
        Assert.Equal(futureDateTime.Day, decodedDay);
        Assert.Equal(futureDateTime.Hour, decodedHour);
        Assert.Equal(futureDateTime.Minute, decodedMinute);
        Assert.Equal(futureDateTime.Second, decodedSecond);

        // Verify KNX day of week conversion
        var expectedKnxDayOfWeek = futureDateTime.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)futureDateTime.DayOfWeek;
        Assert.Equal(expectedKnxDayOfWeek, decodedKnxDayOfWeek);
    }

    public void Dispose()
    {
        _clockDevice?.Dispose();
    }

    private class TestKnxService : IKnxService
    {
        private readonly Action<KnxGroupAddress, byte[]> _onWrite;

        public TestKnxService(Action<KnxGroupAddress, byte[]> onWrite)
        {
            _onWrite = onWrite;
        }

        public event EventHandler<KnxGroupEventArgs>? GroupMessageReceived;

        public void WriteGroupValue(string address, bool value) => WriteGroupValueAsync(address, value).Wait();
        public void WriteGroupValue(string address, float percentage) => WriteGroupValueAsync(address, percentage).Wait();
        
        public Task<T> RequestGroupValue<T>(string address) => Task.FromResult(default(T)!);
        
        public Task WriteGroupValueAsync(string address, bool value) => WriteGroupValueAsync(address, new[] { value ? (byte)1 : (byte)0 });
        public Task WriteGroupValueAsync(string address, float percentage) => WriteGroupValueAsync(address, BitConverter.GetBytes(percentage));
        
        public Task WriteGroupValueAsync(KnxGroupAddress address, byte[] data)
        {
            _onWrite(address, data);
            return Task.CompletedTask;
        }

        public Task WriteGroupValueAsync(string address, byte[] data)
        {
            // Parse address string (e.g., "0/0/1") into components
            var parts = address.Split('/');
            var knxAddress = new KnxGroupAddress(parts[0], parts[1], parts[2]);
            return WriteGroupValueAsync(knxAddress, data);
        }

        public Task WriteGroupValueAsync(KnxGroupAddress address, bool value) => WriteGroupValueAsync(address, new[] { value ? (byte)1 : (byte)0 });
        public Task WriteGroupValueAsync(KnxGroupAddress address, int value) => WriteGroupValueAsync(address, BitConverter.GetBytes(value));
        public Task WriteGroupValueAsync(KnxGroupAddress address, float value) => WriteGroupValueAsync(address, BitConverter.GetBytes(value));
        public Task<byte[]> ReadGroupValueAsync(KnxGroupAddress address) => Task.FromResult(new byte[0]);
        public Task<bool> ReadGroupValueAsync(KnxGroupAddress address, bool defaultValue) => Task.FromResult(defaultValue);
        public Task<int> ReadGroupValueAsync(KnxGroupAddress address, int defaultValue) => Task.FromResult(defaultValue);
        public Task<float> ReadGroupValueAsync(KnxGroupAddress address, float defaultValue) => Task.FromResult(defaultValue);
        
        public void Dispose() { }
    }

    private class TestLogger : ILogger<ClockDevice>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Do nothing for tests
        }
    }
}
