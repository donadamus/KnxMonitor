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
                                  _light.GetType().GetMethod("ProcessKnxMessage",
                                      System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                                      .Invoke(_light, new object[] { lockFeedbackArgs });
                              }

                              {
                                  // Simulate lock feedback
                                  var lockFeedbackArgs = new KnxGroupEventArgs(_light.Addresses.LockFeedback, new KnxValue(value));
                                  _light.GetType().GetMethod("ProcessKnxMessage",
                                      System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                                      .Invoke(_light, new object[] { lockFeedbackArgs });
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

            // Setup light to be ON and LOCKED initially
            //_light.GetType().GetProperty("CurrentState")?.SetValue(_light, 
            //    new LightState(IsOn: true, Lock: Lock.On, LastUpdated: DateTime.Now));

            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.LockControl, It.IsAny<bool>()))
                          .Callback<string, bool>((addr, value) =>
                          {
                              // Simulate lock feedback
                              var lockFeedbackArgs = new KnxGroupEventArgs(_light.Addresses.LockFeedback, new KnxValue(value));
                              _light.GetType().GetMethod("ProcessKnxMessage", 
                                  System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                                  .Invoke(_light, new object[] { lockFeedbackArgs });
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
                              _light.GetType().GetMethod("ProcessKnxMessage",
                                  System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                                  .Invoke(_light, new object[] { lockFeedbackArgs });
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

            // Setup light to be OFF and LOCKED initially
            //_light.GetType().GetProperty("CurrentState")?.SetValue(_light, 
            //    new LightState(Switch: false, Lock: Lock.On, LastUpdated: DateTime.Now));

            _mockKnxService.Setup(x => x.WriteGroupValue(_light.Addresses.LockControl, It.IsAny<bool>()))
                          .Callback<string, bool>((addr, value) =>
                          {
                              // Simulate lock feedback
                              var lockFeedbackArgs = new KnxGroupEventArgs(_light.Addresses.LockFeedback, new KnxValue(value));
                              _light.GetType().GetMethod("ProcessKnxMessage",
                                  System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                                  .Invoke(_light, new object[] { lockFeedbackArgs });
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

    }
}
