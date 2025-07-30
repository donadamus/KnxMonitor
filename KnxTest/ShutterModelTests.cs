using FluentAssertions;
using KnxModel;
using KnxService;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest
{
    [Collection("KnxService collection")]
    public class ShutterModelTests
    {
        private readonly IKnxService _knxService;

        public ShutterModelTests(KnxServiceFixture fixture)
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
            shutter.CurrentState.Position.Value.Should().BeInRange(0, 100);
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
            await shutter.SaveCurrentStateAsync();
            shutter.SavedState.Should().NotBeNull();
            shutter.SavedState!.Position.Should().Be(shutter.CurrentState.Position);
            shutter.SavedState.IsLocked.Should().Be(shutter.CurrentState.IsLocked);

            // Modify shutter state (small movement)
            var originalPosition = shutter.CurrentState.Position;
            var testPosition = Percent.FromPercantage(Math.Min(100, originalPosition.Value + 5));
            
            await shutter.SetPositionAsync(testPosition);
            await Task.Delay(2000); // Wait for movement
            
            // Verify state changed
            await shutter.InitializeAsync(); // Refresh current state
            var currentPosition = shutter.CurrentState.Position;
            Math.Abs(currentPosition.Value - originalPosition.Value).Should().BeGreaterThan(1);

            // Restore original state
            await shutter.RestoreSavedStateAsync();
            await Task.Delay(3000); // Wait for restoration
            
            // Verify restoration
            await shutter.InitializeAsync(); // Refresh current state
            var restoredPosition = shutter.CurrentState.Position;
            Math.Abs(restoredPosition.Value - originalPosition.Value).Should().BeLessThan(3); // Allow 3% tolerance
            
            Console.WriteLine($"Shutter {shutterId} - Original: {originalPosition.Value:F1}%, " +
                            $"Modified: {currentPosition.Value:F1}%, Restored: {restoredPosition.Value:F1}%");
        }

        [Theory]
        [InlineData("R1.1")]
        [InlineData("R7.1")]
        public async Task CanMoveShutterInBothDirections(string shutterId)
        {
            // Arrange
            var shutter = ShutterFactory.CreateShutter(shutterId, _knxService);
            await shutter.InitializeAsync();
            await shutter.SaveCurrentStateAsync();

            try
            {
                var initialPosition = shutter.CurrentState.Position;
                Console.WriteLine($"Testing shutter {shutterId} movement from {initialPosition.Value:F1}%");

                // Test movement based on initial position
                bool testUpFirst = initialPosition.Value > 50; // If down, test UP first

                if (testUpFirst)
                {
                    // Test UP movement
                    await shutter.MoveAsync(ShutterDirection.Up, TimeSpan.FromSeconds(3));
                    await Task.Delay(2000);
                    await shutter.StopAsync();
                    
                    var upPosition = await shutter.ReadPositionAsync();
                    Console.WriteLine($"After UP movement: {upPosition.Value:F1}%");
                    
                    // UP should decrease percentage (toward 0%)
                    (upPosition.Value < initialPosition.Value).Should().BeTrue(
                        $"UP movement should decrease position. Initial: {initialPosition.Value:F1}%, After UP: {upPosition.Value:F1}%");

                    await Task.Delay(1000);

                    // Test DOWN movement
                    await shutter.MoveAsync(ShutterDirection.Down, TimeSpan.FromSeconds(3));
                    await Task.Delay(2000);
                    await shutter.StopAsync();
                    
                    var downPosition = await shutter.ReadPositionAsync();
                    Console.WriteLine($"After DOWN movement: {downPosition.Value:F1}%");
                    
                    // DOWN should increase percentage (toward 100%)
                    (downPosition.Value > upPosition.Value).Should().BeTrue(
                        $"DOWN movement should increase position. After UP: {upPosition.Value:F1}%, After DOWN: {downPosition.Value:F1}%");
                }
                else
                {
                    // Test DOWN movement first
                    await shutter.MoveAsync(ShutterDirection.Down, TimeSpan.FromSeconds(3));
                    await Task.Delay(2000);
                    await shutter.StopAsync();
                    
                    var downPosition = await shutter.ReadPositionAsync();
                    Console.WriteLine($"After DOWN movement: {downPosition.Value:F1}%");
                    
                    // DOWN should increase percentage (toward 100%)
                    (downPosition.Value > initialPosition.Value).Should().BeTrue(
                        $"DOWN movement should increase position. Initial: {initialPosition.Value:F1}%, After DOWN: {downPosition.Value:F1}%");

                    await Task.Delay(1000);

                    // Test UP movement
                    await shutter.MoveAsync(ShutterDirection.Up, TimeSpan.FromSeconds(3));
                    await Task.Delay(2000);
                    await shutter.StopAsync();
                    
                    var upPosition = await shutter.ReadPositionAsync();
                    Console.WriteLine($"After UP movement: {upPosition.Value:F1}%");
                    
                    // UP should decrease percentage (toward 0%)
                    (upPosition.Value < downPosition.Value).Should().BeTrue(
                        $"UP movement should decrease position. After DOWN: {downPosition.Value:F1}%, After UP: {upPosition.Value:F1}%");
                }
            }
            finally
            {
                // Always restore original state
                await shutter.RestoreSavedStateAsync();
            }
        }

        [Theory]
        [InlineData("R2.1")]
        [InlineData("R8.1")]
        public async Task CanSetAbsolutePosition(string shutterId)
        {
            // Arrange
            var shutter = ShutterFactory.CreateShutter(shutterId, _knxService);
            await shutter.InitializeAsync();
            await shutter.SaveCurrentStateAsync();

            try
            {
                var initialPosition = shutter.CurrentState.Position;
                
                // Choose target position based on current position
                var targetPercent = initialPosition.Value < 50 ? 70.0 : 30.0;
                var targetPosition = Percent.FromPercantage(targetPercent);

                Console.WriteLine($"Setting shutter {shutterId} from {initialPosition.Value:F1}% to {targetPercent:F1}%");

                // Act
                await shutter.SetPositionAsync(targetPosition);
                
                // Wait for position to be reached
                var reached = await shutter.WaitForPositionAsync(targetPosition, tolerance: 5.0, TimeSpan.FromSeconds(15));

                // Assert
                reached.Should().BeTrue($"Shutter should reach target position {targetPercent:F1}%");
                
                var finalPosition = await shutter.ReadPositionAsync();
                Console.WriteLine($"Final position: {finalPosition.Value:F1}%");
                
                Math.Abs(finalPosition.Value - targetPercent).Should().BeLessThan(5.0, 
                    $"Final position should be within 5% of target. Target: {targetPercent:F1}%, Actual: {finalPosition.Value:F1}%");
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
            await shutter.SaveCurrentStateAsync();

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
            await shutter.SaveCurrentStateAsync();

            try
            {
                // Act & Assert
                var lockEffective = await shutter.TestLockFunctionalityAsync(TimeSpan.FromSeconds(4));
                
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
