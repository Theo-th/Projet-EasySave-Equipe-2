using EasySave.Core.Models;
using EasySave.Core.Interfaces;
using System.Collections.Concurrent;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Manages job state tracking and persistence.
    /// Handles concurrent updates and notifications.
    /// </summary>
    public class JobStateTracker
    {
        private readonly ConcurrentDictionary<string, BackupJobState> _jobStates = new();
        private readonly IBackupStateRepository _stateRepository;
        private readonly object _stateLock = new();

        public event Action<BackupJobState>? OnStateChanged;

        public JobStateTracker(IBackupStateRepository stateRepository)
        {
            _stateRepository = stateRepository;
        }

        public void RegisterJob(string jobName, BackupJobState initialState)
        {
            _jobStates[jobName] = initialState;
            NotifyStateChange(initialState);
        }

        public bool TryGetState(string jobName, out BackupJobState? state)
        {
            return _jobStates.TryGetValue(jobName, out state);
        }

        public void UpdateJobState(string jobName, Action<BackupJobState> updateAction)
        {
            if (_jobStates.TryGetValue(jobName, out var state))
            {
                lock (_stateLock)
                {
                    updateAction(state);
                    state.LastActionTimestamp = DateTime.Now;
                }
                NotifyStateChange(state);
                SaveStates();
            }
        }

        public void UpdateAllJobStates(BackupState fromState, BackupState toState)
        {
            foreach (var state in _jobStates.Values)
            {
                if (state.State == fromState)
                {
                    lock (_stateLock)
                    {
                        state.State = toState;
                        state.LastActionTimestamp = DateTime.Now;
                    }
                    NotifyStateChange(state);
                }
            }
            SaveStates();
        }

        public void FinalizeJobState(string jobName)
        {
            if (!_jobStates.TryGetValue(jobName, out var state)) return;

            lock (_stateLock)
            {
                if (state.State == BackupState.Active || state.State == BackupState.Paused)
                {
                    state.State = state.RemainingFiles == 0 ? BackupState.Completed : BackupState.Inactive;
                    state.LastActionTimestamp = DateTime.Now;
                }
            }
            NotifyStateChange(state);
            SaveStates();
        }

        public void ClearStates()
        {
            _jobStates.Clear();
        }

        private void NotifyStateChange(BackupJobState state)
        {
            OnStateChanged?.Invoke(state);
        }

        private void SaveStates()
        {
            lock (_stateLock)
            {
                _stateRepository.UpdateState(_jobStates.Values.ToList());
            }
        }
    }
}
