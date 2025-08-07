using FluentAssertions;
using KnxModel;
using KnxTest.Integration.Base;
using KnxTest.Integration.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static KnxTest.Integration.LightIntegrationTests;

namespace KnxTest.Integration
{

    [Collection("KnxService collection")]
    public class DimmerIntegrationTests : LightIntegrationTestsBase<DimmerDevice>
    {
        public DimmerIntegrationTests(KnxServiceFixture fixture) : base(fixture)
        {
        }

        // Data source for tests - only pure lights (not dimmers)
        public static IEnumerable<object[]> DimmerIdsFromConfig
        {
            get
            {
                var config = DimmerFactory.DimmerConfigurations;
                return config//.Where(x => x.Value.Name.ToLower().Contains("off"))
                            .Select(k => new object[] { k.Key });
            }
        }

        internal override async Task InitializeDevice(string deviceId, bool saveCurrentState = true)
        {
            Console.WriteLine($"🆕 Creating new DimmerDevice {deviceId}");
            Device = DimmerFactory.CreateDimmer(deviceId, _knxService);
            await Device.InitializeAsync();
            if (saveCurrentState)
            {
                Device.SaveCurrentState();
            }

            Console.WriteLine($"Light {deviceId} initialized - Switch: {Device.CurrentSwitchState}, Lock: {Device.CurrentLockState}");
        }


        internal override DimmerDevice? Device { get; set; }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task CanLockAndUnlock(string deviceId)
        {
            await TestCanLockAndUnlock(deviceId);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task CanReadLockState(string deviceId)
        {
            await TestCanReadLockState(deviceId);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task CanReadSwitchState(string deviceId)
        {
            await TestCanReadSwitchState(deviceId);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task CanToggleSwitch(string deviceId)
        {
            await TestCanToggleSwitch(deviceId);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task CanTurnOnAndTurnOff(string deviceId)
        {
            await TestCanTurnOnAndTurnOff(deviceId);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task LockPreventsStateChanges(string deviceId)
        {
            await TestLockPreventsStateChanges(deviceId);
        }

        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]
        public override async Task SwitchableDeviceTurnOffWhenLocked(string deviceId)
        {
            await TestSwitchableDeviceTurnOffWhenLocked(deviceId);
        }

    }



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

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
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


    /// <summary>
    /// Integration tests for Light devices using new architecture
    /// Inherits from DeviceTestBase and implements ILockableDeviceTests interface
    /// </summary>
    [Collection("KnxService collection")]
    public abstract class LightIntegrationTestsBase<TDevice> : IntegrationTestBaseNew, ILockableDeviceTests<TDevice>, ISwitchableDeviceTests<TDevice>
        where TDevice : ILightDevice
    {
        internal readonly LockTestHelper _lockTestHelper;
        internal readonly SwitchTestHelper _switchTestHelper;
        internal abstract TDevice? Device { get; set; }

        public LightIntegrationTestsBase(KnxServiceFixture fixture) : base(fixture)
        {
            _lockTestHelper = new LockTestHelper();
            _switchTestHelper = new SwitchTestHelper();
        }
        // ===== DEVICE INITIALIZATION =====

        internal abstract Task InitializeDevice(string deviceId, bool saveCurrentState = true);

        #region ILockableDeviceTests Implementation

        public abstract Task CanLockAndUnlock(string deviceId);
        public async Task TestCanLockAndUnlock(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.CanLockAndUnlock(Device!);

            await Task.CompletedTask;
        }
        public abstract Task LockPreventsStateChanges(string deviceId);
        public async Task TestLockPreventsStateChanges(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.LockPreventsStateChange(Device!);

            await Task.CompletedTask;

        }
        public abstract Task CanReadLockState(string deviceId);
        public async Task TestCanReadLockState(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.CanReadLockState(Device!);

            await Task.CompletedTask;
        }
        public abstract Task SwitchableDeviceTurnOffWhenLocked(string deviceId);
        public async Task TestSwitchableDeviceTurnOffWhenLocked(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Act & Assert
            await _lockTestHelper.SwitchableDeviceTurnOffWhenLocked(Device!);

            await Task.CompletedTask;
        }

        #endregion

        #region ISwitchableDeviceTests Tests

        public abstract Task CanTurnOnAndTurnOff(string deviceId);
        public async Task TestCanTurnOnAndTurnOff(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);

            // Ensure device is unlocked before testing switch functionality
            await _lockTestHelper.EnsureDeviceIsUnlockedBeforeTest(Device!);

            // Act & Assert - Test switch functionality
            await _switchTestHelper.CanTurnOnAndTurnOff(Device!);

            await Task.CompletedTask;
        }
        public abstract Task CanToggleSwitch(string deviceId);
        public async Task TestCanToggleSwitch(string deviceId)
        {
            // Arrange
            await InitializeDevice(deviceId);
            // Ensure device is unlocked before testing toggle functionality
            await _lockTestHelper.EnsureDeviceIsUnlockedBeforeTest(Device!);

            // Act & Assert - Check toggle functionality
            await _switchTestHelper.CanToggleSwitch(Device!);

            await Task.CompletedTask;

        }
        public abstract Task CanReadSwitchState(string deviceId);
        public async Task TestCanReadSwitchState(string deviceId)
        {
            await InitializeDevice(deviceId);

            //Act $ Assert
            await _switchTestHelper.CanReadSwitchState(Device!);

            await Task.CompletedTask;
        }

        #endregion

        #region Cleanup

        public override void Dispose()
        {
            try
            {
                if (Device != null)
                    Device.RestoreSavedStateAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to restore device state during cleanup: {ex.Message}");
            }
            finally
            {
                Device?.Dispose();
                base.Dispose();
            }
        }
        #endregion
    }
}
