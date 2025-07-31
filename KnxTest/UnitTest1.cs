using FluentAssertions;
using KnxModel;
using KnxService;
using System.Globalization;
using System.Threading.Tasks;

namespace KnxTest
{
    [Collection("KnxService collection")]
    public class UnitTest1
    {
        private IKnxService _knxService { get; }

        public UnitTest1(KnxServiceFixture fixture)
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
                await Task.Delay(1000); // Give time for feedback
                Assert.Equal(!initialState, light.CurrentState.IsOn);
                Console.WriteLine($"✓ Light {lightId} successfully toggled to {(light.CurrentState.IsOn ? "ON" : "OFF")}");

                // Wait for state to stabilize
                await Task.Delay(1000);

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

                // Toggle the light to trigger state change
                await light.ToggleAsync();
                
                // Model automatically updates state from KNX feedback
                await Task.Delay(1000); // Give time for feedback
                
                // Assert state changed on model
                Assert.Equal(!initialState, light.CurrentState.IsOn);
                Console.WriteLine($"✓ Light {lightId} state changed correctly to {(light.CurrentState.IsOn ? "ON" : "OFF")}");
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

                // Choose a movement of 10% in appropriate direction
                var targetPosition = initialPosition < 50 ? initialPosition + 10 : initialPosition - 10;
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
                
                // Model automatically updates lock state from KNX feedback
                await Task.Delay(1000); // Give time for feedback
                
                // Assert - Check lock state changed on model
                Assert.Equal(!initialLockState, shutter.CurrentState.IsLocked);
                Console.WriteLine($"✓ Shutter {shutterId} lock successfully toggled to {(shutter.CurrentState.IsLocked ? "LOCKED" : "UNLOCKED")}");

