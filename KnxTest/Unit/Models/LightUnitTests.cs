using System;
using System.Threading.Tasks;
using Moq;
using Xunit;
using KnxModel;
using FluentAssertions;

namespace KnxTest.Unit.Models
{

    public class LightUnitTests : BaseKnxDeviceUnitTests
    {
        
        private readonly Light _light;

        public LightUnitTests() 
        {
            _light = new Light("L1.1", "Test Light", "1", _mockKnxService.Object);
        }

        [Fact]
        public async Task SetLockAsync_True_TurnsOffLightThenLocks()
        {
            // Arrange
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_light.Addresses.Feedback))
                          .ReturnsAsync(true); // Light starts ON

            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_light.Addresses.LockFeedback))
                          .ReturnsAsync(false); // Light starts UNLOCKED

            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.LockControl, It.IsAny<bool>()))
                          .Callback<string, bool>((addr, value) =>
                          {
                              // Simulate lock feedback if Light is ON simulate turning it OFF first
                              if (_light.CurrentState.Switch.ToBool() && value == true)
                              {
                                  var lockFeedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(false));
                                  _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockFeedbackArgs);
                              }

                              {
                                  // Simulate lock feedback
                                  var lockFeedbackArgs = new KnxGroupEventArgs(_light.Addresses.LockFeedback, new KnxValue(value));
                                  _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockFeedbackArgs);
                              }

                          });

            // Initialize the light to subscribe to events
            await _light.InitializeAsync();

            // Act
            await _light.SetLockAsync(Lock.On);

            // Assert
            // Verify final state - for business logic testing, assume lock means OFF
            _light.CurrentState.Lock.Should().Be(Lock.On);  // Light should be LOCKED
            _light.CurrentState.Switch.Should().Be(Switch.Off);  // Light should be OFF after locking

            await _light.SetLockAsync(Lock.Off);
            
        }

        [Fact]
        public async Task SetLockAsync_False_UnlocksWithoutChangingLightState()
        {
            // Arrange
            // Mock InitializeAsync() calls
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_light.Addresses.Feedback))
                          .ReturnsAsync(true); // Light starts ON
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_light.Addresses.LockFeedback))
                          .ReturnsAsync(true); // Light starts LOCKED

            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.LockControl, It.IsAny<bool>()))
                          .Callback<string, bool>((addr, value) =>
                          {
                              // Simulate lock feedback
                              var lockFeedbackArgs = new KnxGroupEventArgs(_light.Addresses.LockFeedback, new KnxValue(value));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockFeedbackArgs);
                          });

            // Initialize the light to subscribe to events
            await _light.InitializeAsync();

            // Act
            await _light.SetLockAsync(Lock.Off);

            // Assert
            // Verify final state
            _light.CurrentState.Switch.Should().Be(Switch.On);   // Light should remain ON
            _light.CurrentState.Lock.Should().Be(Lock.Off); // Light should be UNLOCKED
        }

        [Fact]
        public async Task LockAsync_LocksWithoutChangingLightState()
        {
            // Arrange
            // Mock InitializeAsync() calls
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_light.Addresses.Feedback))
                          .ReturnsAsync(true); // Light starts ON
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_light.Addresses.LockFeedback))
                          .ReturnsAsync(false); // Light starts UNLOCKED

            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.LockControl, It.IsAny<bool>()))
                          .Callback<string, bool>((addr, value) =>
                          {
                              // Simulate lock feedback
                              var lockFeedbackArgs = new KnxGroupEventArgs(_light.Addresses.LockFeedback, new KnxValue(value));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockFeedbackArgs);
                          });

            // Initialize the light to subscribe to events
            await _light.InitializeAsync();
            _light.CurrentState.Lock.Should().Be(Lock.Off);  // Light should not be LOCKED
            // Act
            await _light.LockAsync();

            // Assert
            // Verify final state
            _light.CurrentState.Switch.Should().Be(Switch.On);  // Light should remain ON
            _light.CurrentState.Lock.Should().Be(Lock.On);  // Light should be LOCKED
        }

        [Fact]
        public async Task UnlockAsync_UnlocksWithoutChangingLightState()
        {
            // Arrange
            // Mock InitializeAsync() calls
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_light.Addresses.Feedback))
                          .ReturnsAsync(false); // Light starts OFF
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_light.Addresses.LockFeedback))
                          .ReturnsAsync(true); // Light starts LOCKED

            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.LockControl, It.IsAny<bool>()))
                          .Callback<string, bool>((addr, value) =>
                          {
                              // Simulate lock feedback
                              var lockFeedbackArgs = new KnxGroupEventArgs(_light.Addresses.LockFeedback, new KnxValue(value));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockFeedbackArgs);
                          });

            // Initialize the light to subscribe to events
            await _light.InitializeAsync();

            // Act
            await _light.UnlockAsync();

            // Assert
            // Verify final state
            _light.CurrentState.Switch.Should().Be(Switch.Off);  // Light should remain OFF
            _light.CurrentState.Lock.Should().Be(Lock.Off); // Light should be UNLOCKED
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldCreateLightWithCorrectProperties()
        {
            // Assert
            _light.Id.Should().Be("L1.1");
            _light.Name.Should().Be("Test Light");
            _light.SubGroup.Should().Be("1");
            _light.CurrentState.Switch.Should().Be(Switch.Unknown);
            _light.CurrentState.Lock.Should().Be(Lock.Unknown);
        }

        #endregion

        #region State Control Tests

        [Fact]
        public async Task SetStateAsync_On_ShouldTurnOnLight()
        {
            // Arrange
            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.Control, true))
                          .Callback<string, bool>((addr, value) =>
                          {
                              // Simulate switch feedback
                              var feedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(value));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                          });

            // Act
            await _light.SetStateAsync(Switch.On);

            // Assert
            _light.CurrentState.Switch.Should().Be(Switch.On);
        }

        [Fact]
        public async Task SetStateAsync_Off_ShouldTurnOffLight()
        {
            // Arrange - start with light ON
            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.Control, true))
                          .Callback<string, bool>((addr, value) =>
                          {
                              var feedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(value));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                          });

            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.Control, false))
                          .Callback<string, bool>((addr, value) =>
                          {
                              var feedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(value));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                          });

            await _light.SetStateAsync(Switch.On); // Turn ON first

            // Act
            await _light.SetStateAsync(Switch.Off);

            // Assert
            _light.CurrentState.Switch.Should().Be(Switch.Off);
        }

        [Fact]
        public async Task TurnOnAsync_ShouldTurnOnLight()
        {
            // Arrange
            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.Control, true))
                          .Callback<string, bool>((addr, value) =>
                          {
                              var feedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(value));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                          });

            // Act
            await _light.TurnOnAsync();

            // Assert
            _light.CurrentState.Switch.Should().Be(Switch.On);
        }

        [Fact]
        public async Task TurnOffAsync_ShouldTurnOffLight()
        {
            // Arrange - start ON, then turn OFF
            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.Control, true))
                          .Callback<string, bool>((addr, value) =>
                          {
                              var feedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(value));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                          });

            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.Control, false))
                          .Callback<string, bool>((addr, value) =>
                          {
                              var feedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(value));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                          });

            await _light.TurnOnAsync(); // Turn ON first

            // Act
            await _light.TurnOffAsync();

            // Assert
            _light.CurrentState.Switch.Should().Be(Switch.Off);
        }

        [Fact]
        public async Task ToggleAsync_WhenOff_ShouldTurnOn()
        {
            // Arrange - setup for any WriteGroupValue call
            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.Control, It.IsAny<bool>()))
                          .Callback<string, bool>((addr, value) =>
                          {
                              var feedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(value));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                          });

            // First set state to OFF, then toggle to ON
            await _light.TurnOffAsync();

            // Act
            await _light.ToggleAsync();

            // Assert
            _light.CurrentState.Switch.Should().Be(Switch.On);
        }

        [Fact]
        public async Task ToggleAsync_WhenOn_ShouldTurnOff()
        {
            // Arrange - setup for any WriteGroupValue call
            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.Control, It.IsAny<bool>()))
                          .Callback<string, bool>((addr, value) =>
                          {
                              var feedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(value));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
                          });

            // First set state to ON, then toggle to OFF
            await _light.TurnOnAsync();

            // Act
            await _light.ToggleAsync();

            // Assert
            _light.CurrentState.Switch.Should().Be(Switch.Off);
        }

        #endregion

        #region State Reading Tests

        [Fact]
        public async Task ReadStateAsync_ShouldReturnCurrentState()
        {
            // Arrange
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_light.Addresses.Feedback))
                          .ReturnsAsync(true);

            // Act
            var result = await _light.ReadStateAsync();

            // Assert
            result.Should().Be(Switch.On);
        }

        [Fact]
        public async Task ReadLockStateAsync_ShouldReturnCurrentLockState()
        {
            // Arrange
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_light.Addresses.LockFeedback))
                          .ReturnsAsync(true);

            // Act
            var result = await _light.ReadLockStateAsync();

            // Assert
            result.Should().Be(Lock.On);
        }

        #endregion

        #region Initialization Tests

        [Fact]
        public async Task InitializeAsync_ShouldReadCurrentStateFromKnx()
        {
            // Arrange
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_light.Addresses.Feedback))
                          .ReturnsAsync(true);
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_light.Addresses.LockFeedback))
                          .ReturnsAsync(false);

            // Act
            await _light.InitializeAsync();

            // Assert
            _light.CurrentState.Switch.Should().Be(Switch.On);
            _light.CurrentState.Lock.Should().Be(Lock.Off);
        }

        [Fact]
        public async Task RefreshStateAsync_ShouldUpdateCurrentState()
        {
            // Arrange
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_light.Addresses.Feedback))
                          .ReturnsAsync(false);
            _mockKnxService.Setup(x => x.RequestGroupValue<bool>(_light.Addresses.LockFeedback))
                          .ReturnsAsync(true);

            // Act
            await _light.RefreshStateAsync();

            // Assert
            _light.CurrentState.Switch.Should().Be(Switch.Off);
            _light.CurrentState.Lock.Should().Be(Lock.On);
        }

        #endregion

        #region Wait Methods Tests

        [Fact]
        public async Task WaitForStateAsync_WhenStateMatches_ShouldReturnTrue()
        {
            // Arrange - set current state to ON
            var feedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(true));
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);

            // Act
            var result = await _light.WaitForStateAsync(Switch.On, TimeSpan.FromMilliseconds(100));

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task WaitForLockStateAsync_WhenLockStateMatches_ShouldReturnTrue()
        {
            // Arrange - set current lock state to ON
            var lockFeedbackArgs = new KnxGroupEventArgs(_light.Addresses.LockFeedback, new KnxValue(true));
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockFeedbackArgs);

            // Act
            var result = await _light.WaitForLockStateAsync(Lock.On, TimeSpan.FromMilliseconds(100));

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region Save/Restore Tests

        [Fact]
        public void SaveCurrentState_ShouldStoreCurrent()
        {
            // Arrange - set a specific state
            var feedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(true));
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);

            // Act
            _light.SaveCurrentState();

            // Assert
            _light.SavedState.Should().NotBeNull();
            _light.SavedState!.Switch.Should().Be(Switch.On);
        }

        [Fact]
        public async Task RestoreSavedStateAsync_ShouldRestorePreviousState()
        {
            // Arrange - set initial state and save it
            var feedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(true));
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
            
            _light.SaveCurrentState();

            // Change state
            var newFeedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(false));
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, newFeedbackArgs);

            // Setup mock for restoration
            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.Control, true))
                          .Callback<string, bool>((addr, value) =>
                          {
                              var restoreFeedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(value));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, restoreFeedbackArgs);
                          });

            // Act
            await _light.RestoreSavedStateAsync();

            // Assert
            _light.CurrentState.Switch.Should().Be(Switch.On);
        }

        [Fact]
        public async Task RestoreSavedStateAsync_WhenLocked_ShouldTemporarilyUnlockAndRestore()
        {
            // Arrange - set initial state: light on, unlocked
            var feedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(true));
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, feedbackArgs);
            
            _light.SaveCurrentState(); // Save state: On, unlocked

            // Change to off and locked
            var offFeedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(false));
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, offFeedbackArgs);
            
            var lockArgs = new KnxGroupEventArgs(_light.Addresses.LockFeedback, new KnxValue(true)); // locked
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockArgs);

            // Verify current state is locked and off
            _light.CurrentState.Switch.Should().Be(Switch.Off);
            _light.CurrentState.Lock.Should().Be(Lock.On);

            // Setup mocks for restoration process
            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.LockControl, false)) // unlock
                          .Callback<string, bool>((addr, value) =>
                          {
                              var unlockFeedback = new KnxGroupEventArgs(_light.Addresses.LockFeedback, new KnxValue(false));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, unlockFeedback);
                          });

            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.Control, true)) // restore switch
                          .Callback<string, bool>((addr, value) =>
                          {
                              var restoreFeedbackArgs = new KnxGroupEventArgs(_light.Addresses.Feedback, new KnxValue(value));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, restoreFeedbackArgs);
                          });

            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.LockControl, false)) // restore lock (Off)
                          .Callback<string, bool>((addr, value) =>
                          {
                              var lockFeedback = new KnxGroupEventArgs(_light.Addresses.LockFeedback, new KnxValue(value));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockFeedback);
                          });

            // Act
            await _light.RestoreSavedStateAsync();

            // Assert
            _light.CurrentState.Switch.Should().Be(Switch.On); // switch restored
            _light.CurrentState.Lock.Should().Be(Lock.Off); // lock restored

            // Verify that unlock was called before switch restoration
            _mockKnxService.Verify(s => s.WriteGroupValue(_light.Addresses.LockControl, false), Times.AtLeastOnce());
            _mockKnxService.Verify(s => s.WriteGroupValue(_light.Addresses.Control, true), Times.Once());
        }

        [Fact]
        public async Task RestoreSavedStateAsync_ShouldRestoreLockState()
        {
            // Arrange - set initial state: light off, locked
            var lockArgs = new KnxGroupEventArgs(_light.Addresses.LockFeedback, new KnxValue(true)); // locked
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockArgs);

            _light.SaveCurrentState(); // Save state: locked

            // Change to unlocked
            var unlockArgs = new KnxGroupEventArgs(_light.Addresses.LockFeedback, new KnxValue(false)); // unlocked
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, unlockArgs);

            _light.CurrentState.Lock.Should().Be(Lock.Off); // now unlocked

            // Setup mock for lock restoration
            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.LockControl, true)) // restore lock (On)
                          .Callback<string, bool>((addr, value) =>
                          {
                              var lockFeedback = new KnxGroupEventArgs(_light.Addresses.LockFeedback, new KnxValue(value));
                              _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, lockFeedback);
                          });

            // Act
            await _light.RestoreSavedStateAsync();

            // Assert
            _light.CurrentState.Lock.Should().Be(Lock.On); // lock restored
            _mockKnxService.Verify(s => s.WriteGroupValue(_light.Addresses.LockControl, true), Times.Once());
        }

        #endregion

    }
}
