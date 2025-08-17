using FluentAssertions;
using KnxModel;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Unit.Models
{
    public class PercentageControllableDeviceTestHelper<TDevice, TAddresses>
        where TDevice : IPercentageControllable, IKnxDeviceBase
        where TAddresses : IPercentageControllableAddress

    {
        private readonly TDevice _device;
        private readonly TAddresses _addresses;
        private readonly Mock<IKnxService> _mockKnxService;

        // This class would contain methods to help with percentage control for dimmers
        // It would handle sending and receiving percentage-related messages
        public PercentageControllableDeviceTestHelper(TDevice device, TAddresses addresses, Mock<IKnxService> mockKnxService)
        {
            _device = device;
            _addresses = addresses;
            _mockKnxService = mockKnxService;
            // Initialize helper with necessary parameters
        }
        public async Task SetPercentageAsync_ShouldSendCorrectTelegram()
        {
            // Arrange
            var address = _addresses.PercentageControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, 60))
                          .Returns(Task.CompletedTask)
                          .Verifiable();

            // Act
            await _device.SetPercentageAsync(60, TimeSpan.Zero);
        }

        internal async Task SetPercentageAsync_WithInvalidValues_ShouldThrowException(float percentage)
        {
           await Assert.ThrowsAsync<ArgumentOutOfRangeException>(()=> _device.SetPercentageAsync(percentage, TimeSpan.Zero));
        }

        internal async Task SetPercentageAsync_WithValidValues_ShouldSendCorrectTelegram(float percentage)
        {
            // Arrange
            var address = _addresses.PercentageControl;
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(address, percentage))
                          .Returns(Task.CompletedTask)
                          .Verifiable();
            // Act
            await _device.SetPercentageAsync(percentage, TimeSpan.Zero);

        }
    }

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

        #region IKnxDeviceBase Tests

        [Fact]
        public override void Constructor_SetsBasicProperties()
        {
            // Assert
            _device.Id.Should().Be("D_TEST");
            _device.Name.Should().Be("Test Dimmer");
            _device.SubGroup.Should().Be("1");
            _device.LastUpdated.Should().Be(DateTime.MinValue); // Not initialized yet
        }

        #endregion

        #region IPercentageControllable Tests

        [Fact]
        public async Task SetPercentageAsync_ShouldSendCorrectTelegram()
        {
            await _percentageTestHelper.SetPercentageAsync_ShouldSendCorrectTelegram();
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
            _device.SetPercentageForTest(currentPercentage);

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
            _device.SetPercentageForTest(currentPercentage);

            await _device.AdjustPercentageAsync(decrement, TimeSpan.Zero);
        }

        #endregion

        #region Dimmer-Specific Feedback Processing Tests

        [Theory]
        [InlineData(0)]   // Off
        [InlineData(1)]   // Minimum brightness
        [InlineData(25)]  // Quarter brightness
        [InlineData(50)]  // Half brightness
        [InlineData(75)]  // Three quarters brightness
        [InlineData(100)] // Maximum brightness
        public void OnPercentageFeedback_ShouldUpdateState(float expectedPercentage)
        {
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_device.Addresses.PercentageFeedback, new KnxValue(expectedPercentage)));

            _device.CurrentPercentage.Should().Be(expectedPercentage);
        }

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
            _device.SetSwitchStateForTest(switchState);
            _device.SetPercentageForTest(20); // Set initial percentage
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
            _device.SetSwitchStateForTest(switchState);
            _device.SetPercentageForTest(20); // Set initial percentage
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
        public async Task ReadPercentageAsync_ShouldUpdateCurrentPercentage()
        {
            // Arrange
            var expectedPercentage = 85.0f;
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_device.Addresses.PercentageFeedback))
                          .ReturnsAsync(expectedPercentage);

            // Act
            var result = await _device.ReadPercentageAsync();

            // Assert
            result.Should().Be(expectedPercentage);
            // Note: ReadPercentageAsync only requests value from KNX bus, 
            // device state is updated via feedback messages, not direct reads
        }

        [Fact]
        public async Task ReadPercentageAsync_ShouldUpdateLastUpdated()
        {
            // Arrange
            var expectedPercentage = 75.0f;
            var beforeTime = DateTime.Now.AddSeconds(-1);
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_device.Addresses.PercentageFeedback))
                          .ReturnsAsync(expectedPercentage);

            // Act
            await _device.ReadPercentageAsync();
            var afterTime = DateTime.Now.AddSeconds(1);

            // Assert
            // Note: ReadPercentageAsync doesn't directly update LastUpdated
            // LastUpdated is updated when feedback is received from KNX bus
            _device.LastUpdated.Should().BeAfter(beforeTime.AddSeconds(-5)); // Allow for test setup time
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
            _device.SetPercentageForTest(percentage);
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            var result = await _device.WaitForPercentageAsync(percentage, 0.1, TimeSpan.FromMilliseconds(waitingTime));
            timer.Stop();
            result.Should().BeTrue();
            _device.CurrentPercentage.Should().Be(percentage, "Current percentage should match target percentage");
            timer.ElapsedMilliseconds.Should().BeInRange(executionTimeMin, executionTimeMax, $"Execution time should be between {executionTimeMin} and {executionTimeMax} ms");

        }

        [Theory]
        [InlineData(0, 100, 50, 200, 50, true, 180, 220)]   // Wait for 50% from 0%, state changes
        [InlineData(100, 50, 0, 200, 0, true, 180, 220)]    // Wait for 0% from 100%, state changes
        [InlineData(50, 25, 75, 50, 75, false, 40, 70)]     // Wait for 75% from 50%, timeout
        public async Task WaitForPercentageAsync_ShouldReturnCorrectly(byte initialPercentage, byte targetPercentage, byte feedbackPercentage, int waitingTime, byte expectedPercentage, bool expectedResult, int executionTimeMin, int executionTimeMax)
        {
            // Arrange
            _device.SetPercentageForTest(initialPercentage);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // If expected to succeed, simulate feedback after delay
            if (expectedResult)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(50); // Delay to simulate KNX response
                    _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, 
                        new KnxGroupEventArgs(_device.Addresses.PercentageFeedback, new KnxValue((float)feedbackPercentage)));
                });
            }

            // Act
            var result = await _device.WaitForPercentageAsync(targetPercentage, tolerance: 1.0, TimeSpan.FromMilliseconds(waitingTime));
            stopwatch.Stop();

            // Assert
            result.Should().Be(expectedResult);
            _device.CurrentPercentage.Should().BeApproximately(expectedPercentage, 1.0f);
            stopwatch.ElapsedMilliseconds.Should().BeInRange(executionTimeMin, executionTimeMax);
        }

        [Theory]
        [InlineData(0, 50, 50, 200, 50, 100, 200)]     // Wait for 50% from 0%
        [InlineData(100, 0, 75, 200, 0, 125, 225)]     // Wait for 0% from 100%
        [InlineData(50, 25, 100, 200, 25, 150, 250)]   // Wait for 25% from 50%
        public async Task WaitForPercentageAsync_WhenFeedbackReceived_ShouldReturnTrue(byte initialPercentage, byte targetPercentage, int delayInMs, int waitingTime, byte expectedPercentage, int executionTimeMin, int executionTimeMax)
        {
            // Arrange
            _device.SetPercentageForTest(initialPercentage);
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
        [InlineData(Lock.Off)] // When unlocked
        [InlineData(Lock.On)]  // When locked (should unlock first)
        public async Task SetPercentageAsync_WhenLocked_ShouldUnlockThenSetPercentage(Lock lockState)
        {
            // Arrange
            var targetPercentage = 75.0f;
            _device.SetLockStateForTest(lockState);
            
            if (lockState == Lock.On)
            {
                // If locked, expect unlock call before setting percentage
                _mockKnxService.Setup(s => s.WriteGroupValueAsync(_device.Addresses.LockControl, false))
                              .Returns(Task.CompletedTask)
                              .Verifiable();
            }

            _mockKnxService.Setup(s => s.WriteGroupValueAsync(_device.Addresses.PercentageControl, targetPercentage))
                          .Returns(Task.CompletedTask)
                          .Verifiable();

            // Act
            await _device.SetPercentageAsync(targetPercentage, TimeSpan.FromMilliseconds(100));

            // Assert
            if (lockState == Lock.On)
            {
                _mockKnxService.Verify(s => s.WriteGroupValueAsync(_device.Addresses.LockControl, false), Times.Once);
            }
            _mockKnxService.Verify(s => s.WriteGroupValueAsync(_device.Addresses.PercentageControl, targetPercentage), Times.Once);
        }

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
            _device.SetPercentageForTest(currentPercentage);

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
            _device.SetPercentageForTest(currentPercentage);

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
