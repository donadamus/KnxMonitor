using System;
using System.Threading.Tasks;
using FluentAssertions;
using KnxModel;
using Moq;
using Xunit;

namespace KnxTest.Unit.Models
{
    /// <summary>
    /// Unit tests for new LightDevice implementation
    /// Tests each interface functionality separately
    /// </summary>
    public class LightDeviceTests
    {
        private readonly Mock<IKnxService> _mockKnxService;
        private readonly LightDevice _lightDevice;

        public LightDeviceTests()
        {
            _mockKnxService = new Mock<IKnxService>();
            _lightDevice = new LightDevice("L_TEST", "Test Light", "1", _mockKnxService.Object);
        }

        #region IKnxDeviceBase Tests

        [Fact]
        public void Constructor_SetsBasicProperties()
        {
            // Assert
            _lightDevice.Id.Should().Be("L_TEST");
            _lightDevice.Name.Should().Be("Test Light");
            _lightDevice.SubGroup.Should().Be("1");
            _lightDevice.LastUpdated.Should().Be(DateTime.MinValue); // Not initialized yet
        }

        [Fact]
        public async Task InitializeAsync_UpdatesLastUpdated()
        {
            // Act
            await _lightDevice.InitializeAsync();

            // Assert
            _lightDevice.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task SaveAndRestoreState_WorksCorrectly()
        {
            // Arrange
            await _lightDevice.InitializeAsync();
            await _lightDevice.TurnOnAsync();
            await _lightDevice.LockAsync();

            // Act - Save state
            _lightDevice.SaveCurrentState();

            // Change state
            await _lightDevice.TurnOffAsync();
            await _lightDevice.UnlockAsync();

            // Restore state
            await _lightDevice.RestoreSavedStateAsync();

            // Assert
            _lightDevice.CurrentSwitchState.Should().Be(Switch.On);
            _lightDevice.CurrentLockState.Should().Be(Lock.On);
        }

        #endregion

        #region ISwitchable Tests

        [Fact]
        public async Task TurnOnAsync_UpdatesSwitchState()
        {
            // Act
            await _lightDevice.TurnOnAsync();

            // Assert
            _lightDevice.CurrentSwitchState.Should().Be(Switch.On);
            _lightDevice.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task TurnOffAsync_UpdatesSwitchState()
        {
            // Act
            await _lightDevice.TurnOffAsync();

            // Assert
            _lightDevice.CurrentSwitchState.Should().Be(Switch.Off);
            _lightDevice.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ToggleAsync_SwitchesFromOffToOn()
        {
            // Arrange
            await _lightDevice.TurnOffAsync();

            // Act
            await _lightDevice.ToggleAsync();

            // Assert
            _lightDevice.CurrentSwitchState.Should().Be(Switch.On);
        }

        [Fact]
        public async Task ToggleAsync_SwitchesFromOnToOff()
        {
            // Arrange
            await _lightDevice.TurnOnAsync();

            // Act
            await _lightDevice.ToggleAsync();

            // Assert
            _lightDevice.CurrentSwitchState.Should().Be(Switch.Off);
        }

        [Fact]
        public async Task WaitForSwitchStateAsync_ReturnsTrue_WhenStateMatches()
        {
            // Arrange
            await _lightDevice.TurnOnAsync();

            // Act
            var result = await _lightDevice.WaitForSwitchStateAsync(Switch.On, TimeSpan.FromSeconds(1));

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region ILockableDevice Tests

        [Fact]
        public async Task LockAsync_UpdatesLockState()
        {
            // Act
            await _lightDevice.LockAsync();

            // Assert
            _lightDevice.CurrentLockState.Should().Be(Lock.On);
            _lightDevice.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task UnlockAsync_UpdatesLockState()
        {
            // Act
            await _lightDevice.UnlockAsync();

            // Assert
            _lightDevice.CurrentLockState.Should().Be(Lock.Off);
            _lightDevice.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task WaitForLockStateAsync_ReturnsTrue_WhenStateMatches()
        {
            // Arrange
            await _lightDevice.LockAsync();

            // Act
            var result = await _lightDevice.WaitForLockStateAsync(Lock.On, TimeSpan.FromSeconds(1));

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region Interface Composition Tests

        [Fact]
        public void LightDevice_ImplementsAllRequiredInterfaces()
        {
            // Assert
            _lightDevice.Should().BeAssignableTo<IKnxDeviceBase>();
            _lightDevice.Should().BeAssignableTo<ISwitchable>();
            _lightDevice.Should().BeAssignableTo<ILockableDevice>();
            _lightDevice.Should().BeAssignableTo<ILightDevice>();
        }

        [Fact]
        public async Task CanUseAsISwitchable()
        {
            // Arrange
            ISwitchable switchable = _lightDevice;

            // Act
            await switchable.TurnOnAsync();

            // Assert
            switchable.CurrentSwitchState.Should().Be(Switch.On);
        }

        [Fact]
        public async Task CanUseAsILockableDevice()
        {
            // Arrange
            ILockableDevice lockable = _lightDevice;

            // Act
            await lockable.LockAsync();

            // Assert
            lockable.CurrentLockState.Should().Be(Lock.On);
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _lightDevice?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
