namespace KnxTest.Integration.Interfaces
{
    public interface ISwitchableDeviceTests<TDevice>
    {
        Task CanTurnOnAndTurnOff(string deviceId);
        Task TestCanTurnOnAndTurnOff(string deviceId);
        Task CanToggleSwitch(string deviceId);
        Task TestCanToggleSwitch(string deviceId);
        Task CanReadSwitchState(string deviceId);
        Task TestCanReadSwitchState(string deviceId);
    }
}
