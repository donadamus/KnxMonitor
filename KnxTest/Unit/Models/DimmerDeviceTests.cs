using FluentAssertions;
using KnxModel;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Unit.Models
{


    public class DimmerDeviceTests : LightDeviceTestsBase<DimmerDevice, DimmerAddresses>
    {
        protected override DimmerDevice _device { get; }

        protected override ILogger<DimmerDevice> _logger { get; }

        protected PercentageControllableDeviceTestHelper<DimmerDevice, DimmerAddresses> _percentageTestHelper { get; }

        public DimmerDeviceTests()
        {
            // Initialize DimmerDevice with mock KNX service
            _logger = new Mock<ILogger<DimmerDevice>>().Object;
            _device = new DimmerDevice("D_TEST", "Test Dimmer", "1", _mockKnxService.Object, _logger, TimeSpan.FromSeconds(1));
            _percentageTestHelper = new PercentageControllableDeviceTestHelper<DimmerDevice, DimmerAddresses>(
                _device, _device.Addresses, _mockKnxService);
        }

        #region Dimmer-Specific Feedback Processing Tests


        [Fact]
        public void OnPercentageFeedback_WhenLocked_ShouldStillUpdateState()
        {
            // TODO: Test that percentage feedback updates state even when device is locked
            ((ILockableDevice)_device).SetLockForTest(Lock.On);
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_device.Addresses.PercentageFeedback, new KnxValue(50)));
            _device.CurrentPercentage.Should().Be(50);
        }


        [Theory]
        [InlineData(Switch.Off)]   // Off
        [InlineData(Switch.On)]    // On
        public void OnSwitchFeedback_ShouldNotAffectPercentageState(Switch switchState)
        {
            // TODO: Test that switch feedback only affects switch state, not percentage
            ((ISwitchable)_device).SetSwitchForTest(switchState);
            ((IPercentageControllable)_device).SetPercentageForTest(20); // Set initial percentage
            _mockKnxService.Raise(s => s.GroupMessageReceived += null, _mockKnxService.Object, new KnxGroupEventArgs(_device.Addresses.SwitchFeedback, new KnxValue(switchState == Switch.Off)));
            _device.CurrentPercentage.Should().Be(20);
            _device.CurrentSwitchState.Should().Be(switchState.Opposite());
        }

        #endregion


        #region Interface Composition Tests

        [Fact]
        public void DimmerDevice_ImplementsAllRequiredInterfaces()
        {
            // Assert
            _device.Should().BeAssignableTo<IKnxDeviceBase>();
            _device.Should().BeAssignableTo<ISwitchable>();
            _device.Should().BeAssignableTo<IPercentageControllable>();
            _device.Should().BeAssignableTo<ILockableDevice>();
            _device.Should().BeAssignableTo<IDimmerDevice>();
        }

        #endregion
    }
}
