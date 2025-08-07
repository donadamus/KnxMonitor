using FluentAssertions;
using KnxModel;

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

        internal async Task CanReadSwitchState(ISwitchable device)
        {
            var switchState = await device.ReadSwitchStateAsync();
            switchState.Should().NotBe(Switch.Unknown, $"Device {device.Id} should return valid switch state");

            var response = await device.WaitForSwitchStateAsync(switchState, TimeSpan.FromSeconds(1));
            response.Should().BeTrue($"Device {device.Id} should return expected switch state {switchState}");

            device.CurrentSwitchState.Should().Be(switchState, "Current state should match read switch state");

            Console.WriteLine($"✅ Device {device.Id} switch state read successfully: {switchState}");
        }
    }
}
