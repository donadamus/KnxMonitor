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
            shutter.SavedState.IsLocked.Should().Be(shutter.CurrentState.IsLocked);

            // Modify shutter state (ensure different position)
            var originalPosition = shutter.CurrentState.Position;
            // Choose test position: if current > 50%, go to 30%, otherwise go to 70%
            var testPosition = originalPosition + (originalPosition > 50 ? -1 : 1) * 20.0f;
            
            await shutter.SetPositionAsync(testPosition);
            await Task.Delay(2000); // Wait for movement
            
            // Verify state changed
            await shutter.InitializeAsync(); // Refresh current state
            var currentPosition = shutter.CurrentState.Position;
            Math.Abs(currentPosition - originalPosition).Should().BeGreaterThan(1);

            // Restore original state
            await shutter.RestoreSavedStateAsync();
            await Task.Delay(3000); // Wait for restoration
            
            // Verify restoration
            await shutter.InitializeAsync(); // Refresh current state
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
                    await Task.Delay(2000);
                    await shutter.StopAsync();
                    
                    var upPosition = await shutter.ReadPositionAsync();
                    Console.WriteLine($"After UP movement: {upPosition}%");
                    
                    // UP should decrease percentage (toward 0%)
                    (upPosition < initialPosition).Should().BeTrue(
                        $"UP movement should decrease position. Initial: {initialPosition}%, After UP: {upPosition}%");

                    await Task.Delay(1000);

                    // Test DOWN movement
                    await shutter.MoveAsync(ShutterDirection.Down, TimeSpan.FromSeconds(3));
                    await Task.Delay(2000);
                    await shutter.StopAsync();
                    
                    var downPosition = await shutter.ReadPositionAsync();
                    Console.WriteLine($"After DOWN movement: {downPosition}%");
                    
                    // DOWN should increase percentage (toward 100%)
                    (downPosition > upPosition).Should().BeTrue(
                        $"DOWN movement should increase position. After UP: {upPosition}%, After DOWN: {downPosition}%");
                }
                else
                {
                    // Test DOWN movement first
                    await shutter.MoveAsync(ShutterDirection.Down, TimeSpan.FromSeconds(3));
                    await Task.Delay(2000);
                    await shutter.StopAsync();
                    
                    var downPosition = await shutter.ReadPositionAsync();
                    Console.WriteLine($"After DOWN movement: {downPosition}%");
                    
                    // DOWN should increase percentage (toward 100%)
                    (downPosition > initialPosition).Should().BeTrue(
                        $"DOWN movement should increase position. Initial: {initialPosition}%, After DOWN: {downPosition}%");

                    await Task.Delay(1000);

                    // Test UP movement
                    await shutter.MoveAsync(ShutterDirection.Up, TimeSpan.FromSeconds(3));
                    await Task.Delay(2000);
                    await shutter.StopAsync();
                    
                    var upPosition = await shutter.ReadPositionAsync();
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
                
                var finalPosition = await shutter.ReadPositionAsync();
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
                var initialLockState = shutter.CurrentState.IsLocked;
                Console.WriteLine($"Shutter {shutterId} initial lock state: {initialLockState}");

                // Toggle lock state
                var newLockState = !initialLockState;
                await shutter.SetLockAsync(newLockState);
                
                var currentLockState = await shutter.ReadLockStateAsync();
                currentLockState.Should().Be(newLockState, 
                    $"Lock state should be {newLockState}, but was {currentLockState}");

                // Toggle back
                await shutter.SetLockAsync(initialLockState);
                
                var restoredLockState = await shutter.ReadLockStateAsync();
                restoredLockState.Should().Be(initialLockState, 
                    $"Lock state should be restored to {initialLockState}, but was {restoredLockState}");

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
                // Act & Assert - Manual test implementation (moved from model)
                var initialPosition = await shutter.ReadPositionAsync();
                var initialLockState = await shutter.ReadLockStateAsync();

                // Ensure shutter is locked
                if (!initialLockState)
                {
                    await shutter.SetLockAsync(true);
                }

                // Try to move the shutter
                Console.WriteLine($"Attempting to move locked shutter {shutterId}");
                await shutter.MoveAsync(ShutterDirection.Up, TimeSpan.FromSeconds(4));
                
                // Check if position changed
                var positionAfterMove = await shutter.ReadPositionAsync();
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
    }
}
