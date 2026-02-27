using System;
using System.ComponentModel;
using System.Windows.Input;
using EasySave.Core.Models;
using EasySave.Core.Properties;

namespace EasySave.GUI.Models;

/// <summary>
/// Represents the progress state of a single backup job for UI display.
/// Includes individual control commands (pause, resume, stop).
/// </summary>
public class JobProgressItem : INotifyPropertyChanged
{
    private string _jobName = string.Empty;
    private double _progress = 0;
    private string _progressText = "0%";
    private string _currentFile = string.Empty;
    private string _filesCountText = string.Empty;
    private BackupState _state = BackupState.Active;

    // ----------------------------------------------------------------
    //  Data properties
    // ----------------------------------------------------------------

    public string JobName
    {
        get => _jobName;
        set { if (_jobName != value) { _jobName = value; OnPropertyChanged(nameof(JobName)); } }
    }

    public double Progress
    {
        get => _progress;
        set { if (_progress != value) { _progress = value; OnPropertyChanged(nameof(Progress)); } }
    }

    public string ProgressText
    {
        get => _progressText;
        set { if (_progressText != value) { _progressText = value; OnPropertyChanged(nameof(ProgressText)); } }
    }

    public string CurrentFile
    {
        get => _currentFile;
        set { if (_currentFile != value) { _currentFile = value; OnPropertyChanged(nameof(CurrentFile)); } }
    }

    public string FilesCountText
    {
        get => _filesCountText;
        set { if (_filesCountText != value) { _filesCountText = value; OnPropertyChanged(nameof(FilesCountText)); } }
    }

    // ----------------------------------------------------------------
    //  State and calculated properties
    // ----------------------------------------------------------------

    public BackupState State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                OnPropertyChanged(nameof(State));
                OnPropertyChanged(nameof(IsActive));
                OnPropertyChanged(nameof(IsPaused));
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(IsError));
                OnPropertyChanged(nameof(IsStopped));
                OnPropertyChanged(nameof(CanControl));
            }
        }
    }

    public bool IsActive    => _state == BackupState.Active;
    public bool IsPaused    => _state == BackupState.Paused;
    public bool IsCompleted => _state == BackupState.Completed;
    public bool IsError     => _state == BackupState.Error;
    public bool IsStopped   => _state == BackupState.Inactive;
    public bool CanControl  => IsActive || IsPaused;

    // Localized text properties
    public string StatusInProgressText => Lang.StatusInProgress;
    public string StatusPausedText => Lang.StatusPaused;
    public string StatusCompletedText => Lang.StatusCompleted;
    public string StatusErrorText => Lang.StatusError;
    public string StatusStoppedText => Lang.StatusStopped;
    public string BtnPauseText => Lang.BtnPause;
    public string BtnResumeText => Lang.BtnResume;
    public string BtnStopText => Lang.BtnStopJob;

    // ----------------------------------------------------------------
    //  Control commands (bound in the AXAML DataTemplate)
    // ----------------------------------------------------------------

    public ICommand PauseCommand  { get; }
    public ICommand ResumeCommand { get; }
    public ICommand StopCommand   { get; }

    public JobProgressItem(Action pauseAction, Action resumeAction, Action stopAction)
    {
        PauseCommand  = new RelayCommand(pauseAction);
        ResumeCommand = new RelayCommand(resumeAction);
        StopCommand   = new RelayCommand(stopAction);
    }

    /// <summary>
    /// Refreshes all localized text properties.
    /// Call this when the language changes.
    /// </summary>
    public void RefreshLocalizedTexts()
    {
        OnPropertyChanged(nameof(StatusInProgressText));
        OnPropertyChanged(nameof(StatusPausedText));
        OnPropertyChanged(nameof(StatusCompletedText));
        OnPropertyChanged(nameof(StatusErrorText));
        OnPropertyChanged(nameof(StatusStoppedText));
        OnPropertyChanged(nameof(BtnPauseText));
        OnPropertyChanged(nameof(BtnResumeText));
        OnPropertyChanged(nameof(BtnStopText));
    }

    // ----------------------------------------------------------------

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
