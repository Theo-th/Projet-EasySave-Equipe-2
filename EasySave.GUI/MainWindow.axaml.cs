using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EasySave.Core.ViewModels;
using EasySave.Core.Models;
using EasySave.Core.Properties;
using EasySave.GUI.Helpers;
using EasySave.GUI.Services;
using EasySave.GUI.Handlers;
using EasySave.GUI.Models;
using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Threading;

namespace EasySave.GUI;

/// <summary>
/// Main window of the EasySave application.
/// Refactored architecture with separation of responsibilities.
/// </summary>
public partial class MainWindow : Window
{
    private readonly ViewModelConsole _viewModel;
    private readonly ObservableCollection<JobItem> _jobs;
    private readonly ControlCache _controls;
    private readonly UIUpdateService _uiService;
    private readonly SettingsService _settingsService;
    private readonly JobEventHandler _jobHandler;
    private readonly FileSystemHandler _fileSystemHandler;

    public MainWindow()
    {
        InitializeComponent();

        // 1. Initialization of services
        _settingsService = new SettingsService();
        var settings = _settingsService.LoadSettings();

        // 2. Loading configuration paths
        var logsPath = settings.ContainsKey("LogsPath") ? settings["LogsPath"] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        var configPath = settings.ContainsKey("ConfigPath") ? settings["ConfigPath"] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jobs_config.json");
        var statePath = settings.ContainsKey("StatePath") ? settings["StatePath"] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "state.json");

        // 3. Initializing the ViewModel and the collection
        _viewModel = new ViewModelConsole(LogType.JSON, configPath, statePath, logsPath);
        _jobs = new ObservableCollection<JobItem>();

        // 4. Initializing Helpers and Handlers
        _controls = new ControlCache();
        _controls.InitializeFrom(this);

        _uiService = new UIUpdateService(this, _controls);
        _jobHandler = new JobEventHandler(this, _controls, _viewModel, _uiService, _jobs);
        _fileSystemHandler = new FileSystemHandler(this, _controls, _viewModel, _uiService, _settingsService, logsPath, configPath, statePath);

        // 5. Event subscriptions
        _viewModel.OnProgressChanged += _jobHandler.OnBackupProgressChanged;
        _viewModel.OnProgressChanged += OnBackupStateChanged;
        _viewModel.OnBusinessProcessDetected += OnBusinessProcessDetected;

        // 6. Initializing the interface
        SetupEventHandlers();
        _jobHandler.LoadJobs();
        _uiService.UpdateAllTexts();
        _uiService.UpdatePaths(_fileSystemHandler.LogsPath, _fileSystemHandler.ConfigPath, _fileSystemHandler.StatePath);

