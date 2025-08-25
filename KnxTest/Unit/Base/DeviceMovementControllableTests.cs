using KnxModel;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;

namespace KnxTest.Unit.Base
{
    public abstract class DeviceMovementControllableTests<TDevice, TAddresses> : BaseKnxDeviceUnitTests
    where TDevice : IMovementControllable, IKnxDeviceBase, IActivityStatusReadable
    where TAddresses : IMovementControllableAddress

    {
        protected abstract MovementControllableDeviceTestHelper<TDevice, TAddresses> _movementTestHelper { get; }

        public DeviceMovementControllableTests() : base()
        {
        }

        [Fact]
        public async Task OpenAsync_ShouldSendCorrectTelegram()
        {
            await _movementTestHelper.OpenAsync_ShouldSendCorrectTelegram();
        }

        [Fact]
        public async Task CloseAsync_ShouldSendCorrectTelegram()
        {
            await _movementTestHelper.CloseAsync_ShouldSendCorrectTelegram();
        }

        [Fact]
        public async Task StopAsync_ShouldSendCorrectTelegram()
        {
            await _movementTestHelper.StopAsync_ShouldSendCorrectTelegram();
        }

        [Fact]
        public void Device_ImplementsAllRequiredInterfaces()
        {
            _movementTestHelper.Device_ImplementsAllRequiredInterfaces();
           
        }


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task InitializeAsync_UpdatesLastUpdatedAndStates(bool movementActive)
        {
            await _movementTestHelper.InitializeAsync_UpdatesLastUpdatedAndStates(movementActive);
        }

    }
}
