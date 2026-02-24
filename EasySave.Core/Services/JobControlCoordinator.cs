using System.Threading;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Manages pause and cancellation mechanisms for backup jobs.
    /// Handles both global and per-job control events.
    /// </summary>
    public class JobControlCoordinator
    {
        private readonly ManualResetEventSlim _globalPauseEvent = new(true);
        private readonly Dictionary<string, ManualResetEventSlim> _jobPauseEvents = new();
        private readonly Dictionary<string, CancellationTokenSource> _jobCancellations = new();
        private CancellationTokenSource _globalCts = new();

        /// <summary>
        /// Pauses all backup jobs globally.
        /// </summary>
        public void PauseAllJobs()
        {
            _globalPauseEvent.Reset();
        }

        /// <summary>
        /// Resumes all paused backup jobs globally.
        /// </summary>
        public void ResumeAllJobs()
        {
            _globalPauseEvent.Set();
        }

        /// <summary>
        /// Stops all active backup jobs with cancellation.
        /// </summary>
        public void StopAllJobs()
        {
            _globalCts.Cancel();
            _globalCts = new CancellationTokenSource();
        }

        /// <summary>
        /// Registers a new job with pause and cancellation control mechanisms.
        /// </summary>
        public void RegisterJob(string jobName)
        {
            _jobPauseEvents[jobName] = new ManualResetEventSlim(true);
            _jobCancellations[jobName] = CancellationTokenSource.CreateLinkedTokenSource(_globalCts.Token);
        }

        /// <summary>
        /// Unregisters a job and releases its control resources.
        /// </summary>
        public void UnregisterJob(string jobName)
        {
            if (_jobPauseEvents.TryGetValue(jobName, out var pauseEvent))
            {
                pauseEvent.Dispose();
                _jobPauseEvents.Remove(jobName);
            }

            if (_jobCancellations.TryGetValue(jobName, out var cts))
            {
                cts.Dispose();
                _jobCancellations.Remove(jobName);
            }
        }

        /// <summary>
        /// Pauses a specific backup job by name.
        /// </summary>
        public void PauseJob(string jobName)
        {
            if (_jobPauseEvents.TryGetValue(jobName, out var pauseEvent))
            {
                pauseEvent.Reset();
            }
        }

        /// <summary>
        /// Resumes a specific paused backup job by name.
        /// </summary>
        public void ResumeJob(string jobName)
        {
            if (_jobPauseEvents.TryGetValue(jobName, out var pauseEvent))
            {
                pauseEvent.Set();
            }
        }

        /// <summary>
        /// Stops a specific backup job with cancellation.
        /// </summary>
        public void StopJob(string jobName)
        {
            if (_jobCancellations.TryGetValue(jobName, out var cts))
            {
                cts.Cancel();
            }
        }

        /// <summary>
        /// Blocks the current thread until a specific job is allowed to resume.
        /// </summary>
        public void WaitForResume(string jobName)
        {
            _globalPauseEvent.Wait();
            if (_jobPauseEvents.TryGetValue(jobName, out var jobPauseEvent))
            {
                jobPauseEvent.Wait();
            }
        }

        /// <summary>
        /// Checks if cancellation has been requested for a specific job.
        /// </summary>
        public bool IsCancellationRequested(string jobName)
        {
            return _jobCancellations.TryGetValue(jobName, out var cts) && cts.Token.IsCancellationRequested;
        }

        /// <summary>
        /// Returns the cancellation token for a specific job.
        /// </summary>
        public CancellationToken GetCancellationToken(string jobName)
        {
            return _jobCancellations.TryGetValue(jobName, out var cts) 
                ? cts.Token 
                : CancellationToken.None;
        }

        /// <summary>
        /// Disposes all resources used by the coordinator.
        /// </summary>
        public void Dispose()
        {
            _globalPauseEvent.Dispose();
            _globalCts.Dispose();

            foreach (var pauseEvent in _jobPauseEvents.Values)
            {
                pauseEvent.Dispose();
            }
            _jobPauseEvents.Clear();

            foreach (var cts in _jobCancellations.Values)
            {
                cts.Dispose();
            }
            _jobCancellations.Clear();
        }
    }
}
