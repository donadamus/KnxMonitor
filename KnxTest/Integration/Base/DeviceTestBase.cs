using FluentAssertions;
using KnxModel;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Integration.Base
{
    /// <summary>
    /// Simple base class providing common infrastructure for all device tests
    /// Provides: _knxService connection, basic initialization helpers
    /// Does NOT enforce any specific device type or test structure
    /// </summary>
    public abstract class DeviceTestBase : IDisposable
    {
        protected readonly IKnxService _knxService;

        protected DeviceTestBase(KnxServiceFixture fixture)
        {
            _knxService = fixture.KnxService;
        }

        // ===== SIMPLE CLEANUP =====

        public abstract void Dispose();
    }
}
