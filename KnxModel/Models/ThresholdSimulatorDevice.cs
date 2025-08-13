using Microsoft.Extensions.Logging;
using KnxModel.Models;

namespace KnxModel.Models
{
    /// <summary>
    /// Threshold Simulator Device - acts as a Master device that can simulate 
    /// threshold conditions by sending appropriate KNX telegrams
    /// Similar to ClockDevice in Master mode but for threshold simulation
    /// </summary>
    public class ThresholdSimulatorDevice : IKnxDeviceBase, IDisposable
    {
        private readonly IKnxService _knxService;
        private readonly ILogger<ThresholdSimulatorDevice> _logger;
        private readonly TimeSpan _defaultTimeout;
        private DateTime _lastUpdated = DateTime.MinValue;

        // Threshold addresses - these are the common addresses used by shutters
        private readonly string _brightnessThreshold1Address = KnxAddressConfiguration.CreateBrightnessThreshold1Address();
        private readonly string _brightnessThreshold2Address = KnxAddressConfiguration.CreateBrightnessThreshold2Address();
        private readonly string _outdoorTemperatureThresholdAddress = KnxAddressConfiguration.CreateOutdoorTemperatureThresholdAddress();
        
        // Brightness threshold monitoring device block address
        private readonly string _brightnessThresholdMonitoringBlockAddress = KnxAddressConfiguration.CreateBrightnessThresholdMonitoringBlockAddress();

        // Current simulated states
        private bool _brightnessThreshold1State = false;
        private bool _brightnessThreshold2State = false;
        private bool _outdoorTemperatureThresholdState = false;
        private bool _brightnessThresholdMonitoringBlocked = false;

        // Saved state for testing
        private bool? _savedBrightnessThreshold1State;
        private bool? _savedBrightnessThreshold2State;
        private bool? _savedOutdoorTemperatureThresholdState;
        private bool? _savedBrightnessThresholdMonitoringBlocked;

        public ThresholdSimulatorDevice(
            string id, 
            string name, 
            IKnxService knxService, 
            ILogger<ThresholdSimulatorDevice> logger, 
            TimeSpan defaultTimeout)
        {
            Id = id;
            Name = name;
            _knxService = knxService ?? throw new ArgumentNullException(nameof(knxService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultTimeout = defaultTimeout;

            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} created", Id);
        }

        #region IKnxDeviceBase Implementation

        public string Id { get; }
        public string Name { get; }
        public string DeviceType => "ThresholdSimulator";
        public string SubGroup => "TestingDevices";
        public DateTime LastUpdated => _lastUpdated;

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing ThresholdSimulatorDevice {DeviceId} ({DeviceName})", Id, Name);

            // Read initial brightness threshold monitoring block state from KNX bus
            try
            {
                _brightnessThresholdMonitoringBlocked = await ReadBrightnessThresholdMonitoringBlockStateAsync();
                _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} read initial brightness threshold monitoring block state: {State}", 
                    Id, _brightnessThresholdMonitoringBlocked ? "BLOCKED" : "UNBLOCKED");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ThresholdSimulatorDevice {DeviceId} failed to read initial brightness threshold monitoring block state, defaulting to UNBLOCKED", Id);
                _brightnessThresholdMonitoringBlocked = false;
            }

