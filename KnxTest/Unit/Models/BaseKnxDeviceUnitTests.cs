using Moq;
using KnxModel;

namespace KnxTest.Unit.Models
{
    public class BaseKnxDeviceUnitTests : IDisposable
    {
        protected readonly Mock<IKnxService> _mockKnxService;
        // Base class for common unit tests for KNX devices
        // This can be extended for specific device types like Light, Switch, etc.
        public BaseKnxDeviceUnitTests()
        {
            _mockKnxService = new Mock<IKnxService>(MockBehavior.Strict);
        }
        public void Dispose()
        {
            _mockKnxService.VerifyAll();
        }
    }
}
