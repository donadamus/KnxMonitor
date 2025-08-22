using FluentAssertions;
using KnxModel;
using KnxTest.Unit.Base;
using Microsoft.Extensions.Logging;
using Moq;

namespace KnxTest.Unit.Models
{
    /// <summary>
    /// Base unit tests for devices that implement ILockableDevice
    /// Tests lock-specific functionality
    /// </summary>
    public abstract class LockDeviceTestsBase<TDevice, TAddressess> : BaseKnxDeviceUnitTests
        where TDevice : LockableDeviceBase<TDevice, TAddressess>, ILockableDevice, IKnxDeviceBase
        where TAddressess : ILockableAddress
    {
        protected abstract TDevice _device { get; }

        protected abstract ILogger<TDevice> _logger { get; }

        public LockDeviceTestsBase() : base()
        {
        }




        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _device?.Dispose();
            }
            base.Dispose(disposing); // WAŻNE: Wywołaj base.Dispose(disposing) aby zweryfikować mocki
        }
    }
}
