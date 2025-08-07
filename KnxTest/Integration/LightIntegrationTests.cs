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
            await TestCanLockAndUnlock(deviceId);
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task LockPreventsStateChanges(string deviceId)
        {
            await TestLockPreventsStateChanges(deviceId);
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task CanReadLockState(string deviceId)
        {
            await TestCanReadLockState(deviceId);
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task SwitchableDeviceTurnOffWhenLocked(string deviceId)
        {
            await TestSwitchableDeviceTurnOffWhenLocked(deviceId);
        }

        #endregion

        #region ISwitchableDeviceTests Tests

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task CanTurnOnAndTurnOff(string deviceId)
        {
            await TestCanTurnOnAndTurnOff(deviceId);

        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task CanToggleSwitch(string deviceId)
        {
            await TestCanToggleSwitch(deviceId);
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task CanReadSwitchState(string deviceId)
        {
            await TestCanReadSwitchState(deviceId);
        }

        #endregion
    }
}
