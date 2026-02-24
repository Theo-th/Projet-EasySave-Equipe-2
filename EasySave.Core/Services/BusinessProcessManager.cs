using EasySave.Core.Models;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Manages business process detection and backup state changes.
    /// Handles pausing backups when monitored processes are running.
    /// </summary>
    public class BusinessProcessManager
    {
        private readonly ProcessDetector _processDetector;
        private readonly Action<BackupState, BackupState>? _onStateChangeCallback;
        private readonly Action<string>? _onProcessDetectedCallback;

        public event Action<string>? OnBusinessProcessDetected;

        public BusinessProcessManager(
            ProcessDetector processDetector,
            Action<BackupState, BackupState>? onStateChangeCallback = null,
            Action<string>? onProcessDetectedCallback = null)
        {
            _processDetector = processDetector;
            _onStateChangeCallback = onStateChangeCallback;
            _onProcessDetectedCallback = onProcessDetectedCallback;
            _processDetector.ProcessStatusChanged += OnWatchedProcessStatusChanged;
        }

        /// <summary>
        /// Waits if a business process is detected, pausing the backup temporarily.
        /// Resumes automatically when the process stops.
        /// </summary>
        public void WaitIfBusinessProcess(CancellationToken ct)
        {
            string? runningProcess;
            bool wasPaused = false;

            while (!ct.IsCancellationRequested &&
                   (runningProcess = _processDetector.IsAnyWatchedProcessRunning()) != null)
            {
                if (!wasPaused)
                {
                    wasPaused = true;
                    OnBusinessProcessDetected?.Invoke(runningProcess);
                    _onProcessDetectedCallback?.Invoke(runningProcess);
                    _onStateChangeCallback?.Invoke(BackupState.Active, BackupState.Paused);
                }
                ct.WaitHandle.WaitOne(500);
            }

            if (wasPaused && !ct.IsCancellationRequested)
                _onStateChangeCallback?.Invoke(BackupState.Paused, BackupState.Active);
        }

        private void OnWatchedProcessStatusChanged(object? sender, ProcessStatusChangedEventArgs e)
        {
            if (e.IsRunning)
                OnBusinessProcessDetected?.Invoke(e.Process.ProcessName);
        }
    }
}
