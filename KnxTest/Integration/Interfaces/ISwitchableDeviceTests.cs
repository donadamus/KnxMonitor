namespace KnxTest.Integration.Interfaces
{
    public interface ISwitchableDeviceTests
    {
        Task CanTurnOnAndTurnOff(string deviceId);
        Task CanToggleSwitch(string deviceId);
        Task CanReadSwitchState(string deviceId);
    }
}