            _lastUpdated = DateTime.Now;

            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} initialized as threshold Master - BrightnessMonitoringBlocked: {BrightnessMonitoringBlocked}", 
                Id, _brightnessThresholdMonitoringBlocked ? "BLOCKED" : "UNBLOCKED");
            Console.WriteLine($"ThresholdSimulatorDevice {Id} initialized - ready to simulate thresholds, BrightnessMonitoringBlocked: {(_brightnessThresholdMonitoringBlocked ? "BLOCKED" : "UNBLOCKED")}");
        }

        public void SaveCurrentState()
        {
            _savedBrightnessThreshold1State = _brightnessThreshold1State;
            _savedBrightnessThreshold2State = _brightnessThreshold2State;
            _savedOutdoorTemperatureThresholdState = _outdoorTemperatureThresholdState;
            _savedBrightnessThresholdMonitoringBlocked = _brightnessThresholdMonitoringBlocked;

            _logger.LogDebug("ThresholdSimulatorDevice {DeviceId} state saved - B1: {B1}, B2: {B2}, Temp: {Temp}, Block: {Block}", 
                Id, _savedBrightnessThreshold1State, _savedBrightnessThreshold2State, _savedOutdoorTemperatureThresholdState,
                _savedBrightnessThresholdMonitoringBlocked);
        }

        public async Task RestoreSavedStateAsync(TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? _defaultTimeout;

            if (_savedBrightnessThreshold1State.HasValue)
            {
                await SetBrightnessThreshold1StateAsync(_savedBrightnessThreshold1State.Value);
            }

            if (_savedBrightnessThreshold2State.HasValue)
            {
                await SetBrightnessThreshold2StateAsync(_savedBrightnessThreshold2State.Value);
            }

            if (_savedOutdoorTemperatureThresholdState.HasValue)
            {
                await SetOutdoorTemperatureThresholdStateAsync(_savedOutdoorTemperatureThresholdState.Value);
            }

            if (_savedBrightnessThresholdMonitoringBlocked.HasValue)
            {
                await SetBrightnessThresholdMonitoringBlockStateAsync(_savedBrightnessThresholdMonitoringBlocked.Value);
            }

            _lastUpdated = DateTime.Now;
            _logger.LogDebug("ThresholdSimulatorDevice {DeviceId} state restored", Id);
        }

        #endregion

        #region Threshold Simulation Properties

        /// <summary>
        /// Current simulated brightness threshold 1 state
        /// </summary>
        public bool BrightnessThreshold1Active => _brightnessThreshold1State;

        /// <summary>
        /// Current simulated brightness threshold 2 state
        /// </summary>
        public bool BrightnessThreshold2Active => _brightnessThreshold2State;

        /// <summary>
        /// Current simulated outdoor temperature threshold state
        /// </summary>
        public bool OutdoorTemperatureThresholdActive => _outdoorTemperatureThresholdState;

        /// <summary>
        /// Current brightness threshold monitoring device block state
        /// </summary>
        public bool BrightnessThresholdMonitoringBlocked => _brightnessThresholdMonitoringBlocked;

        #endregion

        #region Threshold Control Methods

        /// <summary>
        /// Simulates brightness threshold 1 being exceeded (true) or not exceeded (false)
        /// Sends KNX telegram to 0/2/3
        /// </summary>
        public async Task SetBrightnessThreshold1StateAsync(bool exceeded)
        {
            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} setting brightness threshold 1 to {State}", 
                Id, exceeded ? "EXCEEDED" : "NOT_EXCEEDED");

            await _knxService.WriteGroupValueAsync(_brightnessThreshold1Address, exceeded);
            _brightnessThreshold1State = exceeded;
            _lastUpdated = DateTime.Now;

            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} brightness threshold 1 set to {State}", 
                Id, exceeded ? "EXCEEDED" : "NOT_EXCEEDED");
            Console.WriteLine($"🌞 ThresholdSimulator {Id} brightness threshold 1: {(exceeded ? "EXCEEDED" : "NOT_EXCEEDED")}");
        }

        /// <summary>
        /// Simulates brightness threshold 2 being exceeded (true) or not exceeded (false)
        /// Sends KNX telegram to 0/2/4
        /// </summary>
        public async Task SetBrightnessThreshold2StateAsync(bool exceeded)
        {
            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} setting brightness threshold 2 to {State}", 
                Id, exceeded ? "EXCEEDED" : "NOT_EXCEEDED");

            await _knxService.WriteGroupValueAsync(_brightnessThreshold2Address, exceeded);
            _brightnessThreshold2State = exceeded;
            _lastUpdated = DateTime.Now;

            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} brightness threshold 2 set to {State}", 
                Id, exceeded ? "EXCEEDED" : "NOT_EXCEEDED");
            Console.WriteLine($"☀️ ThresholdSimulator {Id} brightness threshold 2: {(exceeded ? "EXCEEDED" : "NOT_EXCEEDED")}");
        }

        /// <summary>
        /// Simulates outdoor temperature threshold being exceeded (true) or not exceeded (false)
        /// Sends KNX telegram to 0/2/7
        /// </summary>
        public async Task SetOutdoorTemperatureThresholdStateAsync(bool exceeded)
        {
            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} setting temperature threshold to {State}", 
                Id, exceeded ? "EXCEEDED" : "NOT_EXCEEDED");

            await _knxService.WriteGroupValueAsync(_outdoorTemperatureThresholdAddress, exceeded);
            _outdoorTemperatureThresholdState = exceeded;
            _lastUpdated = DateTime.Now;

            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} temperature threshold set to {State}", 
                Id, exceeded ? "EXCEEDED" : "NOT_EXCEEDED");
            Console.WriteLine($"🌡️ ThresholdSimulator {Id} temperature threshold: {(exceeded ? "EXCEEDED" : "NOT_EXCEEDED")}");
        }

        #endregion

        #region Brightness Threshold Monitoring Device Block Control

        /// <summary>
        /// Blocks the real brightness threshold monitoring device
        /// Sends KNX telegram to 0/2/12 with value true
        /// No automatic feedback - requires manual status request
        /// </summary>
        public async Task BlockBrightnessThresholdMonitoringAsync()
        {
            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} blocking brightness threshold monitoring device", Id);

            await _knxService.WriteGroupValueAsync(_brightnessThresholdMonitoringBlockAddress, true);
            _brightnessThresholdMonitoringBlocked = true;
            _lastUpdated = DateTime.Now;

            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} brightness threshold monitoring blocked", Id);
            Console.WriteLine($"🚫 ThresholdSimulator {Id} brightness threshold monitoring: BLOCKED");
        }

        /// <summary>
        /// Unblocks the real brightness threshold monitoring device
        /// Sends KNX telegram to 0/2/12 with value false
        /// No automatic feedback - requires manual status request
        /// </summary>
        public async Task UnblockBrightnessThresholdMonitoringAsync()
        {
            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} unblocking brightness threshold monitoring device", Id);

            await _knxService.WriteGroupValueAsync(_brightnessThresholdMonitoringBlockAddress, false);
            _brightnessThresholdMonitoringBlocked = false;
            _lastUpdated = DateTime.Now;

            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} brightness threshold monitoring unblocked", Id);
            Console.WriteLine($"✅ ThresholdSimulator {Id} brightness threshold monitoring: UNBLOCKED");
        }

        /// <summary>
        /// Sets the brightness threshold monitoring device block state
        /// </summary>
        /// <param name="blocked">True to block, false to unblock</param>
        public async Task SetBrightnessThresholdMonitoringBlockStateAsync(bool blocked)
        {
            if (blocked)
            {
                await BlockBrightnessThresholdMonitoringAsync();
            }
            else
            {
                await UnblockBrightnessThresholdMonitoringAsync();
            }
        }

        /// <summary>
        /// Reads the current brightness threshold monitoring device block state
        /// Sends a request to 0/2/12 since there is no automatic feedback
        /// </summary>
        public async Task<bool> ReadBrightnessThresholdMonitoringBlockStateAsync()
        {
            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} reading brightness threshold monitoring block state", Id);
            
            try
            {
                var blockState = await _knxService.RequestGroupValue<bool>(_brightnessThresholdMonitoringBlockAddress);
                _brightnessThresholdMonitoringBlocked = blockState;
                _lastUpdated = DateTime.Now;
                
                _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} brightness threshold monitoring block state: {State}", 
                    Id, blockState ? "BLOCKED" : "UNBLOCKED");
                
                return blockState;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ThresholdSimulatorDevice {DeviceId} failed to read brightness threshold monitoring block state", Id);
                throw;
            }
        }

        #endregion

        #region Scenario Simulation Methods

        /// <summary>
        /// Simulates no thresholds exceeded (clear sunny day, comfortable temperature)
        /// </summary>
        public async Task SimulateNormalConditionsAsync()
        {
            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} simulating normal conditions (no thresholds exceeded)", Id);
            
            await SetBrightnessThreshold1StateAsync(false);
            await Task.Delay(100); // Small delay between telegrams
            await SetBrightnessThreshold2StateAsync(false);
            await Task.Delay(100);
            await SetOutdoorTemperatureThresholdStateAsync(false);

            Console.WriteLine($"🌤️ ThresholdSimulator {Id} simulated normal conditions - all thresholds clear");
        }

        /// <summary>
        /// Simulates only brightness threshold 1 exceeded (moderate brightness)
        /// </summary>
        public async Task SimulateModerateBrightnessAsync()
        {
            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} simulating moderate brightness (threshold 1 exceeded)", Id);
            
            await SetBrightnessThreshold1StateAsync(true);
            await Task.Delay(100);
            await SetBrightnessThreshold2StateAsync(false);
            await Task.Delay(100);
            await SetOutdoorTemperatureThresholdStateAsync(false);

            Console.WriteLine($"🌅 ThresholdSimulator {Id} simulated moderate brightness - threshold 1 exceeded");
        }

        /// <summary>
        /// Simulates both brightness thresholds exceeded (high brightness)
        /// </summary>
        public async Task SimulateHighBrightnessAsync()
        {
            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} simulating high brightness (both brightness thresholds exceeded)", Id);
            
            await SetBrightnessThreshold1StateAsync(true);
            await Task.Delay(100);
            await SetBrightnessThreshold2StateAsync(true);
            await Task.Delay(100);
            await SetOutdoorTemperatureThresholdStateAsync(false);

            Console.WriteLine($"☀️ ThresholdSimulator {Id} simulated high brightness - both brightness thresholds exceeded");
        }

        /// <summary>
        /// Simulates maximum sun protection conditions (all thresholds exceeded)
        /// </summary>
        public async Task SimulateMaximumSunProtectionAsync()
        {
            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} simulating maximum sun protection (all thresholds exceeded)", Id);
            
            await SetBrightnessThreshold1StateAsync(true);
            await Task.Delay(100);
            await SetBrightnessThreshold2StateAsync(true);
            await Task.Delay(100);
            await SetOutdoorTemperatureThresholdStateAsync(true);

            Console.WriteLine($"🔥 ThresholdSimulator {Id} simulated maximum sun protection - all thresholds exceeded");
        }

        /// <summary>
        /// Simulates custom threshold combination
        /// </summary>
        public async Task SimulateCustomThresholdStatesAsync(bool brightness1, bool brightness2, bool temperature)
        {
            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} simulating custom thresholds: B1={B1}, B2={B2}, Temp={Temp}", 
                Id, brightness1, brightness2, temperature);
            
            await SetBrightnessThreshold1StateAsync(brightness1);
            await Task.Delay(100);
            await SetBrightnessThreshold2StateAsync(brightness2);
            await Task.Delay(100);
            await SetOutdoorTemperatureThresholdStateAsync(temperature);

            Console.WriteLine($"🎛️ ThresholdSimulator {Id} simulated custom thresholds: B1={brightness1}, B2={brightness2}, Temp={temperature}");
        }

        /// <summary>
        /// Simulates complete testing isolation by blocking real threshold monitoring and setting simulated states
        /// This gives full control over threshold conditions for testing
        /// </summary>
        public async Task SimulateTestingIsolationAsync(bool brightness1 = false, bool brightness2 = false, bool temperature = false)
        {
            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} entering testing isolation mode", Id);
            
            // First block the real brightness threshold monitoring device
            await BlockBrightnessThresholdMonitoringAsync();
            await Task.Delay(200);
            
            // Then set the desired simulated threshold states
            await SimulateCustomThresholdStatesAsync(brightness1, brightness2, temperature);

            Console.WriteLine($"🔒 ThresholdSimulator {Id} entered TESTING ISOLATION mode - real monitoring blocked, simulated states set");
        }

        /// <summary>
        /// Exits testing isolation by unblocking real threshold monitoring and clearing simulated states
        /// </summary>
        public async Task ExitTestingIsolationAsync()
        {
            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} exiting testing isolation mode", Id);
            
            // Clear simulated threshold states first
            await SimulateNormalConditionsAsync();
            await Task.Delay(200);
            
            // Then unblock the real brightness threshold monitoring device
            await UnblockBrightnessThresholdMonitoringAsync();

            Console.WriteLine($"🔓 ThresholdSimulator {Id} exited TESTING ISOLATION mode - real monitoring unblocked");
        }

        #endregion

        #region Address Configuration

        /// <summary>
        /// Override default brightness threshold 1 address (default: 0/2/3)
        /// </summary>
        public ThresholdSimulatorDevice WithBrightnessThreshold1Address(string address)
        {
            // This would require changing the field to non-readonly, or implementing via configuration
            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} brightness threshold 1 address would be set to {Address}", Id, address);
            return this;
        }

        /// <summary>
        /// Override default brightness threshold 2 address (default: 0/2/4)
        /// </summary>
        public ThresholdSimulatorDevice WithBrightnessThreshold2Address(string address)
        {
            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} brightness threshold 2 address would be set to {Address}", Id, address);
            return this;
        }

        /// <summary>
        /// Override default temperature threshold address (default: 0/2/7)
        /// </summary>
        public ThresholdSimulatorDevice WithTemperatureThresholdAddress(string address)
        {
            _logger.LogInformation("ThresholdSimulatorDevice {DeviceId} temperature threshold address would be set to {Address}", Id, address);
            return this;
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            // No resources to dispose for this simple simulator
            // Override in derived classes if needed
        }

        #endregion
    }
}
