using FluentAssertions;
using KnxModel;
using KnxService;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Integration
{
    [Collection("KnxService collection")]
    public class OldLightIntegrationTests : IDisposable
    {
        public static IEnumerable<object[]> LightIdsFromConfig
        {
            get
            {
                var config = LightFactory.LightConfigurations; 
                var res = config.Where(k => k.Value.Name.Contains("Office"))
                    .Select(k => new object[] { k.Key });

                var config2 = DimmerFactory.DimmerConfigurations;
                var res2 = config2//.Where(k => k.Value.Name.Contains("Office"))
                    .Select(k => new object[] { k.Key });

                return res.Concat(res2);
            }
        }


        private static IKnxService _knxServiceMock = new Moq.Mock<IKnxService>().Object;
        private readonly IKnxService _knxService;

        private static readonly Light _defaultLight = new Light("L11", "Test Light 11", "1", _knxServiceMock);
        private ILight _light = _defaultLight;

        public OldLightIntegrationTests(KnxServiceFixture fixture)
        {
            _knxService = fixture.KnxService;
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]

        public async Task OK_CanInitializeLightAndReadState(string lightId)
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

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]

        public async Task OK_CanTurnOnAndOff(string lightId)
        {
            // Arrange
            await InitializeLightAndEnsureLightIsUnlocked(lightId);

            var initialState = _light.CurrentState.Switch;
            initialState.Should().NotBe(Switch.Unknown, "Initial state should not be unknown");

            if (initialState == Switch.On)
            {
                await _light.TurnOffAsync(); // Turn OFF
                _light.CurrentState.Switch.Should().Be(Switch.Off, "Light should be OFF before testing ON/OFF");
                Console.WriteLine($"Light {lightId} was successfully turned OFF");

                await _light.TurnOnAsync(); // Turn ON
                _light.CurrentState.Switch.Should().Be(Switch.On, "Light should be ON after initial turn ON");
                Console.WriteLine($"Light {lightId} was successfully turned ON");
            }
            else
            {
                await _light.TurnOnAsync(); //Turn ON
                _light.CurrentState.Switch.Should().Be(Switch.On, "Light should be ON after turn ON");
                Console.WriteLine($"Light {lightId} was successfully turned ON");

                await _light.TurnOffAsync(); // Turn OFF
                _light.CurrentState.Switch.Should().Be(Switch.Off, "Light should be OFF after turn OFF");
                Console.WriteLine($"Light {lightId} was successfully turned OFF");
            }

        }

        [Fact]
        public void OK_LightFactory_CanCreateAllLights()
        {
            // Act
            var lights = LightFactory.CreateAllLights(_knxServiceMock);
            var lightList = lights.ToList();

            // Assert
            lightList.Should().HaveCount(LightFactory.LightConfigurations.Count,
                "All lights should be created based on the configuration");
            lightList.Should().NotBeEmpty("There should be at least one light created");
            lightList.Should().OnlyHaveUniqueItems("All lights should have unique IDs");
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]

        public void OK_LightModel_HasCorrectConfiguration(string lightId)
        {
            // Act
            _light = LightFactory.CreateLight(lightId, _knxServiceMock);

            LightFactory.LightConfigurations.TryGetValue(lightId, out var config);
            config.Should().NotBeNull($"Configuration for light {lightId} should exist");

            var addressControl = KnxAddressConfiguration.CreateLightControlAddress(config.SubGroup);
            var addressFeedback = KnxAddressConfiguration.CreateLightFeedbackAddress(config.SubGroup);
            var addressLockControl = KnxAddressConfiguration.CreateLightLockAddress(config.SubGroup);
            var addressLockFeedback = KnxAddressConfiguration.CreateLightLockFeedbackAddress(config.SubGroup);

            // Assert addresses
            _light.Addresses.Control.Should().Be(addressControl, $"Control address for light {lightId} should match");
            _light.Addresses.Feedback.Should().Be(addressFeedback, $"Feedback address for light {lightId} should match");
            _light.Addresses.LockControl.Should().Be(addressLockControl, $"Lock control address for light {lightId} should match");
            _light.Addresses.LockFeedback.Should().Be(addressLockFeedback, $"Lock feedback address for light {lightId} should match");

            // Assert light properties
            _light.Id.Should().Be(lightId, $"Light ID should match {lightId}");
            _light.Name.Should().Be(config.Name, $"Light name should match {config.Name}");
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]

        public async Task OK_LockPreventsStateChanges(string lightId)
        {
            Console.WriteLine($"Testing lock functionality prevents state changes for light {lightId}");

            // Arrange
            await InitializeLightAndEnsureLightIsUnlocked(lightId);

            await _light.TurnOnAsync(); // Turn on the light to set initial state
            _light.CurrentState.Switch.Should().Be(Switch.On, "Light should be ON before locking");

            await _light.LockAsync(); // Ensure light is locked before testing
            await _light.ReadLockStateAsync(); // Ensure lock state is updated

            var waitForStateResponse = await _light.WaitForStateAsync(Switch.Off, TimeSpan.FromSeconds(2)); // Wait for state change to OFF

            waitForStateResponse.Should().BeTrue("Should successfully wait for state OFF after locking");

            _light.CurrentState.Switch.Should().Be(Switch.Off, "Light should be automatically off when locked");
            _light.CurrentState.Lock.Should().Be(Lock.On, "Light should be locked before testing state change prevention");

            await _light.TurnOnAsync(); // Attempt to turn on the light while locked

            _light.CurrentState.Switch.Should().Be(Switch.Off,
                "Light state should NOT change when locked - lock should prevent state changes");
            Console.WriteLine($"Light {lightId} state after lock: {_light.CurrentState.Switch}");

            Console.WriteLine($"🎉 Lock prevention test completed successfully for light {lightId}");
        }

        private async Task InitializeLightAndEnsureLightIsUnlocked(string lightId)
        {
            // Initialize the default light and ensure it is unlocked
            await InitializeLight(lightId);
            await EnsureLightIsUnlockedBeforeTest();
        }

        private async Task InitializeLight(string lightId)
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

        private async Task EnsureLightIsUnlockedBeforeTest()
        {
            if (_light.CurrentState.Lock == Lock.On)
            {
                await _light.UnlockAsync(); // Ensure light is unlocked before testing lock prevention
                await _light.ReadLockStateAsync(); // Ensure lock state is updated
            }
            _light.CurrentState.Lock.Should().Be(Lock.Off, "Light should be unlocked before testing lock prevention");
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task OK_CanToggleLightAndToggleBackToTheInitialState(string lightId)
        {
            // Arrange
            await InitializeLightAndEnsureLightIsUnlocked(lightId);

            var initialState = _light.CurrentState.Switch;
            Console.WriteLine($"Testing light {lightId} toggle from {initialState}");

            // Act + Assert (1): Toggle to opposite state
            await _light.ToggleAsync();
            _light.CurrentState.Switch.Should().Be(initialState.Opposite(), 
                $"Light {lightId} should toggle to opposite state {initialState.Opposite()}");

            Console.WriteLine($"✓ Light {lightId} successfully toggled to {_light.CurrentState.Switch}");

            // Act + Assert (2): Toggle back to original state
            await _light.ToggleAsync();
            _light.CurrentState.Switch.Should().Be(initialState, 
                $"Light {lightId} should toggle back to original state {initialState}");

            Console.WriteLine($"✓ Light {lightId} successfully restored to {_light.CurrentState.Switch}");
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task OK_CanReadLightFeedbackAndCurrentStateIsUpdated(string lightId)
        {
            // Arrange
            CreateLightWithoutInitializing(lightId);

            var state = await _light.ReadStateAsync();
            state.Should().NotBe(Switch.Unknown, 
                $"Light {lightId} should return a known state when reading feedback");
            _light.CurrentState.Switch.Should().Be(state, 
                $"Light {lightId} current state should match the read state");
            _light.CurrentState.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1),
                $"Light {lightId} last updated time should be close to now after reading feedback");

            var lockState = await _light.ReadLockStateAsync();
            lockState.Should().NotBe(Lock.Unknown, 
                $"Light {lightId} should return a known lock state when reading feedback");
            _light.CurrentState.Lock.Should().Be(lockState, 
                $"Light {lightId} current lock state should match the read lock state");
            _light.CurrentState.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1),
                $"Light {lightId} last updated time should be close to now after reading feedback");

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
