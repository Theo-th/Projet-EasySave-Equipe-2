using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
using Avalonia;

namespace EasySave.GUI;

/// <summary>
/// Main window of the EasySave application.
/// Refactored architecture with separation of concerns.
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

    /// <summary>
    /// Initializes the main window, services, controls, and event handlers.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // Initialize services
        _settingsService = new SettingsService();
        var settings = _settingsService.LoadSettings();

        // Load paths
        var logsPath = settings.ContainsKey("LogsPath") ? settings["LogsPath"] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        var configPath = settings.ContainsKey("ConfigPath") ? settings["ConfigPath"] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jobs_config.json");
        var statePath = settings.ContainsKey("StatePath") ? settings["StatePath"] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "state.json");

        // Initialize ViewModel
        _viewModel = new ViewModelConsole(LogType.JSON, configPath, statePath, logsPath);
        _jobs = new ObservableCollection<JobItem>();

        // Initialize helpers and handlers
        _controls = new ControlCache();
        _controls.InitializeFrom(this);

        _uiService = new UIUpdateService(this, _controls);
        _jobHandler = new JobEventHandler(this, _controls, _viewModel, _uiService, _jobs);
        _fileSystemHandler = new FileSystemHandler(this, _controls, _viewModel, _uiService, _settingsService, logsPath, configPath, statePath);

        // Subscribe to events
        _viewModel.OnProgressChanged += _jobHandler.OnBackupProgressChanged;
        _viewModel.OnProgressChanged += OnBackupStateChanged;
        _viewModel.OnBusinessProcessDetected += OnBusinessProcessDetected;

        // Initialize interface
        SetupEventHandlers();
        _jobHandler.LoadJobs();
        _uiService.UpdateAllTexts();
        _uiService.UpdatePaths(_fileSystemHandler.LogsPath, _fileSystemHandler.ConfigPath, _fileSystemHandler.StatePath);

        // Initialisation des contrôles de chiffrement
        UpdateEncryptionKeyUI();
        UpdateEncryptionExtensionsUI();

        // Initialisation des processus surveillés
        UpdateWatchedProcessesUI();
    }

    /// <summary>
    /// Sets up event handlers for UI controls and backup actions.
    /// </summary>
    private void SetupEventHandlers()
    {
        var executeButton = this.FindControl<Button>("ExecuteButton");
        var createJobButton = this.FindControl<Button>("CreateJobButton");
        var deleteJobButton = this.FindControl<Button>("DeleteJobButton");
        var viewDetailsButton = this.FindControl<Button>("ViewDetailsButton");
        var browseSourceButton = this.FindControl<Button>("BrowseSourceButton");
        var browseTargetButton = this.FindControl<Button>("BrowseTargetButton");
        var browseLogsButton = this.FindControl<Button>("BrowseLogsButton");
        var browseConfigButton = this.FindControl<Button>("BrowseConfigButton");
        var browseStateButton = this.FindControl<Button>("BrowseStateButton");

        if (executeButton != null)
            executeButton.Click += _jobHandler.ExecuteButton_Click;
        if (createJobButton != null)
            createJobButton.Click += _jobHandler.CreateJobButton_Click;
        if (deleteJobButton != null)
            deleteJobButton.Click += _jobHandler.DeleteJobButton_Click;
        if (viewDetailsButton != null)
            viewDetailsButton.Click += _jobHandler.ViewDetailsButton_Click;
        if (browseSourceButton != null)
            browseSourceButton.Click += async (s, e) => await _fileSystemHandler.BrowseFolder("SourcePathTextBox");
        if (browseTargetButton != null)
            browseTargetButton.Click += async (s, e) => await _fileSystemHandler.BrowseFolder("TargetPathTextBox");
        if (browseLogsButton != null)
            browseLogsButton.Click += async (s, e) => await _fileSystemHandler.BrowseLogsFolder();
        if (browseConfigButton != null)
            browseConfigButton.Click += async (s, e) => await _fileSystemHandler.BrowseConfigFile();
        if (browseStateButton != null)
            browseStateButton.Click += async (s, e) => await _fileSystemHandler.BrowseStateFile();
        if (_controls.LanguageComboBox != null)
            _controls.LanguageComboBox.SelectionChanged += LanguageComboBox_SelectionChanged;

        if (_controls.LogTargetComboBox != null)
            _controls.LogTargetComboBox.SelectionChanged += LogTargetComboBox_SelectionChanged;

        // Gestion clé de cryptage
        if (_controls.EditEncryptionKeyButton != null)
            _controls.EditEncryptionKeyButton.Click += EditEncryptionKeyButton_Click;
        // Gestion extensions à chiffrer
        if (_controls.AddExtensionButton != null)
            _controls.AddExtensionButton.Click += AddExtensionButton_Click;
        if (_controls.RemoveExtensionButton != null)
            _controls.RemoveExtensionButton.Click += RemoveExtensionButton_Click;

        // Gestion processus métier
        if (_controls.AddProcessButton != null)
            _controls.AddProcessButton.Click += AddProcessButton_Click;
        if (_controls.RemoveProcessButton != null)
            _controls.RemoveProcessButton.Click += RemoveProcessButton_Click;

        // Backup control buttons: Pause / Resume / Stop
        if (_controls.PauseButton != null)
            _controls.PauseButton.Click += PauseButton_Click;
        if (_controls.ResumeButton != null)
            _controls.ResumeButton.Click += ResumeButton_Click;
        if (_controls.StopButton != null)
            _controls.StopButton.Click += StopButton_Click;
    }

    /// <summary>
    /// Handler for pausing the current backup.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void PauseButton_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.PauseBackup();
        _uiService.UpdateStatus(EasySave.Core.Properties.Lang.StatusBackupPaused, true);
    }

    /// <summary>
    /// Handler for resuming a paused backup.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void ResumeButton_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.ResumeBackup();
        _uiService.UpdateStatus(EasySave.Core.Properties.Lang.StatusBackupResumed, true);
    }

    /// <summary>
    /// Handler for stopping the current backup.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void StopButton_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.StopBackup();
        _uiService.UpdateStatus(EasySave.Core.Properties.Lang.StatusBackupStopped, false);
    }

    /// <summary>
    /// Updates Pause/Resume button visibility according to backup state.
    /// </summary>
    /// <param name="state">Current backup job state.</param>
    private void OnBackupStateChanged(BackupJobState state)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (state.State == BackupState.Paused)
            {
                if (_controls.PauseButton != null) _controls.PauseButton.IsVisible = false;
                if (_controls.ResumeButton != null) _controls.ResumeButton.IsVisible = true;
            }
            else if (state.State == BackupState.Active)
            {
                if (_controls.PauseButton != null) _controls.PauseButton.IsVisible = true;
                if (_controls.ResumeButton != null) _controls.ResumeButton.IsVisible = false;
            }
            else if (state.State == BackupState.Completed || state.State == BackupState.Error)
            {
                // Réinitialiser les boutons
                if (_controls.PauseButton != null) _controls.PauseButton.IsVisible = true;
                if (_controls.ResumeButton != null) _controls.ResumeButton.IsVisible = false;
            }
        });
    }

    /// <summary>
    /// Updates the displayed encryption key in the UI.
    /// </summary>
    private void UpdateEncryptionKeyUI()
    {
        if (_controls.EncryptionKeyTextBox != null)
            _controls.EncryptionKeyTextBox.Text = _viewModel.GetEncryptionKey();
    }

    /// <summary>
    /// Updates the list of encryption extensions in the UI.
    /// </summary>
    private void UpdateEncryptionExtensionsUI()
    {
        if (_controls.EncryptionExtensionsListBox != null)
        {
            _controls.EncryptionExtensionsListBox.ItemsSource = new ObservableCollection<string>(_viewModel.GetEncryptionExtensions());
        }
    }

    /// <summary>
    /// Updates the list of watched business processes in the UI.
    /// </summary>
    private void UpdateWatchedProcessesUI()
    {
        if (_controls.WatchedProcessesListBox != null)
        {
            _controls.WatchedProcessesListBox.ItemsSource = new ObservableCollection<string>(_viewModel.GetWatchedProcesses());
        }
    }

    /// <summary>
    /// Handler called when a business process is detected during backup.
    /// </summary>
    /// <param name="processName">Name of the detected process.</param>
    private void OnBusinessProcessDetected(string processName)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _uiService.UpdateStatus($"Sauvegarde interrompue : processus métier '{processName}' détecté !", false);
        });
    }

    /// <summary>
    /// Handler for adding a business process to watch.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void AddProcessButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.AddProcessTextBox == null || string.IsNullOrWhiteSpace(_controls.AddProcessTextBox.Text))
            return;
        var processName = _controls.AddProcessTextBox.Text.Trim();
        _viewModel.AddWatchedProcess(processName);
        UpdateWatchedProcessesUI();
        _controls.AddProcessTextBox.Text = string.Empty;
    }

    /// <summary>
    /// Handler for removing a selected watched business process.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void RemoveProcessButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.WatchedProcessesListBox == null || _controls.WatchedProcessesListBox.SelectedItem == null)
            return;
        var processName = _controls.WatchedProcessesListBox.SelectedItem as string;
        if (!string.IsNullOrEmpty(processName))
        {
            _viewModel.RemoveWatchedProcess(processName);
            UpdateWatchedProcessesUI();
        }
    }

    /// <summary>
    /// Handler for editing the encryption key.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private async void EditEncryptionKeyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.EncryptionKeyTextBox == null)
            return;
        var dialog = new Window
        {
            Title = "Modifier la clé de cryptage",
            Width = 350,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Children =
                {
                    new TextBlock { Text = "Nouvelle clé :", Margin = new Avalonia.Thickness(0,0,0,8) },
                    new TextBox { Name = "NewKeyTextBox", Width = 200 },
                    new Button { Name = "ValidateKeyButton", Content = "Valider", Margin = new Avalonia.Thickness(0,12,0,0), Width = 80, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right }
                }
            }
        };
        var sp = (StackPanel)dialog.Content!;
        var newKeyBox = (TextBox)sp.Children[1];
        var validateBtn = (Button)sp.Children[2];
        validateBtn.Click += (s, ev) =>
        {
            _viewModel.SetEncryptionKey(newKeyBox.Text ?? "");
            UpdateEncryptionKeyUI();
            dialog.Close();
        };
        await dialog.ShowDialog(this);
    }

    /// <summary>
    /// Handler for adding an encryption extension.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void AddExtensionButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.AddExtensionTextBox == null || string.IsNullOrWhiteSpace(_controls.AddExtensionTextBox.Text))
            return;
        var ext = _controls.AddExtensionTextBox.Text.Trim();
        _viewModel.AddEncryptionExtension(ext);
        UpdateEncryptionExtensionsUI();
        _controls.AddExtensionTextBox.Text = string.Empty;
    }

    /// <summary>
    /// Handler for removing a selected encryption extension.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void RemoveExtensionButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.EncryptionExtensionsListBox == null || _controls.EncryptionExtensionsListBox.SelectedItem == null)
            return;
        var ext = _controls.EncryptionExtensionsListBox.SelectedItem as string;
        if (!string.IsNullOrEmpty(ext))
        {
            _viewModel.RemoveEncryptionExtension(ext);
            UpdateEncryptionExtensionsUI();
        }
    }

    /// <summary>
    /// Handler for changing the application language via ComboBox.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Selection changed event arguments.</param>
    private void LanguageComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedIndex >= 0)
        {
            string culture = comboBox.SelectedIndex switch
            {
                0 => "fr-FR",
                1 => "en-US",
                _ => "fr-FR"
            };

            if (Thread.CurrentThread.CurrentUICulture.Name == culture)
                return;

            LocalizationManager.SetLanguage(culture);
            _uiService.UpdateAllTexts();
            _jobHandler.LoadJobs();

            string message = Thread.CurrentThread.CurrentUICulture.Name == "fr-FR"
                ? "Langue changée avec succès"
                : "Language changed successfully";
            _uiService.UpdateStatus(message, true);
        }
    }

    /// <summary>
    /// Handler for changing the log target via ComboBox.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Selection changed event arguments.</param>
    private void LogTargetComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_controls.LogTargetComboBox != null && _controls.LogTargetComboBox.SelectedIndex >= 0)
        {
            var index = _controls.LogTargetComboBox.SelectedIndex;
            string target = index switch
            {
                0 => "Local",
                1 => "Server",
                2 => "Both",
                _ => "Both"
            };

            _viewModel.SetLogTarget(target);
            _uiService.UpdateStatus($"Cible des logs : {target}", true);
        }
    }
}