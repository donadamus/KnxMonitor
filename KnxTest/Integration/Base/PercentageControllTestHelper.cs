using FluentAssertions;
using KnxModel;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace KnxTest.Integration.Base
{
    public class PercentageControllTestHelper
    {
        private readonly ILogger logger;

        public PercentageControllTestHelper(ILogger logger)
        {
            this.logger = logger;
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
            var startigPercentage = dimmerDevice.CurrentPercentage;
            var firstAdjustment = 30;
            var secondAdjustment = -20;
            if (startigPercentage > 50)
            {
                firstAdjustment = -30;
                secondAdjustment = 20;
            }
            // test first adjustment
            var targetPercentage = startigPercentage + firstAdjustment;
            await dimmerDevice.AdjustPercentageAsync(firstAdjustment);

            var waitResult = await dimmerDevice.WaitForPercentageAsync(targetPercentage, 1, TimeSpan.FromSeconds(1));

            waitResult.Should().BeTrue($"Device {dimmerDevice.Id} should be at {targetPercentage}% after {firstAdjustment} adjustment");
            dimmerDevice.CurrentPercentage.Should().BeApproximately(targetPercentage, 1,
                $"Device {dimmerDevice.Id} should be at {targetPercentage}% after {firstAdjustment} adjustment");

            // test second adjustment (opposite direction)
            targetPercentage += secondAdjustment;
            await dimmerDevice.AdjustPercentageAsync(secondAdjustment);

            waitResult = await dimmerDevice.WaitForPercentageAsync(targetPercentage, 1, TimeSpan.FromSeconds(1));

            waitResult.Should().BeTrue($"Device {dimmerDevice.Id} should be at {targetPercentage}% after {secondAdjustment} adjustment");
            dimmerDevice.CurrentPercentage.Should().BeApproximately(targetPercentage, 1,
                $"Device {dimmerDevice.Id} should be at {targetPercentage}% after {secondAdjustment} adjustment");

            logger.LogInformation($"Device {dimmerDevice.Id} percentage adjustment functionality works correctly");
        }

        
        internal async Task CanSetToMaximum(IPercentageControllable dimmerDevice)
        {
            if (dimmerDevice.CurrentPercentage > 90)
            {
                await SetDevicePercentageAndAssert(dimmerDevice, 90);
            }
            // Set to maximum (100%)
            await SetDevicePercentageAndAssert(dimmerDevice, 100);
            
            Console.WriteLine($"✅ Device {dimmerDevice.Id} can be set to maximum (100%)");
        }

        internal async Task CanSetToMinimum(IPercentageControllable dimmerDevice)
        {
            if (dimmerDevice.CurrentPercentage < 10)
            {
                await SetDevicePercentageAndAssert(dimmerDevice, 10);
            }
            // Set to minimum (0%)
            await SetDevicePercentageAndAssert(dimmerDevice, 0);
            
            Console.WriteLine($"✅ Device {dimmerDevice.Id} can be set to minimum (0%)");
        }

        internal async Task PercentageRangeValidation(IPercentageControllable dimmerDevice)
        {
            // Test that setting invalid percentage values throws appropriate exceptions or handles gracefully
            
            // Test setting percentage above 100%
            try
            {
                await dimmerDevice.SetPercentageAsync(150);
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
                await dimmerDevice.SetPercentageAsync(-10);
                // If we get here without exception, check that it's clamped to 0%
                dimmerDevice.CurrentPercentage.Should().BeGreaterThanOrEqualTo(0f, 
                    $"Device {dimmerDevice.Id} should clamp percentage to minimum 0%");
            }
            catch (ArgumentOutOfRangeException)
            {
                // Expected behavior - exception is thrown for invalid range
                Console.WriteLine($"✅ Device {dimmerDevice.Id} properly validates percentage < 0%");
            }

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

            await SetDevicePercentageAndAssert(dimmerDevice, 50);
            await Task.CompletedTask;
        }

        internal async Task CanSetSpecificPercentages(IPercentageControllable device)
        {
            var targetPercentage = device.CurrentPercentage switch
            {
                < 20 => 30,
                < 40 => 50,
                < 60 => 70,
                < 80 => 90,
                _ => 70 
            };

            await SetDevicePercentageAndAssert(device, targetPercentage);

            await Task.CompletedTask;
        }

        internal async Task CanWaitForPercentageState(IPercentageControllable dimmerDevice)
        {
            // Ensure device is at 0% before starting percentage tests
            await EnsureDeviceIsAtZeroPercentBeforeTest(dimmerDevice);
            // Set device to a known percentage first
            await SetDevicePercentageAndAssert(dimmerDevice, 25);
            
            // Test waiting for current state (should return immediately)
            var waitResult = await dimmerDevice.WaitForPercentageAsync(25, 1, TimeSpan.Zero);
            dimmerDevice.CurrentPercentage.Should().BeApproximately(25, 1,
                $"Device {dimmerDevice.Id} should be at 25% after waiting for current state");
            waitResult.Should().BeTrue($"Device {dimmerDevice.Id} should immediately return true when waiting for current state");

            // Test waiting for a different state with tolerance
            await dimmerDevice.SetPercentageAsync(75, TimeSpan.Zero);
            waitResult = await dimmerDevice.WaitForPercentageAsync(75, 1);
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
                logger.LogInformation($"Device {device.Id} is already at {targetPercentage}%, no action needed.");
                return;
            }
            await device.SetPercentageAsync(targetPercentage, timeout);
            var waitResult = await device.WaitForPercentageAsync(targetPercentage, 1, TimeSpan.FromSeconds(1));
            waitResult.Should().BeTrue($"Device {device.Id} should be at {targetPercentage}% after operation");
            device.CurrentPercentage.Should().BeApproximately (targetPercentage,1,
                $"Device {device.Id} should be at {targetPercentage}% after operation");
            logger.LogInformation($"Device {device.Id} successfully set to {targetPercentage}%");
        }
    }
}
