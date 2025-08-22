using FluentAssertions;
using KnxModel;
using Moq;

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

        internal void OnPercentageFeedback_ShouldUpdateState(float expectedPercentage)
        {
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_addresses.PercentageFeedback, new KnxValue(expectedPercentage)));

            _device.CurrentPercentage.Should().Be(expectedPercentage);
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
    }
}
