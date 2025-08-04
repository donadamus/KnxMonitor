using FluentAssertions;
using KnxModel;
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
        private readonly IKnxService _knxService;

        public LockTestHelper(IKnxService knxService)
        {
            _knxService = knxService;
        }

        // ===== LOCK-SPECIFIC HELPER METHODS =====

        public async Task EnsureDeviceIsUnlockedBeforeTest(ILockable device)
        {
            device.CurrentState.Lock.Should().NotBe(Lock.Unknown, 
                "Device lock state should be known before test");

            if (device.CurrentState.Lock == Lock.On)
            {
                await ChangeLockStateAndForceUpdateIfNeeded(device, Lock.Off);
            }

            device.CurrentState.Lock.Should().Be(Lock.Off, 
                    "Device should be unlocked after unlock operation");
            Console.WriteLine($"✅ Device {device.Id} is now unlocked");            
        }

        public async Task CanLockAndUnlock(ILockable device)
        {
            // Ensure device is unlocked before starting tests
            await EnsureDeviceIsUnlockedBeforeTest(device);

            // Test Lock
            await ChangeLockStateAndForceUpdateIfNeeded(device, Lock.On);
            
            // Test Unlock
            await ChangeLockStateAndForceUpdateIfNeeded(device, Lock.Off);
        }


        private async Task ChangeLockStateAndForceUpdateIfNeeded(ILockable device, Lock targetLockState)
        {
            await device.SetLockAsync(targetLockState, TimeSpan.FromMilliseconds(200));
            if (device.CurrentState.Lock != targetLockState)
            {
                // If state did not change, read status to force update
                await device.ReadLockStateAsync();
            }
            // Assert final state
            device.CurrentState.Lock.Should().Be(targetLockState, 
                $"Device should be {targetLockState} after SetLockAsync");
            Console.WriteLine($"✅ Device {device.Id} lock state changed to {targetLockState}");
        }

        public async Task LockPreventsStateChange(ILockable device)
        {
            var switchableDevice = device as ILight;
            if (switchableDevice == null)
            {
                throw new InvalidOperationException("Lock prevention test requires a switchable device (ILight)");
            }

            // Ensure device is unlocked before starting tests
            await EnsureDeviceIsUnlockedBeforeTest(device);

            // Lock the device
            await ChangeLockStateAndForceUpdateIfNeeded(device, Lock.On);

            var waitResult = await switchableDevice.WaitForStateAsync(Switch.Off, TimeSpan.FromSeconds(1));
            waitResult.Should().BeTrue("Device should be OFF after locking");
            switchableDevice.CurrentState.Switch.Should().Be(Switch.Off, 
                "Device should be OFF after locking");

            await switchableDevice.TurnOnAsync(); // Attempt to turn ON while locked
            switchableDevice.CurrentState.Switch.Should().Be(Switch.Off, 
                "Device should remain OFF after attempting to turn ON while locked");

            Console.WriteLine($"✅ Device {device.Id} lock properly prevents state changes");
        }

        public async Task CanReadLockState(ILockable device)
        {
            var lockState = await device.ReadLockStateAsync();
            lockState.Should().NotBe(Lock.Unknown, $"Device {device.Id} should return valid lock state");
            
            var response = await device.WaitForLockStateAsync(lockState, TimeSpan.FromSeconds(1));
            response.Should().BeTrue($"Device {device.Id} should return expected lock state {lockState}");

            device.CurrentState.Lock.Should().Be(lockState, "Current state should match read lock state");
            device.CurrentState.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1),
                "LastUpdated should be recent after reading lock state");

            Console.WriteLine($"✅ Device {device.Id} lock state read successfully: {lockState}");
        }

        public async Task AssertDeviceAutoOffWhenLocked(ILockable device)
        {
            // Ensure device is a switchable device for this test
            if (!(device is ILight switchableDevice))
            {
                throw new InvalidOperationException("Auto-off test requires a switchable device (ILight)");
            }

            // Turn device ON first
            await switchableDevice.TurnOnAsync();
            switchableDevice.CurrentState.Switch.Should().Be(Switch.On, "Device should be ON before testing auto-off");

            // Lock the device
            await device.LockAsync();
            await device.ReadLockStateAsync();

            // Should automatically turn OFF
            var waitResult = await switchableDevice.WaitForStateAsync(Switch.Off, TimeSpan.FromSeconds(3));
            waitResult.Should().BeTrue("Device should automatically turn OFF when locked");
            
            switchableDevice.CurrentState.Switch.Should().Be(Switch.Off, "Device should be OFF after locking");
            device.CurrentState.Lock.Should().Be(Lock.On, "Device should be locked");

            Console.WriteLine($"✅ Device {device.Id} automatically turned OFF when locked");
        }
    }
}
