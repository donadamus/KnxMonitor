using FluentAssertions;
using KnxModel;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace KnxTest.Unit.Base
{
    public abstract class DevicePercentageControllableTests<TDevice, TAddresses> : BaseKnxDeviceUnitTests
        where TDevice : IPercentageControllable, IKnxDeviceBase
        where TAddresses : IPercentageControllableAddress

    {
        protected abstract PercentageControllableDeviceTestHelper<TDevice, TAddresses> _percentageTestHelper { get; }

        public DevicePercentageControllableTests() : base()
        {
        }

        [Theory]
        [InlineData(0)]   // Off (minimum brightness)
        [InlineData(1)]   // Minimum dimming level
        [InlineData(25)]  // Quarter brightness
        [InlineData(50)]  // Half brightness
        [InlineData(75)]  // Three quarters brightness
        [InlineData(100)] // Maximum brightness
        public async Task SetPercentageAsync_WithValidValues_ShouldSendCorrectTelegram(float percentage)
        {
            // TODO: Test SetPercentageAsync with various valid percentage values for dimming
            await _percentageTestHelper.SetPercentageAsync_WithValidValues_ShouldSendCorrectTelegram(percentage);
        }

        [Fact]
        public async Task SetPercentageAsync_ShouldSendCorrectTelegram()
        {
            await _percentageTestHelper.SetPercentageAsync_ShouldSendCorrectTelegram();
        }

        [Theory]
        [InlineData(101)] // Above maximum
        [InlineData(255)] // Byte maximum
        public async Task SetPercentageAsync_WithInvalidValues_ShouldThrowException(float percentage)
        {
            // TODO: Test that SetPercentageAsync throws exception for invalid percentage values
            // Parameter: percentage
            await _percentageTestHelper.SetPercentageAsync_WithInvalidValues_ShouldThrowException(percentage);
        }

        [Theory]
        [InlineData(0)]   // Off
        [InlineData(1)]   // Minimum brightness
        [InlineData(25)]  // Quarter brightness
        [InlineData(50)]  // Half brightness
        [InlineData(75)]  // Three quarters brightness
        [InlineData(100)] // Maximum brightness
        public void OnPercentageFeedback_ShouldUpdateState(float expectedPercentage)
        {
            _percentageTestHelper.OnPercentageFeedback_ShouldUpdateState(expectedPercentage);
            
        }

        [Theory]
        [InlineData(5)]   // Small increment
        [InlineData(10)]  // Standard increment
        [InlineData(25)]  // Large increment
        public async Task IncreasePercentageAsync_ShouldSendCorrectTelegram(float increment)
        {
            await _percentageTestHelper.IncreasePercentageAsync_ShouldSendCorrectTelegram(increment);
        }

        [Theory]
        [InlineData(-5)]   // Small decrement
        [InlineData(-10)]  // Standard decrement
        [InlineData(-25)]  // Large decrement
        public async Task DecreasePercentageAsync_ShouldSendCorrectTelegram(float decrement)
        {
            await _percentageTestHelper.DecreasePercentageAsync_ShouldSendCorrectTelegram(decrement);
            
        }

        [Fact]
        public async Task ReadPercentageAsync_ShouldRequestCorrectAddress()
        {
            await _percentageTestHelper.ReadPercentageAsync_ShouldRequestCorrectAddress();
            
        }

        [Theory]
        [InlineData(0)]   // Off
        [InlineData(50)]  // Half brightness
        [InlineData(100)] // Full brightness
        public async Task ReadPercentageAsync_ShouldReturnCorrectValue(byte expectedPercentage)
        {
            await _percentageTestHelper.ReadPercentageAsync_ShouldReturnCorrectValue(expectedPercentage);
            
        }


        [Fact]
        public async Task ReadPercentageAsync_WhenKnxServiceThrows_ShouldPropagateException()
        {
            await _percentageTestHelper.ReadPercentageAsync_WhenKnxServiceThrows_ShouldPropagateException();
        }

        [Theory]
        [InlineData(0, 100, 0, 50)]   // Wait for 0% (off)
        [InlineData(50, 200, 0, 50)]  // Wait for 50% with timeout
        [InlineData(100, 0, 0, 50)]   // Wait for 100% (full brightness)
        public async Task WaitForPercentageAsync_ImmediateReturnTrueWhenAlreadyInState(float percentage, int waitingTime, int executionTimeMin, int executionTimeMax)
        {
            await _percentageTestHelper.WaitForPercentageAsync_ImmediateReturnTrueWhenAlreadyInState(percentage, waitingTime, executionTimeMin, executionTimeMax);
           
        }

        [Theory]
        [InlineData(0, 100, 100, 200, 100, true, 100, 200)]
        [InlineData(100, 50, 50, 200, 50, true, 100, 200)]
        [InlineData(50, 25, 75, 20, 50, false, 20, 70)]
        public async Task WaitForPercentageAsync_ShouldReturnCorrectly(byte initialPercentage, byte targetPercentage, byte feedbackPercentage, int waitingTime, byte expectedPercentage, bool expectedResult, int executionTimeMin, int executionTimeMax)
        {
            await _percentageTestHelper.WaitForPercentageAsync_ShouldReturnCorrectly(
                initialPercentage, targetPercentage, feedbackPercentage, waitingTime, expectedPercentage, expectedResult, executionTimeMin, executionTimeMax);
        }

        [Theory]
        [InlineData(0, 50, 50, 200, 50, 100, 200)]     // Wait for 50% from 0%
        [InlineData(100, 0, 75, 200, 0, 100, 225)]     // Wait for 0% from 100%
        [InlineData(50, 25, 100, 200, 25, 100, 250)]   // Wait for 25% from 50%
        public async Task WaitForPercentageAsync_WhenFeedbackReceived_ShouldReturnTrue(byte initialPercentage, byte targetPercentage, int delayInMs, int waitingTime, byte expectedPercentage, int executionTimeMin, int executionTimeMax)
        {
            await _percentageTestHelper.WaitForPercentageAsync_WhenFeedbackReceived_ShouldReturnTrue(
                initialPercentage, targetPercentage, delayInMs, waitingTime, expectedPercentage, executionTimeMin, executionTimeMax);
            
        }

        [Theory]
        [InlineData(95, 5, 100)] // Increase to max
        [InlineData(5, 10, 15)]  // Normal increase
        [InlineData(90, 20, 100)] // Increase with clamping to max
        public async Task IncreasePercentageAsync_ShouldNotExceedMaximum(float currentPercentage, float increment, float expectedResult)
        {
            await _percentageTestHelper.IncreasePercentageAsync_ShouldNotExceedMaximum(currentPercentage, increment, expectedResult);
        }

        [Theory]
        [InlineData(5, -5, 0)]   // Decrease to min
        [InlineData(15, -10, 5)] // Normal decrease
        [InlineData(10, -20, 0)] // Decrease with clamping to min
        public async Task DecreasePercentageAsync_ShouldNotGoBelowMinimum(float currentPercentage, float decrement, float expectedResult)
        {
            await _percentageTestHelper.DecreasePercentageAsync_ShouldNotGoBelowMinimum(currentPercentage, decrement, expectedResult);
            
        }

        [Theory]
        [InlineData(150)] // Above 100%
        [InlineData(200)] // Way above 100%
        public void InvalidPercentageFeedback_ShouldBeHandledGracefully(float invalidPercentage)
        {
            _percentageTestHelper.InvalidPercentageFeedback_ShouldBeHandledGracefully(invalidPercentage);
           
        }

        [Theory]
        [InlineData(Switch.Off)]   // Off
        [InlineData(Switch.On)]    // On
        [InlineData(Switch.Unknown)] // Unknown state
        public void OnPercentageFeedback_ShouldNotAffectSwitchState(Switch switchState)
        {
            _percentageTestHelper.OnPercentageFeedback_ShouldNotAffectSwitchState(switchState);
            
        }

        [Theory]
        [InlineData(0)]
        [InlineData(20)]
        public void OnAnyFeedbackToUnknownAddress_ShouldProcessCorrectlyAndDoesNotChangeState(float percentage)
        {
            _percentageTestHelper.OnAnyFeedbackToUnknownAddress_ShouldProcessCorrectlyAndDoesNotChangeState(percentage);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(20)]
        public void SaveCurrentState_ShouldStoreCurrentValues(float percentage)
        {
            _percentageTestHelper.SaveCurrentState_ShouldStoreCurrentValues(percentage);
        }

        [Fact]
        public void Device_ImplementsAllRequiredInterfaces()
        {
            _percentageTestHelper.Device_ImplementsAllRequiredInterfaces();

        }

        [Theory]
        [InlineData(30, 60)]
        [InlineData(70, 40)]
        [InlineData(100,100)]
        [InlineData(0,0)]
        [InlineData(0, 100)]
        [InlineData(100, 0)]
        public async Task RestoreSavedStateAsync_ShouldSendCorrectTelegrams(float initialPercentage, float percentage)
        {

            await _percentageTestHelper.RestoreSavedStateAsync_ShouldSendCorrectTelegrams(initialPercentage, percentage);
            
        }



        [Theory]
        [InlineData(0)]
        [InlineData(20)]
        [InlineData(60)]
        [InlineData(100)]

        public async Task InitializeAsync_UpdatesLastUpdatedAndStates(float percentage)
        {
           await _percentageTestHelper.InitializeAsync_UpdatesLastUpdatedAndStates(percentage);
        }


    }
}
