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
    public abstract class IntegrationTestBase<TDevice> : IDisposable
        where TDevice : IKnxDeviceBase
    {
        protected readonly IKnxService _knxService;
        internal TDevice? Device { get; set; }
        protected IntegrationTestBase(KnxServiceFixture fixture)
        {
            _knxService = fixture.KnxService;
        }

        // ===== ASYNC CLEANUP =====

        public virtual void Dispose()
        {
            try
            {
                Device?.RestoreSavedStateAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to restore device state during cleanup: {ex.Message}");
            }
            finally
            {
                Device?.Dispose();
                GC.SuppressFinalize(this);
            }

        }
    }
}
