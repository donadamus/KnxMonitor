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
    public class LightIntegrationTests : DeviceTestBaseNew, ILockableDeviceTests
    {
        private readonly LockTestHelper _lockTestHelper;
        private ILightDevice _device = default;

        public LightIntegrationTests(KnxServiceFixture fixture) : base(fixture)
        {
            _lockTestHelper = new LockTestHelper();
        }

        // Data source for tests - only pure lights (not dimmers)
        public static IEnumerable<object[]> LightIdsFromConfig
        {
            get
            {
                var config = LightFactory.LightConfigurations;
                return config.Where(x => x.Value.Name.ToLower().Contains("of"))
                            .Select(k => new object[] { k.Key });
            }
        }

        // ===== DEVICE INITIALIZATION =====

        private async Task InitializeDevice(string deviceId)
        {
            _device = LightFactory.CreateLight(deviceId, _knxService);
            await _device.InitializeAsync();
            
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
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task LockPreventsStateChanges(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.LockPreventsStateChange(_device!);
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task CanReadLockState(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.CanReadLockState(_device!);
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task DeviceAutoOffWhenLocked(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.DeviceAutoOffWhenLocked(_device!);
        }

        #endregion

        #region Light-Specific Tests

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task CanTurnOnAndOff(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            await _lockTestHelper.EnsureDeviceIsUnlockedBeforeTest(_device!);

            // Test ON
            await _device!.TurnOnAsync();
            _device.CurrentSwitchState.Should().Be(Switch.On, $"Light {deviceId} should be ON");
            Console.WriteLine($"✅ Light {deviceId} turned ON successfully");

            // Test OFF
            await _device.TurnOffAsync();
            _device.CurrentSwitchState.Should().Be(Switch.Off, $"Light {deviceId} should be OFF");
            Console.WriteLine($"✅ Light {deviceId} turned OFF successfully");
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task CanToggle(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            await _lockTestHelper.EnsureDeviceIsUnlockedBeforeTest(_device!);
            var initialState = _device!.CurrentSwitchState;

            // Act & Assert - Toggle to opposite
            await _device.ToggleAsync();
            _device.CurrentSwitchState.Should().Be(initialState.Opposite(), 
                $"Light {deviceId} should toggle to opposite state");

            // Act & Assert - Toggle back
            await _device.ToggleAsync();
            _device.CurrentSwitchState.Should().Be(initialState, 
                $"Light {deviceId} should toggle back to original state");

            Console.WriteLine($"✅ Light {deviceId} toggle functionality works correctly");
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task CanReadState(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act - Read state
            var state = await _device.ReadSwitchStateAsync();

            // Assert
            state.Should().NotBe(Switch.Unknown, $"Light {deviceId} should return valid state");
            _device.CurrentSwitchState.Should().Be(state, "Current state should match read state");
            _device.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1),
                "LastUpdated should be recent after reading state");

            Console.WriteLine($"✅ Light {deviceId} state read successfully: {state}");
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task CanSaveAndRestoreState(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            await _lockTestHelper.EnsureDeviceIsUnlockedBeforeTest(_device!);

            // Set specific state
            await _device!.TurnOnAsync();
            _device.CurrentSwitchState.Should().Be(Switch.On, "Light should be ON before saving");

            // Act - Save state
            _device.SaveCurrentState();
            Switch? savedState = null;

            // Change state
            await _device.TurnOffAsync();
            _device.CurrentSwitchState.Should().Be(Switch.Off, "Light should be OFF after changing state");

            // Act - Restore state
            await _device.RestoreSavedStateAsync();

            // Assert
            _device.CurrentSwitchState.Should().Be(savedState ?? Switch.On, "Light should be restored to saved state");

            Console.WriteLine($"✅ Light {deviceId} save and restore works correctly");
        }

        #endregion

        #region Cleanup

        public override async ValueTask DisposeAsync()
        {
            try
            {
                if (_device != null)
                    await _device.RestoreSavedStateAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to restore device state during cleanup: {ex.Message}");
            }
            finally
            {
                _device?.Dispose();
            }
        }

        #endregion
    }
}
