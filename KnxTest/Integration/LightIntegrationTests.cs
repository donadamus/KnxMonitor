using FluentAssertions;
using KnxModel;
using KnxService;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Integration
{
    [Collection("KnxService collection")]
    public class LightIntegrationTests
    {
        private readonly IKnxService _knxService;

        public LightIntegrationTests(KnxServiceFixture fixture)
        {
            _knxService = fixture.KnxService;
        }

        [Theory]
        [InlineData("L11")]  // Light 11
        [InlineData("L12")]  // Light 12
        [InlineData("L13")]  // Light 13
        [InlineData("L14")]  // Light 14
        [InlineData("L15")]  // Light 15
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
        [InlineData("L11")]
        [InlineData("L13")]
        [InlineData("L15")]
        [InlineData("L08")]
        public async Task CanSaveAndRestoreLightState(string lightId)
        {
            // Arrange
            var light = LightFactory.CreateLight(lightId, _knxService);
            await light.InitializeAsync();

            // Act & Assert - Save state
            light.SaveCurrentState();
            light.SavedState.Should().NotBeNull();
            light.SavedState!.IsOn.Should().Be(light.CurrentState.IsOn);

            // Modify light state
            var originalState = light.CurrentState.IsOn;
            var testState = !originalState;
            
            await light.SetStateAsync(testState);
            
            // Verify state changed
            light.CurrentState.IsOn.Should().Be(testState);

            // Restore original state
            await light.RestoreSavedStateAsync();
            
            // Verify restoration
            light.CurrentState.IsOn.Should().Be(originalState);
            
        }

        [Theory]
        [InlineData("L12")]
        [InlineData("L14")]
        public async Task CanToggleLightState(string lightId)
        {
            // Arrange
            var light = LightFactory.CreateLight(lightId, _knxService);
            await light.InitializeAsync();
            light.SaveCurrentState();

            try
            {
                var initialState = light.CurrentState.IsOn;
                Console.WriteLine($"Testing light {lightId} toggle from {(initialState ? "ON" : "OFF")}");

                // Test toggle
                await light.ToggleAsync();
                
                // Verify state changed via feedback (natural device behavior)
                light.CurrentState.IsOn.Should().Be(!initialState, $"Light state should be toggled via feedback");
                
                Console.WriteLine($"Light {lightId} successfully toggled to {(light.CurrentState.IsOn ? "ON" : "OFF")}");

                // Toggle back
                await light.ToggleAsync();
                
                // Verify state restored via feedback (natural device behavior)
                light.CurrentState.IsOn.Should().Be(initialState, $"Light state should be restored via feedback");
                
                Console.WriteLine($"Light {lightId} successfully toggled back to {(light.CurrentState.IsOn ? "ON" : "OFF")}");
            }
            finally
            {
                // Always restore original state
                await light.RestoreSavedStateAsync();
            }
        }

        [Theory]
        [InlineData("L11")]
        [InlineData("L15")]
        public async Task CanTurnOnAndOff(string lightId)
        {
            // Arrange
            var light = LightFactory.CreateLight(lightId, _knxService);
            await light.InitializeAsync();
            light.SaveCurrentState();

            try
            {
                var initialState = light.CurrentState.IsOn;
                Console.WriteLine($"Testing light {lightId} on/off from {(initialState ? "ON" : "OFF")}");

                // Test turn ON
                await light.TurnOnAsync();
                
                // Verify state via feedback (natural device behavior)
                light.CurrentState.IsOn.Should().BeTrue("Light should be ON via feedback");
                Console.WriteLine($"Light {lightId} successfully turned ON");

                // Test turn OFF
                await light.TurnOffAsync();
                
                // Verify state via feedback (natural device behavior)
                light.CurrentState.IsOn.Should().BeFalse("Light should be OFF via feedback");
                Console.WriteLine($"Light {lightId} successfully turned OFF");
            }
            finally
            {
                // Always restore original state
                await light.RestoreSavedStateAsync();
            }
        }

        [Theory]
        [InlineData("L13")]
        public async Task WaitForStateAsync_ReturnsCorrectly(string lightId)
        {
            // Arrange
            var light = LightFactory.CreateLight(lightId, _knxService);
            await light.InitializeAsync();
            light.SaveCurrentState();

            try
            {
                var initialState = light.CurrentState.IsOn;
                var targetState = !initialState;

                // Act - Change state and wait
                await light.SetStateAsync(targetState);
                var success = await light.WaitForStateAsync(targetState, TimeSpan.FromSeconds(5));

                // Assert
                success.Should().BeTrue($"Should successfully wait for state {targetState}");
                light.CurrentState.IsOn.Should().Be(targetState);
                
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
        [InlineData("L11", "11", "Light 11")]
        [InlineData("L15", "15", "Light 15")]
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
        [InlineData("L11")]
        [InlineData("L13")]
        [InlineData("L15")]
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
                await light.SetLockAsync(true);
                light.CurrentState.IsLocked.Should().BeTrue("Light should be locked via feedback");
                Console.WriteLine($"✅ Light {lightId} successfully locked");

                // Act & Assert - Unlock the light
                await light.SetLockAsync(false);
                light.CurrentState.IsLocked.Should().BeFalse("Light should be unlocked via feedback");
                Console.WriteLine($"✅ Light {lightId} successfully unlocked");

                // Test convenience methods
                await light.LockAsync();
                light.CurrentState.IsLocked.Should().BeTrue();
                Console.WriteLine($"✅ Light {lightId} LockAsync() works");

                await light.UnlockAsync();
                light.CurrentState.IsLocked.Should().BeFalse();
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
        [InlineData("L11", true)]
        [InlineData("L13", false)]
        public async Task CanWaitForLightLockState(string lightId, bool targetLockState)
        {
            // Arrange
            var light = LightFactory.CreateLight(lightId, _knxService);
            await light.InitializeAsync();
            light.SaveCurrentState();

            try
            {
                Console.WriteLine($"Testing wait for lock state {(targetLockState ? "LOCKED" : "UNLOCKED")} for light {lightId}");

                // Act - Set opposite state first
                await light.SetLockAsync(!targetLockState);
                
                // Start waiting for target state in background
                var waitTask = light.WaitForLockStateAsync(targetLockState, TimeSpan.FromSeconds(10));
                
                // Trigger state change after a delay
                await Task.Delay(500);
                await light.SetLockAsync(targetLockState);

                // Assert
                var result = await waitTask;
                result.Should().BeTrue();
                light.CurrentState.IsLocked.Should().Be(targetLockState);
                
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
        [InlineData("L12")]
        [InlineData("L14")]
        [InlineData("L25")]
        public async Task LockPreventsStateChanges(string lightId)
        {
            // Arrange
            var light = LightFactory.CreateLight(lightId, _knxService);
            await light.InitializeAsync();
            
            // Save initial state for cleanup
            light.SaveCurrentState();
            var initialState = light.CurrentState.IsOn;
            var initialLockState = light.CurrentState.IsLocked;
            
            Console.WriteLine($"Testing lock functionality prevents state changes for light {lightId}");
            Console.WriteLine($"Initial state: {(initialState ? "ON" : "OFF")}, Lock: {(initialLockState ? "LOCKED" : "UNLOCKED")}");

            try
            {
                // Step 1: Ensure light is unlocked and set to known state
                await light.SetLockAsync(false);
                await light.SetStateAsync(true); // Turn ON
                light.CurrentState.IsOn.Should().BeTrue("Light should be ON when unlocked");
                light.CurrentState.IsLocked.Should().BeFalse("Light should be unlocked");
                Console.WriteLine($"✅ Step 1: Light {lightId} set to ON and unlocked");

                // Step 2: Try to lock the light - for lights we don't expect feedback, use zero timeout
                Console.WriteLine($"Step 2: Setting lock for light {lightId} (lights may not provide lock feedback)");
                await light.SetLockAsync(true, TimeSpan.Zero);
                
                // For lights, we don't wait for feedback - just assume the lock command was sent
                // The real test is whether the lock actually prevents state changes
                Console.WriteLine($"✅ Step 2: Lock command sent to light {lightId}, testing if it prevents state changes");
                var stateBeforeLockTest = light.CurrentState.IsOn;

                // Step 3: Now test if lock actually prevents state changes
                Console.WriteLine($"Step 3: Testing if lock prevents state changes...");
                
                // Try to turn OFF while locked
                await light.SetStateAsync(false);
                
                // Check if state changed (it shouldn't have)
                bool stateChangedWhileLocked = light.CurrentState.IsOn != stateBeforeLockTest;
                
                if (stateChangedWhileLocked)
                {
                    Console.WriteLine($"❌ FAILURE: Light {lightId} state changed while locked! Lock is not preventing state changes.");
                    Console.WriteLine($"State before lock test: {(stateBeforeLockTest ? "ON" : "OFF")}, State after attempting change: {(light.CurrentState.IsOn ? "ON" : "OFF")}");
                    light.CurrentState.IsOn.Should().Be(stateBeforeLockTest, 
                        "Light state should NOT change when locked - lock should prevent state changes");
                }
                else
                {
                    Console.WriteLine($"✅ Step 3: Light {lightId} correctly ignored state change while locked");
                }

                // Step 4: Unlock the light
                await light.SetLockAsync(false);
                light.CurrentState.IsLocked.Should().BeFalse("Light should be unlocked");
                Console.WriteLine($"✅ Step 4: Light {lightId} unlocked");

                // Step 5: Verify state can be changed after unlocking
                var targetState = !light.CurrentState.IsOn;
                await light.SetStateAsync(targetState);
                light.CurrentState.IsOn.Should().Be(targetState, 
                    "Light state should change normally after unlocking");
                Console.WriteLine($"✅ Step 5: Light {lightId} state changed to {(targetState ? "ON" : "OFF")} after unlocking");

                Console.WriteLine($"🎉 Lock prevention test completed successfully for light {lightId}");
            }
            finally
            {
                // Always restore original state and lock state
                try
                {
                    await light.SetLockAsync(false); // Ensure unlocked for cleanup
                    await light.RestoreSavedStateAsync();
                    if (initialLockState)
                    {
                        await light.SetLockAsync(true); // Restore original lock state if needed
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Warning during cleanup: {ex.Message}");
                }
                Console.WriteLine($"Light {lightId} lock prevention test completed and state restored");
            }
        }
    }
}
