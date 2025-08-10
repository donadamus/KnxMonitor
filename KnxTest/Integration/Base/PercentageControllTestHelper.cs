using FluentAssertions;
using KnxModel;
using Xunit.Abstractions;

namespace KnxTest.Integration.Base
{
    public class PercentageControllTestHelper
    {
        private readonly ITestOutputHelper output;

        public PercentageControllTestHelper(Xunit.Abstractions.ITestOutputHelper output)
        {
            this.output = output;
        }
        public async Task EnsureDeviceIsAtZeroPercentBeforeTest(IPercentageControllable device)
        {
            device.CurrentPercentage.Should().NotBe(-1, "Device percentage should be known before test");
            if (device.CurrentPercentage > 0)
            {
                await SetDevicePercentageAndAssert(device, 0);
            }
            else
            {
                Console.WriteLine($"✅ Device {device.Id} is already at 0%, no action needed.");
            }
        }

        internal async Task CanAdjustPercentage(IPercentageControllable dimmerDevice)
        {
            // Set to a known starting point
            await SetDevicePercentageAndAssert(dimmerDevice, 30, TimeSpan.FromSeconds(20));

            // Test positive adjustment
            await dimmerDevice.AdjustPercentageAsync(20, TimeSpan.FromSeconds(20));

            var waitResult = await dimmerDevice.WaitForPercentageAsync(50, 1, TimeSpan.FromSeconds(1));


            waitResult.Should().BeTrue($"Device {dimmerDevice.Id} should be at 50% after +20 adjustment");
            dimmerDevice.CurrentPercentage.Should().BeApproximately(50, 1,
                $"Device {dimmerDevice.Id} should be at 50% after +20 adjustment");

            // Test negative adjustment
            await dimmerDevice.AdjustPercentageAsync(-15, TimeSpan.FromSeconds(20));

            waitResult = await dimmerDevice.WaitForPercentageAsync(35, 1, TimeSpan.FromSeconds(1));

            waitResult.Should().BeTrue($"Device {dimmerDevice.Id} should be at 35% after -15 adjustment");
            dimmerDevice.CurrentPercentage.Should().BeApproximately(35, 1,
                $"Device {dimmerDevice.Id} should be at 35% after -15 adjustment");

            Console.WriteLine($"✅ Device {dimmerDevice.Id} percentage adjustment functionality works correctly");
        }

        
        internal async Task CanSetToMaximum(IPercentageControllable dimmerDevice)
        {
            // Set to maximum (100%)
            await SetDevicePercentageAndAssert(dimmerDevice, 100, TimeSpan.FromSeconds(20));
            
            Console.WriteLine($"✅ Device {dimmerDevice.Id} can be set to maximum (100%)");
        }

        internal async Task CanSetToMinimum(IPercentageControllable dimmerDevice)
        {
            // Set to minimum (0%)
            await SetDevicePercentageAndAssert(dimmerDevice, 0, TimeSpan.FromSeconds(20));
            
            Console.WriteLine($"✅ Device {dimmerDevice.Id} can be set to minimum (0%)");
        }

        internal async Task PercentageRangeValidation(IPercentageControllable dimmerDevice)
        {
            // Test that setting invalid percentage values throws appropriate exceptions or handles gracefully
            
            // Test setting percentage above 100%
            try
            {
                await dimmerDevice.SetPercentageAsync(150, TimeSpan.FromSeconds(10));
                // If we get here without exception, check that it's clamped to 100%
                dimmerDevice.CurrentPercentage.Should().BeLessThanOrEqualTo(100f, 
                    $"Device {dimmerDevice.Id} should clamp percentage to maximum 100%");
            }
            catch (ArgumentOutOfRangeException)
            {
                // Expected behavior - exception is thrown for invalid range
                Console.WriteLine($"✅ Device {dimmerDevice.Id} properly validates percentage > 100%");
            }

            // Test setting percentage below 0%
            try
            {
                await dimmerDevice.SetPercentageAsync(-10, TimeSpan.FromSeconds(10));
                // If we get here without exception, check that it's clamped to 0%
                dimmerDevice.CurrentPercentage.Should().BeGreaterThanOrEqualTo(0f, 
                    $"Device {dimmerDevice.Id} should clamp percentage to minimum 0%");
            }
            catch (ArgumentOutOfRangeException)
            {
                // Expected behavior - exception is thrown for invalid range
                Console.WriteLine($"✅ Device {dimmerDevice.Id} properly validates percentage < 0%");
            }

            // Test valid boundary values
            await SetDevicePercentageAndAssert(dimmerDevice, 0, TimeSpan.FromSeconds(20));
            await SetDevicePercentageAndAssert(dimmerDevice, 100, TimeSpan.FromSeconds(20));
            
            Console.WriteLine($"✅ Device {dimmerDevice.Id} percentage range validation completed");
        }

