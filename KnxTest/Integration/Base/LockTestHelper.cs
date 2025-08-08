using FluentAssertions;
using KnxModel;
using KnxTest.Integration.Interfaces;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Integration.Base
{
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

            await Task.CompletedTask;
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
            await Task.CompletedTask;
        }

        public async Task CanReadLockState(ILockableDevice device)
        {
            var lockState = await device.ReadLockStateAsync();
            lockState.Should().NotBe(Lock.Unknown, $"Device {device.Id} should return valid lock state");
            
            var response = await device.WaitForLockStateAsync(lockState, TimeSpan.FromSeconds(1));
            response.Should().BeTrue($"Device {device.Id} should return expected lock state {lockState}");

            device.CurrentLockState.Should().Be(lockState, "Current state should match read lock state");

            Console.WriteLine($"✅ Device {device.Id} lock state read successfully: {lockState}");

            await Task.CompletedTask;
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
            await Task.CompletedTask;
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
