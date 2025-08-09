using System;
using System.Threading.Tasks;
using FluentAssertions;
using KnxModel;
using Moq;
using Xunit;

namespace KnxTest.Unit.Models
{
    /// <summary>
    /// Comprehensive unit tests for ShutterDevice implementation
    /// Tests all interface functionality separately for clean separation of concerns
    /// </summary>
    public class ShutterDeviceTestsOld
    {
        private readonly Mock<IKnxService> _mockKnxService;
        private readonly ShutterDevice _shutterDevice;

        public ShutterDeviceTestsOld()
        {
            _mockKnxService = new Mock<IKnxService>();
            _shutterDevice = new ShutterDevice("shutter_001", "Living Room Shutter", "1", _mockKnxService.Object, null);
        }

        #region IKnxDeviceBase Tests

        [Fact]
        public void Constructor_ValidParameters_SetsProperties()
        {
            // Assert
            _shutterDevice.Id.Should().Be("shutter_001");
            _shutterDevice.Name.Should().Be("Living Room Shutter");
            _shutterDevice.SubGroup.Should().Be("1");
        }

        [Fact]
        public void Constructor_NullId_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new ShutterDevice(null!, "Test", "1", _mockKnxService.Object, null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task InitializeAsync_UpdatesLastUpdated()
        {
            // Act
            await _shutterDevice.InitializeAsync();
            
            // Assert
            _shutterDevice.LastUpdated.Should().BeAfter(DateTime.MinValue);
            _shutterDevice.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task SaveAndRestoreState_PreservesPosition()
        {
            // Arrange
            await _shutterDevice.InitializeAsync();
            await _shutterDevice.SetPercentageAsync(75.0f);
            
            // Act - Save state
            _shutterDevice.SaveCurrentState();
            
            // Change state
            await _shutterDevice.SetPercentageAsync(25.0f);
            _shutterDevice.CurrentPercentage.Should().Be(25.0f);
            
            // Restore state
            await _shutterDevice.RestoreSavedStateAsync();
            
            // Assert
            _shutterDevice.CurrentPercentage.Should().Be(75.0f);
        }

        #endregion

        #region IPercentageControllable Tests

        [Fact]
        public async Task SetPercentageAsync_ValidValue_UpdatesCurrentPercentage()
        {
            // Arrange
            const float expectedPercentage = 45.5f;
            
            // Act
            await _shutterDevice.SetPercentageAsync(expectedPercentage);
            
            // Assert
            _shutterDevice.CurrentPercentage.Should().Be(expectedPercentage);
        }

        [Fact]
        public async Task SetPercentageAsync_NegativeValue_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Func<Task> act = async () => await _shutterDevice.SetPercentageAsync(-5.0f);
            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Fact]
        public async Task SetPercentageAsync_ValueOver100_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Func<Task> act = async () => await _shutterDevice.SetPercentageAsync(105.0f);
            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Fact]
        public async Task ReadPercentageAsync_ReturnsCurrentPercentage()
        {
            // Arrange
            await _shutterDevice.SetPercentageAsync(30.0f);
            
            // Act
            var result = await _shutterDevice.ReadPercentageAsync();
            
            // Assert
            result.Should().Be(30.0f);
        }

        [Fact]
        public async Task AdjustPercentageAsync_PositiveDelta_IncreasesPercentage()
        {
            // Arrange
            await _shutterDevice.SetPercentageAsync(50.0f);
            
            // Act
            await _shutterDevice.AdjustPercentageAsync(20.0f);
            
            // Assert
            _shutterDevice.CurrentPercentage.Should().Be(70.0f);
        }

        [Fact]
        public async Task AdjustPercentageAsync_NegativeDelta_DecreasesPercentage()
        {
            // Arrange
            await _shutterDevice.SetPercentageAsync(60.0f);
            
            // Act
            await _shutterDevice.AdjustPercentageAsync(-25.0f);
            
            // Assert
            _shutterDevice.CurrentPercentage.Should().Be(35.0f);
        }

