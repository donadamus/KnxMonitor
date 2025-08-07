using FluentAssertions;
using KnxModel;

namespace KnxTest.Integration.Base
{
    public class PercentageControllTestHelper
    {
        public PercentageControllTestHelper()
        {
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

        internal async Task TestCanReadPercentage(IPercentageControllable dimmerDevice)
        {
            // Act
            var percentage = await dimmerDevice.ReadPercentageAsync();
            // Assert
            percentage.Should().BeGreaterThanOrEqualTo(0, 
                $"Device {dimmerDevice.Id} should return a valid percentage >= 0");
            percentage.Should().BeLessThanOrEqualTo(100,
                $"Device {dimmerDevice.Id} should return a valid percentage <= 100");
            dimmerDevice.CurrentPercentage.Should().Be(percentage,
                $"Device {dimmerDevice.Id} should have its CurrentPercentage updated to {percentage}");
            Console.WriteLine($"✅ Device {dimmerDevice.Id} read percentage: {percentage}%");

        }

        internal async Task TestCanSetPercentage(IPercentageControllable dimmerDevice)
        {
            // Ensure device is at 0% before starting percentage tests
            await EnsureDeviceIsAtZeroPercentBeforeTest(dimmerDevice);

            await SetDevicePercentageAndAssert(dimmerDevice, 10, TimeSpan.FromSeconds(10));
            await SetDevicePercentageAndAssert(dimmerDevice, 25, TimeSpan.FromSeconds(10));
            await SetDevicePercentageAndAssert(dimmerDevice, 75, TimeSpan.FromSeconds(10));
            await SetDevicePercentageAndAssert(dimmerDevice, 50, TimeSpan.FromSeconds(10));
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