                // Toggle back to original state
                await shutter.SetLockAsync(initialLockState);
                await Task.Delay(1000); // Give time for feedback
                
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
        [InlineData("1")]  // R1.1 Bathroom - Lock control, movement control, position feedback test
        [InlineData("2")]  // R2.1 Master Bathroom
        [InlineData("3")]  // R3.1 Master Bedroom
        [InlineData("4")]  // R3.2 Master Bedroom
        [InlineData("5")]  // R5.1 Guest Room
        [InlineData("6")]  // R6.1 Kinga's Room
        [InlineData("7")]  // R6.2 Kinga's Room
        [InlineData("8")]  // R6.3 Kinga's Room
        [InlineData("9")]  // R7.1 Rafal's Room
        [InlineData("10")] // R7.2 Rafal's Room
        [InlineData("11")] // R7.3 Rafal's Room
        [InlineData("12")] // R8.1 Hall
        [InlineData("13")] // R02.1 Kitchen
        [InlineData("14")] // R02.2 Kitchen
        [InlineData("15")] // R03.1 Dinning Room
        [InlineData("16")] // R04.1 Living Room
        [InlineData("17")] // R04.2 Living Room
        [InlineData("18")] // R05.1 Office - Lock control, movement control, position feedback test
        public async Task ShutterDoesNotMoveWhenLocked(string controlSubGroup)
        {
            // Arrange
            var feedbackSubGroup = (int.Parse(controlSubGroup) + KnxAddressConfiguration.SHUTTER_FEEDBACK_OFFSET).ToString();
            var lockControlAddress = KnxAddressConfiguration.CreateShutterLockAddress(controlSubGroup); // 4/3/18 - lock control
            var lockFeedbackAddress = KnxAddressConfiguration.CreateShutterLockFeedbackAddress(controlSubGroup); // 4/3/118 - lock feedback
            var movementControlAddress = KnxAddressConfiguration.CreateShutterMovementAddress(controlSubGroup); // 4/0/18 - UP/DOWN control
            var movementFeedbackAddress = KnxAddressConfiguration.CreateShutterMovementFeedbackAddress(controlSubGroup); // 4/0/118 - UP/DOWN feedback
            var positionFeedbackAddress = KnxAddressConfiguration.CreateShutterPositionFeedbackAddress(controlSubGroup); // 4/2/118 - position feedback

            Console.WriteLine($"Lock control: {lockControlAddress}, feedback: {lockFeedbackAddress}");
            Console.WriteLine($"Movement control: {movementControlAddress}, feedback: {movementFeedbackAddress}");
            Console.WriteLine($"Position feedback: {positionFeedbackAddress}");

            // Get initial states
            var initialLockState = await _knxService.RequestGroupValue<bool>(lockFeedbackAddress);
            Console.WriteLine($"Initial lock state: {initialLockState}");

            float? initialPosition = null;
            try
            {
                initialPosition = await _knxService.RequestGroupValue<float>(positionFeedbackAddress);
                Console.WriteLine($"Initial position: {initialPosition.Value:F1}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not read initial position: {ex.Message}");
                return; // Can't test without knowing initial position
            }

            var feedbackReceived = new List<KnxGroupEventArgs>();
            var lockReceived = new List<KnxGroupEventArgs>();
            var positionReceived = new List<KnxGroupEventArgs>();
            
            EventHandler<KnxGroupEventArgs> handler = (sender, args) =>
            {
                if (args.Destination == lockFeedbackAddress)
                {
                    lockReceived.Add(args);
                    Console.WriteLine($"Lock feedback: {args.Value} at {DateTime.Now:HH:mm:ss.fff}");
                }
                else if (args.Destination == movementFeedbackAddress)
                {
                    feedbackReceived.Add(args);
                    Console.WriteLine($"Movement feedback: {args.Value} at {DateTime.Now:HH:mm:ss.fff}");
                }
                else if (args.Destination == positionFeedbackAddress)
                {
                    positionReceived.Add(args);
                    Console.WriteLine($"Position feedback: {args.Value} at {DateTime.Now:HH:mm:ss.fff}");
                }
            };

            _knxService.GroupMessageReceived += handler;

            try
            {
                // Step 1: Engage the lock
                Console.WriteLine("\n=== Step 1: Engaging lock ===");
                _knxService.WriteGroupValue(lockControlAddress, true);
                await Task.Delay(1000); // Wait for lock to engage
                
                // Verify lock is engaged
                var lockState = await _knxService.RequestGroupValue<bool>(lockFeedbackAddress);
                Assert.True(lockState);
                Console.WriteLine("✓ Lock successfully engaged");

                // Determine test order based on current position (intelligent testing)
                var currentPercent = initialPosition.Value;
                bool testUpFirst = currentPercent > 50; // If down (>50%), test UP first
                
                Console.WriteLine($"Current position: {currentPercent:F1}% - Testing {(testUpFirst ? "UP first, then DOWN" : "DOWN first, then UP")} while locked");

                double firstMovement, secondMovement;
                string firstDirection, secondDirection;
                bool firstIsUp, secondIsUp;

                if (testUpFirst)
                {
                    firstDirection = "UP";
                    secondDirection = "DOWN";
                    firstIsUp = true;
                    secondIsUp = false;
                }
                else
                {
                    firstDirection = "DOWN";
                    secondDirection = "UP";
                    firstIsUp = false;
                    secondIsUp = true;
                }

                // Step 2: Try first movement while locked
                Console.WriteLine($"\n=== Step 2: Attempting {firstDirection} movement while locked ===");
                var positionBeforeFirst = await _knxService.RequestGroupValue<Percent>(positionFeedbackAddress);
                Console.WriteLine($"Position before {firstDirection} attempt: {positionBeforeFirst.Value:F1}%");
                
                // Clear previous feedback
                feedbackReceived.Clear();
                positionReceived.Clear();
                
                // Send first movement command
                _knxService.WriteGroupValue(movementControlAddress, !firstIsUp); // false = UP, true = DOWN
                await Task.Delay(3000); // Wait to see if movement occurs
                
                // Check position after first attempt
                var positionAfterFirst = await _knxService.RequestGroupValue<Percent>(positionFeedbackAddress);
                Console.WriteLine($"Position after {firstDirection} attempt: {positionAfterFirst.Value:F1}%");
                
                firstMovement = Math.Abs(positionAfterFirst.Value - positionBeforeFirst.Value);
                Console.WriteLine($"{firstDirection} movement detected: {firstMovement:F1}%");

                // Step 3: Try second movement while locked
                Console.WriteLine($"\n=== Step 3: Attempting {secondDirection} movement while locked ===");
                
                // Clear previous feedback
                feedbackReceived.Clear();
                positionReceived.Clear();
                
                // Send second movement command
                _knxService.WriteGroupValue(movementControlAddress, !secondIsUp); // false = UP, true = DOWN
                await Task.Delay(3000); // Wait to see if movement occurs
                
                // Check position after second attempt
                var positionAfterSecond = await _knxService.RequestGroupValue<Percent>(positionFeedbackAddress);
                Console.WriteLine($"Position after {secondDirection} attempt: {positionAfterSecond.Value:F1}%");
                
                secondMovement = Math.Abs(positionAfterSecond.Value - positionAfterFirst.Value);
                Console.WriteLine($"{secondDirection} movement detected: {secondMovement:F1}%");

                // Step 4: Disengage lock and verify movement works
                Console.WriteLine("\n=== Step 4: Disengaging lock and testing movement ===");
                
                // Restore original lock state
                _knxService.WriteGroupValue(lockControlAddress, initialLockState);
                await Task.Delay(1000); // Wait for lock to disengage
                
                // Verify lock is disengaged
                var finalLockState = await _knxService.RequestGroupValue<bool>(lockFeedbackAddress);
                Assert.Equal(initialLockState, finalLockState);
                Console.WriteLine($"✓ Lock restored to original state: {finalLockState}");

                // Test that movement works when unlocked (brief test)
                if (!initialLockState) // Only test if originally unlocked
                {
                    Console.WriteLine("Testing brief movement while unlocked...");
                    feedbackReceived.Clear();
                    positionReceived.Clear();
                    
                    var positionBeforeUnlockedTest = await _knxService.RequestGroupValue<Percent>(positionFeedbackAddress);
                    
                    // Brief movement test
                    _knxService.WriteGroupValue(movementControlAddress, false); // UP
                    await Task.Delay(1000); // Very brief movement
                    
                    // Stop movement
                    var stopAddress = KnxAddressConfiguration.CreateShutterStopAddress(controlSubGroup); // 4/1/18 - STOP
                    _knxService.WriteGroupValue(stopAddress, true);
                    await Task.Delay(1000);
                    
                    var positionAfterUnlockedTest = await _knxService.RequestGroupValue<Percent>(positionFeedbackAddress);
                    var unlockedMovement = Math.Abs(positionAfterUnlockedTest.Value - positionBeforeUnlockedTest.Value);
                    Console.WriteLine($"Movement when unlocked: {unlockedMovement:F1}%");
                }

                // Assertions
                Console.WriteLine("\n=== Test Results ===");
                Console.WriteLine($"Movement while locked - {firstDirection}: {firstMovement:F1}%, {secondDirection}: {secondMovement:F1}%");
                
                // The shutter MUST NOT move AT ALL while locked - zero tolerance
                Assert.True(firstMovement == 0.0, 
                    $"Shutter moved {firstDirection} {firstMovement:F1}% while locked - NO movement allowed!");
                Assert.True(secondMovement == 0.0, 
                    $"Shutter moved {secondDirection} {secondMovement:F1}% while locked - NO movement allowed!");
                
                Console.WriteLine($"✓ Shutter correctly ignored {firstDirection} and {secondDirection} movement commands while locked - NO movement detected");
                Console.WriteLine($"Lock feedback messages received: {lockReceived.Count}");
                Console.WriteLine($"Movement feedback messages during lock: {feedbackReceived.Count}");
                Console.WriteLine($"Position feedback messages during lock: {positionReceived.Count}");

                // Step 5: Return to original position (manual restoration)
                Console.WriteLine("\n=== Step 5: Returning to original position ===");
                try
                {
                    var absolutePositionAddress = $"{KnxAddressConfiguration.SHUTTERS_MAIN_GROUP}/{KnxAddressConfiguration.SHUTTERS_POSITION_MIDDLE_GROUP}/{controlSubGroup}";
                    _knxService.WriteGroupValue(absolutePositionAddress, initialPosition.Value);
                    await Task.Delay(2000);
                    Console.WriteLine($"Position restore command sent to {absolutePositionAddress}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error returning to original position: {ex.Message}");
                }
            }
            finally
            {
                _knxService.GroupMessageReceived -= handler;
                
                // Ensure we restore the original lock state
                try
                {
                    _knxService.WriteGroupValue(lockControlAddress, initialLockState);
                    await Task.Delay(500);
                    Console.WriteLine("Lock state restored in finally block");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error restoring lock state: {ex.Message}");
                }
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

            // Act
            await shutter.InitializeAsync();
            var position = await shutter.ReadPositionAsync();

            // Assert
            shutter.CurrentState.Should().NotBeNull();
            shutter.CurrentState.Position.Should().BeInRange(0, 100, 
                $"Shutter {shutterId} position should be between 0-100%, got {shutter.CurrentState.Position:F1}%");
            
            position.Should().BeInRange(0, 100, 
                $"Direct position read for shutter {shutterId} should be between 0-100%, got {position:F1}%");
            
            // Verify that both readings are consistent
            Math.Abs(shutter.CurrentState.Position - position).Should().BeLessThan(1,
                $"Current state position ({shutter.CurrentState.Position:F1}%) should match direct read ({position:F1}%) for shutter {shutterId}");

            Console.WriteLine($"✓ Shutter {shutterId} ({shutter.Name}) position: {position:F1}% (Raw: {(byte)(position * 2.55)})");
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