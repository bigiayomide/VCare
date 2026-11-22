using Microsoft.Extensions.Logging;
using VCare.SharedKernel.Abstractions;

namespace Medications.Application.Services
{
    /// <summary>
    /// One pass of the missed-dose job. Every instance of the API runs the timer,
    /// but only the one holding the lock does the pass; the others skip it.
    /// </summary>
    internal sealed class MissedDoseJob(IJobLock jobLock, MissedDoseRecorder recorder, ILogger<MissedDoseJob> logger)
    {
        internal const string LockName = "VCare.Medications.MissedDoses";

        /// <summary>The doses recorded, or null when another instance is running the pass.</summary>
        public async Task<int?> RunOnceAsync(CancellationToken cancellationToken)
        {
            await using var held = await jobLock.TryAcquireAsync(LockName, cancellationToken);
            if (held is null)
            {
                logger.LogDebug("Another instance is recording missed doses; skipping this pass.");
                return null;
            }
            return await recorder.RecordMissedDosesAsync(cancellationToken);
        }
    }
}