        [Fact]
        public async Task AdjustPercentageAsync_DeltaExceedsMax_ClampsTo100()
        {
            // Arrange
            await _shutterDevice.SetPercentageAsync(90.0f);
            
            // Act
            await _shutterDevice.AdjustPercentageAsync(20.0f);
            
            // Assert
            _shutterDevice.CurrentPercentage.Should().Be(100.0f);
        }

        [Fact]
        public async Task AdjustPercentageAsync_DeltaExceedsMin_ClampsTo0()
        {
            // Arrange
            await _shutterDevice.SetPercentageAsync(15.0f);
            
            // Act
            await _shutterDevice.AdjustPercentageAsync(-25.0f);
            
            // Assert
            _shutterDevice.CurrentPercentage.Should().Be(0.0f);
        }

        [Fact]
        public async Task WaitForPercentageAsync_TargetReached_ReturnsTrue()
        {
            // Arrange
            await _shutterDevice.SetPercentageAsync(50.0f);
            
            // Act
            var result = await _shutterDevice.WaitForPercentageAsync(50.0f, 2.0, TimeSpan.FromSeconds(1));
            
            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task WaitForPercentageAsync_InvalidTarget_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Func<Task> act = async () => await _shutterDevice.WaitForPercentageAsync(-10.0f, 2.0, TimeSpan.FromSeconds(1));
            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        #endregion

        #region ILockableDevice Tests

        [Fact]
        public async Task LockAsync_UpdatesCurrentLockState()
        {
            // Act
            await _shutterDevice.LockAsync();
            
            // Assert
            _shutterDevice.CurrentLockState.Should().Be(Lock.On);
        }

        [Fact]
        public async Task UnlockAsync_UpdatesCurrentLockState()
        {
            // Arrange
            await _shutterDevice.LockAsync();
            
            // Act
            await _shutterDevice.UnlockAsync();
            
            // Assert
            _shutterDevice.CurrentLockState.Should().Be(Lock.Off);
        }

        [Fact]
        public async Task ReadLockStateAsync_ReturnsCurrentLockState()
        {
            // Arrange
            await _shutterDevice.LockAsync();
            
            // Act
            var result = await _shutterDevice.ReadLockStateAsync();
            
            // Assert
            result.Should().Be(Lock.On);
        }

        [Fact]
        public async Task WaitForLockStateAsync_TargetReached_ReturnsTrue()
        {
            // Arrange
            await _shutterDevice.LockAsync();
            
            // Act
            var result = await _shutterDevice.WaitForLockStateAsync(Lock.On, TimeSpan.FromSeconds(1));
            
            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task SaveAndRestoreState_PreservesLockState()
        {
            // Arrange
            await _shutterDevice.InitializeAsync();
            await _shutterDevice.LockAsync();
            
            // Act - Save state
            _shutterDevice.SaveCurrentState();
            
            // Change state
            await _shutterDevice.UnlockAsync();
            _shutterDevice.CurrentLockState.Should().Be(Lock.Off);
            
            // Restore state
            await _shutterDevice.RestoreSavedStateAsync();
            
            // Assert
            _shutterDevice.CurrentLockState.Should().Be(Lock.On);
        }

        #endregion

        #region IActivityStatusReadable Tests

        [Fact]
        public async Task ReadActivityStatusAsync_ReturnsCurrentActivityState()
        {
            // Act
            var result = await _shutterDevice.ReadActivityStatusAsync();
            
            // Assert
            result.Should().Be(_shutterDevice.IsActive);
        }

        [Fact]
        public void IsActive_InitiallyFalse()
        {
            // Assert
            _shutterDevice.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task SetPercentageAsync_SimulatesMovement()
        {
            // This test verifies that movement simulation works
            // In real implementation, IsActive would be read from KNX bus
            
            // Act - Start movement (this should simulate movement in our implementation)
            var movementTask = _shutterDevice.SetPercentageAsync(50.0f);
            
            // Small delay to let movement start (in real implementation, we'd read from KNX)
            await Task.Delay(10);
            
            // Complete the movement
            await movementTask;
            
            // Assert - Movement should be completed
            _shutterDevice.CurrentPercentage.Should().Be(50.0f);
        }

        [Fact]
        public async Task StopAsync_UpdatesActivityStatus()
        {
            // Act
            await _shutterDevice.StopAsync();
            
            // Assert - In our simulation, stop sets IsActive to false
            _shutterDevice.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task WaitForInactiveAsync_WhenAlreadyInactive_ReturnsTrue()
        {
            // Arrange - ensure device is inactive
            await _shutterDevice.StopAsync();
            
            // Act
            var result = await _shutterDevice.WaitForInactiveAsync(TimeSpan.FromSeconds(1));
            
            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task WaitForActiveAsync_Timeout_ReturnsFalse()
        {
            // Arrange - ensure device is inactive and will stay inactive
            await _shutterDevice.StopAsync();
            
            // Act - wait for active with very short timeout
            var result = await _shutterDevice.WaitForActiveAsync(TimeSpan.FromMilliseconds(100));
            
            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region IShutterDevice Convenience Methods Tests

        [Fact]
        public async Task OpenAsync_SetsPositionTo0()
        {
            // Arrange
            await _shutterDevice.SetPercentageAsync(100.0f); // Start closed
            
            // Act
            await _shutterDevice.OpenAsync();
            
            // Assert
            _shutterDevice.CurrentPercentage.Should().Be(0.0f);
        }

        [Fact]
        public async Task CloseAsync_SetsPositionTo100()
        {
            // Arrange
            await _shutterDevice.SetPercentageAsync(0.0f); // Start open
            
            // Act
            await _shutterDevice.CloseAsync();
            
            // Assert
            _shutterDevice.CurrentPercentage.Should().Be(100.0f);
        }

        [Fact]
        public async Task StopAsync_UpdatesLastUpdated()
        {
            // Arrange
            var beforeStop = DateTime.Now;
            await Task.Delay(10);
            
            // Act
            await _shutterDevice.StopAsync();
            
            // Assert
            _shutterDevice.LastUpdated.Should().BeOnOrAfter(beforeStop);
        }

        #endregion

        #region Interface Composition Tests

        [Fact]
        public void ShutterDevice_ImplementsAllRequiredInterfaces()
        {
            // Assert
            _shutterDevice.Should().BeAssignableTo<IKnxDeviceBase>();
            _shutterDevice.Should().BeAssignableTo<IPercentageControllable>();
            _shutterDevice.Should().BeAssignableTo<ILockableDevice>();
            _shutterDevice.Should().BeAssignableTo<IActivityStatusReadable>();
            _shutterDevice.Should().BeAssignableTo<IShutterDevice>();
        }

        [Fact]
        public async Task ShutterDevice_AllInterfaceMethodsWork()
        {
            // Test IKnxDeviceBase
            await _shutterDevice.InitializeAsync();
            _shutterDevice.SaveCurrentState();
            await _shutterDevice.RestoreSavedStateAsync();
            
            // Test IPercentageControllable
            await _shutterDevice.SetPercentageAsync(50.0f);
            await _shutterDevice.ReadPercentageAsync();
            await _shutterDevice.AdjustPercentageAsync(10.0f);
            
            // Test ILockableDevice
            await _shutterDevice.LockAsync();
            await _shutterDevice.ReadLockStateAsync();
            await _shutterDevice.UnlockAsync();
            
            // Test IActivityStatusReadable
            await _shutterDevice.ReadActivityStatusAsync();
            var isActive = _shutterDevice.IsActive;
            await _shutterDevice.WaitForInactiveAsync(TimeSpan.FromMilliseconds(100));
            
            // Test IShutterDevice convenience methods
            await _shutterDevice.OpenAsync();
            await _shutterDevice.CloseAsync();
            await _shutterDevice.StopAsync();
            
            // If we reach here, all interface methods work
            Assert.True(true);
        }

        [Fact]
        public async Task CanUseAsIActivityStatusReadable()
        {
            // Arrange
            IActivityStatusReadable activityReadable = _shutterDevice;
            
            // Act
            var isActive = activityReadable.IsActive;
            await activityReadable.ReadActivityStatusAsync();
            
            // Assert
            activityReadable.Should().NotBeNull();
            isActive.Should().BeFalse(); // Initially inactive
        }

        #endregion
    }
}
