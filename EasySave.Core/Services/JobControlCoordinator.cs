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

        public void PauseAllJobs()
        {
            _globalPauseEvent.Reset();
        }

        public void ResumeAllJobs()
        {
            _globalPauseEvent.Set();
        }

        public void StopAllJobs()
        {
            _globalCts.Cancel();
            _globalCts = new CancellationTokenSource();
        }

        public void RegisterJob(string jobName)
        {
            _jobPauseEvents[jobName] = new ManualResetEventSlim(true);
            _jobCancellations[jobName] = CancellationTokenSource.CreateLinkedTokenSource(_globalCts.Token);
        }

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

        public void PauseJob(string jobName)
        {
            if (_jobPauseEvents.TryGetValue(jobName, out var pauseEvent))
            {
                pauseEvent.Reset();
            }
        }

        public void ResumeJob(string jobName)
        {
            if (_jobPauseEvents.TryGetValue(jobName, out var pauseEvent))
            {
                pauseEvent.Set();
            }
        }

        public void StopJob(string jobName)
        {
            if (_jobCancellations.TryGetValue(jobName, out var cts))
            {
                cts.Cancel();
            }
        }

        public void WaitForResume(string jobName)
        {
            _globalPauseEvent.Wait();
            if (_jobPauseEvents.TryGetValue(jobName, out var jobPauseEvent))
            {
                jobPauseEvent.Wait();
            }
        }

        public bool IsCancellationRequested(string jobName)
        {
            return _jobCancellations.TryGetValue(jobName, out var cts) && cts.Token.IsCancellationRequested;
        }

        public CancellationToken GetCancellationToken(string jobName)
        {
            return _jobCancellations.TryGetValue(jobName, out var cts) 
                ? cts.Token 
                : CancellationToken.None;
        }

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
