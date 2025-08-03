using FluentAssertions;
using KnxModel;
using KnxService;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Integration
{
    [Collection("KnxService collection")]
    public class LightIntegrationTests : IDisposable
    {
        public static IEnumerable<object[]> LightIdsFromConfig
        {
            get
            {
                var config = LightFactory.LightConfigurations; 
                return config//.Where(k => k.Value.Name.Contains("Office"))
                    .Select(k => new object[] { k.Key });
            }
        }


        private static IKnxService _knxServiceMock = new Moq.Mock<IKnxService>().Object;
        private readonly IKnxService _knxService;

        private static readonly Light _defaultLight = new Light("L11", "Test Light 11", "1", _knxServiceMock);
        private ILight _light = _defaultLight;

        public LightIntegrationTests(KnxServiceFixture fixture)
        {
            _knxService = fixture.KnxService;
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]

        public async Task CanInitializeLightAndReadState(string lightId)
        {
            // Arrange
            var light = LightFactory.CreateLight(lightId, _knxService);

            // Act
            await light.InitializeAsync();

            // Assert
            light.Id.Should().Be(lightId);
            light.CurrentState.Should().NotBeNull();
            light.CurrentState.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
            
            Console.WriteLine($"Light {lightId} initialized: {light}");
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]

        public async Task CanSaveAndRestoreLightState(string lightId)
        {
            // Arrange
            var light = LightFactory.CreateLight(lightId, _knxService);
            await light.InitializeAsync();

            // Act & Assert - Save state
            light.SaveCurrentState();
            light.SavedState.Should().NotBeNull();
            light.SavedState!.Switch.Should().Be(light.CurrentState.Switch);

            // Modify light state
            var originalState = light.CurrentState.Switch;
            var testState = originalState.Opposite();
            
            await light.SetStateAsync(testState);
            
            // Verify state changed
            light.CurrentState.Switch.Should().Be(testState);

            // Restore original state
            await light.RestoreSavedStateAsync();
            
            // Verify restoration
            light.CurrentState.Switch.Should().Be(originalState);
            
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]

        public async Task CanToggleLightState(string lightId)
        {
            // Arrange
            var light = LightFactory.CreateLight(lightId, _knxService);
            await light.InitializeAsync();
            light.SaveCurrentState();

            try
            {
                var initialState = light.CurrentState.Switch;
                Console.WriteLine($"Testing light {lightId} toggle from {initialState}");

                // Test toggle
                await light.ToggleAsync();
                
                // Verify state changed via feedback (natural device behavior)
                light.CurrentState.Switch.Should().Be(initialState.Opposite(), $"Light state should be toggled via feedback");
                
                Console.WriteLine($"Light {lightId} successfully toggled to {light.CurrentState.Switch}");

                // Toggle back
                await light.ToggleAsync();
                
                // Verify state restored via feedback (natural device behavior)
                light.CurrentState.Switch.Should().Be(initialState, $"Light state should be restored via feedback");
                
                Console.WriteLine($"Light {lightId} successfully toggled back to {light.CurrentState.Switch}");
            }
            finally
            {
                // Always restore original state
                await light.RestoreSavedStateAsync();
            }
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]

        public async Task CanTurnOnAndOff(string lightId)
        {
            // Arrange
            var light = LightFactory.CreateLight(lightId, _knxService);
            await light.InitializeAsync();
            light.SaveCurrentState();

            try
            {
                var initialState = light.CurrentState.Switch;
                Console.WriteLine($"Testing light {lightId} on/off from {initialState}");

                // Test turn ON
                await light.TurnOnAsync();
                
                // Verify state via feedback (natural device behavior)
                light.CurrentState.Switch.Should().Be(Switch.On,"Light should be ON via feedback");
                Console.WriteLine($"Light {lightId} successfully turned ON");

                // Test turn OFF
                await light.TurnOffAsync();
                
                // Verify state via feedback (natural device behavior)
                light.CurrentState.Switch.Should().Be(Switch.Off,"Light should be OFF via feedback");
                Console.WriteLine($"Light {lightId} successfully turned OFF");
            }
            finally
            {
                // Always restore original state
                await light.RestoreSavedStateAsync();
            }
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]

        public async Task WaitForStateAsync_ReturnsCorrectly(string lightId)
        {
            // Arrange
            var light = LightFactory.CreateLight(lightId, _knxService);
            await light.InitializeAsync();
            light.SaveCurrentState();

            try
            {
                var initialState = light.CurrentState.Switch;
                var targetState = initialState.Opposite();

                // Act - Change state and wait
                await light.SetStateAsync(targetState);
                var success = await light.WaitForStateAsync(targetState, TimeSpan.FromSeconds(2));

                // Assert
                success.Should().BeTrue($"Should successfully wait for state {targetState}");
                light.CurrentState.Switch.Should().Be(targetState);
                
                Console.WriteLine($"Light {lightId} wait test passed");
            }
            finally
            {
                // Always restore original state
                await light.RestoreSavedStateAsync();
            }
        }

        [Fact]
        public void LightFactory_CanCreateAllLights()
        {
            // Act
            var lights = LightFactory.CreateAllLights(_knxService);
            var lightList = lights.ToList();

            // Assert
            lightList.Should().HaveCountGreaterThan(10, "Should create multiple lights");
            
            var lightIds = lightList.Select(l => l.Id).ToList();
            lightIds.Should().Contain("L11");
            lightIds.Should().Contain("L15");
            
            Console.WriteLine($"Created {lightList.Count} lights: {string.Join(", ", lightIds.Take(10))}...");
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]

        public void LightModel_HasCorrectConfiguration(string lightId, string expectedSubGroup, string expectedName)
        {
            // Act
            var light = LightFactory.CreateLight(lightId, _knxService);

            // Assert
            light.Id.Should().Be(lightId);
            light.SubGroup.Should().Be(expectedSubGroup);
            light.Name.Should().Be(expectedName);
            
            // Verify addresses are calculated correctly
            var feedbackSubGroup = (int.Parse(expectedSubGroup) + KnxAddressConfiguration.LIGHT_FEEDBACK_OFFSET).ToString();
            var lockSubGroup = (int.Parse(expectedSubGroup) + KnxAddressConfiguration.LIGHTS_LOCK_MIDDLE_GROUP).ToString();
            var lockFeedbackSubGroup = (int.Parse(lockSubGroup) + KnxAddressConfiguration.LIGHT_FEEDBACK_OFFSET).ToString();
            
            light.Addresses.Control.Should().Be($"1/1/{expectedSubGroup}");
            light.Addresses.Feedback.Should().Be($"1/1/{feedbackSubGroup}");
            light.Addresses.LockControl.Should().Be($"1/2/{lockSubGroup}");
            light.Addresses.LockFeedback.Should().Be($"1/2/{lockFeedbackSubGroup}");
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]

        public async Task CanSetAndReadLightLockState(string lightId)
        {
            // Arrange
            var light = LightFactory.CreateLight(lightId, _knxService);
            await light.InitializeAsync();
            light.SaveCurrentState();

            try
            {
                // Test locking
                Console.WriteLine($"Testing lock functionality for light {lightId}");
                
                // Act & Assert - Lock the light
                await light.SetLockAsync(Lock.On);
                light.CurrentState.Lock.Should().Be(Lock.On, "Light should be locked via feedback");
                Console.WriteLine($"✅ Light {lightId} successfully locked");

                // Act & Assert - Unlock the light
                await light.SetLockAsync(Lock.Off);
                light.CurrentState.Lock.Should().Be(Lock.Off, "Light should be unlocked via feedback");
                Console.WriteLine($"✅ Light {lightId} successfully unlocked");

                // Test convenience methods
                await light.LockAsync();
                light.CurrentState.Lock.Should().Be(Lock.On);
                Console.WriteLine($"✅ Light {lightId} LockAsync() works");

                await light.UnlockAsync();
                light.CurrentState.Lock.Should().Be(Lock.Off);
                Console.WriteLine($"✅ Light {lightId} UnlockAsync() works");
            }
            finally
            {
                // Cleanup - restore original state
                await light.RestoreSavedStateAsync();
                Console.WriteLine($"Light {lightId} lock test completed");
            }
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]

        public async Task CanWaitForLightLockState(string lightId, Lock targetLockState)
        {
            // Arrange
            var light = LightFactory.CreateLight(lightId, _knxService);
            await light.InitializeAsync();
            light.SaveCurrentState();

            try
            {
                Console.WriteLine($"Testing wait for lock state {targetLockState} for light {lightId}");

                // Act - Set opposite state first
                var oppositeLockState = targetLockState == Lock.On ? Lock.Off : Lock.On;
                await light.SetLockAsync(oppositeLockState);
                
                // Start waiting for target state in background
                var waitTask = light.WaitForLockStateAsync(targetLockState, TimeSpan.FromSeconds(10));
                
                // Trigger state change after a delay
                await Task.Delay(500);
                await light.SetLockAsync(targetLockState);

                // Assert
                var result = await waitTask;
                result.Should().BeTrue();
                light.CurrentState.Lock.Should().Be(targetLockState);
                
                Console.WriteLine($"✅ Light {lightId} wait for lock state test passed");
            }
            finally
            {
                // Cleanup
                await light.RestoreSavedStateAsync();
                Console.WriteLine($"Light {lightId} wait lock test completed");
            }
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]

        public async Task LockPreventsStateChanges(string lightId)
        {
            // Arrange
            var light = LightFactory.CreateLight(lightId, _knxService);
            await light.InitializeAsync();
            
            // Save initial state for cleanup
            light.SaveCurrentState();
            var initialState = light.CurrentState.Switch;
            var initialLockState = light.CurrentState.Lock;
            
            Console.WriteLine($"Testing lock functionality prevents state changes for light {lightId}");
            Console.WriteLine($"Initial state: {initialState}, Lock: {initialLockState}");

            try
            {
                // Step 1: Ensure light is unlocked and set to known state
                await light.SetLockAsync(Lock.Off);
                await light.SetStateAsync(Switch.On); // Turn ON
                light.CurrentState.Switch.Should().Be(Switch.On,"Light should be ON when unlocked");
                light.CurrentState.Lock.Should().Be(Lock.Off,"Light should be unlocked");
                Console.WriteLine($"✅ Step 1: Light {lightId} set to ON and unlocked");

                // Step 2: Try to lock the light - for lights we don't expect feedback, use zero timeout
                Console.WriteLine($"Step 2: Setting lock for light {lightId} (lights may not provide lock feedback)");
                await light.SetLockAsync(Lock.On, TimeSpan.Zero);
                
                // For lights, we don't wait for feedback - just assume the lock command was sent
                // The real test is whether the lock actually prevents state changes
                Console.WriteLine($"✅ Step 2: Lock command sent to light {lightId}, testing if it prevents state changes");
                var stateBeforeLockTest = light.CurrentState.Switch;

                // Step 3: Now test if lock actually prevents state changes
                Console.WriteLine($"Step 3: Testing if lock prevents state changes...");
                
                // Try to turn OFF while locked
                await light.SetStateAsync(Switch.Off);
                
                // Check if state changed (it shouldn't have)
                bool stateChangedWhileLocked = light.CurrentState.Switch != stateBeforeLockTest;
                
                if (stateChangedWhileLocked)
                {
                    Console.WriteLine($"❌ FAILURE: Light {lightId} state changed while locked! Lock is not preventing state changes.");
                    Console.WriteLine($"State before lock test: {stateBeforeLockTest}, State after attempting change: {light.CurrentState.Switch}");
                    light.CurrentState.Switch.Should().Be(stateBeforeLockTest, 
                        "Light state should NOT change when locked - lock should prevent state changes");
                }
                else
                {
                    Console.WriteLine($"✅ Step 3: Light {lightId} correctly ignored state change while locked");
                }

                // Step 4: Unlock the light
                await light.SetLockAsync(Lock.Off);
                light.CurrentState.Lock.Should().Be(Lock.Off,"Light should be unlocked");
                Console.WriteLine($"✅ Step 4: Light {lightId} unlocked");

                // Step 5: Verify state can be changed after unlocking
                var targetState = light.CurrentState.Switch.Opposite();
                await light.SetStateAsync(targetState);
                light.CurrentState.Switch.Should().Be(targetState, 
                    "Light state should change normally after unlocking");
                Console.WriteLine($"✅ Step 5: Light {lightId} state changed to {targetState} after unlocking");

                Console.WriteLine($"🎉 Lock prevention test completed successfully for light {lightId}");
            }
            finally
            {
                // Always restore original state and lock state
                try
                {
                    await light.SetLockAsync(Lock.Off); // Ensure unlocked for cleanup
                    await light.RestoreSavedStateAsync();
                    if (initialLockState == Lock.On)
                    {
                        await light.SetLockAsync(Lock.On); // Restore original lock state if needed
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Warning during cleanup: {ex.Message}");
                }
                Console.WriteLine($"Light {lightId} lock prevention test completed and state restored");
            }
        }

        [Theory]
        [MemberData(nameof(LightIdsFromConfig))]
        public async Task CanToggleLightAndToggleBackToTheInitialState(string lightId)
        {
            // Arrange
            _light = LightFactory.CreateLight(lightId, _knxService);
            await _light.InitializeAsync();

            //_light.RemoveLock(); // Ensure no lock is set for this test

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
        public async Task CanReadLightFeedback(string lightId)
        {
            // Arrange
            _light = LightFactory.CreateLight(lightId, _knxService);
            await _light.InitializeAsync();

            // Read initial state from model (updated during InitializeAsync)
            var initialState = _light.CurrentState.Switch;
            Console.WriteLine($"Light {lightId} initial state: {initialState}");
            _light.CurrentState.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));

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
