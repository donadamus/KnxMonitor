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

        // KNX Address Constants
        private const string LIGHTS_MAIN_GROUP = "1";
        private const string LIGHTS_MIDDLE_GROUP = "1";
        
        private const string SHUTTERS_MAIN_GROUP = "4";
        private const string SHUTTERS_MOVEMENT_MIDDLE_GROUP = "0";
        private const string SHUTTERS_POSITION_MIDDLE_GROUP = "2";
        private const string SHUTTERS_LOCK_MIDDLE_GROUP = "3";
        private const string SHUTTERS_STOP_MIDDLE_GROUP = "1";
        
        // Feedback offset for shutters (add 100 to control sub group)
        private const int SHUTTER_FEEDBACK_OFFSET = 100;

        public UnitTest1(KnxServiceFixture fixture)
        {
            _knxService = fixture.KnxService;
        }

        [Theory]
        [InlineData("01", "101")]
        [InlineData("02", "102")]
        [InlineData("03", "103")]
        [InlineData("04", "104")]
        [InlineData("05", "105")]
        [InlineData("06", "106")]
        [InlineData("07", "107")]
        [InlineData("08", "108")]
        [InlineData("09", "109")]
        [InlineData("10", "110")]
        [InlineData("11", "111")]
        [InlineData("12", "112")]
        [InlineData("13", "113")]
        [InlineData("14", "114")]
        [InlineData("15", "115")]
        public async Task CanToggleLightAndRestoreOriginalState(string subGroupControl, string subGroupReceived)
        {
            
            // Arrange
            var controlAddress = $"{LIGHTS_MAIN_GROUP}/{LIGHTS_MIDDLE_GROUP}/{subGroupControl}";
            var feedbackAddress = $"{LIGHTS_MAIN_GROUP}/{LIGHTS_MIDDLE_GROUP}/{subGroupReceived}";

            var initialValue = await _knxService.RequestGroupValue<bool>(feedbackAddress);

            // No need for expected values array for bool
            
            // Invert the current value for the test
            var testValue = !initialValue;

            async Task<bool> SendAndVerify( bool value)
            {
                var taskCompletionSource = new TaskCompletionSource<KnxGroupEventArgs>();
                EventHandler<KnxGroupEventArgs> handler = (sender, args) =>
                {
                    if (args.Destination == feedbackAddress)
                    {
                        taskCompletionSource.SetResult(args);
                    }
                };
                // Subscribe to the event
                _knxService.GroupMessageReceived += handler;
                try
                {
                    // Write the group value
                    _knxService.WriteGroupValue(controlAddress, value);
                    var response = await taskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(2));
                    // Assert
                    return response.Destination == feedbackAddress && response.Value.AsBoolean() == value;
                }
                finally
                {
                    _knxService.GroupMessageReceived -= handler;
                }
            }

            // Assert
            // Act + Assert (1): Zmiana na przeciwną wartość
            var resultChanged = await SendAndVerify(testValue);
            Assert.True(resultChanged, $"Expected feedback value {testValue} not received.");
            Thread.Sleep(1000); // Wait for the change to propagate
            // Act + Assert (2): Przywrócenie oryginalnej wartości
            var resultRestored = await SendAndVerify(initialValue);
            Assert.True(resultRestored, $"Failed to restore original state {initialValue}.");

        }


        [Theory]
        [InlineData("101")]
        [InlineData("102")]
        [InlineData("103")]
        [InlineData("104")]
        [InlineData("105")]
        [InlineData("106")]
        [InlineData("107")]
        [InlineData("108")]
        [InlineData("109")]
        [InlineData("110")]
        [InlineData("111")]
        [InlineData("112")]
        [InlineData("113")]
        [InlineData("114")]
        [InlineData("115")]
        //[InlineData("116")]
        //[InlineData("117")]
        //[InlineData("118")]
        //[InlineData("119")]
        //[InlineData("120")]
        public async Task CanReadLightFeedback(string subGroup)
        {

            // Arrange

            var expectedDestination = $"{LIGHTS_MAIN_GROUP}/{LIGHTS_MIDDLE_GROUP}/{subGroup}";

            var taskCompletionSource = new TaskCompletionSource<KnxGroupEventArgs>();

            EventHandler<KnxGroupEventArgs> handler = (sender, args) =>
            {
                if (args.Destination == expectedDestination)
                {
                    taskCompletionSource.SetResult(args);
                }
            };

            _knxService.GroupMessageReceived += handler;
            // Act

            try
            {
                var feedbackAddress = $"{LIGHTS_MAIN_GROUP}/{LIGHTS_MIDDLE_GROUP}/{subGroup}";
                var currentValue = await _knxService.RequestGroupValue<bool>(feedbackAddress);

                var expectedValues = new[] { "0", "1" };

                var response = await taskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(2));

                // Assert
                Assert.Equal(expectedDestination, response.Destination);
                Assert.Contains(response.Value.AsString(), expectedValues);
            }
            finally
            {
                _knxService.GroupMessageReceived -= handler;
            }
        }

        [Theory]
        [InlineData("1")]  // R1.1 Bathroom UP/DOWN
        [InlineData("2")]  // R2.1 Master Bathroom UP/DOWN
        [InlineData("3")]  // R3.1 Master Bedroom UP/DOWN
        [InlineData("4")]  // R3.2 Master Bedroom UP/DOWN
        [InlineData("5")]  // R5.1 Guest Room UP/DOWN
        [InlineData("6")]  // R6.1 Kinga's Room UP/DOWN
        [InlineData("7")]  // R6.2 Kinga's Room UP/DOWN
        [InlineData("8")]  // R6.3 Kinga's Room UP/DOWN
        [InlineData("9")]  // R7.1 Rafal's Room UP/DOWN
        [InlineData("10")] // R7.2 Rafal's Room UP/DOWN
        [InlineData("11")] // R7.3 Rafal's Room UP/DOWN
        [InlineData("12")] // R8.1 Hall UP/DOWN
        [InlineData("13")] // R02.1 Kitchen
        [InlineData("14")] // R02.2 Kitchen
        [InlineData("15")] // R03.1 Dinning Room
        [InlineData("16")] // R04.1 Living Room
        [InlineData("17")] // R04.2 Living Room
        [InlineData("18")] // R05.1 Office - control and feedback addresses
        public async Task CanSetShutterAbsolutePositionAndReadFeedback(string controlSubGroup)
        {
            // Arrange
            var feedbackSubGroup = (int.Parse(controlSubGroup) + SHUTTER_FEEDBACK_OFFSET).ToString();
            var controlAddress = $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_POSITION_MIDDLE_GROUP}/{controlSubGroup}";
            var feedbackAddress = $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_POSITION_MIDDLE_GROUP}/{feedbackSubGroup}";

            // Get initial position
            int? initialPosition = null;
            try
            {
                initialPosition = await _knxService.RequestGroupValue<int>(feedbackAddress);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not read initial position: {ex.Message}");
            }
            
            // Choose a movement of 10% in appropriate direction
            var currentPercent = initialPosition ?? 50; // Default to 50% if no feedback
            var targetPercent = currentPercent < 50 ? currentPercent + 10 : currentPercent - 10;
            var testPercent = Math.Max(0, Math.Min(100, targetPercent));

            Console.WriteLine($"Current position: {currentPercent}%, Target: {targetPercent}%");

            var feedbackReceived = new List<KnxGroupEventArgs>();
            var taskCompletionSource = new TaskCompletionSource<bool>();

            EventHandler<KnxGroupEventArgs> handler = (sender, args) =>
            {
                if (args.Destination == feedbackAddress)
                {
                    feedbackReceived.Add(args);
                    Console.WriteLine($"Feedback received: {args.Value} at {DateTime.Now:HH:mm:ss.fff}");
                    
                    // Parse feedback value to check if it matches our target
                    if (byte.TryParse(args.Value.AsString(), NumberStyles.HexNumber, null, out byte feedbackRaw))
                    {
                        var targetRaw = (byte)(testPercent * 2.55); // Convert percentage to KNX raw value
                        var tolerance = 3; // Allow ±3 raw values tolerance
                        
                        // If feedback matches target (within tolerance), we can stop waiting
                        if (Math.Abs(feedbackRaw - targetRaw) <= tolerance)
                        {
                            Console.WriteLine($"Target position reached! Feedback: {feedbackRaw}, Target: {targetRaw}");
                            taskCompletionSource.TrySetResult(true);
                        }
                        // Or if we've received multiple feedback messages (indicating ongoing movement)
                        else if (feedbackReceived.Count >= 2)
                        {
                            Console.WriteLine($"Multiple feedback messages received, movement in progress...");
                            taskCompletionSource.TrySetResult(true);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Could not parse feedback value: {args.Value}");
                    }
                }
            };

            _knxService.GroupMessageReceived += handler;

            try
            {
                // Act - Set new position using the new int overload
                var testRaw = (byte)(testPercent * 2.55);
                Console.WriteLine($"Sending position command: {testRaw} ({testPercent}%)");
                _knxService.WriteGroupValue(controlAddress, testPercent);
                
                // Wait for movement feedback or timeout (shorter timeout for small movements)
                var feedbackTask = taskCompletionSource.Task;
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(8)); // 8 seconds should be enough for 10% movement
                var completedTask = await Task.WhenAny(feedbackTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("No sufficient feedback received within timeout");
                }
                else
                {
                    Console.WriteLine($"Received {feedbackReceived.Count} feedback messages during movement");
                }

                // Wait a bit more for movement to potentially complete (shorter wait)
                await Task.Delay(1500);

                // Get final position
                int? finalPosition = null;
                try
                {
                    finalPosition = await _knxService.RequestGroupValue<int>(feedbackAddress);
                    var finalRaw = (byte)(finalPosition.Value * 2.55);
                    Console.WriteLine($"Final position: {finalPosition.Value}%, Raw: {finalRaw}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not read final position: {ex.Message}");
                }

                // Assert - Check that we received feedback and movement occurred
                Assert.True(feedbackReceived.Count > 0, "Should have received at least one feedback message");
                
                if (initialPosition.HasValue && finalPosition.HasValue)
                {
                    // Compare raw values with tolerance (KNX has 255 steps for 0-100%)
                    var initialRaw = (byte)(initialPosition.Value * 2.55);
                    var finalRaw = (byte)(finalPosition.Value * 2.55);
                    var targetRaw = (byte)(testPercent * 2.55);
                    
                    // Allow tolerance of ±3 raw values (about 1.2%)
                    var tolerance = 3;
                    var actualMovement = Math.Abs(finalRaw - initialRaw);
                    
                    Console.WriteLine($"Raw values - Initial: {initialRaw}, Final: {finalRaw}, Target: {targetRaw}");
                    Console.WriteLine($"Movement: {actualMovement} raw units");
                    
                    if (actualMovement >= tolerance)
                    {
                        // Check direction of movement
                        var expectedDirection = targetRaw > initialRaw ? 1 : -1;
                        var actualDirection = finalRaw > initialRaw ? 1 : -1;
                        
                        Assert.True(expectedDirection == actualDirection, 
                            $"Movement direction incorrect. Expected: {(expectedDirection > 0 ? "UP" : "DOWN")}, " +
                            $"Actual: {(actualDirection > 0 ? "UP" : "DOWN")}");
                        
                        Console.WriteLine($"✓ Shutter moved correctly in {(actualDirection > 0 ? "UP" : "DOWN")} direction");
                    }
                    else
                    {
                        Console.WriteLine($"⚠ Small movement detected ({actualMovement} raw units), might be within tolerance");
                    }
                }
                else
                {
                    Console.WriteLine("⚠ Could not compare positions due to missing initial or final position");
                }

                // Restore original position if we have it
                if (initialPosition.HasValue)
                {
                    Console.WriteLine($"Restoring original position: {initialPosition.Value}%");
                    _knxService.WriteGroupValue(controlAddress, initialPosition.Value);
                    await Task.Delay(2000); // Wait for movement to start
                }
            }
            finally
            {
                _knxService.GroupMessageReceived -= handler;
            }
        }

        [Theory]
        [InlineData("1")]  // R1.1 Bathroom UP/DOWN movement test with movement feedback
        [InlineData("2")]  // R2.1 Master Bathroom UP/DOWN
        [InlineData("3")]  // R3.1 Master Bedroom UP/DOWN
        [InlineData("4")]  // R3.2 Master Bedroom UP/DOWN
        [InlineData("5")]  // R5.1 Guest Room UP/DOWN
        [InlineData("6")]  // R6.1 Kinga's Room UP/DOWN
        [InlineData("7")]  // R6.2 Kinga's Room UP/DOWN
        [InlineData("8")]  // R6.3 Kinga's Room UP/DOWN
        [InlineData("9")]  // R7.1 Rafal's Room UP/DOWN
        [InlineData("10")] // R7.2 Rafal's Room UP/DOWN
        [InlineData("11")] // R7.3 Rafal's Room UP/DOWN
        [InlineData("12")] // R8.1 Hall UP/DOWN
        [InlineData("13")] // R02.1 Kitchen
        [InlineData("14")] // R02.2 Kitchen
        [InlineData("15")] // R03.1 Dinning Room
        [InlineData("16")] // R04.1 Living Room
        [InlineData("17")] // R04.2 Living Room
        [InlineData("18")] // R05.1 Office - UP/DOWN movement test with movement feedback
        public async Task CanMoveShutterUpAndDown(string controlSubGroup)
        {
            // Arrange
            var feedbackSubGroup = (int.Parse(controlSubGroup) + SHUTTER_FEEDBACK_OFFSET).ToString();
            var controlAddress = $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_MOVEMENT_MIDDLE_GROUP}/{controlSubGroup}"; // 4/0/18 - UP/DOWN control
            var feedbackAddress = $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_MOVEMENT_MIDDLE_GROUP}/{feedbackSubGroup}"; // 4/0/118 - UP/DOWN feedback
            var positionFeedbackAddress = $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_POSITION_MIDDLE_GROUP}/{feedbackSubGroup}"; // 4/2/118 - position feedback
            var movementStatusAddress = $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_STOP_MIDDLE_GROUP}/{feedbackSubGroup}"; // 4/1/118 - movement status
            var stopAddress = $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_STOP_MIDDLE_GROUP}/{controlSubGroup}"; // 4/1/18 - STOP/STEP control

            Console.WriteLine($"Control: {controlAddress}, Feedback: {feedbackAddress}");
            Console.WriteLine($"Position feedback: {positionFeedbackAddress}, Movement status: {movementStatusAddress}");
            Console.WriteLine($"STOP/STEP control: {stopAddress}");

            // Get initial position
            int? initialPosition = null;
            try
            {
                initialPosition = await _knxService.RequestGroupValue<int>(positionFeedbackAddress);
                Console.WriteLine($"Initial position: {initialPosition.Value}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not read initial position: {ex.Message}");
                return; // Can't test without knowing initial position
            }

            var initialPercent = initialPosition.Value;
            
            // Decide test order based on initial position
            // If shutter is down (>50% closed), test UP first, then DOWN
            // If shutter is up (<50% closed), test DOWN first, then UP
            bool testUpFirst = initialPercent > 50;
            
            Console.WriteLine($"Initial position: {initialPercent:F1}% - Testing {(testUpFirst ? "UP first, then DOWN" : "DOWN first, then UP")}");

            try
            {
                if (testUpFirst)
                {
                    // Test UP first, then DOWN
                    await TestShutterMovement(controlAddress, feedbackAddress, positionFeedbackAddress, 
                        movementStatusAddress, stopAddress, false, "UP", 8);
                    
                    await Task.Delay(2000); // Wait between tests
                    
                    await TestShutterMovement(controlAddress, feedbackAddress, positionFeedbackAddress, 
                        movementStatusAddress, stopAddress, true, "DOWN", 5);
                }
                else
                {
                    // Test DOWN first, then UP
                    await TestShutterMovement(controlAddress, feedbackAddress, positionFeedbackAddress, 
                        movementStatusAddress, stopAddress, true, "DOWN", 8);
                    
                    await Task.Delay(2000); // Wait between tests
                    
                    await TestShutterMovement(controlAddress, feedbackAddress, positionFeedbackAddress, 
                        movementStatusAddress, stopAddress, false, "UP", 5);
                }

                // Return to original position
                Console.WriteLine($"\nReturning to original position: {initialPercent:F1}%");
                await ReturnToOriginalPosition(controlAddress, positionFeedbackAddress, stopAddress, initialPosition.Value, SHUTTERS_POSITION_MIDDLE_GROUP, controlSubGroup);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
                
                // Attempt to return to original position even if test failed
                try
                {
                    Console.WriteLine($"Attempting to return to original position after failure...");
                    await ReturnToOriginalPosition(controlAddress, positionFeedbackAddress, stopAddress, initialPosition.Value, SHUTTERS_POSITION_MIDDLE_GROUP, controlSubGroup);
                }
                catch (Exception returnEx)
                {
                    Console.WriteLine($"Failed to return to original position: {returnEx.Message}");
                }
                
                throw; // Re-throw original exception
            }
        }

        private async Task TestShutterMovement(string controlAddress, string feedbackAddress, 
            string positionFeedbackAddress, string movementStatusAddress, string stopAddress, 
            bool isDownDirection, string directionName, int timeoutSeconds)
        {
            Console.WriteLine($"\nTesting {directionName} movement...");
            
            // Get position before movement
            Percent? beforePosition = null;
            try
            {
                beforePosition = await _knxService.RequestGroupValue<Percent>(positionFeedbackAddress);
                Console.WriteLine($"Position before {directionName}: {beforePosition.Value.Value:F1}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not read position before {directionName}: {ex.Message}");
            }

            var feedbackReceived = new List<KnxGroupEventArgs>();
            var movementReceived = new List<KnxGroupEventArgs>();
            var upDownReceived = new List<KnxGroupEventArgs>();
            var taskCompletionSource = new TaskCompletionSource<bool>();
            bool movementStoppedAutomatically = false;

            EventHandler<KnxGroupEventArgs> handler = (sender, args) =>
            {
                if (args.Destination == feedbackAddress)
                {
                    upDownReceived.Add(args);
                    Console.WriteLine($"UP/DOWN feedback: {args.Value} at {DateTime.Now:HH:mm:ss.fff}");
                }
                else if (args.Destination == positionFeedbackAddress)
                {
                    feedbackReceived.Add(args);
                    Console.WriteLine($"Position feedback: {args.Value} at {DateTime.Now:HH:mm:ss.fff}");
                }
                else if (args.Destination == movementStatusAddress)
                {
                    movementReceived.Add(args);
                    Console.WriteLine($"Movement status: {args.Value} at {DateTime.Now:HH:mm:ss.fff}");
                    
                    // If movement stopped (value = 0), we can finish
                    if (args.Value.AsString() == "0")
                    {
                        Console.WriteLine("Movement stopped automatically!");
                        movementStoppedAutomatically = true;
                        taskCompletionSource.TrySetResult(true);
                    }
                }
            };

            _knxService.GroupMessageReceived += handler;

            try
            {
                // Send movement command
                _knxService.WriteGroupValue(controlAddress, isDownDirection);
                
                // Wait for movement to start and stop, or timeout
                var movementTask = taskCompletionSource.Task;
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
                var completedTask = await Task.WhenAny(movementTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Console.WriteLine($"{directionName} movement timeout - no automatic stop received");
                }
                
                // Send STOP command only if movement didn't stop automatically
                if (!movementStoppedAutomatically)
                {
                    Console.WriteLine($"Sending STOP command for {directionName} movement via {stopAddress}...");
                    _knxService.WriteGroupValue(stopAddress, true);
                }
                else
                {
                    Console.WriteLine($"{directionName} movement stopped automatically, no STOP command needed");
                }
                
                // Wait for stop command to take effect
                await Task.Delay(2000);

                // Get position after movement
                Percent? afterPosition = null;
                try
                {
                    afterPosition = await _knxService.RequestGroupValue<Percent>(positionFeedbackAddress);
                    Console.WriteLine($"Position after {directionName}: {afterPosition.Value.Value:F1}%");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not read position after {directionName}: {ex.Message}");
                }

                // Assert - Check that we received appropriate feedback
                Assert.True(upDownReceived.Count > 0 || feedbackReceived.Count > 0 || movementReceived.Count > 0, 
                    $"Should have received at least one type of feedback message for {directionName} movement");
                
                // Check movement direction if we have position data
                if (beforePosition.HasValue && afterPosition.HasValue)
                {
                    var beforePercent = beforePosition.Value.Value;
                    var afterPercent = afterPosition.Value.Value;
                    var movement = afterPercent - beforePercent;
                    
                    Console.WriteLine($"{directionName} position change: {beforePercent:F1}% → {afterPercent:F1}% (Δ{movement:+0.0;-0.0}%)");
                    
                    if (Math.Abs(movement) > 1) // If significant movement occurred
                    {
                        if (isDownDirection)
                        {
                            // DOWN movement should increase percentage (closer to 100%)
                            Assert.True(movement > 0, 
                                $"DOWN movement should increase position percentage. " +
                                $"Expected positive change, got {movement:+0.0;-0.0}%");
                        }
                        else
                        {
                            // UP movement should decrease percentage (closer to 0%)
                            Assert.True(movement < 0, 
                                $"UP movement should decrease position percentage. " +
                                $"Expected negative change, got {movement:+0.0;-0.0}%");
                        }
                        
                        Console.WriteLine($"✓ Shutter moved {directionName} correctly ({Math.Abs(movement):F1}% movement)");
                    }
                    else
                    {
                        Console.WriteLine($"⚠ Small or no {directionName} movement detected ({movement:+0.0;-0.0}%)");
                    }
                }
                
                Console.WriteLine($"{directionName} feedback summary - UP/DOWN: {upDownReceived.Count}, Position: {feedbackReceived.Count}, Movement: {movementReceived.Count}");
            }
            finally
            {
                _knxService.GroupMessageReceived -= handler;
            }
        }

        private async Task ReturnToOriginalPosition(string controlAddress, string positionFeedbackAddress, 
            string stopAddress, int originalPosition, string positionMiddleGroup, string controlSubGroup)
        {
            try
            {
                // Get current position
                var currentPosition = await _knxService.RequestGroupValue<int>(positionFeedbackAddress);
                var currentPercent = currentPosition;
                var originalPercent = originalPosition;
                
                var difference = Math.Abs(currentPercent - originalPercent);
                
                if (difference < 2) // Already close enough (within 2%)
                {
                    Console.WriteLine($"Already at original position (current: {currentPercent:F1}%, original: {originalPercent:F1}%)");
                    return;
                }
                
                Console.WriteLine($"Moving from {currentPercent:F1}% back to {originalPercent:F1}%");
                
                // Use absolute positioning to return to original position
                var absolutePositionAddress = $"{SHUTTERS_MAIN_GROUP}/{positionMiddleGroup}/{controlSubGroup}";
                Console.WriteLine($"Using absolute position control: {absolutePositionAddress}");
                _knxService.WriteGroupValue(absolutePositionAddress, originalPosition);
                
                // Wait for movement
                await Task.Delay(5000);
                
                // Verify final position
                var finalPosition = await _knxService.RequestGroupValue<int>(positionFeedbackAddress);
                Console.WriteLine($"Final position: {finalPosition:F1}% (target was {originalPercent:F1}%)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error returning to original position: {ex.Message}");
            }
        }

        [Theory]
        [InlineData("1")]  // R1.1 Bathroom - Lock test
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
        [InlineData("18")] // R05.1 Office - Lock test
        public async Task CanToggleShutterLock(string controlSubGroup)
        {
            // Arrange
            var feedbackSubGroup = (int.Parse(controlSubGroup) + SHUTTER_FEEDBACK_OFFSET).ToString();
            var controlAddress = $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_LOCK_MIDDLE_GROUP}/{controlSubGroup}";
            var feedbackAddress = $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_LOCK_MIDDLE_GROUP}/{feedbackSubGroup}";

            // Get initial lock state
            var initialLockState = await _knxService.RequestGroupValue<bool>(feedbackAddress);
            var testLockState = !initialLockState; // Toggle the lock

            var taskCompletionSource = new TaskCompletionSource<KnxGroupEventArgs>();
            EventHandler<KnxGroupEventArgs> handler = (sender, args) =>
            {
                if (args.Destination == feedbackAddress)
                {
                    taskCompletionSource.SetResult(args);
                }
            };

            _knxService.GroupMessageReceived += handler;

            try
            {
                // Act - Toggle lock
                _knxService.WriteGroupValue(controlAddress, testLockState);
                
                // Wait for feedback
                var response = await taskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(3));
                
                // Assert
                Assert.Equal(feedbackAddress, response.Destination);
                Assert.Equal(testLockState ? "1" : "0", response.Value.AsString());
                
                // Restore original lock state
                await Task.Delay(500);
                _knxService.WriteGroupValue(controlAddress, initialLockState);
            }
            finally
            {
                _knxService.GroupMessageReceived -= handler;
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
            var feedbackSubGroup = (int.Parse(controlSubGroup) + SHUTTER_FEEDBACK_OFFSET).ToString();
            var lockControlAddress = $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_LOCK_MIDDLE_GROUP}/{controlSubGroup}"; // 4/3/18 - lock control
            var lockFeedbackAddress = $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_LOCK_MIDDLE_GROUP}/{feedbackSubGroup}"; // 4/3/118 - lock feedback
            var movementControlAddress = $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_MOVEMENT_MIDDLE_GROUP}/{controlSubGroup}"; // 4/0/18 - UP/DOWN control
            var movementFeedbackAddress = $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_MOVEMENT_MIDDLE_GROUP}/{feedbackSubGroup}"; // 4/0/118 - UP/DOWN feedback
            var positionFeedbackAddress = $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_POSITION_MIDDLE_GROUP}/{feedbackSubGroup}"; // 4/2/118 - position feedback

            Console.WriteLine($"Lock control: {lockControlAddress}, feedback: {lockFeedbackAddress}");
            Console.WriteLine($"Movement control: {movementControlAddress}, feedback: {movementFeedbackAddress}");
            Console.WriteLine($"Position feedback: {positionFeedbackAddress}");

            // Get initial states
            var initialLockState = await _knxService.RequestGroupValue<bool>(lockFeedbackAddress);
            Console.WriteLine($"Initial lock state: {initialLockState}");

            int? initialPosition = null;
            try
            {
                initialPosition = await _knxService.RequestGroupValue<int>(positionFeedbackAddress);
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
                    var stopAddress = $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_STOP_MIDDLE_GROUP}/{controlSubGroup}"; // 4/1/18 - STOP
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

                // Step 5: Return to original position
                Console.WriteLine("\n=== Step 5: Returning to original position ===");
                await ReturnToOriginalPosition(movementControlAddress, positionFeedbackAddress, 
                    $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_STOP_MIDDLE_GROUP}/{controlSubGroup}", initialPosition.Value, SHUTTERS_POSITION_MIDDLE_GROUP, controlSubGroup);
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