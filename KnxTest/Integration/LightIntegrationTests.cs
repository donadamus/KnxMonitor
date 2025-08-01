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
                
                // Wait for state change
                var success = await light.WaitForStateAsync(!initialState, TimeSpan.FromSeconds(5));
                success.Should().BeTrue($"Light should toggle from {initialState} to {!initialState}");
                
                var toggledState = await light.ReadStateAsync();
                toggledState.Should().Be(!initialState, $"Light state should be toggled");
                
                Console.WriteLine($"Light {lightId} successfully toggled to {(toggledState ? "ON" : "OFF")}");

                // Toggle back
                await light.ToggleAsync();
                
                var successBack = await light.WaitForStateAsync(initialState, TimeSpan.FromSeconds(5));
                successBack.Should().BeTrue($"Light should toggle back to {initialState}");
                
                var finalState = await light.ReadStateAsync();
                finalState.Should().Be(initialState, $"Light state should be restored");
                
                Console.WriteLine($"Light {lightId} successfully toggled back to {(finalState ? "ON" : "OFF")}");
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
                var onSuccess = await light.WaitForStateAsync(true, TimeSpan.FromSeconds(5));
                onSuccess.Should().BeTrue("Light should turn ON");
                
                var onState = await light.ReadStateAsync();
                onState.Should().BeTrue("Light should be ON");
                Console.WriteLine($"Light {lightId} successfully turned ON");

                // Test turn OFF
                await light.TurnOffAsync();
                var offSuccess = await light.WaitForStateAsync(false, TimeSpan.FromSeconds(5));
                offSuccess.Should().BeTrue("Light should turn OFF");
                
                var offState = await light.ReadStateAsync();
                offState.Should().BeFalse("Light should be OFF");
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
                light.CurrentState.IsLocked.Should().BeTrue();
                
                var lockState = await light.ReadLockStateAsync();
                lockState.Should().BeTrue();
                Console.WriteLine($"✅ Light {lightId} successfully locked");

                // Act & Assert - Unlock the light
                await light.SetLockAsync(false);
                light.CurrentState.IsLocked.Should().BeFalse();
                
                lockState = await light.ReadLockStateAsync();
                lockState.Should().BeFalse();
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
    }
}
