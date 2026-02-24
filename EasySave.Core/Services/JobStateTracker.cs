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

        /// <summary>
        /// Initializes a new instance of <see cref="JobStateTracker"/> with the specified state repository.
        /// </summary>
        public JobStateTracker(IBackupStateRepository stateRepository)
        {
            _stateRepository = stateRepository;
        }

        /// <summary>
        /// Registers a new job with its initial state.
        /// </summary>
        public void RegisterJob(string jobName, BackupJobState initialState)
        {
            _jobStates[jobName] = initialState;
            NotifyStateChange(initialState);
        }

        /// <summary>
        /// Attempts to retrieve the state of a specific job.
        /// </summary>
        public bool TryGetState(string jobName, out BackupJobState? state)
        {
            return _jobStates.TryGetValue(jobName, out state);
        }

        /// <summary>
        /// Updates the state of a specific job using the provided action.
        /// </summary>
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

        /// <summary>
        /// Updates all jobs with a specific state to a new state.
        /// </summary>
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

        /// <summary>
        /// Finalizes the state of a job based on completion status.
        /// </summary>
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

        /// <summary>
        /// Clears all tracked job states.
        /// </summary>
        public void ClearStates()
        {
            _jobStates.Clear();
        }

        /// <summary>
        /// Notifies all subscribers of a state change event.
        /// </summary>
        private void NotifyStateChange(BackupJobState state)
        {
            OnStateChanged?.Invoke(state);
        }

        /// <summary>
        /// Saves the current states to the configured repository.
        /// </summary>
        private void SaveStates()
        {
            lock (_stateLock)
            {
                _stateRepository.UpdateState(_jobStates.Values.ToList());
            }
        }
    }
}
