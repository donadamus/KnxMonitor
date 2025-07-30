using System;
using Xunit;
using FluentAssertions;
using KnxModel;

namespace KnxTest
{
    public class KnxValueTests
    {
        [Fact]
        public void KnxValue_ShouldConvertBooleanCorrectly()
        {
            // Test boolean conversion from different types
            var boolValue1 = new KnxValue(true);
            var boolValue2 = new KnxValue("1");
            var boolValue3 = new KnxValue((byte)1);
            
            boolValue1.AsBoolean().Should().BeTrue();
            boolValue2.AsBoolean().Should().BeTrue();
            boolValue3.AsBoolean().Should().BeTrue();
            
            var falseValue = new KnxValue("0");
            falseValue.AsBoolean().Should().BeFalse();
        }
        
        [Fact]
        public void KnxValue_ShouldConvertPercentCorrectly()
        {
            // Test percentage conversion
            var percentValue1 = new KnxValue((byte)170); // 66.7% in KNX
            var percentValue2 = new KnxValue("66.7");
            
            var percent1 = percentValue1.AsPercent();
            var percent2 = percentValue2.AsPercent();
            
            percent1.Value.Should().BeApproximately(66.7, 0.1);
            percent2.Value.Should().BeApproximately(66.7, 0.1);
        }
        
        [Fact]
        public void KnxValue_ShouldAutoDetectTypeFromAddress()
        {
            // Position feedback (percentage)
            var positionValue = new KnxValue((byte)170);
            var typedPosition = positionValue.GetTypedValue("4/2/17"); // Position address
            
            typedPosition.Should().BeOfType<Percent>();
            ((Percent)typedPosition).Value.Should().BeApproximately(66.7, 0.1);
            
            // Lock feedback (boolean)
            var lockValue = new KnxValue((byte)1);
            var typedLock = lockValue.GetTypedValue("4/3/17"); // Lock address
            
            typedLock.Should().BeOfType<bool>();
            ((bool)typedLock).Should().BeTrue();
        }
        
        [Fact]
        public void KnxValue_ShouldProvideRichToString()
        {
            var value = new KnxValue((byte)170);
            var valueString = value.ToString();
            
            valueString.Should().Contain("66.7");
            valueString.Should().Contain("Raw: 170");
            valueString.Should().Contain("Length: 1");
        }
        
        [Fact]
        public void KnxValue_ShouldHandleGenericConversion()
        {
            var value = new KnxValue((byte)170);
            
            var asPercent = value.AutoConvert<Percent>();
            var asByte = value.AutoConvert<byte>();
            var asBool = value.AutoConvert<bool>();
            
            asPercent.Value.Should().BeApproximately(66.7, 0.1);
            asByte.Should().Be(170);
            asBool.Should().BeTrue(); // Non-zero = true
        }
    }
}
