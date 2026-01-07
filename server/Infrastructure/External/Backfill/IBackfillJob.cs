namespace RiotProxy.Infrastructure.External.Backfill
{
    /// <summary>
    /// Interface for backfill jobs that process batches of data migrations.
    /// </summary>
    public interface IBackfillJob
    {
        /// <summary>
        /// Gets the name of this backfill job.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the total number of items that need to be processed.
        /// </summary>
        Task<int> GetTotalItemsAsync(CancellationToken ct = default);

        /// <summary>
        /// Process a batch of items.
        /// </summary>
        /// <param name="startIndex">Zero-based start index</param>
        /// <param name="batchSize">Number of items to process</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Number of items actually processed</returns>
        Task<int> ProcessBatchAsync(int startIndex, int batchSize, CancellationToken ct = default);

        /// <summary>
        /// Called when the backfill job is complete.
        /// </summary>
        Task OnCompleteAsync(CancellationToken ct = default);

        /// <summary>
        /// Called when an error occurs during processing.
        /// </summary>
        Task OnErrorAsync(Exception ex, CancellationToken ct = default);
    }
}