        // 7. Initialization of specific controls (Encryption and Process)
        UpdateEncryptionKeyUI();
        UpdateEncryptionExtensionsUI();
        UpdateWatchedProcessesUI();
    }

    private void SetupEventHandlers()
    {
        // Job management buttons
        if (this.FindControl<Button>("ExecuteButton") is Button execBtn) execBtn.Click += _jobHandler.ExecuteButton_Click;
        if (this.FindControl<Button>("CreateJobButton") is Button createBtn) createBtn.Click += _jobHandler.CreateJobButton_Click;
        if (this.FindControl<Button>("DeleteJobButton") is Button deleteBtn) deleteBtn.Click += _jobHandler.DeleteJobButton_Click;
        if (this.FindControl<Button>("ViewDetailsButton") is Button detailsBtn) detailsBtn.Click += _jobHandler.ViewDetailsButton_Click;

        // File system navigation buttons
        if (this.FindControl<Button>("BrowseSourceButton") is Button bSrc) bSrc.Click += async (s, e) => await _fileSystemHandler.BrowseFolder("SourcePathTextBox");
        if (this.FindControl<Button>("BrowseTargetButton") is Button bTrg) bTrg.Click += async (s, e) => await _fileSystemHandler.BrowseFolder("TargetPathTextBox");
        if (this.FindControl<Button>("BrowseLogsButton") is Button bLog) bLog.Click += async (s, e) => await _fileSystemHandler.BrowseLogsFolder();
        if (this.FindControl<Button>("BrowseConfigButton") is Button bCfg) bCfg.Click += async (s, e) => await _fileSystemHandler.BrowseConfigFile();
        if (this.FindControl<Button>("BrowseStateButton") is Button bSt) bSt.Click += async (s, e) => await _fileSystemHandler.BrowseStateFile();

        // Global settings
        if (_controls.LanguageComboBox != null) _controls.LanguageComboBox.SelectionChanged += LanguageComboBox_SelectionChanged;
        if (_controls.LogTargetComboBox != null) _controls.LogTargetComboBox.SelectionChanged += LogTargetComboBox_SelectionChanged;

        // Encryption
        if (_controls.EditEncryptionKeyButton != null) _controls.EditEncryptionKeyButton.Click += EditEncryptionKeyButton_Click;
        if (_controls.AddExtensionButton != null) _controls.AddExtensionButton.Click += AddExtensionButton_Click;
        if (_controls.RemoveExtensionButton != null) _controls.RemoveExtensionButton.Click += RemoveExtensionButton_Click;

        // Business process
        if (_controls.AddProcessButton != null) _controls.AddProcessButton.Click += AddProcessButton_Click;
        if (_controls.RemoveProcessButton != null) _controls.RemoveProcessButton.Click += RemoveProcessButton_Click;

        // Backup flow controls
        if (_controls.PauseButton != null) _controls.PauseButton.Click += PauseButton_Click;
        if (_controls.ResumeButton != null) _controls.ResumeButton.Click += ResumeButton_Click;
        if (_controls.StopButton != null) _controls.StopButton.Click += StopButton_Click;

        // Choice Log Format (XML, JSON)
        if (_controls.LogFormatComboBox != null) _controls.LogFormatComboBox.SelectionChanged += LogFormatComboBox_SelectionChanged;
    }

    // --- Backup Control Event Handlers ---

    private void PauseButton_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.PauseBackup();
        _uiService.UpdateStatus(Lang.StatusBackupPaused, true);
    }

    private void ResumeButton_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.ResumeBackup();
        _uiService.UpdateStatus(Lang.StatusBackupResumed, true);
    }

    private void StopButton_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.StopBackup();
        _uiService.UpdateStatus(Lang.StatusBackupStopped, false);
    }

    private void OnBackupStateChanged(BackupJobState state)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            bool isPaused = state.State == BackupState.Paused;
            bool isActive = state.State == BackupState.Active;

            if (_controls.PauseButton != null) _controls.PauseButton.IsVisible = !isPaused;
            if (_controls.ResumeButton != null) _controls.ResumeButton.IsVisible = isPaused;
        });
    }

    // --- Encryption and Process Management ---

    private void UpdateEncryptionKeyUI() => _controls.EncryptionKeyTextBox!.Text = _viewModel.GetEncryptionKey();

    private void UpdateEncryptionExtensionsUI() =>
        _controls.EncryptionExtensionsListBox!.ItemsSource = new ObservableCollection<string>(_viewModel.GetEncryptionExtensions());

    private void UpdateWatchedProcessesUI() =>
        _controls.WatchedProcessesListBox!.ItemsSource = new ObservableCollection<string>(_viewModel.GetWatchedProcesses());

    private void OnBusinessProcessDetected(string processName)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            _uiService.UpdateStatus($"Sauvegarde interrompue : processus métier '{processName}' détecté !", false));
    }

    private void AddProcessButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_controls.AddProcessTextBox?.Text)) return;
        _viewModel.AddWatchedProcess(_controls.AddProcessTextBox.Text.Trim());
        UpdateWatchedProcessesUI();
        _controls.AddProcessTextBox.Text = string.Empty;
    }

    private void RemoveProcessButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.WatchedProcessesListBox?.SelectedItem is string processName)
        {
            _viewModel.RemoveWatchedProcess(processName);
            UpdateWatchedProcessesUI();
        }
    }

    private async void EditEncryptionKeyButton_Click(object? sender, RoutedEventArgs e)
    {
        // Quick dialog for the key
        var newKeyBox = new TextBox { Width = 200 };
        var validateBtn = new Button { Content = "Valider", Margin = new Thickness(0, 10, 0, 0) };
        var dialog = new Window
        {
            Title = "Clé de cryptage",
            Width = 250,
            Height = 120,
            Content = new StackPanel { Margin = new Thickness(10), Children = { newKeyBox, validateBtn } }
        };
        validateBtn.Click += (s, ev) => { _viewModel.SetEncryptionKey(newKeyBox.Text ?? ""); UpdateEncryptionKeyUI(); dialog.Close(); };
        await dialog.ShowDialog(this);
    }

    private void AddExtensionButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_controls.AddExtensionTextBox?.Text)) return;
        _viewModel.AddEncryptionExtension(_controls.AddExtensionTextBox.Text.Trim());
        UpdateEncryptionExtensionsUI();
        _controls.AddExtensionTextBox.Text = string.Empty;
    }

    private void RemoveExtensionButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.EncryptionExtensionsListBox?.SelectedItem is string ext)
        {
            _viewModel.RemoveEncryptionExtension(ext);
            UpdateEncryptionExtensionsUI();
        }
    }

    // --- Application settings ---

    private void LanguageComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedIndex >= 0)
        {
            string culture = cb.SelectedIndex == 1 ? "en-US" : "fr-FR";
            if (Thread.CurrentThread.CurrentUICulture.Name == culture) return;

            LocalizationManager.SetLanguage(culture);
            _uiService.UpdateAllTexts();
            _jobHandler.LoadJobs();
        }
    }

    private void LogTargetComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_controls.LogTargetComboBox?.SelectedIndex is int idx and >= 0)
        {
            string target = idx switch { 0 => "Local", 1 => "Server", _ => "Both" };
            _viewModel.SetLogTarget(target);
            _uiService.UpdateStatus($"Cible des logs : {target}", true);
        }
    }

    private void LogFormatComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_controls.LogFormatComboBox?.SelectedIndex is int idx and >= 0)
        {
            string format = idx == 0 ? "JSON" : "XML";
            _viewModel.ChangeLogFormat(format);
            _uiService.UpdateStatus($"Format des logs modifié : {format}", true);
        }
    }
}