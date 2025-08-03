using FluentAssertions;
using KnxModel;
using KnxService;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Unit.Models
{
    public class ShutterUnitTests : BaseKnxDeviceUnitTests
    {
        private readonly IShutter _shutter;

        public ShutterUnitTests()
        {
            _shutter = new Shutter("R1.1", "Test Bathroom", "1", _mockKnxService.Object);
        }

        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Assert
            _shutter.Id.Should().Be("R1.1");
            _shutter.Name.Should().Be("Test Bathroom");
            _shutter.SubGroup.Should().Be("1");
            
            _shutter.Addresses.MovementControl.Should().Be("4/0/1");
            _shutter.Addresses.MovementFeedback.Should().Be("4/0/101");
            _shutter.Addresses.PositionControl.Should().Be("4/2/1");
            _shutter.Addresses.PositionFeedback.Should().Be("4/2/101");
            _shutter.Addresses.LockControl.Should().Be("4/3/1");
            _shutter.Addresses.LockFeedback.Should().Be("4/3/101");
            _shutter.Addresses.StopControl.Should().Be("4/1/1");
            _shutter.Addresses.MovementStatusFeedback.Should().Be("4/1/101");
        }

        [Fact]
        public void Constructor_ThrowsOnNullParameters()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => new Shutter(null!, "name", "1", _mockKnxService.Object));
            Assert.Throws<ArgumentNullException>(() => new Shutter("id", null!, "1", _mockKnxService.Object));
            Assert.Throws<ArgumentNullException>(() => new Shutter("id", "name", null!, _mockKnxService.Object));
            Assert.Throws<ArgumentNullException>(() => new Shutter("id", "name", "1", null!));
        }

        [Fact]
        public async Task InitializeAsync_ReadsAllStatesFromKnx()
        {
            // Arrange
            var expectedPosition = 45.0f;
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_shutter.Addresses.PositionFeedback))
                          .ReturnsAsync(expectedPosition);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.LockFeedback))
                          .ReturnsAsync(true); // locked
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.MovementStatusFeedback))
                          .ReturnsAsync(false); // stopped

            // Act
            await _shutter.InitializeAsync();

            // Assert
            _shutter.CurrentState.Position.Should().Be(expectedPosition);
            _shutter.CurrentState.Lock.Should().Be(Lock.On);
            _shutter.CurrentState.MovementState.Should().Be(ShutterMovementState.Inactive);
            _shutter.CurrentState.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task SaveCurrentState_SavesCurrentState()
        {
            // Arrange
            var expectedPosition = 75.0f;
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_shutter.Addresses.PositionFeedback))
                          .ReturnsAsync(expectedPosition);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.LockFeedback))
                          .ReturnsAsync(false); // unlocked
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.MovementStatusFeedback))
                          .ReturnsAsync(false); // stopped

            // Act
            await _shutter.InitializeAsync(); // Ensure current state is set
            _shutter.SaveCurrentState();

            // Assert
            _shutter.SavedState.Should().NotBeNull();
            _shutter.SavedState!.Position.Should().Be(expectedPosition);
            _shutter.SavedState.Lock.Should().Be(Lock.Off);
            _shutter.SavedState.MovementState.Should().Be(ShutterMovementState.Inactive);
        }

        [Fact]
        public async Task RestoreSavedStateAsync_ThrowsWhenNoSavedState()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _shutter.RestoreSavedStateAsync());
        }

        [Fact]
        public async Task RestoreSavedStateAsync_ShouldRestorePosition()
        {
            // Arrange - set up initial state: shutter at 75%, unlocked
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_shutter.Addresses.PositionFeedback)).ReturnsAsync(75f);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.LockFeedback)).ReturnsAsync(false);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.MovementStatusFeedback)).ReturnsAsync(false);

            await _shutter.InitializeAsync();
            _shutter.SaveCurrentState(); // Save state: 75%, unlocked

            // Change position to 25%
            var positionArgs = new KnxGroupEventArgs(_shutter.Addresses.PositionFeedback, new KnxValue(25f));
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, positionArgs);

            _shutter.CurrentState.Position.Should().Be(25f); // verify change

            // Setup mock for position restoration
            _mockKnxService.Setup(s => s.WriteGroupValue(_shutter.Addresses.PositionControl, 75f))
                .Callback<string, float>((addr, value) =>
                {
                    var positionFeedback = new KnxGroupEventArgs(_shutter.Addresses.PositionFeedback, new KnxValue(value));
                    _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, positionFeedback);
                });

            // Act
            await _shutter.RestoreSavedStateAsync();

            // Assert
            _shutter.CurrentState.Position.Should().Be(75f); // position restored
            _mockKnxService.Verify(s => s.WriteGroupValue(_shutter.Addresses.PositionControl, 75f), Times.Once());
        }

        [Fact]
        public async Task RestoreSavedStateAsync_WhenLocked_ShouldTemporarilyUnlockAndRestore()
        {
            // Arrange - set up initial state: shutter at 80%, unlocked
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_shutter.Addresses.PositionFeedback)).ReturnsAsync(80f);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.LockFeedback)).ReturnsAsync(false);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.MovementStatusFeedback)).ReturnsAsync(false);

            await _shutter.InitializeAsync();
            _shutter.SaveCurrentState(); // Save state: 80%, unlocked

            // Change position to 40% and lock the shutter
            var positionArgs = new KnxGroupEventArgs(_shutter.Addresses.PositionFeedback, new KnxValue(40f));
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, positionArgs);
            
            var lockArgs = new KnxGroupEventArgs(_shutter.Addresses.LockFeedback, new KnxValue(true)); // locked
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockArgs);

            // Verify current state is locked at 40%
            _shutter.CurrentState.Position.Should().Be(40f);
            _shutter.CurrentState.Lock.Should().Be(Lock.On);

            // Setup mocks for restoration process
            _mockKnxService.Setup(s => s.WriteGroupValue(_shutter.Addresses.LockControl, false)) // unlock
                .Callback<string, bool>((addr, value) =>
                {
                    var unlockFeedback = new KnxGroupEventArgs(_shutter.Addresses.LockFeedback, new KnxValue(false));
                    _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, unlockFeedback);
                });

            _mockKnxService.Setup(s => s.WriteGroupValue(_shutter.Addresses.PositionControl, 80f)) // restore position
                .Callback<string, float>((addr, value) =>
                {
                    var positionFeedback = new KnxGroupEventArgs(_shutter.Addresses.PositionFeedback, new KnxValue(value));
                    _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, positionFeedback);
                });

            _mockKnxService.Setup(s => s.WriteGroupValue(_shutter.Addresses.LockControl, false)) // restore lock (Off)
                .Callback<string, bool>((addr, value) =>
                {
                    var lockFeedback = new KnxGroupEventArgs(_shutter.Addresses.LockFeedback, new KnxValue(value));
                    _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockFeedback);
                });

            // Act
            await _shutter.RestoreSavedStateAsync();

            // Assert
            _shutter.CurrentState.Position.Should().Be(80f); // position restored
            _shutter.CurrentState.Lock.Should().Be(Lock.Off); // lock restored

            // Verify that unlock was called before position restoration
            _mockKnxService.Verify(s => s.WriteGroupValue(_shutter.Addresses.LockControl, false), Times.AtLeastOnce());
            _mockKnxService.Verify(s => s.WriteGroupValue(_shutter.Addresses.PositionControl, 80f), Times.Once());
        }

        [Fact]
        public async Task RestoreSavedStateAsync_ShouldRestoreLockState()
        {
            // Arrange - set up initial state: shutter at 50%, locked
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_shutter.Addresses.PositionFeedback)).ReturnsAsync(50f);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.LockFeedback)).ReturnsAsync(true); // locked
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.MovementStatusFeedback)).ReturnsAsync(false);

            await _shutter.InitializeAsync();
            _shutter.SaveCurrentState(); // Save state: 50%, locked

            // Change to unlocked state
            var lockArgs = new KnxGroupEventArgs(_shutter.Addresses.LockFeedback, new KnxValue(false)); // unlocked
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockArgs);

            _shutter.CurrentState.Lock.Should().Be(Lock.Off); // now unlocked

            // Setup mock for lock restoration
            _mockKnxService.Setup(s => s.WriteGroupValue(_shutter.Addresses.LockControl, true)) // restore lock (On)
                .Callback<string, bool>((addr, value) =>
                {
                    var lockFeedback = new KnxGroupEventArgs(_shutter.Addresses.LockFeedback, new KnxValue(value));
                    _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockFeedback);
                });

            // Act
            await _shutter.RestoreSavedStateAsync();

            // Assert
            _shutter.CurrentState.Lock.Should().Be(Lock.On); // lock restored
            _mockKnxService.Verify(s => s.WriteGroupValue(_shutter.Addresses.LockControl, true), Times.Once());
        }

        [Fact]
        public async Task SetPositionAsync_CallsKnxServiceWithCorrectAddress()
        {
            // Arrange
            var targetPosition = 60.0f;
            
            // Setup for RefreshCurrentStateAsync calls
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_shutter.Addresses.PositionFeedback))
                          .ReturnsAsync(targetPosition);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.LockFeedback))
                          .ReturnsAsync(false);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.MovementStatusFeedback))
                          .ReturnsAsync(false);

            // Initialize shutter to start listening to feedback events
            await _shutter.InitializeAsync();

            // Setup WriteGroupValue to trigger feedback event when position control is written
            _mockKnxService.Setup(s => s.WriteGroupValue(_shutter.Addresses.PositionControl, It.IsAny<float>()))
                          .Callback<string, float>((address, positionValue) =>
                          {
                              // Simulate KNX feedback response with the same value that was written
                              // Convert float to double for KnxValue since AsPercentageValue() supports double but not float
                              var feedbackArgs = new KnxGroupEventArgs(_shutter.Addresses.PositionFeedback, new KnxValue((double)positionValue));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                          });

            // Act
            await _shutter.SetPositionAsync(targetPosition);

            // Assert
        }

        [Fact]
        public async Task MoveAsync_CallsKnxServiceWithCorrectValues()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_shutter.Addresses.PositionFeedback))
                          .ReturnsAsync(50.0f);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.LockFeedback))
                          .ReturnsAsync(false);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.MovementStatusFeedback))
                          .ReturnsAsync(false);

            // Initialize shutter to start listening to feedback events
            await _shutter.InitializeAsync();

            // Setup WriteGroupValue to trigger feedback event when movement control is written
            _mockKnxService.Setup(s => s.WriteGroupValue(_shutter.Addresses.MovementControl, It.IsAny<bool>()))
                          .Callback<string, bool>((address, movementValue) =>
                          {
                              // Simulate movement start feedback, then stop after short delay
                              var startArgs = new KnxGroupEventArgs(_shutter.Addresses.MovementStatusFeedback, new KnxValue(true));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, startArgs);
                              
                              // Simulate automatic stop after movement
                              Task.Run(async () =>
                              {
                                  await Task.Delay(10); // Short delay to simulate movement
                                  var stopArgs = new KnxGroupEventArgs(_shutter.Addresses.MovementStatusFeedback, new KnxValue(false));
                                  _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, stopArgs);
                              });
                          });

            // Act - Test UP direction
            await _shutter.MoveAsync(ShutterDirection.Up);

            // Assert

            // Act - Test DOWN direction
            await _shutter.MoveAsync(ShutterDirection.Down);

            // Assert
        }

        [Fact]
        public async Task MoveAsync_WithDuration_CallsStopAfterDelay()
        {
            // Arrange
            var startingPosition = 50.0f;
            var endingPosition = 60.0f;

            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_shutter.Addresses.PositionFeedback))
                          .ReturnsAsync(startingPosition);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.LockFeedback))
                          .ReturnsAsync(false);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.MovementStatusFeedback))
                          .ReturnsAsync(false);
            _mockKnxService.Setup(s => s.WriteGroupValue(_shutter.Addresses.MovementControl, It.IsAny<bool>()))
                .Callback<string, bool>((address, movementValue) =>
                {
                    // Simulate movement start feedback
                    {
                        var startArgs = new KnxGroupEventArgs(_shutter.Addresses.MovementStatusFeedback, new KnxValue(true));
                        _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, startArgs);
                    }

                    {
                        var startArgs = new KnxGroupEventArgs(_shutter.Addresses.MovementFeedback, new KnxValue(movementValue));
                        _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, startArgs);
                    }

                    {
                        var startArgs = new KnxGroupEventArgs(_shutter.Addresses.PositionFeedback, new KnxValue(endingPosition));
                        _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, startArgs);
                    }


                });
            _mockKnxService.Setup(s => s.WriteGroupValue(_shutter.Addresses.StopControl, It.IsAny<bool>()))
                .Callback<string, bool>((address, movementValue) =>
                {
                    // Simulate movement stop
                    {
                        var startArgs = new KnxGroupEventArgs(_shutter.Addresses.MovementStatusFeedback, new KnxValue(false));
                        _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, startArgs);
                    }
                });

            // Initialize shutter to start listening to feedback events
            await _shutter.InitializeAsync();

            //assert that the shutter is not moving before the test
            _shutter.CurrentState.MovementState.Should().Be(ShutterMovementState.Inactive);
            _shutter.CurrentState.Position.Should().Be(startingPosition);

            // Act
            await _shutter.MoveAsync(ShutterDirection.Up, TimeSpan.FromMilliseconds(100));

            // Assert that the shutter is stopped after the move
            _shutter.CurrentState.MovementState.Should().Be(ShutterMovementState.Inactive);
            _shutter.CurrentState.Position.Should().Be(endingPosition);

            // Assert
        }

        [Fact]
        public async Task StopAsync_CallsKnxServiceWithStopCommand()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_shutter.Addresses.StopControl, true))
                          .Callback<string, bool>((address, stopValue) =>
                          {
                              // Simulate KNX feedback response for stop command
                              var feedbackArgs = new KnxGroupEventArgs(_shutter.Addresses.MovementStatusFeedback, new KnxValue(false));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                          });

            // Act
            await _shutter.StopAsync(TimeSpan.Zero);

        }

        [Fact]
        public async Task SetLockAsync_CallsKnxServiceWithCorrectValues()
        {
            // Initialize shutter to start listening to feedback events
            await _shutter.InitializeAsync();

            // Setup WriteGroupValue to trigger feedback event when lock control is written
            _mockKnxService.Setup(s => s.WriteGroupValue(_shutter.Addresses.LockControl, It.IsAny<bool>()))
                          .Callback<string, bool>((address, lockValue) =>
                          {
                              // Simulate KNX feedback response with the same value that was written
                              var feedbackArgs = new KnxGroupEventArgs(_shutter.Addresses.LockFeedback, new KnxValue(lockValue));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                          });

            // Act - Lock
            await _shutter.SetLockAsync(Lock.On);

            // Assert

            // Act - Unlock
            await _shutter.SetLockAsync(Lock.Off);

            // Assert
        }

        [Theory]
        [InlineData(false, ShutterMovementState.Inactive)]    // Inactive = Stopped
        [InlineData(true, ShutterMovementState.Active)]   // Active = Moving (can't distinguish direction)
        public async Task ReadMovementStateAsync_ReturnsCorrectState(bool knxValue, ShutterMovementState expectedState)
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.MovementStatusFeedback))
                          .ReturnsAsync(knxValue);

            // Act
            var result = await _shutter.ReadMovementStateAsync();

            // Assert
            result.Should().Be(expectedState);
        }

        [Fact]
        public async Task ReadLockStateAsync_ReturnsCorrectBooleanValue()
        {
            // Arrange & Act & Assert - Locked
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.LockFeedback))
                          .ReturnsAsync(true);
            var lockedResult = await _shutter.ReadLockStateAsync();
            lockedResult.Should().Be(Lock.On);

            // Arrange & Act & Assert - Unlocked
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.LockFeedback))
                          .ReturnsAsync(false);
            var unlockedResult = await _shutter.ReadLockStateAsync();
            unlockedResult.Should().Be(Lock.Off);
        }

        [Fact]
        public async Task WaitForPositionAsync_ReturnsTrueWhenPositionReached()
        {
            // Arrange
            var targetPosition = 50.0f;
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_shutter.Addresses.PositionFeedback))
                          .ReturnsAsync(targetPosition);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.LockFeedback))
                          .ReturnsAsync(false);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.MovementStatusFeedback))
                          .ReturnsAsync(false);

            // Initialize the shutter state to match target position
            await _shutter.InitializeAsync();

            // Act
            var result = await _shutter.WaitForPositionAsync(targetPosition, tolerance: 1.0, TimeSpan.FromSeconds(1));

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task WaitForMovementStopAsync_ReturnsTrueWhenStopped()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<float>(_shutter.Addresses.PositionFeedback))
                          .ReturnsAsync(50.0f);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.LockFeedback))
                          .ReturnsAsync(false);
            _mockKnxService.Setup(s => s.RequestGroupValue<bool>(_shutter.Addresses.MovementStatusFeedback))
                          .ReturnsAsync(false); // stopped

            // Initialize the shutter state to stopped
            await _shutter.InitializeAsync();

            // Act
            var result = await _shutter.WaitForMovementStopAsync(TimeSpan.FromSeconds(1));

            // Assert
            result.Should().BeTrue();
        }

        #region Additional Tests

        [Fact]
        public async Task ReadPositionAsync_ShouldReturnCorrectValue()
        {
            // Arrange
            _mockKnxService.Setup(x => x.RequestGroupValue<float>(_shutter.Addresses.PositionFeedback))
                          .ReturnsAsync(75);

            // Act
            var result = await _shutter.ReadPositionAsync();

            // Assert
            result.Should().Be(75);
        }

        [Fact]
        public async Task LockAsync_ShouldSendCorrectCommand()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_shutter.Addresses.LockControl, true));

            // Act
            await _shutter.LockAsync(TimeSpan.Zero);

            // Assert - verification handled by VerifyAll in base class
        }

        [Fact]
        public async Task UnlockAsync_ShouldSendCorrectCommand()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_shutter.Addresses.LockControl, false));

            // Act
            await _shutter.UnlockAsync(TimeSpan.Zero);

            // Assert - verification handled by VerifyAll in base class
        }

        [Fact]
        public async Task OpenAsync_ShouldSetPositionToZero()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_shutter.Addresses.PositionControl, 0.0f));

            // Act
            await _shutter.OpenAsync();

            // Assert - verification handled by VerifyAll in base class
        }

        [Fact]
        public async Task CloseAsync_ShouldSetPositionToHundred()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_shutter.Addresses.PositionControl, 100.0f))
                .Callback<string, float>((address, positionValue) =>
                            {
                                // Simulate KNX feedback response with the same value that was written
                                var feedbackArgs = new KnxGroupEventArgs(_shutter.Addresses.PositionFeedback, new KnxValue((double)positionValue));
                                _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                            });

            // Act
            await _shutter.CloseAsync();
            _shutter.CurrentState.Position.Should().Be(100.0f);
            // Assert - verification handled by VerifyAll in base class
        }

        [Fact]
        public async Task MoveToPresetAsync_ShouldSetCorrectPosition()
        {
            // Arrange
            _mockKnxService.Setup(s => s.WriteGroupValue(_shutter.Addresses.PositionControl, 25.0f))
                .Callback<string, float>((address, positionValue) =>
                            {
                                // Simulate KNX feedback response with the same value that was written
                                var feedbackArgs = new KnxGroupEventArgs(_shutter.Addresses.PositionFeedback, new KnxValue((double)positionValue));
                                _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                            });

            // Act
            await _shutter.MoveToPresetAsync("Quarter Open", 25.0f);
            _shutter.CurrentState.Position.Should().Be(25.0f);
            // Assert - verification handled by VerifyAll in base class
        }

        #endregion

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            // Act
            var result = _shutter.ToString();

            // Assert
            result.Should().Contain("R1.1");
            result.Should().Contain("Test Bathroom");
            result.Should().Contain("Position:");
            result.Should().Contain("Locked:");
            result.Should().Contain("Movement:");
        }
    }
}
