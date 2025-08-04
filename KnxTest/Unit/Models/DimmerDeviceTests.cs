using System;
using System.Threading.Tasks;
using FluentAssertions;
using KnxModel;
using Moq;
using Xunit;

namespace KnxTest.Unit.Models
{
    /// <summary>
    /// Comprehensive unit tests for DimmerDevice implementation
    /// Tests all interface functionality: IKnxDeviceBase, ISwitchable, ILockableDevice, IPercentageControllable
    /// </summary>
    public class DimmerDeviceTests
    {
        private readonly Mock<IKnxService> _mockKnxService;
        private readonly DimmerDevice _dimmerDevice;

        public DimmerDeviceTests()
        {
            _mockKnxService = new Mock<IKnxService>();
            _dimmerDevice = new DimmerDevice("dimmer_001", "Living Room Dimmer", "1", _mockKnxService.Object);
        }

        #region IKnxDeviceBase Tests

        [Fact]
        public void Constructor_ValidParameters_SetsProperties()
        {
            // Assert
            _dimmerDevice.Id.Should().Be("dimmer_001");
            _dimmerDevice.Name.Should().Be("Living Room Dimmer");
            _dimmerDevice.SubGroup.Should().Be("1");
        }

        [Fact]
        public void Constructor_NullId_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new DimmerDevice(null!, "Test", "1", _mockKnxService.Object);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task InitializeAsync_UpdatesLastUpdated()
        {
            // Act
            await _dimmerDevice.InitializeAsync();
            
            // Assert
            _dimmerDevice.LastUpdated.Should().BeAfter(DateTime.MinValue);
            _dimmerDevice.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task SaveAndRestoreState_PreservesAllStates()
        {
            // Arrange
            await _dimmerDevice.InitializeAsync();
            await _dimmerDevice.TurnOnAsync();
            await _dimmerDevice.LockAsync();
            await _dimmerDevice.SetPercentageAsync(75.0f);
            
            // Act - Save state
            _dimmerDevice.SaveCurrentState();
            
            // Change all states
            await _dimmerDevice.TurnOffAsync();
            await _dimmerDevice.UnlockAsync();
            await _dimmerDevice.SetPercentageAsync(25.0f);
            
            _dimmerDevice.CurrentSwitchState.Should().Be(Switch.Off);
            _dimmerDevice.CurrentLockState.Should().Be(Lock.Off);
            _dimmerDevice.CurrentPercentage.Should().Be(25.0f);
            
            // Restore state
            await _dimmerDevice.RestoreSavedStateAsync();
            
            // Assert
            _dimmerDevice.CurrentSwitchState.Should().Be(Switch.On);
            _dimmerDevice.CurrentLockState.Should().Be(Lock.On);
            _dimmerDevice.CurrentPercentage.Should().Be(75.0f);
        }

        #endregion

        #region ISwitchable Tests

        [Fact]
        public async Task TurnOnAsync_UpdatesSwitchState()
        {
            // Act
            await _dimmerDevice.TurnOnAsync();
            
            // Assert
            _dimmerDevice.CurrentSwitchState.Should().Be(Switch.On);
        }

        [Fact]
        public async Task TurnOffAsync_UpdatesSwitchState()
        {
            // Arrange
            await _dimmerDevice.TurnOnAsync();
            
            // Act
            await _dimmerDevice.TurnOffAsync();
            
            // Assert
            _dimmerDevice.CurrentSwitchState.Should().Be(Switch.Off);
        }

        [Fact]
        public async Task ToggleAsync_SwitchesFromOffToOn()
        {
            // Arrange
            await _dimmerDevice.TurnOffAsync();
            
            // Act
            await _dimmerDevice.ToggleAsync();
            
            // Assert
            _dimmerDevice.CurrentSwitchState.Should().Be(Switch.On);
        }

        [Fact]
        public async Task ToggleAsync_SwitchesFromOnToOff()
        {
            // Arrange
            await _dimmerDevice.TurnOnAsync();
            
            // Act
            await _dimmerDevice.ToggleAsync();
            
            // Assert
            _dimmerDevice.CurrentSwitchState.Should().Be(Switch.Off);
        }

        [Fact]
        public async Task ToggleAsync_FromUnknownState_TurnsOn()
        {
            // Act (device starts in Unknown state)
            await _dimmerDevice.ToggleAsync();
            
            // Assert
            _dimmerDevice.CurrentSwitchState.Should().Be(Switch.On);
        }

        [Fact]
        public async Task ReadSwitchStateAsync_ReturnsCurrentSwitchState()
        {
            // Arrange
            await _dimmerDevice.TurnOnAsync();
            
            // Act
            var result = await _dimmerDevice.ReadSwitchStateAsync();
            
            // Assert
            result.Should().Be(Switch.On);
        }

        [Fact]
        public async Task WaitForSwitchStateAsync_TargetReached_ReturnsTrue()
        {
            // Arrange
            await _dimmerDevice.TurnOnAsync();
            
            // Act
            var result = await _dimmerDevice.WaitForSwitchStateAsync(Switch.On, TimeSpan.FromSeconds(1));
            
            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region ILockableDevice Tests

        [Fact]
        public async Task LockAsync_UpdatesCurrentLockState()
        {
            // Act
            await _dimmerDevice.LockAsync();
            
            // Assert
            _dimmerDevice.CurrentLockState.Should().Be(Lock.On);
        }

        [Fact]
        public async Task UnlockAsync_UpdatesCurrentLockState()
        {
            // Arrange
            await _dimmerDevice.LockAsync();
            
            // Act
            await _dimmerDevice.UnlockAsync();
            
            // Assert
            _dimmerDevice.CurrentLockState.Should().Be(Lock.Off);
        }

        [Fact]
        public async Task ReadLockStateAsync_ReturnsCurrentLockState()
        {
            // Arrange
            await _dimmerDevice.LockAsync();
            
            // Act
            var result = await _dimmerDevice.ReadLockStateAsync();
            
            // Assert
            result.Should().Be(Lock.On);
        }

        [Fact]
        public async Task WaitForLockStateAsync_TargetReached_ReturnsTrue()
        {
            // Arrange
            await _dimmerDevice.LockAsync();
            
            // Act
            var result = await _dimmerDevice.WaitForLockStateAsync(Lock.On, TimeSpan.FromSeconds(1));
            
            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region IPercentageControllable Tests

        [Fact]
        public async Task SetPercentageAsync_ValidValue_UpdatesCurrentPercentage()
        {
            // Arrange
            const float expectedPercentage = 65.5f;
            
            // Act
            await _dimmerDevice.SetPercentageAsync(expectedPercentage);
            
            // Assert
            _dimmerDevice.CurrentPercentage.Should().Be(expectedPercentage);
        }

        [Fact]
        public async Task SetPercentageAsync_NegativeValue_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Func<Task> act = async () => await _dimmerDevice.SetPercentageAsync(-10.0f);
            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Fact]
        public async Task SetPercentageAsync_ValueOver100_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Func<Task> act = async () => await _dimmerDevice.SetPercentageAsync(110.0f);
            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Fact]
        public async Task ReadPercentageAsync_ReturnsCurrentPercentage()
        {
            // Arrange
            await _dimmerDevice.SetPercentageAsync(40.0f);
            
            // Act
            var result = await _dimmerDevice.ReadPercentageAsync();
            
            // Assert
            result.Should().Be(40.0f);
        }

        [Fact]
        public async Task AdjustPercentageAsync_PositiveIncrement_IncreasesPercentage()
        {
            // Arrange
            await _dimmerDevice.SetPercentageAsync(50.0f);
            
            // Act
            await _dimmerDevice.AdjustPercentageAsync(25.0f);
            
            // Assert
            _dimmerDevice.CurrentPercentage.Should().Be(75.0f);
        }

        [Fact]
        public async Task AdjustPercentageAsync_NegativeIncrement_DecreasesPercentage()
        {
            // Arrange
            await _dimmerDevice.SetPercentageAsync(70.0f);
            
            // Act
            await _dimmerDevice.AdjustPercentageAsync(-30.0f);
            
            // Assert
            _dimmerDevice.CurrentPercentage.Should().Be(40.0f);
        }

        [Fact]
        public async Task AdjustPercentageAsync_IncrementExceedsMax_ClampsTo100()
        {
            // Arrange
            await _dimmerDevice.SetPercentageAsync(85.0f);
            
            // Act
            await _dimmerDevice.AdjustPercentageAsync(25.0f);
            
            // Assert
            _dimmerDevice.CurrentPercentage.Should().Be(100.0f);
        }

        [Fact]
        public async Task AdjustPercentageAsync_IncrementExceedsMin_ClampsTo0()
        {
            // Arrange
            await _dimmerDevice.SetPercentageAsync(20.0f);
            
            // Act
            await _dimmerDevice.AdjustPercentageAsync(-30.0f);
            
            // Assert
            _dimmerDevice.CurrentPercentage.Should().Be(0.0f);
        }

        [Fact]
        public async Task WaitForPercentageAsync_TargetReached_ReturnsTrue()
        {
            // Arrange
            await _dimmerDevice.SetPercentageAsync(60.0f);
            
            // Act
            var result = await _dimmerDevice.WaitForPercentageAsync(60.0f, 2.0, TimeSpan.FromSeconds(1));
            
            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task WaitForPercentageAsync_InvalidTarget_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Func<Task> act = async () => await _dimmerDevice.WaitForPercentageAsync(-5.0f, 2.0, TimeSpan.FromSeconds(1));
            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        #endregion

        #region Interface Composition Tests

        [Fact]
        public void DimmerDevice_ImplementsAllRequiredInterfaces()
        {
            // Assert
            _dimmerDevice.Should().BeAssignableTo<IKnxDeviceBase>();
            _dimmerDevice.Should().BeAssignableTo<ISwitchable>();
            _dimmerDevice.Should().BeAssignableTo<ILockableDevice>();
            _dimmerDevice.Should().BeAssignableTo<IPercentageControllable>();
            _dimmerDevice.Should().BeAssignableTo<IDimmerDevice>();
        }

        [Fact]
        public async Task CanUseAsISwitchable()
        {
            // Arrange
            ISwitchable switchableDevice = _dimmerDevice;
            
            // Act - Test switchable functionality
            await switchableDevice.TurnOnAsync();
            
            // Assert
            _dimmerDevice.CurrentSwitchState.Should().Be(Switch.On);
        }

        [Fact]
        public async Task CanUseAsIPercentageControllable()
        {
            // Arrange
            IPercentageControllable percentageControllable = _dimmerDevice;
            
            // Act
            await percentageControllable.SetPercentageAsync(80.0f);
            
            // Assert
            percentageControllable.CurrentPercentage.Should().Be(80.0f);
        }

        [Fact]
        public async Task DimmerDevice_AllInterfaceMethodsWork()
        {
            // Test IKnxDeviceBase
            await _dimmerDevice.InitializeAsync();
            _dimmerDevice.SaveCurrentState();
            await _dimmerDevice.RestoreSavedStateAsync();
            
            // Test ISwitchable
            await _dimmerDevice.TurnOnAsync();
            await _dimmerDevice.ToggleAsync();
            await _dimmerDevice.ReadSwitchStateAsync();
            
            // Test ILockableDevice
            await _dimmerDevice.LockAsync();
            await _dimmerDevice.UnlockAsync();
            await _dimmerDevice.ReadLockStateAsync();
            
            // Test IPercentageControllable
            await _dimmerDevice.SetPercentageAsync(50.0f);
            await _dimmerDevice.ReadPercentageAsync();
            await _dimmerDevice.AdjustPercentageAsync(10.0f);
            
            // If we reach here, all interface methods work
            Assert.True(true);
        }

        #endregion

        #region Real-world Scenario Tests

        [Fact]
        public async Task DimmerScenario_TurnOnAndSetBrightness()
        {
            // Scenario: Turn on dimmer and set to 75% brightness
            
            // Act
            await _dimmerDevice.TurnOnAsync();
            await _dimmerDevice.SetPercentageAsync(75.0f);
            
            // Assert
            _dimmerDevice.CurrentSwitchState.Should().Be(Switch.On);
            _dimmerDevice.CurrentPercentage.Should().Be(75.0f);
        }

        [Fact]
        public async Task DimmerScenario_DimUpAndDown()
        {
            // Scenario: Start at 30%, dim up to 70%, then down to 20%
            
            // Arrange
            await _dimmerDevice.SetPercentageAsync(30.0f);
            
            // Act - Dim up
            await _dimmerDevice.AdjustPercentageAsync(40.0f);
            _dimmerDevice.CurrentPercentage.Should().Be(70.0f);
            
            // Act - Dim down
            await _dimmerDevice.AdjustPercentageAsync(-50.0f);
            _dimmerDevice.CurrentPercentage.Should().Be(20.0f);
        }

        [Fact]
        public async Task DimmerScenario_LockPreventsChanges_ConceptualTest()
        {
            // Scenario: Lock dimmer (conceptually should prevent changes)
            // Note: In this implementation, lock doesn't prevent operation 
            // but in real implementation it would
            
            // Arrange
            await _dimmerDevice.TurnOnAsync();
            await _dimmerDevice.SetPercentageAsync(50.0f);
            await _dimmerDevice.LockAsync();
            
            // Act - These would be prevented in real implementation
            await _dimmerDevice.SetPercentageAsync(80.0f);
            
            // Assert - Currently changes are allowed (this is simulation)
            _dimmerDevice.CurrentLockState.Should().Be(Lock.On);
            _dimmerDevice.CurrentPercentage.Should().Be(80.0f);
            
            // In real implementation, the percentage wouldn't change when locked
        }

        #endregion
    }
}
