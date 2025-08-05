using FluentAssertions;
using KnxModel;
using KnxTest.Integration.Interfaces;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Integration.Base
{
    public class SwitchTestHelper
    {
        public SwitchTestHelper()
        {
        }
        // ===== SWITCH-SPECIFIC HELPER METHODS =====
        public async Task EnsureDeviceIsTurnedOffBeforeTest(ISwitchable device)
        {
            device.CurrentSwitchState.Should().NotBe(Switch.Unknown,
                "Device switch state should be known before test");
            if (device.CurrentSwitchState == Switch.On)
            {
                await TurnDeviceOnOrOffAndAssert(device, Switch.Off);
            }
            else
            {
                Console.WriteLine($"✅ Device {device.Id} is already OFF, no action needed.");
            }
        }

        private async Task TurnDeviceOnOrOffAndAssert(ISwitchable device, Switch off)
        {
            if (device.CurrentSwitchState == off)
            {
                Console.WriteLine($"Device {device.Id} is already in state {off}, no action needed.");
                return;
            }
            if (off == Switch.On)
            {
                await TurnDeviceOn(device);
            }
            else
            {
                await TurnDeviceOff(device);
            }
            Console.WriteLine($"✅ Device {device.Id} successfully turned {off}");
        }

        private static async Task TurnDeviceOff(ISwitchable device)
        {
            await device.TurnOffAsync();
            var waitResult = await device.WaitForSwitchStateAsync(Switch.Off, TimeSpan.FromSeconds(1));
            waitResult.Should().BeTrue($"Device {device.Id} should be OFF after operation");
            device.CurrentSwitchState.Should().Be(Switch.Off,
                $"Device {device.Id} should be OFF after operation");
            Console.WriteLine($"✅ Device {device.Id} successfully turned OFF");
        }

        private static async Task TurnDeviceOn(ISwitchable device)
        {
            await device.TurnOnAsync();
            var waitResult = await device.WaitForSwitchStateAsync(Switch.On, TimeSpan.FromSeconds(1));
            waitResult.Should().BeTrue($"Device {device.Id} should be ON after operation");
            device.CurrentSwitchState.Should().Be(Switch.On,
                $"Device {device.Id} should be ON after operation");
            Console.WriteLine($"✅ Device {device.Id} successfully turned ON");
        }

        internal async Task CanTurnOnAndTurnOff(ISwitchable device)
        {

            // Test ON
            await TurnDeviceOn(device); 

            // Test OFF
            await TurnDeviceOff(device);
        }

        internal async Task CanToggleSwitch(ISwitchable device)
        {
            var initialState = device.CurrentSwitchState;

            // Act & Assert - Toggle to opposite
            await ToggleSwitch(device);

            // Act & Assert - Toggle back
            await ToggleSwitch(device);

            Console.WriteLine($"✅ Device {device.Id} toggle switch functionality works correctly");
        }

        private static async Task ToggleSwitch(ISwitchable device )
        {
            Switch initialState = device.CurrentSwitchState;
            initialState.Should().NotBe(Switch.Unknown, "Initial switch state should be known before toggling");

            await device.ToggleAsync();
            var result = await device.WaitForSwitchStateAsync(initialState.Opposite(), TimeSpan.FromSeconds(1));
            result.Should().BeTrue($"Device {device.Id} should toggle to opposite state");
            device.CurrentSwitchState.Should().Be(initialState.Opposite(),
                $"Device {device.Id} should toggle to opposite state");
            Console.WriteLine($"✅ Device {device.Id} successfully toggled from {initialState} to {device.CurrentSwitchState}");
        }
    }









    /// <summary>
    /// Helper class providing lock-specific test methods
    /// Can be used as a mixin/composition in test classes that need lock functionality testing
    /// </summary>
    public class LockTestHelper
    {

        public LockTestHelper()
        {
        }

        // ===== LOCK-SPECIFIC HELPER METHODS =====

        public async Task EnsureDeviceIsUnlockedBeforeTest(ILockableDevice device)
        {
            device.CurrentLockState.Should().NotBe(Lock.Unknown, 
                "Device lock state should be known before test");

            if (device.CurrentLockState == Lock.On)
            {
                await ChangeLockStateAndForceUpdateIfNeeded(device, Lock.Off);
            }

            device.CurrentLockState.Should().Be(Lock.Off, 
                    "Device should be unlocked after unlock operation");
            Console.WriteLine($"✅ Device {device.Id} is now unlocked");            
        }

        public async Task CanLockAndUnlock(ILockableDevice device)
        {
            // Ensure device is unlocked before starting tests
            await EnsureDeviceIsUnlockedBeforeTest(device);

            // Test Lock
            await ChangeLockStateAndForceUpdateIfNeeded(device, Lock.On);
            
            // Test Unlock
            await ChangeLockStateAndForceUpdateIfNeeded(device, Lock.Off);
        }


        private async Task ChangeLockStateAndForceUpdateIfNeeded(ILockableDevice device, Lock targetLockState)
        {
            await device.SetLockAsync(targetLockState, TimeSpan.FromMilliseconds(200));
            if (device.CurrentLockState != targetLockState)
            {
                // If state did not change, read status to force update
                await device.ReadLockStateAsync();
            }
            // Assert final state
            device.CurrentLockState.Should().Be(targetLockState, 
                $"Device should be {targetLockState} after SetLockAsync");
            Console.WriteLine($"✅ Device {device.Id} lock state changed to {targetLockState}");
        }

        private async Task TurnDeviceOnOrOffAndAssert(ISwitchable device, Switch switchState)
        {
            var currentState = device.CurrentSwitchState;
            if (currentState == switchState)
            {
                Console.WriteLine($"Device {device.Id} is already in state {switchState}, no action needed.");
                return;
            }
            if (switchState == Switch.On)
            {
                await device.TurnOnAsync();
            }
            else
            {
                await device.TurnOffAsync();
            }
            var waitResult = await device.WaitForSwitchStateAsync(switchState, TimeSpan.FromSeconds(1));
            waitResult.Should().BeTrue($"Device {device.Id} should be {switchState} after operation");
            device.CurrentSwitchState.Should().Be(switchState,
                $"Device {device.Id} should be {switchState} after operation");
            Console.WriteLine($"✅ Device {device.Id} successfully turned {switchState}");
        }

        public async Task LockPreventsStateChange(ILockableDevice device)
        {
            var switchableDevice = device as ISwitchable;
            if (switchableDevice == null)
            {
                throw new InvalidOperationException("Lock prevention test requires a switchable device (ILight)");
            }

            // Ensure device is unlocked before starting tests
            await EnsureDeviceIsUnlockedBeforeTest(device);

            // Lock the device
            await ChangeLockStateAndForceUpdateIfNeeded(device, Lock.On);

            var waitResult = await switchableDevice.WaitForSwitchStateAsync(Switch.Off, TimeSpan.FromSeconds(1));
            waitResult.Should().BeTrue("Device should be OFF after locking");
            switchableDevice.CurrentSwitchState.Should().Be(Switch.Off, 
                "Device should be OFF after locking");

            await switchableDevice.TurnOnAsync(); // Attempt to turn ON while locked
            switchableDevice.CurrentSwitchState.Should().Be(Switch.Off, 
                "Device should remain OFF after attempting to turn ON while locked");

            Console.WriteLine($"✅ Device {device.Id} lock properly prevents state changes");
        }

        public async Task CanReadLockState(ILockableDevice device)
        {
            var lockState = await device.ReadLockStateAsync();
            lockState.Should().NotBe(Lock.Unknown, $"Device {device.Id} should return valid lock state");
            
            var response = await device.WaitForLockStateAsync(lockState, TimeSpan.FromSeconds(1));
            response.Should().BeTrue($"Device {device.Id} should return expected lock state {lockState}");

            device.CurrentLockState.Should().Be(lockState, "Current state should match read lock state");

            Console.WriteLine($"✅ Device {device.Id} lock state read successfully: {lockState}");
        }

        public async Task SwitchableDeviceTurnOffWhenLocked(ILockableDevice device)
        {
            // Ensure device is a switchable device for this test
            if (!(device is ISwitchable switchableDevice))
            {
                await Task.CompletedTask;
                return; // Skip if not switchable
            }
            // Ensure device is unlocked before starting tests
            await EnsureDeviceIsUnlockedBeforeTest(device);

            // Turn device ON first
            await TurnDeviceOnOrOffAndAssert(switchableDevice, Switch.On);

            // Lock the device
            await ChangeLockStateAndForceUpdateIfNeeded(device, Lock.On);

            // Should automatically turn OFF
            await CheckIfTheDeviceHasSwitchedOffWhenLocked(device, switchableDevice);

            Console.WriteLine($"✅ Device {device.Id} automatically turned OFF when locked");
        }

        private static async Task CheckIfTheDeviceHasSwitchedOffWhenLocked(ILockableDevice device, ISwitchable switchableDevice)
        {
            var waitResult = await switchableDevice.WaitForSwitchStateAsync(Switch.Off, TimeSpan.FromSeconds(3));
            waitResult.Should().BeTrue("Device should automatically turn OFF when locked");

            switchableDevice.CurrentSwitchState.Should().Be(Switch.Off, "Device should be OFF after locking");
            device.CurrentLockState.Should().Be(Lock.On, "Device should be locked");
        }
    }
}
