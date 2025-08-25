using FluentAssertions;
using KnxModel;
using Moq;
using Xunit;

namespace KnxTest.Unit.Helpers
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

        internal async Task DecreasePercentageAsync_ShouldNotGoBelowMinimum(float currentPercentage, float decrement, float expectedResult)
        {
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(_addresses.PercentageControl, expectedResult))
                          .Returns(Task.CompletedTask)
                          .Callback(() =>
                          {
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_addresses.PercentageFeedback, new KnxValue(expectedResult)));
                          })
                          .Verifiable();
            ((IPercentageControllable)_device).SetPercentageForTest(currentPercentage);

            await _device.AdjustPercentageAsync(decrement, TimeSpan.FromMilliseconds(100));

            _device.CurrentPercentage.Should().Be(expectedResult);
        }

        internal async Task DecreasePercentageAsync_ShouldSendCorrectTelegram(float decrement)
        {
            var currentPercentage = 50; // Assume starting at 50%
            var expectedResult = Math.Max(0, currentPercentage + decrement);
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(_addresses.PercentageControl, expectedResult))
                          .Returns(Task.CompletedTask)
                          .Verifiable();
            ((IPercentageControllable)_device).SetPercentageForTest(currentPercentage);

            await _device.AdjustPercentageAsync(decrement, TimeSpan.Zero);
        }

        internal void Device_ImplementsAllRequiredInterfaces()
        {
            // Assert
            _device.Should().BeAssignableTo<IKnxDeviceBase>();
            _device.Should().BeAssignableTo<IPercentageControllable>();
        }

        internal async Task IncreasePercentageAsync_ShouldNotExceedMaximum(float currentPercentage, float increment, float expectedResult)
        {
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(_addresses.PercentageControl, expectedResult))
             .Returns(Task.CompletedTask)
             .Callback(() =>
             {
                 _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_addresses.PercentageFeedback, new KnxValue(expectedResult)));
             })
             .Verifiable();
            ((IPercentageControllable)_device).SetPercentageForTest(currentPercentage);

            await _device.AdjustPercentageAsync(increment, TimeSpan.FromMilliseconds(100));

            _device.CurrentPercentage.Should().Be(expectedResult);
        }

        internal async Task IncreasePercentageAsync_ShouldSendCorrectTelegram(float increment)
        {
            var currentPercentage = 50; // Assume starting at 50%
            var expectedResult = Math.Max(0, currentPercentage + increment);
            _mockKnxService.Setup(s => s.WriteGroupValueAsync(_addresses.PercentageControl, expectedResult))
                          .Returns(Task.CompletedTask)
                          .Verifiable();
            ((IPercentageControllable)_device).SetPercentageForTest(currentPercentage);

            await _device.AdjustPercentageAsync(increment, TimeSpan.Zero);

        }

        internal async Task InitializeAsync_UpdatesLastUpdatedAndStates(float percentage)
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_addresses.PercentageFeedback))
                          .ReturnsAsync(percentage)
                          .Verifiable();
            
            // Act
            await _device.InitializeAsync();

            // Assert
            _device.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            _device.CurrentPercentage.Should().Be(percentage);
        }

        internal void InvalidPercentageFeedback_ShouldBeHandledGracefully(float invalidPercentage)
        {
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_addresses.PercentageFeedback, new KnxValue(invalidPercentage)));
        }

        internal void OnAnyFeedbackToUnknownAddress_ShouldProcessCorrectlyAndDoesNotChangeState(float percentage)
        {
            _device.SetPercentageForTest(percentage);
            var currentDate = _device.LastUpdated;
            var unknownAddress = "9/9/9";
            var feedbackArgsTrue = new KnxGroupEventArgs(unknownAddress, new KnxValue(true));
            var feedbackArgsFalse = new KnxGroupEventArgs(unknownAddress, new KnxValue(false));
            // Act
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgsTrue);
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgsFalse);
            // Assert
            // Ensure no state change for unknown address
            _device.CurrentPercentage.Should().Be(percentage);
            _device.LastUpdated.Should().Be(currentDate, "LastUpdated should not change on unknown feedback");

        }

        internal void OnPercentageFeedback_ShouldNotAffectSwitchState(Switch switchState)
        {
            
            var switchableDevice = _device as ISwitchable;

            if (switchableDevice == null)
            {
                return;
            }

            switchableDevice!.SetSwitchForTest(switchState);
            ((IPercentageControllable)_device).SetPercentageForTest(20); // Set initial percentage
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_addresses.PercentageFeedback, new KnxValue(50)));
            _device.CurrentPercentage.Should().Be(50);
            switchableDevice.CurrentSwitchState.Should().Be(switchState);
        }

        internal void OnPercentageFeedback_ShouldUpdateState(float expectedPercentage)
        {
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_addresses.PercentageFeedback, new KnxValue(expectedPercentage)));

            _device.CurrentPercentage.Should().Be(expectedPercentage);
        }

        internal async Task ReadPercentageAsync_ShouldRequestCorrectAddress()
        {
            // Arrange
            var expectedValue = 75.0f;
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_addresses.PercentageFeedback))
                          .ReturnsAsync(expectedValue)
                          .Verifiable();

            // Act
            var result = await _device.ReadPercentageAsync();

            // Assert
            result.Should().Be(expectedValue);
            _mockKnxService.Verify(s => s.RequestGroupValue<float>(_addresses.PercentageFeedback), Times.Once);
        }

        internal async Task ReadPercentageAsync_ShouldReturnCorrectValue(byte expectedPercentage)
        {
            // Arrange
            var expectedValue = (float)expectedPercentage;
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_addresses.PercentageFeedback))
                          .ReturnsAsync(expectedValue);

            // Act
            var result = await _device.ReadPercentageAsync();

            // Assert
            result.Should().Be(expectedValue);
        }

        internal async Task ReadPercentageAsync_WhenKnxServiceThrows_ShouldPropagateException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("KNX service error");
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_addresses.PercentageFeedback))
                          .ThrowsAsync(expectedException);

            // Act & Assert
            await _device.Invoking(d => d.ReadPercentageAsync())
                        .Should().ThrowAsync<InvalidOperationException>()
                        .WithMessage("KNX service error");

        }

        internal async Task RestoreSavedStateAsync_ShouldSendCorrectTelegrams(float initialPercentage, float percentage)
        {
            // Arrange
            _device.SetSavedPercentageForTest(initialPercentage);
            _device.SetPercentageForTest(percentage);

            if (initialPercentage != percentage)
            {
                _mockKnxService.Setup(s => s.WriteGroupValueAsync(_addresses.PercentageControl, initialPercentage)).Returns(Task.CompletedTask).Verifiable();
            }

            // Act
            await _device.RestoreSavedStateAsync(TimeSpan.Zero);
        }

        internal void SaveCurrentState_ShouldStoreCurrentValues(float percentage)
        {
            // Arrange
            _device.SetPercentageForTest(percentage);

            // Act
            _device.SaveCurrentState();

            // Assert
            _device.SavedPercentage.Should().Be(percentage, "Saved percentage should match current state");
            _device.CurrentPercentage.Should().Be(percentage, "Current percentage should remain unchanged");

        }

        internal async Task SetPercentageAsync_WithInvalidValues_ShouldThrowException(float percentage)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _device.SetPercentageAsync(percentage, TimeSpan.Zero));
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

        internal async Task WaitForPercentageAsync_ImmediateReturnTrueWhenAlreadyInState(float percentage, int waitingTime, int executionTimeMin, int executionTimeMax)
        {
            ((IPercentageControllable)_device).SetPercentageForTest(percentage);
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            var result = await _device.WaitForPercentageAsync(percentage, 0.1, TimeSpan.FromMilliseconds(waitingTime));
            timer.Stop();
            result.Should().BeTrue();
            _device.CurrentPercentage.Should().Be(percentage, "Current percentage should match target percentage");
            timer.ElapsedMilliseconds.Should().BeInRange(executionTimeMin, executionTimeMax, $"Execution time should be between {executionTimeMin} and {executionTimeMax} ms");

        }

        internal async Task WaitForPercentageAsync_ShouldReturnCorrectly(byte initialPercentage, byte targetPercentage, byte feedbackPercentage, int waitingTime, byte expectedPercentage, bool expectedResult, int executionTimeMin, int executionTimeMax)
        {
            // Arrange
            _device.SetPercentageForTest(initialPercentage);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _ = Task.Run(async () =>
            {
                await Task.Delay(50); // Delay to simulate KNX response
                _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object,
                    new KnxGroupEventArgs(_addresses.PercentageFeedback, new KnxValue((float)feedbackPercentage)));
            });

            // Act
            var result = await _device.WaitForPercentageAsync(targetPercentage, tolerance: 1.0, TimeSpan.FromMilliseconds(waitingTime));
            stopwatch.Stop();

            // Assert
            result.Should().Be(expectedResult);
            _device.CurrentPercentage.Should().BeApproximately(expectedPercentage, 1.0f);
            stopwatch.ElapsedMilliseconds.Should().BeInRange(executionTimeMin, executionTimeMax);

        }

        internal async Task WaitForPercentageAsync_WhenFeedbackReceived_ShouldReturnTrue(byte initialPercentage, byte targetPercentage, int delayInMs, int waitingTime, byte expectedPercentage, int executionTimeMin, int executionTimeMax)
        {
            // Arrange
            ((IPercentageControllable)_device).SetPercentageForTest(initialPercentage);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Schedule feedback after delay
            _ = Task.Run(async () =>
            {
                await Task.Delay(delayInMs);
                _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object,
                    new KnxGroupEventArgs(_addresses.PercentageFeedback, new KnxValue((float)expectedPercentage)));
            });

            // Act
            var result = await _device.WaitForPercentageAsync(targetPercentage, tolerance: 1.0, TimeSpan.FromMilliseconds(waitingTime));
            stopwatch.Stop();

            // Assert
            result.Should().BeTrue("feedback should have changed percentage to target");
            _device.CurrentPercentage.Should().BeApproximately(expectedPercentage, 1.0f);
            stopwatch.ElapsedMilliseconds.Should().BeInRange(executionTimeMin, executionTimeMax);
        }
    }
}
