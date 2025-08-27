namespace KnxModel
{
    public interface IMovementControllable : IIdentifable
    {


        bool CurrentDirection { get; internal set; }
        /// <summary>
        /// Open shutter completely using UP command (MovementControl = 1)
        /// More reliable than SetPercentageAsync(0) due to timing-based position tracking
        /// </summary>
        Task OpenAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Close shutter completely using DOWN command (MovementControl = 0)
        /// More reliable than SetPercentageAsync(100) due to timing-based position tracking
        /// </summary>
        Task CloseAsync(TimeSpan? timeout = null);

        /// <summary>
        /// Stop shutter movement
        /// </summary>
        Task StopAsync(TimeSpan? timeout = null);
    }
}
