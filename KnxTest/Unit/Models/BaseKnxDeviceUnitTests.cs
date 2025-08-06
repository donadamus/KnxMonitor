using System;
using Moq;
using KnxModel;

namespace KnxTest.Unit.Models
{
    public abstract class BaseKnxDeviceUnitTests : IDisposable
    {
        protected readonly Mock<IKnxService> _mockKnxService;
        private bool _disposed = false;

        // Base class for common unit tests for KNX devices
        // This can be extended for specific device types like Light, Switch, etc.
        public BaseKnxDeviceUnitTests()
        {
            _mockKnxService = new Mock<IKnxService>(MockBehavior.Strict);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _mockKnxService.VerifyAll();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public abstract void Constructor_SetsBasicProperties();


    }
}
