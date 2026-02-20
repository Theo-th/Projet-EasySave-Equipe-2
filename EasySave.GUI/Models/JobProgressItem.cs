using System.ComponentModel;

namespace EasySave.GUI.Models;

/// <summary>
/// Represents the progress state of a single backup job for UI display.
/// </summary>
public class JobProgressItem : INotifyPropertyChanged
{
    private string _jobName = string.Empty;
    private double _progress = 0;
    private string _progressText = "0%";
    private string _currentFile = string.Empty;
    private bool _isCompleted = false;

    public string JobName
    {
        get => _jobName;
        set
        {
            if (_jobName != value)
            {
                _jobName = value;
                OnPropertyChanged(nameof(JobName));
            }
        }
    }

    public double Progress
    {
        get => _progress;
        set
        {
            if (_progress != value)
            {
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }
    }

    public string ProgressText
    {
        get => _progressText;
        set
        {
            if (_progressText != value)
            {
                _progressText = value;
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    public string CurrentFile
    {
        get => _currentFile;
        set
        {
            if (_currentFile != value)
            {
                _currentFile = value;
                OnPropertyChanged(nameof(CurrentFile));
            }
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (_isCompleted != value)
            {
                _isCompleted = value;
                OnPropertyChanged(nameof(IsCompleted));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
