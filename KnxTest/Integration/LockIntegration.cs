using FluentAssertions;
using KnxModel;
using KnxService;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Integration
{
    [Collection("KnxService collection")]
    public class LightIntegration : LockIntegration
    {
        public static IEnumerable<object[]> LightIdsFromConfig
        {
            get
            {
                var config = LightFactory.LightConfigurations;
                return config.Where(k => k.Value.Name.Contains("Office"))
                    .Select(k => new object[] { k.Key });
            }
        }

        public LightIntegration(KnxServiceFixture fixture) : base(fixture)
        {
            // Constructor for LightIntegration, passing the fixture to the base class
        }

        public virtual async Task OK_CanInitializeLightAndReadState(string lightId)
        {
            // Act
            await InitializeLight(lightId);

            // Assert
            _light.CurrentState.Should().NotBeNull($"Light {lightId} should have a valid current state after initialization");
            _light.CurrentState.Switch.Should().NotBe(Switch.Unknown, $"Light {lightId} should have a known switch state after initialization");
            _light.CurrentState.Lock.Should().NotBe(Lock.Unknown, $"Light {lightId} should have a known lock state after initialization");
            _light.CurrentState.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1),
                $"Light {lightId} last updated time should be close to now after initialization");
        }

    }
    
    [Collection("KnxService collection")]
    public class DImmIntegration : LightIntegration
    {
        public static IEnumerable<object[]> DimmIdsFromConfig
        {
            get
            {
                //var config = LightFactory.LightConfigurations;
                //var res = config.Where(k => k.Value.Name.Contains("Office"))
                //    .Select(k => new object[] { k.Key });

                var config2 = DimmerFactory.DimmerConfigurations;
                return config2//.Where(k => k.Value.Name.Contains("Office"))
                    .Select(k => new object[] { k.Key });

                //return res.Concat(res2);
            }
        }

        public DImmIntegration(KnxServiceFixture fixture) : base(fixture)
        {
            // Constructor for LightIntegration, passing the fixture to the base class
        }

        [Theory]
        [MemberData(nameof(DimmIdsFromConfig))]
        [MemberData(nameof(LightIdsFromConfig))]
        public override async Task OK_CanInitializeLightAndReadState(string lightId)
        {
            await base.OK_CanInitializeLightAndReadState(lightId);
            Console.WriteLine($"DimmIntegration: Initialized light with ID {lightId}");
        }
    }



    [Collection("KnxService collection")]
    public abstract class LockIntegration : IDisposable
    {
        


        private static IKnxService _knxServiceMock = new Moq.Mock<IKnxService>().Object;
        protected readonly IKnxService _knxService;

        private static readonly Light _defaultLight = new Light("L11", "Test Light 11", "1", _knxServiceMock);
        protected ILight _light = _defaultLight;

        public LockIntegration(KnxServiceFixture fixture)
        {
            _knxService = fixture.KnxService;
        }

        //[Theory]
        //[MemberData(nameof(LightIdsFromConfig))]

        

        private async Task InitializeLightAndEnsureLightIsUnlocked(string lightId)
        {
            // Initialize the default light and ensure it is unlocked
            await InitializeLight(lightId);
            await EnsureLightIsUnlockedBeforeTest();
        }

        protected async Task InitializeLight(string lightId)
        {
            _light = LightFactory.CreateLight(lightId, _knxService);
            await _light.InitializeAsync();

            var initialState = _light.CurrentState.Switch;
            var initialLockState = _light.CurrentState.Lock;

            Console.WriteLine($"Initial state: {initialState}, Lock: {initialLockState}");
        }

        private void CreateLightWithoutInitializing(string lightId)
        {
            _light = LightFactory.CreateLight(lightId, _knxService);
        }

        protected async Task EnsureLightIsUnlockedBeforeTest()
        {
            if (_light.CurrentState.Lock == Lock.On)
            {
                await _light.UnlockAsync(); // Ensure light is unlocked before testing lock prevention
                await _light.ReadLockStateAsync(); // Ensure lock state is updated
            }
            _light.CurrentState.Lock.Should().Be(Lock.Off, "Light should be unlocked before testing lock prevention");
        }

        public void Dispose()
        {
            if (_light != _defaultLight)
            {
                _light.RestoreSavedStateAsync().GetAwaiter().GetResult();
                _light.Dispose();
            }
        }
    }
}
