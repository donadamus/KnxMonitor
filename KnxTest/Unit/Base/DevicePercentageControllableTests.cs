using KnxModel;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;

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

    }
}
