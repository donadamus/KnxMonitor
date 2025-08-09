using FluentAssertions;
using KnxModel;
using KnxTest.Integration.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static KnxTest.Integration.LightIntegrationTests;

namespace KnxTest.Integration
{
    [Collection("KnxService collection")]
    public class LightIntegrationTests : LightIntegrationTestsBase<LightDevice>
    {
        internal override LightDevice? Device { get; set; }

        public LightIntegrationTests(KnxServiceFixture fixture) : base(fixture)
        {
            
        }

        // Data source for tests - only pure lights (not dimmers)
        public static IEnumerable<object[]> LightIdsFromConfig
        {
            get
            {
                var config = LightFactory.LightConfigurations;
                return config//.Where(x => x.Value.Name.ToLower().Contains("off"))
                            .Select(k => new object[] { k.Key });
            }
        }

        internal override async Task InitializeDevice(string deviceId, bool saveCurrentState = true)
        {
            Console.WriteLine($"🆕 Creating new LightDevice {deviceId}");
            Device = LightFactory.CreateLight(deviceId, _knxService);
            await Device.InitializeAsync();
            if (saveCurrentState)
            {
                Device.SaveCurrentState();
            }

            Console.WriteLine($"Light {deviceId} initialized - Switch: {Device.CurrentSwitchState}, Lock: {Device.CurrentLockState}");
        }


        #region ILockableDeviceTests Implementation

       // [Theory]
       // [MemberData(nameof(LightIdsFromConfig))]
        public override async Task CanLockAndUnlock(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.CanLockAndUnlock(Device!);


        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task LockPreventsStateChanges(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.LockPreventsStateChange(Device!);
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task CanReadLockState(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.CanReadLockState(Device!);
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task SwitchableDeviceTurnOffWhenLocked(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.SwitchableDeviceTurnOffWhenLocked(Device!);
        }

        #endregion

        #region ISwitchableDeviceTests Tests

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task CanTurnOnAndTurnOff(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await _switchTestHelper.CanTurnOnAndTurnOff(Device!);
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task CanToggleSwitch(string deviceId)
        {
            await InitializeDeviceAndEnsureUnlocked(deviceId);
            await _switchTestHelper.CanToggleSwitch(Device!);
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task CanReadSwitchState(string deviceId)
        {
            await InitializeDevice(deviceId);
            await _switchTestHelper.CanReadSwitchState(Device!);
        }

        #endregion
    }
}
