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
    [Collection("KnxService collection")]
    public class OldLightOldIntegrationTests(KnxServiceFixture fixture) : OldLockableDeviceTestBase<ILightOld>(fixture)
    {
        // Data source for tests - only pure lights (not dimmers)
        public static IEnumerable<object[]> LightIdsFromConfig
        {
            get
            {
                var config = LightFactory.LightConfigurations;
                return config.Where(x => !x.Value.Name.ToLower().Contains("dimmer"))
                            .Select(k => new object[] { k.Key });
            }
        }

        // ===== OWN DEVICE MANAGEMENT =====

        protected override async Task InitializeDevice(string deviceId)
        {
            _device = LightFactory.CreateLightOld(deviceId, _knxService);
            await _device.InitializeAsync();
            
            Console.WriteLine($"Light {deviceId} initialized - Switch: {_device.CurrentState.Switch}, Lock: {_device.CurrentState.Lock}");
        }

        #region ILockableDeviceTests Implementation

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task CanLockAndUnlock(string deviceId)
        {
            await AssertCanLockAndUnlock(deviceId);
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task LockPreventsStateChanges(string deviceId)
        {
            await AssertLockPreventsStateChanges(deviceId);
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task CanReadLockState(string deviceId)
        {
            await AssertCanReadLockState(deviceId);
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task SwitchableDeviceTurnOffWhenLocked(string deviceId)
        {
            await AssertSwitchableDeviceTurnOffWhenLocked(deviceId);
        }

        #endregion

        #region Light-Specific Switch Control Tests

        //[Theory]
        //[MemberData(nameof(LightIdsFromConfig))]
        //public virtual async Task OK_CanInitializeAndReadState(string deviceId)
        //{
        //    // Act
        //    await InitializeDevice(deviceId);

        //    // Assert
        //    _device.CurrentState.Should().NotBeNull($"Light {deviceId} should have valid current state");
        //    _device.CurrentState.Switch.Should().NotBe(Switch.Unknown, $"Light {deviceId} should have known switch state");
        //    _device.CurrentState.Lock.Should().NotBe(Lock.Unknown, $"Light {deviceId} should have known lock state");
        //    _device.CurrentState.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1),
        //        $"Light {deviceId} should have recent LastUpdated time");

        //    Console.WriteLine($"✅ Light {deviceId} initialized successfully - Switch: {_device.CurrentState.Switch}, Lock: {_device.CurrentState.Lock}");
        //}

        //[Theory]
        //[MemberData(nameof(LightIdsFromConfig))]
        //public virtual async Task OK_CanTurnOnAndOff(string deviceId)
        //{
        //    // Arrange
        //    await InitializeDevice(deviceId);
        //    await _lockTestHelper.EnsureDeviceIsUnlocked(_device);

        //    // Test ON
        //    await _device.TurnOnAsync();
        //    _device.CurrentState.Switch.Should().Be(Switch.On, $"Light {deviceId} should be ON");
        //    Console.WriteLine($"✅ Light {deviceId} turned ON successfully");

        //    // Test OFF
        //    await _device.TurnOffAsync();
        //    _device.CurrentState.Switch.Should().Be(Switch.Off, $"Light {deviceId} should be OFF");
        //    Console.WriteLine($"✅ Light {deviceId} turned OFF successfully");
        //}

        //[Theory]
        //[MemberData(nameof(LightIdsFromConfig))]
        //public virtual async Task OK_CanToggle(string deviceId)
        //{
        //    // Arrange
        //    await InitializeDevice(deviceId);
        //    await _lockTestHelper.EnsureDeviceIsUnlocked(_device);
        //    var initialState = _device.CurrentState.Switch;

        //    // Act & Assert - Toggle to opposite
        //    await _device.ToggleAsync();
        //    _device.CurrentState.Switch.Should().Be(initialState.Opposite(), 
        //        $"Light {deviceId} should toggle to opposite state");

        //    // Act & Assert - Toggle back
        //    await _device.ToggleAsync();
        //    _device.CurrentState.Switch.Should().Be(initialState, 
        //        $"Light {deviceId} should toggle back to original state");

        //    Console.WriteLine($"✅ Light {deviceId} toggle functionality works correctly");
        //}

        //[Theory]
        //[MemberData(nameof(LightIdsFromConfig))]
        //public virtual async Task OK_CanReadFeedbackAndCurrentStateIsUpdated(string deviceId)
        //{
        //    // Arrange
        //    await InitializeDevice(deviceId);

        //    // Act - Read state
        //    var state = await _device.ReadStateAsync();

        //    // Assert
        //    state.Should().NotBe(Switch.Unknown, $"Light {deviceId} should return valid state");
        //    _device.CurrentState.Switch.Should().Be(state, "Current state should match read state");
        //    _device.CurrentState.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1),
        //        "LastUpdated should be recent after reading state");

        //    // Act - Read lock state
        //    var lockState = await _device.ReadLockStateAsync();

        //    // Assert
        //    lockState.Should().NotBe(Lock.Unknown, $"Light {deviceId} should return valid lock state");
        //    _device.CurrentState.Lock.Should().Be(lockState, "Current lock state should match read lock state");

        //    Console.WriteLine($"✅ Light {deviceId} feedback reading works correctly");
        //}

        #endregion

        #region General Device State Management Tests

        //[Theory]
        //[MemberData(nameof(LightIdsFromConfig))]
        //public virtual async Task OK_CanSaveAndRestoreState(string deviceId)
        //{
        //    // Arrange
        //    await InitializeDevice(deviceId);
        //    await _lockTestHelper.EnsureDeviceIsUnlocked(_device);

        //    // Set specific state
        //    await _device.TurnOnAsync();
        //    _device.CurrentState.Switch.Should().Be(Switch.On, "Light should be ON before saving");

        //    // Act - Save state
        //    _device.SaveCurrentState();
        //    var savedState = _device.SavedState?.Switch;

        //    // Change state
        //    await _device.TurnOffAsync();
        //    _device.CurrentState.Switch.Should().Be(Switch.Off, "Light should be OFF after changing state");

        //    // Act - Restore state
        //    await _device.RestoreSavedStateAsync();

        //    // Assert
        //    _device.CurrentState.Switch.Should().Be(savedState ?? Switch.On, "Light should be restored to saved state");

        //    Console.WriteLine($"✅ Light {deviceId} save and restore works correctly");
        //}

        //[Theory]
        //[MemberData(nameof(LightIdsFromConfig))]
        //public virtual async Task OK_HasCorrectAddressConfiguration(string deviceId)
        //{
        //    // Arrange
        //    await InitializeDevice(deviceId);

        //    // Get expected addresses from configuration
        //    LightFactory.LightConfigurations.TryGetValue(deviceId, out var config);
        //    config.Should().NotBeNull($"Configuration for light {deviceId} should exist");

        //    var expectedControl = KnxAddressConfiguration.CreateLightControlAddress(config.SubGroup);
        //    var expectedFeedback = KnxAddressConfiguration.CreateLightFeedbackAddress(config.SubGroup);
        //    var expectedLockControl = KnxAddressConfiguration.CreateLightLockAddress(config.SubGroup);
        //    var expectedLockFeedback = KnxAddressConfiguration.CreateLightLockFeedbackAddress(config.SubGroup);

        //    // Assert addresses
        //    _device.Addresses.Control.Should().Be(expectedControl, $"Control address for light {deviceId} should match");
        //    _device.Addresses.Feedback.Should().Be(expectedFeedback, $"Feedback address for light {deviceId} should match");
        //    _device.Addresses.LockControl.Should().Be(expectedLockControl, $"Lock control address for light {deviceId} should match");
        //    _device.Addresses.LockFeedback.Should().Be(expectedLockFeedback, $"Lock feedback address for light {deviceId} should match");

        //    // Assert device properties
        //    _device.Id.Should().Be(deviceId, $"Light ID should match {deviceId}");
        //    _device.Name.Should().Be(config.Name, $"Light name should match {config.Name}");

        //    Console.WriteLine($"✅ Light {deviceId} address configuration is correct");
        //}

        public override void Dispose()
        {
            _device?.RestoreSavedStateAsync().GetAwaiter().GetResult();
            _device?.Dispose();
        }

        #endregion
    }
}