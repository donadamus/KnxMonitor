using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KnxModel;
using KnxTest.Integration.Base;
using KnxTest.Integration.Interfaces;
using Xunit;

namespace KnxTest.Integration
{
    /// <summary>
    /// Integration tests for Light devices using new architecture
    /// Inherits from DeviceTestBase and implements ILockableDeviceTests interface
    /// </summary>
    [Collection("KnxService collection")]
    public class LightIntegrationTests : IntegrationTestBaseNew, ILockableDeviceTests, ISwitchableDeviceTests
    {
        private readonly LockTestHelper _lockTestHelper;
        private readonly SwitchTestHelper _switchTestHelper;
        private ILightDevice? _device;

        public LightIntegrationTests(KnxServiceFixture fixture) : base(fixture)
        {
            _lockTestHelper = new LockTestHelper();
            _switchTestHelper = new SwitchTestHelper();
        }

        // Data source for tests - only pure lights (not dimmers)
        public static IEnumerable<object[]> LightIdsFromConfig
        {
            get
            {
                var config = LightFactory.LightConfigurations;
                return config.Where(x => x.Value.Name.ToLower().Contains("off"))
                            .Select(k => new object[] { k.Key });
            }
        }

        // ===== DEVICE INITIALIZATION =====

        private async Task InitializeDevice(string deviceId, bool saveCurrentState = true)
        {
            Console.WriteLine($"🆕 Creating new LightDevice {deviceId}");
            _device = LightFactory.CreateLight(deviceId, _knxService);
            await _device.InitializeAsync();
            if (saveCurrentState)
            {
                _device.SaveCurrentState();
            }

            Console.WriteLine($"Light {deviceId} initialized - Switch: {_device.CurrentSwitchState}, Lock: {_device.CurrentLockState}");
        }

        #region ILockableDeviceTests Implementation

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task CanLockAndUnlock(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.CanLockAndUnlock(_device!);

            await Task.CompletedTask;
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task LockPreventsStateChanges(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.LockPreventsStateChange(_device!);

            await Task.CompletedTask;
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task CanReadLockState(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.CanReadLockState(_device!);

            await Task.CompletedTask;
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task SwitchableDeviceTurnOffWhenLocked(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.SwitchableDeviceTurnOffWhenLocked(_device!);

            await Task.CompletedTask;
        }

        #endregion

        #region Light-Specific Tests

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task CanTurnOnAndTurnOff(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            
            // Ensure device is unlocked before testing switch functionality
            await _lockTestHelper.EnsureDeviceIsUnlockedBeforeTest(_device!);

            // Act & Assert - Test switch functionality
            await _switchTestHelper.CanTurnOnAndTurnOff(_device!);

            await Task.CompletedTask;
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task CanToggleSwitch(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            // Ensure device is unlocked before testing toggle functionality
            await _lockTestHelper.EnsureDeviceIsUnlockedBeforeTest(_device!);

            // Act & Assert - Check toggle functionality
            await _switchTestHelper.CanToggleSwitch(_device!);

            await Task.CompletedTask;
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task CanReadSwitchState(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act - Read state
            var state = await _device!.ReadSwitchStateAsync();

            // Assert
            state.Should().NotBe(Switch.Unknown, $"Light {deviceId} should return valid state");
            _device.CurrentSwitchState.Should().Be(state, "Current state should match read state");
            _device.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1),
                "LastUpdated should be recent after reading state");

            Console.WriteLine($"✅ Light {deviceId} state read successfully: {state}");

            await Task.CompletedTask;
        }

        #endregion

        #region Cleanup

        public override void Dispose()
        {
            try
            {
                if (_device != null)
                    _device.RestoreSavedStateAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to restore device state during cleanup: {ex.Message}");
            }
            finally
            {
                _device?.Dispose();
                base.Dispose();
            }
        }
        #endregion
    }
}
