using KnxModel.Models.Helpers;
using Microsoft.Extensions.Logging;

namespace KnxModel
{

    public abstract class LockableDeviceBase<TDevice, TAddressess> : KnxDeviceBase<TDevice, TAddressess>, ILockableDevice
        where TDevice : IKnxDeviceBase, ILockableDevice
        where TAddressess : ILockableAddress
    {
        public Lock CurrentLockState { get; private set; }
        Lock ILockableDevice.CurrentLockState { get => CurrentLockState; set => CurrentLockState = value; }
        public Lock? SavedLockState { get; private set; }
        Lock? ILockableDevice.SavedLockState { get => SavedLockState; set => SavedLockState = value; }


        private LockableDeviceHelper<TDevice, TAddressess>? _lockableHelper;

        public LockableDeviceBase(string id, string name, string subGroup, TAddressess addresses, IKnxService knxService, ILogger<TDevice> logger, TimeSpan defaultTimeout)
            :base(id, name, subGroup, addresses, knxService, logger, defaultTimeout)
        {
            _eventManager.MessageReceived += OnKnxMessageReceived;
        }

        internal virtual void Initialize(TDevice owner)
        {
            _lockableHelper = new LockableDeviceHelper<TDevice, TAddressess>(owner, 
                                Addresses,
                                _knxService, 
                                _logger, 
                                _defaultTimeout);
        }

        public override async Task InitializeAsync()
        {
            CurrentLockState = await ReadLockStateAsync();
            LastUpdated = DateTime.Now;

            _logger.LogInformation("{type} {DeviceId} initialized - Lock: {LockState}", typeof(TDevice).Name, Id, CurrentLockState);
        }

        #region ILockableDevice Implementation

        public async Task LockAsync(TimeSpan? timeout = null)
        {
            await (_lockableHelper ?? throw new InvalidOperationException("Helper not initialized")).LockAsync(timeout);
        }
        public async Task SetLockAsync(Lock lockState, TimeSpan? timeout = null)
        {
            await (_lockableHelper ?? throw new InvalidOperationException("Helper not initialized")).SetLockAsync(lockState, timeout);
        }
        public async Task UnlockAsync(TimeSpan? timeout = null)
        {
            await (_lockableHelper ?? throw new InvalidOperationException("Helper not initialized")).UnlockAsync(timeout);
        }

        public async Task<Lock> ReadLockStateAsync()
        {
            return await (_lockableHelper ?? throw new InvalidOperationException("Helper not initialized")).ReadLockStateAsync();
        }

        public async Task<bool> WaitForLockStateAsync(Lock targetState, TimeSpan timeout)
        {
            return await (_lockableHelper ?? throw new InvalidOperationException("Helper not initialized")).WaitForLockStateAsync(targetState, timeout);
        }

        #endregion

        #region Event Handling

        private void OnKnxMessageReceived(object? sender, KnxGroupEventArgs e)
        {
            // Process lockable messages (LockControl/LockFeedback) 
            _lockableHelper?.ProcessLockMessage(e);
        }

        #endregion
        #region IKnxDeviceBase Implementation


        public override void SaveCurrentState()
        {
            SavedLockState = CurrentLockState;
        }

        public override async Task RestoreSavedStateAsync(TimeSpan? timeout = null)
        {
            if (SavedLockState.HasValue && SavedLockState.Value != CurrentLockState)
            {
                switch (SavedLockState.Value)
                {
                    case Lock.On:
                        await LockAsync(timeout);
                        break;
                    case Lock.Off:
                        await UnlockAsync(timeout);
                        break;
                }
            }
        }

        
        #endregion
    }
}