        internal async Task CanReadPercentage(IPercentageControllable dimmerDevice)
        {
            // Act
            var percentage = await dimmerDevice.ReadPercentageAsync();
            // Assert
            percentage.Should().BeGreaterThanOrEqualTo(0, 
                $"Device {dimmerDevice.Id} should return a valid percentage >= 0");
            percentage.Should().BeLessThanOrEqualTo(100,
                $"Device {dimmerDevice.Id} should return a valid percentage <= 100");
            dimmerDevice.CurrentPercentage.Should().BeApproximately(percentage, 0.1f,
                $"Device {dimmerDevice.Id} should have its CurrentPercentage updated to {percentage}");
            Console.WriteLine($"✅ Device {dimmerDevice.Id} read percentage: {percentage}%");

        }

        internal async Task CanSetPercentage(IPercentageControllable dimmerDevice)
        {
            // Ensure device is at 0% before starting percentage tests
            //await EnsureDeviceIsAtZeroPercentBeforeTest(dimmerDevice);

            await SetDevicePercentageAndAssert(dimmerDevice, 50, TimeSpan.FromSeconds(20));
            await Task.CompletedTask;
        }

        internal async Task CanSetSpecificPercentages(IPercentageControllable device)
        {
            await SetDevicePercentageAndAssert(device, 20, TimeSpan.FromSeconds(20));
            await SetDevicePercentageAndAssert(device, 40, TimeSpan.FromSeconds(20));
            await SetDevicePercentageAndAssert(device, 80, TimeSpan.FromSeconds(20));

            await Task.CompletedTask;
        }

        internal async Task CanWaitForPercentageState(IPercentageControllable dimmerDevice)
        {
            // Ensure device is at 0% before starting percentage tests
            await EnsureDeviceIsAtZeroPercentBeforeTest(dimmerDevice);
            // Set device to a known percentage first
            await SetDevicePercentageAndAssert(dimmerDevice, 25, TimeSpan.FromSeconds(20));
            
            // Test waiting for current state (should return immediately)
            var waitResult = await dimmerDevice.WaitForPercentageAsync(25, 1, TimeSpan.Zero);
            dimmerDevice.CurrentPercentage.Should().BeApproximately(25, 1,
                $"Device {dimmerDevice.Id} should be at 25% after waiting for current state");
            waitResult.Should().BeTrue($"Device {dimmerDevice.Id} should immediately return true when waiting for current state");

            // Test waiting for a different state with tolerance
            await dimmerDevice.SetPercentageAsync(75, TimeSpan.Zero);
            waitResult = await dimmerDevice.WaitForPercentageAsync(75, 1, TimeSpan.FromSeconds(20));
            dimmerDevice.CurrentPercentage.Should().BeApproximately(75, 1,
                $"Device {dimmerDevice.Id} should be at 75% after waiting for that state");
            waitResult.Should().BeTrue($"Device {dimmerDevice.Id} should reach 75% within tolerance");

            waitResult = await dimmerDevice.WaitForPercentageAsync(40, 1, TimeSpan.FromMilliseconds(100));
            // This should return false since we are waiting for 40% but the device is at 75% no
            dimmerDevice.CurrentPercentage.Should().BeApproximately(75, 1,
                $"Device {dimmerDevice.Id} should still be at 75% after waiting for unreachable state");
            waitResult.Should().BeFalse($"Device {dimmerDevice.Id} should not reach 40% when it is at 75%");

            Console.WriteLine($"✅ Device {dimmerDevice.Id} wait for percentage state functionality works correctly");

            await Task.CompletedTask;
        }

        private async Task SetDevicePercentageAndAssert(IPercentageControllable device, float targetPercentage, TimeSpan? timeout = null)
        {
            if (device.CurrentPercentage == targetPercentage)
            {
                Console.WriteLine($"Device {device.Id} is already at {targetPercentage}%, no action needed.");
                return;
            }
            await device.SetPercentageAsync(targetPercentage, timeout);
            var waitResult = await device.WaitForPercentageAsync(targetPercentage, 1, TimeSpan.FromSeconds(1));
            waitResult.Should().BeTrue($"Device {device.Id} should be at {targetPercentage}% after operation");
            device.CurrentPercentage.Should().BeApproximately (targetPercentage,1,
                $"Device {device.Id} should be at {targetPercentage}% after operation");
            Console.WriteLine($"✅ Device {device.Id} successfully set to {targetPercentage}%");
        }
    }
}
