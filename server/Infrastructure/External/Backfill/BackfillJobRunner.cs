namespace RiotProxy.Infrastructure.External.Backfill
{
    /// <summary>
    /// Orchestrates backfill job execution with batch processing, progress tracking, and error recovery.
    /// </summary>
    public class BackfillJobRunner
    {
        private readonly int _batchSize;
        private readonly TimeSpan _delayBetweenBatches;

        public BackfillJobRunner(int batchSize = 100, TimeSpan? delayBetweenBatches = null)
        {
            if (batchSize <= 0)
                throw new ArgumentException("Batch size must be greater than 0", nameof(batchSize));

            _batchSize = batchSize;
            _delayBetweenBatches = delayBetweenBatches ?? TimeSpan.FromMilliseconds(100);
        }

        /// <summary>
        /// Run a backfill job to completion.
        /// </summary>
        public async Task<BackfillJobResult> RunAsync(IBackfillJob job, CancellationToken ct = default)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            var result = new BackfillJobResult { JobName = job.Name };
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                Console.WriteLine($"[{job.Name}] Starting backfill job...");

                // Get total count
                int totalItems = await job.GetTotalItemsAsync(ct);
                result.TotalItems = totalItems;

                if (totalItems == 0)
                {
                    Console.WriteLine($"[{job.Name}] No items to process.");
                    await job.OnCompleteAsync(ct);
                    result.Status = BackfillJobStatus.Completed;
                    return result;
                }

                Console.WriteLine($"[{job.Name}] Found {totalItems} items to process.");

                // Process in batches
                int processed = 0;
                int startIndex = 0;

                while (startIndex < totalItems && !ct.IsCancellationRequested)
                {
                    int batchProcessed = await job.ProcessBatchAsync(startIndex, _batchSize, ct);
                    processed += batchProcessed;
                    startIndex += _batchSize;

                    // Log progress
                    double progressPercent = (processed / (double)totalItems) * 100;
                    Console.WriteLine($"[{job.Name}] Progress: {processed}/{totalItems} ({progressPercent:F1}%)");

                    // Delay between batches to avoid rate limiting
                    if (startIndex < totalItems)
                    {
                        await Task.Delay(_delayBetweenBatches, ct);
                    }
                }

                result.ItemsProcessed = processed;
                result.Status = BackfillJobStatus.Completed;

                Console.WriteLine($"[{job.Name}] Backfill job completed. Processed {processed}/{totalItems} items in {stopwatch.Elapsed.TotalSeconds:F2}s");

                // Call completion hook
                await job.OnCompleteAsync(ct);
            }
            catch (OperationCanceledException)
            {
                result.Status = BackfillJobStatus.Cancelled;
                Console.WriteLine($"[{job.Name}] Backfill job cancelled.");
            }
            catch (Exception ex)
            {
                result.Status = BackfillJobStatus.Failed;
                result.Error = ex.Message;
                Console.WriteLine($"[{job.Name}] Backfill job failed: {ex.Message}");

                try
                {
                    await job.OnErrorAsync(ex, ct);
                }
                catch (Exception errorHandlerEx)
                {
                    Console.WriteLine($"[{job.Name}] Error handler also failed: {errorHandlerEx.Message}");
                }
            }
            finally
            {
                stopwatch.Stop();
                result.DurationSeconds = stopwatch.Elapsed.TotalSeconds;
            }

            return result;
        }
    }

    /// <summary>
    /// Result of a backfill job execution.
    /// </summary>
    public class BackfillJobResult
    {
        public string JobName { get; set; } = string.Empty;
        public BackfillJobStatus Status { get; set; }
        public int TotalItems { get; set; }
        public int ItemsProcessed { get; set; }
        public double DurationSeconds { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Status of a backfill job execution.
    /// </summary>
    public enum BackfillJobStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }
}
