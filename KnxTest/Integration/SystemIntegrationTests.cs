using FluentAssertions;
using KnxModel;
using KnxService;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Integration
{
    // All tests have been moved to their respective test classes:
    // - Light tests moved to LightIntegrationTests.cs
    // - Shutter tests moved to ShutterIntegrationTests.cs
    // - Dimmer tests should be added to DimmerIntegrationTests.cs when that class is refactored

    public class KnxServiceFixture : IDisposable
    {
        public IKnxService KnxService { get; private set; }
        public KnxServiceFixture()
        {
            KnxService = new KnxService.KnxService();
        }
        public void Dispose()
        {
            KnxService.Dispose();
        }
    }


    [CollectionDefinition("KnxService collection")]
    public class KnxServiceCollection : ICollectionFixture<KnxServiceFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
