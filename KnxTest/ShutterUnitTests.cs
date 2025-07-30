using FluentAssertions;
using KnxModel;
using KnxService;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest
{
    public class ShutterUnitTests
    {
        private readonly Mock<IKnxService> _mockKnxService;
        private readonly IShutter _shutter;

        public ShutterUnitTests()
        {
            _mockKnxService = new Mock<IKnxService>();
            _shutter = new Shutter("R1.1", "Test Bathroom", "1", _mockKnxService.Object);
        }

        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Assert
            _shutter.Id.Should().Be("R1.1");
            _shutter.Name.Should().Be("Test Bathroom");
            _shutter.SubGroup.Should().Be("1");
            
            // Verify addresses are calculated correctly
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
            var expectedPosition = Percent.FromPercantage(45.0);
            _mockKnxService.Setup(s => s.RequestGroupValue<Percent>("4/2/101"))
                          .ReturnsAsync(expectedPosition);
            _mockKnxService.Setup(s => s.RequestGroupValue("4/3/101"))
                          .ReturnsAsync("1"); // locked
            _mockKnxService.Setup(s => s.RequestGroupValue("4/1/101"))
                          .ReturnsAsync("0"); // stopped

            // Act
            await _shutter.InitializeAsync();

            // Assert
            _shutter.CurrentState.Position.Should().Be(expectedPosition);
            _shutter.CurrentState.IsLocked.Should().BeTrue();
            _shutter.CurrentState.MovementState.Should().Be(ShutterMovementState.Stopped);
            _shutter.CurrentState.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task SaveCurrentStateAsync_SavesCurrentState()
        {
            // Arrange
            var expectedPosition = Percent.FromPercantage(75.0);
            _mockKnxService.Setup(s => s.RequestGroupValue<Percent>("4/2/101"))
                          .ReturnsAsync(expectedPosition);
            _mockKnxService.Setup(s => s.RequestGroupValue("4/3/101"))
                          .ReturnsAsync("0"); // unlocked
            _mockKnxService.Setup(s => s.RequestGroupValue("4/1/101"))
                          .ReturnsAsync("0"); // stopped

            // Act
            await _shutter.SaveCurrentStateAsync();

            // Assert
            _shutter.SavedState.Should().NotBeNull();
            _shutter.SavedState!.Position.Should().Be(expectedPosition);
            _shutter.SavedState.IsLocked.Should().BeFalse();
            _shutter.SavedState.MovementState.Should().Be(ShutterMovementState.Stopped);
        }

        [Fact]
        public async Task RestoreSavedStateAsync_ThrowsWhenNoSavedState()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _shutter.RestoreSavedStateAsync());
        }

        [Fact]
        public async Task SetPositionAsync_CallsKnxServiceWithCorrectAddress()
        {
            // Arrange
            var targetPosition = Percent.FromPercantage(60.0);
            
            // Setup for RefreshCurrentStateAsync calls
            _mockKnxService.Setup(s => s.RequestGroupValue<Percent>("4/2/101"))
                          .ReturnsAsync(targetPosition);
            _mockKnxService.Setup(s => s.RequestGroupValue("4/3/101"))
                          .ReturnsAsync("0");
            _mockKnxService.Setup(s => s.RequestGroupValue("4/1/101"))
                          .ReturnsAsync("0");

            // Act
            await _shutter.SetPositionAsync(targetPosition);

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("4/2/1", targetPosition), Times.Once);
        }

        [Fact]
        public async Task MoveAsync_CallsKnxServiceWithCorrectValues()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<Percent>("4/2/101"))
                          .ReturnsAsync(Percent.FromPercantage(50.0));
            _mockKnxService.Setup(s => s.RequestGroupValue("4/3/101"))
                          .ReturnsAsync("0");
            _mockKnxService.Setup(s => s.RequestGroupValue("4/1/101"))
                          .ReturnsAsync("0");

            // Act - Test UP direction
            await _shutter.MoveAsync(ShutterDirection.Up);

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("4/0/1", false), Times.Once); // UP = false

            // Act - Test DOWN direction
            await _shutter.MoveAsync(ShutterDirection.Down);

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("4/0/1", true), Times.Once); // DOWN = true
        }

        [Fact]
        public async Task MoveAsync_WithDuration_CallsStopAfterDelay()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<Percent>("4/2/101"))
                          .ReturnsAsync(Percent.FromPercantage(50.0));
            _mockKnxService.Setup(s => s.RequestGroupValue("4/3/101"))
                          .ReturnsAsync("0");
            _mockKnxService.Setup(s => s.RequestGroupValue("4/1/101"))
                          .ReturnsAsync("0");

            // Act
            await _shutter.MoveAsync(ShutterDirection.Up, TimeSpan.FromMilliseconds(100));

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("4/0/1", false), Times.Once); // Movement command
            _mockKnxService.Verify(s => s.WriteGroupValue("4/1/1", true), Times.Once);  // Stop command
        }

        [Fact]
        public async Task StopAsync_CallsKnxServiceWithStopCommand()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<Percent>("4/2/101"))
                          .ReturnsAsync(Percent.FromPercantage(50.0));
            _mockKnxService.Setup(s => s.RequestGroupValue("4/3/101"))
                          .ReturnsAsync("0");
            _mockKnxService.Setup(s => s.RequestGroupValue("4/1/101"))
                          .ReturnsAsync("0");

            // Act
            await _shutter.StopAsync();

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("4/1/1", true), Times.Once);
        }

        [Fact]
        public async Task SetLockAsync_CallsKnxServiceWithCorrectValues()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue<Percent>("4/2/101"))
                          .ReturnsAsync(Percent.FromPercantage(50.0));
            _mockKnxService.Setup(s => s.RequestGroupValue("4/3/101"))
                          .ReturnsAsync("1");
            _mockKnxService.Setup(s => s.RequestGroupValue("4/1/101"))
                          .ReturnsAsync("0");

            // Act - Lock
            await _shutter.SetLockAsync(true);

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("4/3/1", true), Times.Once);

            // Act - Unlock
            await _shutter.SetLockAsync(false);

            // Assert
            _mockKnxService.Verify(s => s.WriteGroupValue("4/3/1", false), Times.Once);
        }

        [Theory]
        [InlineData("0", ShutterMovementState.Stopped)]
        [InlineData("1", ShutterMovementState.MovingUp)]
        [InlineData("2", ShutterMovementState.MovingDown)]
        [InlineData("3", ShutterMovementState.Unknown)]
        public async Task ReadMovementStateAsync_ReturnsCorrectState(string knxValue, ShutterMovementState expectedState)
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue("4/1/101"))
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
            _mockKnxService.Setup(s => s.RequestGroupValue("4/3/101"))
                          .ReturnsAsync("1");
            var lockedResult = await _shutter.ReadLockStateAsync();
            lockedResult.Should().BeTrue();

            // Arrange & Act & Assert - Unlocked
            _mockKnxService.Setup(s => s.RequestGroupValue("4/3/101"))
                          .ReturnsAsync("0");
            var unlockedResult = await _shutter.ReadLockStateAsync();
            unlockedResult.Should().BeFalse();
        }

        [Fact]
        public async Task WaitForPositionAsync_ReturnsTrueWhenPositionReached()
        {
            // Arrange
            var targetPosition = Percent.FromPercantage(50.0);
            _mockKnxService.Setup(s => s.RequestGroupValue<Percent>("4/2/101"))
                          .ReturnsAsync(targetPosition);

            // Act
            var result = await _shutter.WaitForPositionAsync(targetPosition, tolerance: 1.0, TimeSpan.FromSeconds(1));

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task WaitForMovementStopAsync_ReturnsTrueWhenStopped()
        {
            // Arrange
            _mockKnxService.Setup(s => s.RequestGroupValue("4/1/101"))
                          .ReturnsAsync("0"); // stopped

            // Act
            var result = await _shutter.WaitForMovementStopAsync(TimeSpan.FromSeconds(1));

            // Assert
            result.Should().BeTrue();
        }

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
