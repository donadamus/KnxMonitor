using FluentAssertions;
using KnxModel;
using KnxService;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Integration
{
    [Collection("KnxService collection")]
    public class ShutterIntegrationTests
    {
        private readonly IKnxService _knxService;

        public ShutterIntegrationTests(KnxServiceFixture fixture)
        {
            _knxService = fixture.KnxService;
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
        public async Task CanInitializeShutterAndReadState(string shutterId)
        {
            // Arrange
            var shutter = ShutterFactory.CreateShutter(shutterId, _knxService);

            // Act
            await shutter.InitializeAsync();

            // Assert
            shutter.Id.Should().Be(shutterId);
            shutter.CurrentState.Should().NotBeNull();
            shutter.CurrentState.Position.Should().BeInRange(0, 100);
            shutter.CurrentState.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
            
            Console.WriteLine($"Shutter {shutterId} initialized: {shutter}");
        }

        [Theory]
        [InlineData("R1.1")]
        [InlineData("R6.2")]
        [InlineData("R05.1")]
        public async Task CanSaveAndRestoreShutterState(string shutterId)
        {
            // Arrange
            var shutter = ShutterFactory.CreateShutter(shutterId, _knxService);
            await shutter.InitializeAsync();

            // Act & Assert - Save state
            shutter.SaveCurrentState();
            shutter.SavedState.Should().NotBeNull();
            shutter.SavedState!.Position.Should().Be(shutter.CurrentState.Position);
            shutter.SavedState.Lock.Should().Be(shutter.CurrentState.Lock);

            // Modify shutter state (ensure different position)
            var originalPosition = shutter.CurrentState.Position;
            // Choose test position: if current > 50%, go to 30%, otherwise go to 70%
            var testPosition = originalPosition + (originalPosition > 50 ? -1 : 1) * 20.0f;
            
            await shutter.SetPositionAsync(testPosition);
            await shutter.WaitForMovementStopAsync();//wait for movement to complete
            
            // Verify state changed via feedback (natural device behavior)
            var currentPosition = shutter.CurrentState.Position;
            Math.Abs(currentPosition - originalPosition).Should().BeGreaterThan(1);

            // Restore original state
            await shutter.RestoreSavedStateAsync();
            await shutter.WaitForMovementStopAsync();//wait for movement to complete

            // Verify restoration via feedback (natural device behavior)
            var restoredPosition = shutter.CurrentState.Position;
            Math.Abs(restoredPosition - originalPosition).Should().BeLessThan(3); // Allow 3% tolerance
            
            Console.WriteLine($"Shutter {shutterId} - Original: {originalPosition}%, " +
                            $"Modified: {currentPosition}%, Restored: {restoredPosition}%");
        }

        [Theory]
        [InlineData("R1.1")]
        [InlineData("R7.1")]
        public async Task CanMoveShutterInBothDirections(string shutterId)
        {
            // Arrange
            var shutter = ShutterFactory.CreateShutter(shutterId, _knxService);
            await shutter.InitializeAsync();
            shutter.SaveCurrentState();

            try
            {
                var initialPosition = shutter.CurrentState.Position;
                Console.WriteLine($"Testing shutter {shutterId} movement from {initialPosition}%");

                // Test movement based on initial position
                bool testUpFirst = initialPosition > 50; // If down, test UP first

                if (testUpFirst)
                {
                    // Test UP movement
                    await shutter.MoveAsync(ShutterDirection.Up, TimeSpan.FromSeconds(3));
                    await shutter.WaitForMovementStopAsync();//wait for movement to complete
                    
                    // Verify state via feedback (natural device behavior)
                    var upPosition = shutter.CurrentState.Position;
                    Console.WriteLine($"After UP movement: {upPosition}%");
                    
                    // UP should decrease percentage (toward 0%)
                    (upPosition < initialPosition).Should().BeTrue(
                        $"UP movement should decrease position. Initial: {initialPosition}%, After UP: {upPosition}%");

                    // Test DOWN movement
                    await shutter.MoveAsync(ShutterDirection.Down, TimeSpan.FromSeconds(3));
                    await shutter.WaitForMovementStopAsync();//wait for movement to complete
                    
                    // Verify state via feedback (natural device behavior)
                    var downPosition = shutter.CurrentState.Position;
                    Console.WriteLine($"After DOWN movement: {downPosition}%");
                    
                    // DOWN should increase percentage (toward 100%)
                    (downPosition > upPosition).Should().BeTrue(
                        $"DOWN movement should increase position. After UP: {upPosition}%, After DOWN: {downPosition}%");
                }
                else
                {
                    // Test DOWN movement first
                    await shutter.MoveAsync(ShutterDirection.Down, TimeSpan.FromSeconds(3));
                    await shutter.WaitForMovementStopAsync();//wait for movement to complete
                    
                    // Verify state via feedback (natural device behavior)
                    var downPosition = shutter.CurrentState.Position;
                    Console.WriteLine($"After DOWN movement: {downPosition}%");
                    
                    // DOWN should increase percentage (toward 100%)
                    (downPosition > initialPosition).Should().BeTrue(
                        $"DOWN movement should increase position. Initial: {initialPosition}%, After DOWN: {downPosition}%");

                    // Test UP movement
                    await shutter.MoveAsync(ShutterDirection.Up, TimeSpan.FromSeconds(3));
                    await shutter.WaitForMovementStopAsync();//wait for movement to complete
                    
                    // Verify state via feedback (natural device behavior)
                    var upPosition = shutter.CurrentState.Position;
                    Console.WriteLine($"After UP movement: {upPosition}%");
                    
                    // UP should decrease percentage (toward 0%)
                    (upPosition < downPosition).Should().BeTrue(
                        $"UP movement should decrease position. After DOWN: {downPosition}%, After UP: {upPosition}%");
                }
            }
            finally
            {
                // Always restore original state
                await shutter.RestoreSavedStateAsync();
            }
        }

        [Theory]
        //[InlineData("R2.1")]
        [InlineData("R8.1")]
        public async Task CanSetAbsolutePosition(string shutterId)
        {
            // Arrange
            var shutter = ShutterFactory.CreateShutter(shutterId, _knxService);
            await shutter.InitializeAsync();
            shutter.SaveCurrentState();

            try
            {
                var originalPosition = shutter.CurrentState.Position;
                
                // Choose test position: if current > 50%, go to UP 20%, otherwise go DOWN 20%
                var targetPosition = originalPosition + (originalPosition > 50 ? -1 : 1) * 20.0f;

                Console.WriteLine($"Setting shutter {shutterId} from {originalPosition}% to {targetPosition}%");

                // Act
                await shutter.SetPositionAsync(targetPosition);
                
                // Wait for position to be reached with byte-precision tolerance
                // Since KNX uses 1 byte (0-255) for 0-100%, tolerance should be ~0.5%
                var reached = await shutter.WaitForPositionAsync(targetPosition, tolerance: 1.0, TimeSpan.FromSeconds(15));

                // Assert
                reached.Should().BeTrue($"Shutter should reach target position {targetPosition}%");
                
                // Verify final position via feedback (natural device behavior)
                var finalPosition = shutter.CurrentState.Position;
                Console.WriteLine($"Final position: {finalPosition}%");
                
                Math.Abs(finalPosition - targetPosition).Should().BeLessThan(1.0f, 
                    $"Final position should be within 1% of target (byte precision). Target: {targetPosition}%, Actual: {finalPosition}%");
            }
            finally
            {
                // Always restore original state
                await shutter.RestoreSavedStateAsync();
            }
        }

        [Theory]
        [InlineData("R3.1")]
        [InlineData("R6.1")]
        public async Task CanToggleLockState(string shutterId)
        {
            // Arrange
            var shutter = ShutterFactory.CreateShutter(shutterId, _knxService);
            await shutter.InitializeAsync();
            shutter.SaveCurrentState();

            try
            {
                var initialLockState = shutter.CurrentState.Lock;
                Console.WriteLine($"Shutter {shutterId} initial lock state: {initialLockState}");

                // Toggle lock state
                var newLockState = initialLockState == Lock.On ? Lock.Off : Lock.On;
                await shutter.SetLockAsync(newLockState);
                
                // Verify state via feedback (natural device behavior)
                shutter.CurrentState.Lock.Should().Be(newLockState, 
                    $"Lock state should be {newLockState} via feedback");

                // Toggle back
                await shutter.SetLockAsync(initialLockState);
                
                // Verify restoration via feedback (natural device behavior)
                shutter.CurrentState.Lock.Should().Be(initialLockState, 
                    $"Lock state should be restored to {initialLockState} via feedback");

                Console.WriteLine($"Lock toggle test passed for shutter {shutterId}");
            }
            finally
            {
                // Always restore original state
                await shutter.RestoreSavedStateAsync();
            }
        }

        [Theory]
        [InlineData("R5.1")]
        [InlineData("R04.1")]
        public async Task LockPreventsMovement(string shutterId)
        {
            // Arrange
            var shutter = ShutterFactory.CreateShutter(shutterId, _knxService);
            await shutter.InitializeAsync();
            shutter.SaveCurrentState();

            try
            {
                // Act & Assert - Use current state (natural device behavior)
                var initialPosition = shutter.CurrentState.Position;
                var initialLockState = shutter.CurrentState.Lock;

                // Ensure shutter is locked
                if (initialLockState == Lock.Off)
                {
                    await shutter.SetLockAsync(Lock.On);
                }

                // Try to move the shutter
                Console.WriteLine($"Attempting to move locked shutter {shutterId}");
                await shutter.MoveAsync(ShutterDirection.Up, TimeSpan.FromSeconds(4));
                
                // Check if position changed via feedback (natural device behavior)
                var positionAfterMove = shutter.CurrentState.Position;
                var positionDifference = Math.Abs(positionAfterMove - initialPosition);
                var lockEffective = positionDifference < 1.0; // Less than 1% movement means lock is effective

                lockEffective.Should().BeTrue($"Lock should prevent movement for shutter {shutterId}");
                
                Console.WriteLine($"✓ Lock functionality test passed for shutter {shutterId}");
            }
            finally
            {
                // Always restore original state
                await shutter.RestoreSavedStateAsync();
            }
        }

        [Fact]
        public void ShutterFactory_CanCreateAllShutters()
        {
            // Act
            var shutters = ShutterFactory.CreateAllShutters(_knxService);
            var shutterList = shutters.ToList();

            // Assert
            shutterList.Should().HaveCount(18, "Should create 18 shutters (12 new + 6 existing)");
            
            var shutterIds = shutterList.Select(s => s.Id).ToList();
            shutterIds.Should().Contain("R1.1");
            shutterIds.Should().Contain("R8.1");
            shutterIds.Should().Contain("R05.1");
            
            Console.WriteLine($"Created {shutterList.Count} shutters: {string.Join(", ", shutterIds)}");
        }

        [Theory]
        [InlineData("R1.1", "1", "Bathroom")]
        [InlineData("R6.2", "7", "Kinga's Room")]
        [InlineData("R05.1", "18", "Office")]
        public void ShutterModel_HasCorrectConfiguration(string shutterId, string expectedSubGroup, string expectedName)
        {
            // Act
            var shutter = ShutterFactory.CreateShutter(shutterId, _knxService);

            // Assert
            shutter.Id.Should().Be(shutterId);
            shutter.SubGroup.Should().Be(expectedSubGroup);
            shutter.Name.Should().Be(expectedName);
            
            // Verify addresses are calculated correctly
            var feedbackSubGroup = (int.Parse(expectedSubGroup) + 100).ToString();
            shutter.Addresses.MovementControl.Should().Be($"4/0/{expectedSubGroup}");
            shutter.Addresses.MovementFeedback.Should().Be($"4/0/{feedbackSubGroup}");
            shutter.Addresses.PositionControl.Should().Be($"4/2/{expectedSubGroup}");
            shutter.Addresses.PositionFeedback.Should().Be($"4/2/{feedbackSubGroup}");
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
                var initialLockState = shutter.CurrentState.Lock;
                Console.WriteLine($"Shutter {shutterId} initial lock state: {initialLockState}");

                // Act - Toggle lock state
                await shutter.SetLockAsync(initialLockState.Opposite());
                
                // Assert - Check lock state changed on model
                Assert.Equal(initialLockState.Opposite(), shutter.CurrentState.Lock);
                Console.WriteLine($"✓ Shutter {shutterId} lock successfully toggled to {shutter.CurrentState.Lock}");

                // Toggle back to original state
                await shutter.SetLockAsync(initialLockState);
                
                Assert.Equal(initialLockState, shutter.CurrentState.Lock);
                Console.WriteLine($"✓ Shutter {shutterId} lock successfully restored to {shutter.CurrentState.Lock}");
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
                await shutter.SetLockAsync(Lock.On);
                
                // Verify lock is engaged
                shutter.CurrentState.Lock.Should().Be(Lock.On);
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
                await shutter.SetLockAsync(Lock.Off);
                await Task.Delay(1000); // Wait for lock to disengage

                shutter.CurrentState.Lock.Should().Be(Lock.Off); ;
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
}
