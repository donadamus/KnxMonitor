namespace KnxModel.Types
{
    public interface ISunProtectionThresholdAddresses
    {
        string BrightnessThreshold1 { get; }
        string BrightnessThreshold2 { get; }
        string OutdoorTemperatureThreshold { get; }
        string SunProtectionStatus { get; }
    }
    
    public interface ISunProtectionBlockableAddresses
    {
        string SunProtectionBlockControl { get; }
        string SunProtectionBlockFeedback { get; }
    }
}
