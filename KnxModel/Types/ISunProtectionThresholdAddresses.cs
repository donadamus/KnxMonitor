namespace KnxModel.Types
{
    public interface ISunProtectionThresholdAddresses
    {
        string BrightnessThreshold1 { get; }
        string BrightnessThreshold2 { get; }
        string OutdoorTemperatureThreshold { get; }
    }
}
