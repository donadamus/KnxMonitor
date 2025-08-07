using FluentAssertions;
using KnxModel;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Integration.Base
{
    /// <summary>
    /// Async version of DeviceTestBase for tests that require async cleanup
    /// </summary>
    public abstract class IntegrationTestBase : IDisposable
    {
        protected readonly IKnxService _knxService;

        protected IntegrationTestBase(KnxServiceFixture fixture)
        {
            _knxService = fixture.KnxService;
        }

        // ===== ASYNC CLEANUP =====

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
