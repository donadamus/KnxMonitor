using FluentAssertions;
using KnxModel;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;

namespace KnxTest.Unit.Base
{
    public abstract class DeviceSwitchableTests<TDevice, TAddresses> : BaseKnxDeviceUnitTests
    where TDevice : ISwitchable, IKnxDeviceBase
    where TAddresses : ISwitchableAddress

    {
        protected abstract SwitchableDeviceTestHelper<TDevice, TAddresses> _switchableTestHelper { get; }

        public DeviceSwitchableTests() : base()
        {
        }

        [Fact]
        public async Task TurnOnAsync_ShouldSendCorrectTelegram()
        {
            await _switchableTestHelper.TurnOnAsync_ShouldSendCorrectTelegram();
        }


        [Fact]
        public async Task TurnOffAsync_ShouldSendCorrectTelegram()
        {
            await _switchableTestHelper.TurnOffAsync_ShouldSendCorrectTelegram();
        }

        [Theory]
        [InlineData(Switch.Off, true)]  // Off -> On (should send true)
        [InlineData(Switch.On, false)]  // On -> Off (should send false)
        [InlineData(Switch.Unknown, true)] // Unknown -> On (default behavior)
        public async Task ToggleAsync_ShouldSendCorrectTelegram(Switch initialState, bool expectedValue)
        {
            await _switchableTestHelper.ToggleAsync_ShouldSendCorrectTelegram(initialState, expectedValue);

        }

        #region State Reading Tests

        [Fact]
        public async Task ReadSwitchStateAsync_ShouldRequestCorrectAddress()
        {
            await _switchableTestHelper.ReadSwitchStateAsync_ShouldRequestCorrectAddress();
        }

        [Theory]
        [InlineData(true, Switch.On)]
        [InlineData(false, Switch.Off)]
        public async Task ReadSwitchStateAsync_ShouldReturnCorrectValue(bool value, Switch switchState)
        {
            await _switchableTestHelper.ReadSwitchStateAsync_ShouldReturnCorrectValue(value, switchState);
        }


        #endregion

        #region Wait Methods Tests

        [Theory]
        [InlineData(Switch.On, 0, 0, 50)] // Wait for Switch.On
        [InlineData(Switch.On, 200, 0, 50)] // Wait for Switch.On with timeout
        [InlineData(Switch.Off, 0, 0, 50)] // Wait for Switch.Off
        [InlineData(Switch.Off, 200, 0, 50)] // Wait for Switch.Off with timeout
        [InlineData(Switch.Unknown, 0, 0, 50)] // Wait for Switch.Unknown
        [InlineData(Switch.Unknown, 200, 0, 50)] // Wait for Switch.OfUnknownf with timeout
        public async Task WaitForSwitchStateAsync_ImmediateReturnTrueWhenAlreadyInState(Switch switchState, int waitingTime, int executionTimeMin, int executionTimeMax)
        {
            await _switchableTestHelper.WaitForSwitchStateAsync_ImmediateReturnTrueWhenAlreadyInState(switchState, waitingTime, executionTimeMin, executionTimeMax);
        }

        [Theory]
        [InlineData(Switch.On, 200, Switch.Off, 50, Switch.On, false, 50, 100)] // Wait for Switch.Off with delay
        [InlineData(Switch.Off, 200, Switch.On, 50, Switch.Off, false, 50, 100)] // Wait for Switch.On with delay
        [InlineData(Switch.Unknown, 200, Switch.On, 50, Switch.Unknown, false, 50, 100)] // Wait for Switch.On from Unknown with delay
        [InlineData(Switch.Unknown, 200, Switch.Off, 50, Switch.Unknown, false, 50, 100)] // Wait for Switch.Off from Unknown with delay

        public async Task WaitForSwitchStateAsync_ShouldReturnCorrectly(Switch initialState, int delayInMs, Switch switchState, int waitingTime, Switch expectedState, bool expectedResult, int executionTimeMin, int executionTimeMax)
        {
            await _switchableTestHelper.WaitForSwitchStateAsync_ShouldReturnCorrectly(initialState, delayInMs, switchState, waitingTime, expectedState, expectedResult, executionTimeMin, executionTimeMax);
        }


        [Theory]
        [InlineData(Switch.On, 50, Switch.Off, 200, Switch.Off, 50, 150)] // Wait for Switch.Off with delay
        [InlineData(Switch.Off, 50, Switch.On, 200, Switch.On, 50, 150)] // Wait for Switch.On with delay
        [InlineData(Switch.Unknown, 0, Switch.On, 200, Switch.On, 0, 100)] // Wait for Switch.On from Unknown
        [InlineData(Switch.Unknown, 50, Switch.On, 200, Switch.On, 50, 150)] // Wait for Switch.On from Unknown with delay
        [InlineData(Switch.Unknown, 0, Switch.Off, 200, Switch.Off, 0, 100)] // Wait for Switch.Off from Unknown
        [InlineData(Switch.Unknown, 50, Switch.Off, 200, Switch.Off, 50, 150)] // Wait for Switch.Off from Unknown with delay

        public async Task WaitForSwitchStateAsync_WhenFeedbackReceived_ShouldReturnTrue(Switch initialState, int delayInMs, Switch switchState, int waitingTime, Switch expectedState, int executionTimeMin, int executionTimeMax)
        {
            await _switchableTestHelper.WaitForSwitchStateAsync_WhenFeedbackReceived_ShouldReturnTrue(initialState, delayInMs, switchState, waitingTime, expectedState, executionTimeMin, executionTimeMax);

        }

        #endregion

        [Theory]
        [InlineData(Switch.On, true)]  // Switch.On -> true
        [InlineData(Switch.Off, false)] // Switch.Off -> false
        public void OnSwitchFeedback_ShouldUpdateState(Switch expectedSwitchState, bool feedback)
        {
            _switchableTestHelper.OnSwitchFeedback_ShouldUpdateState(expectedSwitchState, feedback);
            
        }

    }
}
