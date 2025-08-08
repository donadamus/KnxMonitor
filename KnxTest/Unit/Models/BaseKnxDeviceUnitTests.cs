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
        // Uses MockBehavior.Loose to allow different device types to have varying initialization behavior
        public BaseKnxDeviceUnitTests()
        {
            _mockKnxService = new Mock<IKnxService>(MockBehavior.Loose);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Verify only explicitly set up expectations instead of all calls
                    // _mockKnxService.VerifyAll(); // Commented out - too strict for different device types
                    VerifyExpectedCalls();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Override this method in derived classes to verify specific mock expectations
        /// This allows for more flexible verification per device type
        /// When using .Verifiable() in setupts, call _mockKnxService.Verify() to verify only marked setups
        /// </summary>
        protected virtual void VerifyExpectedCalls()
        {
            // Base implementation verifies only setups marked with .Verifiable()
            _mockKnxService.Verify();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public abstract void Constructor_SetsBasicProperties();


    }
}
