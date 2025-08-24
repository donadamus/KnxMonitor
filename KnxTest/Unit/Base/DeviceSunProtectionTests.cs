using FluentAssertions;
using KnxModel;
using KnxModel.Types;
using KnxTest.Unit.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KnxTest.Unit.Base
{
    public abstract class DeviceSunProtectionTests<TDevice, TAddresses> : BaseKnxDeviceUnitTests 
        where TDevice : class, ISunProtectionThresholdCapableDevice, IKnxDeviceBase
        where TAddresses : ISunProtectionThresholdAddresses
    {
        protected abstract SunProtectionDeviceTestHelper<TDevice, TAddresses> _sunProtectionTestHelper { get; }

        public DeviceSunProtectionTests() : base()
        {
        }

        [Fact]
        public void BrightnessThreshold1_WhenActivated_ShouldUpdateDeviceState()
        {
            _sunProtectionTestHelper.BrightnessThreshold1_WhenActivated_ShouldUpdateDeviceState();
        }

        [Fact]
        public void BrightnessThreshold1_WhenDeactivated_ShouldUpdateDeviceState()
        {
            _sunProtectionTestHelper.BrightnessThreshold1_WhenDeactivated_ShouldUpdateDeviceState();
        }

        [Fact]
        public void BrightnessThreshold2_WhenActivated_ShouldUpdateDeviceState()
        {
            _sunProtectionTestHelper.BrightnessThreshold2_WhenActivated_ShouldUpdateDeviceState();
        }

        [Fact]
        public void BrightnessThreshold2_WhenDeactivated_ShouldUpdateDeviceState()
        {
            _sunProtectionTestHelper.BrightnessThreshold2_WhenDeactivated_ShouldUpdateDeviceState();
        }

        [Fact]
        public void TemperatureThreshold_WhenActivated_ShouldUpdateDeviceState()
        {
            _sunProtectionTestHelper.TemperatureThreshold_WhenActivated_ShouldUpdateDeviceState();
        }

        [Fact]
        public void TemperatureThreshold_WhenDeactivated_ShouldUpdateDeviceState()
        {
            _sunProtectionTestHelper.TemperatureThreshold_WhenDeactivated_ShouldUpdateDeviceState();
        }

        [Fact]
        public async Task ReadBrightnessThreshold1StateAsync_ShouldRequestCorrectAddress()
        {
            await _sunProtectionTestHelper.ReadBrightnessThreshold1StateAsync_ShouldRequestCorrectAddress();
        }

        [Fact]
        public async Task ReadBrightnessThreshold2StateAsync_ShouldRequestCorrectAddress()
        {
            await _sunProtectionTestHelper.ReadBrightnessThreshold2StateAsync_ShouldRequestCorrectAddress();
        }

        [Fact]
        public async Task ReadTemperatureThresholdStateAsync_ShouldRequestCorrectAddress()
        {
            await _sunProtectionTestHelper.ReadTemperatureThresholdStateAsync_ShouldRequestCorrectAddress();
        }
    }
}
