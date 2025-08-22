using FluentAssertions;
using KnxModel;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Unit.Models
{


    public class DimmerDeviceTests : LightDeviceTestsBase<DimmerDevice, DimmerAddresses>
    {
        protected override DimmerDevice _device { get; }

        protected override ILogger<DimmerDevice> _logger { get; }

        protected PercentageControllableDeviceTestHelper<DimmerDevice, DimmerAddresses> _percentageTestHelper { get; }

        public DimmerDeviceTests()
        {
            // Initialize DimmerDevice with mock KNX service
            _logger = new Mock<ILogger<DimmerDevice>>().Object;
            _device = new DimmerDevice("D_TEST", "Test Dimmer", "1", _mockKnxService.Object, _logger, TimeSpan.FromSeconds(1));
            _percentageTestHelper = new PercentageControllableDeviceTestHelper<DimmerDevice, DimmerAddresses>(
                _device, _device.Addresses, _mockKnxService);
        }


        #region IPercentageControllable Tests


        [Theory]
        [InlineData(5)]   // Small increment
        [InlineData(10)]  // Standard increment
        [InlineData(25)]  // Large increment
        public async Task IncreaseBrightnessAsync_ShouldSendCorrectTelegram(float increment)
        {
            var currentPercentage = 50; // Assume starting at 50%
            var expectedResult = Math.Max(0, currentPercentage + increment);
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(_device.Addresses.PercentageControl, expectedResult))
                          .Returns(Task.CompletedTask)
                          .Verifiable();
            ((IPercentageControllable)_device).SetPercentageForTest(currentPercentage);

            await _device.AdjustPercentageAsync(increment, TimeSpan.Zero);
        }

        [Theory]
        [InlineData(-5)]   // Small decrement
        [InlineData(-10)]  // Standard decrement
        [InlineData(-25)]  // Large decrement
        public async Task DecreaseBrightnessAsync_ShouldSendCorrectTelegram(float decrement)
        {
            var currentPercentage = 50; // Assume starting at 50%
            var expectedResult = Math.Max(0, currentPercentage + decrement);
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(_device.Addresses.PercentageControl, expectedResult))
                          .Returns(Task.CompletedTask)
                          .Verifiable();
            ((IPercentageControllable)_device).SetPercentageForTest(currentPercentage);

            await _device.AdjustPercentageAsync(decrement, TimeSpan.Zero);
        }

        #endregion

        #region Dimmer-Specific Feedback Processing Tests


        [Fact]
        public void OnPercentageFeedback_WhenLocked_ShouldStillUpdateState()
        {
            // TODO: Test that percentage feedback updates state even when device is locked
            _device.SetLockStateForTest(Lock.On);
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_device.Addresses.PercentageFeedback, new KnxValue(50)));
            _device.CurrentPercentage.Should().Be(50);
        }

        [Theory]
        [InlineData(Switch.Off)]   // Off
        [InlineData(Switch.On)]    // On
        [InlineData(Switch.Unknown)] // Unknown state
        public void OnPercentageFeedback_ShouldNotAffectSwitchState(Switch switchState)
        {
            // TODO: Test that percentage feedback only affects percentage, not switch state
            ((ISwitchable)_device).SetSwitchForTest(switchState);
            ((IPercentageControllable)_device).SetPercentageForTest(20); // Set initial percentage
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_device.Addresses.PercentageFeedback, new KnxValue(50)));
            _device.CurrentPercentage.Should().Be(50);
            _device.CurrentSwitchState.Should().Be(switchState);
        }

        [Theory]
        [InlineData(Switch.Off)]   // Off
        [InlineData(Switch.On)]    // On
        public void OnSwitchFeedback_ShouldNotAffectPercentageState(Switch switchState)
        {
            // TODO: Test that switch feedback only affects switch state, not percentage
            ((ISwitchable)_device).SetSwitchForTest(switchState);
            ((IPercentageControllable)_device).SetPercentageForTest(20); // Set initial percentage
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_device.Addresses.SwitchFeedback, new KnxValue(switchState == Switch.Off)));
            _device.CurrentPercentage.Should().Be(20);
            _device.CurrentSwitchState.Should().Be(switchState.Opposite());
        }

        #endregion

        #region Dimmer-Specific State Reading Tests

        [Fact]
        public async Task ReadPercentageAsync_ShouldRequestCorrectAddress()
        {
            // Arrange
            var expectedValue = 75.0f;
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_device.Addresses.PercentageFeedback))
                          .ReturnsAsync(expectedValue)
                          .Verifiable();

            // Act
            var result = await _device.ReadPercentageAsync();

            // Assert
            result.Should().Be(expectedValue);
            _mockKnxService.Verify(s => s.RequestGroupValue<float>(_device.Addresses.PercentageFeedback), Times.Once);
        }

        [Theory]
        [InlineData(0)]   // Off
        [InlineData(50)]  // Half brightness
        [InlineData(100)] // Full brightness
        public async Task ReadPercentageAsync_ShouldReturnCorrectValue(byte expectedPercentage)
        {
            // Arrange
            var expectedValue = (float)expectedPercentage;
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_device.Addresses.PercentageFeedback))
                          .ReturnsAsync(expectedValue);

            // Act
            var result = await _device.ReadPercentageAsync();

            // Assert
            result.Should().Be(expectedValue);
        }


        [Fact]
        public async Task ReadPercentageAsync_WhenKnxServiceThrows_ShouldPropagateException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("KNX service error");
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_device.Addresses.PercentageFeedback))
                          .ThrowsAsync(expectedException);

            // Act & Assert
            await _device.Invoking(d => d.ReadPercentageAsync())
                        .Should().ThrowAsync<InvalidOperationException>()
                        .WithMessage("KNX service error");
        }

        #endregion

        #region Dimmer-Specific Wait Methods Tests

        [Theory]
        [InlineData(0, 100, 0, 50)]   // Wait for 0% (off)
        [InlineData(50, 200, 0, 50)]  // Wait for 50% with timeout
        [InlineData(100, 0, 0, 50)]   // Wait for 100% (full brightness)
        public async Task WaitForPercentageAsync_ImmediateReturnTrueWhenAlreadyInState(float percentage, int waitingTime, int executionTimeMin, int executionTimeMax)
        {
            // TODO: Test WaitForPercentageAsync: immediate return when already at target percentage
            // Parameters: percentage, waitingTime, executionTimeMin, executionTimeMax
            ((IPercentageControllable)_device).SetPercentageForTest(percentage);
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            var result = await _device.WaitForPercentageAsync(percentage, 0.1, TimeSpan.FromMilliseconds(waitingTime));
            timer.Stop();
            result.Should().BeTrue();
            _device.CurrentPercentage.Should().Be(percentage, "Current percentage should match target percentage");
            timer.ElapsedMilliseconds.Should().BeInRange(executionTimeMin, executionTimeMax, $"Execution time should be between {executionTimeMin} and {executionTimeMax} ms");

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
            // Arrange
            ((IPercentageControllable)_device).SetPercentageForTest(initialPercentage);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Schedule feedback after delay
            _ = Task.Run(async () =>
            {
                await Task.Delay(delayInMs);
                _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, 
                    new KnxGroupEventArgs(_device.Addresses.PercentageFeedback, new KnxValue((float)expectedPercentage)));
            });

            // Act
            var result = await _device.WaitForPercentageAsync(targetPercentage, tolerance: 1.0, TimeSpan.FromMilliseconds(waitingTime));
            stopwatch.Stop();

            // Assert
            result.Should().BeTrue("feedback should have changed percentage to target");
            _device.CurrentPercentage.Should().BeApproximately(expectedPercentage, 1.0f);
            stopwatch.ElapsedMilliseconds.Should().BeInRange(executionTimeMin, executionTimeMax);
        }

        #endregion

        #region Dimmer-Specific Command Tests

        [Theory]
        [InlineData(95, 5, 100)] // Increase to max
        [InlineData(5, 10, 15)]  // Normal increase
        [InlineData(90, 20, 100)] // Increase with clamping to max
        public async Task IncreaseBrightnessAsync_ShouldNotExceedMaximum(float currentPercentage, float increment, float expectedResult)
        {
             _mockKnxService.Setup(s => s.WriteGroupValueAsync(_device.Addresses.PercentageControl, expectedResult))
              .Returns(Task.CompletedTask)
              .Callback(() =>
              {
                  _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_device.Addresses.PercentageFeedback, new KnxValue(expectedResult)));
              })
              .Verifiable();
            ((IPercentageControllable)_device).SetPercentageForTest(currentPercentage);

            await _device.AdjustPercentageAsync(increment, TimeSpan.FromMilliseconds(100));

            _device.CurrentPercentage.Should().Be(expectedResult);

        }

        [Theory]
        [InlineData(5, -5, 0)]   // Decrease to min
        [InlineData(15, -10, 5)] // Normal decrease
        [InlineData(10, -20, 0)] // Decrease with clamping to min
        public async Task DecreaseBrightnessAsync_ShouldNotGoBelowMinimum(float currentPercentage, float decrement, float expectedResult)
        {
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(_device.Addresses.PercentageControl, expectedResult))
                          .Returns(Task.CompletedTask)
                          .Callback(() =>
                          {
                            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_device.Addresses.PercentageFeedback, new KnxValue(expectedResult)));
                          })
                          .Verifiable();
            ((IPercentageControllable)_device).SetPercentageForTest(currentPercentage);

            await _device.AdjustPercentageAsync(decrement, TimeSpan.FromMilliseconds(100));
            
            _device.CurrentPercentage.Should().Be(expectedResult);
        }

        #endregion

        #region Dimmer-Specific Edge Cases Tests

        [Theory]
        [InlineData(150)] // Above 100%
        [InlineData(200)] // Way above 100%
        public void InvalidPercentageFeedback_ShouldBeHandledGracefully(float invalidPercentage)
        {
            // TODO: Test that invalid percentage feedback is handled gracefully
            // Parameter: invalidPercentage
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_device.Addresses.PercentageFeedback, new KnxValue(invalidPercentage)));
        }

        #endregion

        #region Interface Composition Tests

        [Fact]
        public void DimmerDevice_ImplementsAllRequiredInterfaces()
        {
            // Assert
            _device.Should().BeAssignableTo<IKnxDeviceBase>();
            _device.Should().BeAssignableTo<ISwitchable>();
            _device.Should().BeAssignableTo<IPercentageControllable>();
            _device.Should().BeAssignableTo<ILockableDevice>();
            _device.Should().BeAssignableTo<IDimmerDevice>();
        }

        #endregion
    }
}
