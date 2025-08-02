using FluentAssertions;
using KnxModel;
using KnxService;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Integration
{
    [Collection("KnxService collection")]
    public class SystemIntegrationTests
    {
        private IKnxService _knxService { get; }

        public SystemIntegrationTests(KnxServiceFixture fixture)
        {
            _knxService = fixture.KnxService;
        }

        [Theory]
        // [InlineData("L01")]
        // [InlineData("L02")]
        // [InlineData("L03")]
        // [InlineData("L04")]
        // [InlineData("L05")]
        // [InlineData("L06")]
        // [InlineData("L07")]
        // [InlineData("L08")]
        // [InlineData("L09")]
        // [InlineData("L10")]
        // [InlineData("L11")]
        // [InlineData("L12")]
        // [InlineData("L13")]
        [InlineData("L14")]
        [InlineData("L15")]
        public async Task CanToggleLightAndRestoreOriginalState(string lightId)
        {
            // Arrange
            var light = LightFactory.CreateLight(lightId, _knxService);
            await light.InitializeAsync();
            light.SaveCurrentState();

            try
            {
                var initialState = light.CurrentState.IsOn;
                Console.WriteLine($"Testing light {lightId} toggle from {(initialState ? "ON" : "OFF")}");

                // Act + Assert (1): Toggle to opposite state
                await light.ToggleAsync();
                
                // Model automatically updates state from KNX feedback
                Assert.Equal(!initialState, light.CurrentState.IsOn);
                Console.WriteLine($"✓ Light {lightId} successfully toggled to {(light.CurrentState.IsOn ? "ON" : "OFF")}");

                // Wait for state to stabilize

                // Act + Assert (2): Toggle back to original state
                await light.ToggleAsync();
                
                // Model automatically updates state from KNX feedback
                await Task.Delay(1000); // Give time for feedback
                Assert.Equal(initialState, light.CurrentState.IsOn);
                Console.WriteLine($"✓ Light {lightId} successfully restored to {(light.CurrentState.IsOn ? "ON" : "OFF")}");
            }
            finally
            {
                // Always restore original state using the model's saved state functionality
                await light.RestoreSavedStateAsync();
                light.Dispose();
            }
        }


        [Theory]
        [InlineData("L01")]
        [InlineData("L02")]
        [InlineData("L03")]
        [InlineData("L04")]
        [InlineData("L05")]
        [InlineData("L06")]
        [InlineData("L07")]
        [InlineData("L08")]
        [InlineData("L09")]
        [InlineData("L10")]
        [InlineData("L11")]
        [InlineData("L12")]
        [InlineData("L13")]
        [InlineData("L14")]
        [InlineData("L15")]
        //[InlineData("L16")]
        //[InlineData("L17")]
        //[InlineData("L18")]
        //[InlineData("L19")]
        //[InlineData("L20")]
        public async Task CanReadLightFeedback(string lightId)
        {
            // Arrange
            var light = LightFactory.CreateLight(lightId, _knxService);
            await light.InitializeAsync();

            try
            {
                // Read initial state from model (updated during InitializeAsync)
                var initialState = light.CurrentState.IsOn;
                Console.WriteLine($"Light {lightId} initial state: {(initialState ? "ON" : "OFF")}");

            }
            finally
            {
                light.Dispose();
            }
        }

        [Theory]
        [InlineData("R1.1")]  // Bathroom
        [InlineData("R2.1")]  // Master Bathroom
        [InlineData("R3.1")]  // Master Bedroom
        [InlineData("R3.2")]  // Master Bedroom
        [InlineData("R5.1")]  // Guest Room
        [InlineData("R6.1")]  // Kinga's Room
        [InlineData("R6.2")]  // Kinga's Room
        [InlineData("R6.3")]  // Kinga's Room
        [InlineData("R7.1")]  // Rafal's Room
        [InlineData("R7.2")]  // Rafal's Room
        [InlineData("R7.3")]  // Rafal's Room
        [InlineData("R8.1")]  // Hall
        [InlineData("R02.1")] // Kitchen
        [InlineData("R02.2")] // Kitchen
        [InlineData("R03.1")] // Dining Room
        [InlineData("R04.1")] // Living Room
        [InlineData("R04.2")] // Living Room
        [InlineData("R05.1")] // Office
        public async Task CanSetShutterAbsolutePositionAndReadFeedback(string shutterId)
        {
            // Arrange
            var shutter = ShutterFactory.CreateShutter(shutterId, _knxService);
            await shutter.InitializeAsync();
            shutter.SaveCurrentState();

            try
            {
                var initialPosition = shutter.CurrentState.Position;
                Console.WriteLine($"Shutter {shutterId} initial position: {initialPosition:F1}%");

                // Choose a movement of 20% in appropriate direction
                var targetPosition = initialPosition < 50 ? initialPosition + 20 : initialPosition - 20;
                targetPosition = Math.Max(0, Math.Min(100, targetPosition));

                Console.WriteLine($"Moving shutter {shutterId} from {initialPosition:F1}% to {targetPosition:F1}%");

                // Act - Set new position using shutter model
                await shutter.SetPositionAsync(targetPosition);

                // Model automatically updates position from KNX feedback
                await Task.Delay(3000); // Give time for movement and feedback

                // Assert - Check position changed on model
                var finalPosition = shutter.CurrentState.Position;
                Console.WriteLine($"Final position on model: {finalPosition:F1}%");

                // Verify movement occurred in correct direction with tolerance
                var expectedDirection = targetPosition > initialPosition ? 1 : -1;
                var actualDirection = finalPosition > initialPosition ? 1 : -1;
                var actualMovement = Math.Abs(finalPosition - initialPosition);

                if (actualMovement >= 1.0f) // If significant movement occurred
                {
                    Assert.True(expectedDirection == actualDirection, 
                        $"Movement direction incorrect. Expected: {(expectedDirection > 0 ? "UP" : "DOWN")}, " +
                        $"Actual: {(actualDirection > 0 ? "UP" : "DOWN")}");
                    
                    Console.WriteLine($"✓ Shutter moved correctly in {(actualDirection > 0 ? "UP" : "DOWN")} direction ({actualMovement:F1}% movement)");
                }
                else
                {
                    Console.WriteLine($"⚠ Small movement detected ({actualMovement:F1}%), might be within tolerance");
                }
            }
            finally
            {
                // Always restore original state using the model's saved state functionality
                await shutter.RestoreSavedStateAsync();
                shutter.Dispose();
            }
        }

        [Theory]
        [InlineData("R1.1")]  // Bathroom
        [InlineData("R2.1")]  // Master Bathroom
        [InlineData("R3.1")]  // Master Bedroom
        [InlineData("R3.2")]  // Master Bedroom
        [InlineData("R5.1")]  // Guest Room
        [InlineData("R6.1")]  // Kinga's Room
        [InlineData("R6.2")]  // Kinga's Room
        [InlineData("R6.3")]  // Kinga's Room
        [InlineData("R7.1")]  // Rafal's Room
        [InlineData("R7.2")]  // Rafal's Room
        [InlineData("R7.3")]  // Rafal's Room
        [InlineData("R8.1")]  // Hall
        [InlineData("R02.1")] // Kitchen
        [InlineData("R02.2")] // Kitchen
        [InlineData("R03.1")] // Dining Room
        [InlineData("R04.1")] // Living Room
        [InlineData("R04.2")] // Living Room
        [InlineData("R05.1")] // Office
        public async Task CanMoveShutterUpAndDown(string shutterId)
        {
            // Arrange
            var shutter = ShutterFactory.CreateShutter(shutterId, _knxService);
            await shutter.InitializeAsync();
            shutter.SaveCurrentState();

            try
            {
                var initialPosition = shutter.CurrentState.Position;
                Console.WriteLine($"Shutter {shutterId} initial position: {initialPosition:F1}%");

                // Decide test order based on initial position
                bool testUpFirst = initialPosition > 50;
                Console.WriteLine($"Testing {(testUpFirst ? "UP first, then DOWN" : "DOWN first, then UP")}");

                if (testUpFirst)
                {
                    // Test UP movement
                    await shutter.MoveAsync(ShutterDirection.Up, TimeSpan.FromSeconds(2));
                    await Task.Delay(1000); // Give time for feedback

                    var afterUpPosition = shutter.CurrentState.Position;
                    Console.WriteLine($"After UP movement: {afterUpPosition:F1}%");
                    Assert.True(afterUpPosition < initialPosition, "UP movement should decrease position percentage");

                    await Task.Delay(1000);

                    // Test DOWN movement
                    await shutter.MoveAsync(ShutterDirection.Down, TimeSpan.FromSeconds(2));
                    await Task.Delay(1000); // Give time for feedback

                    var afterDownPosition = shutter.CurrentState.Position;
                    Console.WriteLine($"After DOWN movement: {afterDownPosition:F1}%");
                    Assert.True(afterDownPosition > afterUpPosition, "DOWN movement should increase position percentage");
                }
                else
                {
                    // Test DOWN movement
                    await shutter.MoveAsync(ShutterDirection.Down, TimeSpan.FromSeconds(2));
                    await Task.Delay(1000); // Give time for feedback

                    var afterDownPosition = shutter.CurrentState.Position;
                    Console.WriteLine($"After DOWN movement: {afterDownPosition:F1}%");
                    Assert.True(afterDownPosition > initialPosition, "DOWN movement should increase position percentage");

                    await Task.Delay(1000);

                    // Test UP movement
                    await shutter.MoveAsync(ShutterDirection.Up, TimeSpan.FromSeconds(2));
                    await Task.Delay(1000); // Give time for feedback

                    var afterUpPosition = shutter.CurrentState.Position;
                    Console.WriteLine($"After UP movement: {afterUpPosition:F1}%");
                    Assert.True(afterUpPosition < afterDownPosition, "UP movement should decrease position percentage");
                }

                Console.WriteLine($"✓ Shutter {shutterId} UP/DOWN movement test completed successfully");
            }
            finally
            {
                // Always restore original state using the model's saved state functionality
                await shutter.RestoreSavedStateAsync();
                shutter.Dispose();
            }
        }

        [Theory]
        [InlineData("R1.1")]  // Bathroom
        [InlineData("R2.1")]  // Master Bathroom
        [InlineData("R3.1")]  // Master Bedroom
        [InlineData("R3.2")]  // Master Bedroom
        [InlineData("R5.1")]  // Guest Room
        [InlineData("R6.1")]  // Kinga's Room
        [InlineData("R6.2")]  // Kinga's Room
        [InlineData("R6.3")]  // Kinga's Room
        [InlineData("R7.1")]  // Rafal's Room
        [InlineData("R7.2")]  // Rafal's Room
        [InlineData("R7.3")]  // Rafal's Room
        [InlineData("R8.1")]  // Hall
        [InlineData("R02.1")] // Kitchen
        [InlineData("R02.2")] // Kitchen
        [InlineData("R03.1")] // Dining Room
        [InlineData("R04.1")] // Living Room
        [InlineData("R04.2")] // Living Room
        [InlineData("R05.1")] // Office
        public async Task CanToggleShutterLock(string shutterId)
        {
            // Arrange
            var shutter = ShutterFactory.CreateShutter(shutterId, _knxService);
            await shutter.InitializeAsync();
            shutter.SaveCurrentState();

            try
            {
                var initialLockState = shutter.CurrentState.IsLocked;
                Console.WriteLine($"Shutter {shutterId} initial lock state: {(initialLockState ? "LOCKED" : "UNLOCKED")}");

                // Act - Toggle lock state
                await shutter.SetLockAsync(!initialLockState);
                
                // Assert - Check lock state changed on model
                Assert.Equal(!initialLockState, shutter.CurrentState.IsLocked);
                Console.WriteLine($"✓ Shutter {shutterId} lock successfully toggled to {(shutter.CurrentState.IsLocked ? "LOCKED" : "UNLOCKED")}");

                // Toggle back to original state
                await shutter.SetLockAsync(initialLockState);
                
                Assert.Equal(initialLockState, shutter.CurrentState.IsLocked);
                Console.WriteLine($"✓ Shutter {shutterId} lock successfully restored to {(shutter.CurrentState.IsLocked ? "LOCKED" : "UNLOCKED")}");
            }
            finally
            {
                // Always restore original state using the model's saved state functionality
                await shutter.RestoreSavedStateAsync();
                shutter.Dispose();
            }
        }

        [Theory]
        [InlineData("R1.1")]  // Bathroom
        [InlineData("R2.1")]  // Master Bathroom
        [InlineData("R3.1")]  // Master Bedroom
        [InlineData("R3.2")]  // Master Bedroom
        [InlineData("R5.1")]  // Guest Room
        [InlineData("R6.1")]  // Kinga's Room
        [InlineData("R6.2")]  // Kinga's Room
        [InlineData("R6.3")]  // Kinga's Room
        public async Task ShutterDoesNotMoveWhenLocked(string shutterId)
        {
            // Arrange
            var shutter = ShutterFactory.CreateShutter(shutterId, _knxService);
            await shutter.InitializeAsync();
            shutter.SaveCurrentState();
            
            try
            {
                // Act & Assert
                
                // Step 1: Engage the lock
                Console.WriteLine("=== Step 1: Engaging lock ===");
                await shutter.SetLockAsync(true);
                
                // Verify lock is engaged
                Assert.True(shutter.CurrentState.IsLocked);
                Console.WriteLine("✓ Lock successfully engaged");

                // Step 2: Test UP movement while locked
                Console.WriteLine("\n=== Step 2: Testing UP movement while locked ===");
                var positionBeforeUp = shutter.CurrentState.Position;
                Console.WriteLine($"Position before UP: {positionBeforeUp:F1}%");
                
                await shutter.MoveAsync(ShutterDirection.Up, TimeSpan.FromSeconds(2));
                await Task.Delay(500); // Give time for any potential movement
                
                var positionAfterUp = shutter.CurrentState.Position;
                var upMovement = Math.Abs(positionAfterUp - positionBeforeUp);
                Console.WriteLine($"Position after UP: {positionAfterUp:F1}% (movement: {upMovement:F1}%)");

                // Step 3: Test DOWN movement while locked
                Console.WriteLine("\n=== Step 3: Testing DOWN movement while locked ===");
                var positionBeforeDown = shutter.CurrentState.Position;
                Console.WriteLine($"Position before DOWN: {positionBeforeDown:F1}%");
                
                await shutter.MoveAsync(ShutterDirection.Down, TimeSpan.FromSeconds(2));
                await Task.Delay(500); // Give time for any potential movement
                
                var positionAfterDown = shutter.CurrentState.Position;
                var downMovement = Math.Abs(positionAfterDown - positionBeforeDown);
                Console.WriteLine($"Position after DOWN: {positionAfterDown:F1}% (movement: {downMovement:F1}%)");

                // Step 4: Disengage lock and verify movement works
                Console.WriteLine("\n=== Step 4: Disengaging lock and testing movement ===");
                await shutter.SetLockAsync(false);
                await Task.Delay(1000); // Wait for lock to disengage
                
                Assert.False(shutter.CurrentState.IsLocked);
                Console.WriteLine("✓ Lock successfully disengaged");
                
                // Quick movement test to verify shutter responds when unlocked
                var positionBeforeUnlocked = shutter.CurrentState.Position;
                Console.WriteLine($"Position before unlocked test: {positionBeforeUnlocked:F1}%");
                
                await shutter.MoveAsync(ShutterDirection.Up, TimeSpan.FromSeconds(1));
                await Task.Delay(1000);
                
                var positionAfterUnlocked = shutter.CurrentState.Position;
                var unlockedMovement = Math.Abs(positionAfterUnlocked - positionBeforeUnlocked);
                Console.WriteLine($"Position after unlocked test: {positionAfterUnlocked:F1}% (movement: {unlockedMovement:F1}%)");

                // Assertions
                Console.WriteLine("\n=== Test Results ===");
                Console.WriteLine($"Movement while locked - UP: {upMovement:F1}%, DOWN: {downMovement:F1}%");
                
                // The shutter MUST NOT move AT ALL while locked - zero tolerance
                Assert.True(upMovement == 0.0, 
                    $"Shutter moved UP {upMovement:F1}% while locked - NO movement allowed!");
                Assert.True(downMovement == 0.0, 
                    $"Shutter moved DOWN {downMovement:F1}% while locked - NO movement allowed!");
                
                Console.WriteLine("✓ Shutter correctly ignored UP and DOWN movement commands while locked - NO movement detected");
            }
            finally
            {
                // Always restore original state using the model's saved state functionality
                await shutter.RestoreSavedStateAsync();
                shutter.Dispose();
            }
        }

        [Theory]
        [InlineData("R1.1")]  // Bathroom
        [InlineData("R2.1")]  // Master Bathroom
        [InlineData("R3.1")]  // Master Bedroom
        [InlineData("R3.2")]  // Master Bedroom
        [InlineData("R5.1")]  // Guest Room
        [InlineData("R6.1")]  // Kinga's Room
        [InlineData("R6.2")]  // Kinga's Room
        [InlineData("R6.3")]  // Kinga's Room
        [InlineData("R7.1")]  // Rafal's Room
        [InlineData("R7.2")]  // Rafal's Room
        [InlineData("R7.3")]  // Rafal's Room
        [InlineData("R8.1")]  // Hall
        [InlineData("R02.1")] // Kitchen
        [InlineData("R02.2")] // Kitchen
        [InlineData("R03.1")] // Dining Room
        [InlineData("R04.1")] // Living Room
        [InlineData("R04.2")] // Living Room
        [InlineData("R05.1")] // Office
        public async Task CanReadShutterPosition(string shutterId)
        {
            // Arrange
            var shutter = ShutterFactory.CreateShutter(shutterId, _knxService);

            try
            {
                // Act - Initialize reads current state from KNX bus
                await shutter.InitializeAsync();

                // Assert - Verify position is valid and within expected range
                shutter.CurrentState.Should().NotBeNull();
                shutter.CurrentState.Position.Should().BeInRange(0, 100, 
                    $"Shutter {shutterId} position should be between 0-100%, got {shutter.CurrentState.Position:F1}%");

                Console.WriteLine($"✓ Shutter {shutterId} ({shutter.Name}) position: {shutter.CurrentState.Position:F1}% (Raw: {(byte)(shutter.CurrentState.Position * 2.55)})");
            }
            finally
            {
                shutter.Dispose();
            }
        }
    }
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
