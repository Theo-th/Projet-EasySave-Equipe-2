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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        
        // Loading multi-threading settings
        int maxJobs = 3;
        int fileSizeThresholdMB = 10;
        List<string>? priorityExtensions = null;
        
        if (settings.ContainsKey("MaxSimultaneousJobs") && int.TryParse(settings["MaxSimultaneousJobs"], out int parsedMaxJobs))
            maxJobs = parsedMaxJobs;
        if (settings.ContainsKey("FileSizeThresholdMB") && int.TryParse(settings["FileSizeThresholdMB"], out int parsedThreshold))
            fileSizeThresholdMB = parsedThreshold;
        if (settings.ContainsKey("PriorityExtensions"))
            priorityExtensions = settings["PriorityExtensions"].Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        // 3. Initializing the ViewModel and the collection
        _viewModel = new ViewModelConsole(LogType.JSON, configPath, statePath, logsPath, maxJobs, fileSizeThresholdMB, priorityExtensions);
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
        UpdatePriorityExtensionsUI();
        UpdateWatchedProcessesUI();
        LoadThreadingSettings(settings);
    }

    private void LoadThreadingSettings(Dictionary<string, string> settings)
    {
        // Charger les paramètres multi-threading depuis les settings
        if (_controls.MaxJobsTextBox != null)
        {
            _controls.MaxJobsTextBox.Text = settings.ContainsKey("MaxSimultaneousJobs") 
                ? settings["MaxSimultaneousJobs"] 
                : "3";
        }
        
        if (_controls.FileSizeThresholdTextBox != null)
        {
            _controls.FileSizeThresholdTextBox.Text = settings.ContainsKey("FileSizeThresholdMB") 
                ? settings["FileSizeThresholdMB"] 
                : "10";
        }
    }

    private void SetupEventHandlers()
    {
        // Navigation sidebar
        if (this.FindControl<Button>("PathsNavButton") is Button pathsNav) pathsNav.Click += (s, e) => ShowSection("Paths");
        if (this.FindControl<Button>("SecurityNavButton") is Button secNav) secNav.Click += (s, e) => ShowSection("Security");
        if (this.FindControl<Button>("ThreadingNavButton") is Button thrNav) thrNav.Click += (s, e) => ShowSection("Threading");
        if (this.FindControl<Button>("LogsNavButton") is Button logsNav) logsNav.Click += (s, e) => ShowSection("Logs");

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

        // Priority extensions
        if (_controls.AddPriorityExtensionButton != null) _controls.AddPriorityExtensionButton.Click += AddPriorityExtensionButton_Click;
        if (_controls.RemovePriorityExtensionButton != null) _controls.RemovePriorityExtensionButton.Click += RemovePriorityExtensionButton_Click;

        // Business process
        if (_controls.AddProcessButton != null) _controls.AddProcessButton.Click += AddProcessButton_Click;
        if (_controls.RemoveProcessButton != null) _controls.RemoveProcessButton.Click += RemoveProcessButton_Click;

        // Multi-threading settings
        if (_controls.SaveThreadingSettingsButton != null) _controls.SaveThreadingSettingsButton.Click += SaveThreadingSettingsButton_Click;

        // Backup flow controls
        if (_controls.PauseButton != null) _controls.PauseButton.Click += PauseButton_Click;
        if (_controls.ResumeButton != null) _controls.ResumeButton.Click += ResumeButton_Click;
        if (_controls.StopButton != null) _controls.StopButton.Click += StopButton_Click;

        if (_controls.SaveIpButton != null) _controls.SaveIpButton.Click += SaveIpButton_Click;
        
        // Initialize first section visible
        ShowSection("Paths");
    }

    private void ShowSection(string sectionName)
    {
        // Hide all sections
        if (this.FindControl<StackPanel>("PathsSection") is StackPanel pathsSec) pathsSec.IsVisible = false;
        if (this.FindControl<StackPanel>("SecuritySection") is StackPanel secSec) secSec.IsVisible = false;
        if (this.FindControl<StackPanel>("ThreadingSection") is StackPanel thrSec) thrSec.IsVisible = false;
        if (this.FindControl<StackPanel>("LogsSection") is StackPanel logsSec) logsSec.IsVisible = false;

        // Remove active class from all buttons
        if (this.FindControl<Button>("PathsNavButton") is Button pathsNav) pathsNav.Classes.Remove("active");
        if (this.FindControl<Button>("SecurityNavButton") is Button secNav) secNav.Classes.Remove("active");
        if (this.FindControl<Button>("ThreadingNavButton") is Button thrNav) thrNav.Classes.Remove("active");
        if (this.FindControl<Button>("LogsNavButton") is Button logsNav) logsNav.Classes.Remove("active");

        // Show selected section and mark button as active
        switch (sectionName)
        {
            case "Paths":
                if (this.FindControl<StackPanel>("PathsSection") is StackPanel ps) ps.IsVisible = true;
                if (this.FindControl<Button>("PathsNavButton") is Button pn) pn.Classes.Add("active");
                break;
            case "Security":
                if (this.FindControl<StackPanel>("SecuritySection") is StackPanel ss) ss.IsVisible = true;
                if (this.FindControl<Button>("SecurityNavButton") is Button sn) sn.Classes.Add("active");
                break;
            case "Threading":
                if (this.FindControl<StackPanel>("ThreadingSection") is StackPanel ts) ts.IsVisible = true;
                if (this.FindControl<Button>("ThreadingNavButton") is Button tn) tn.Classes.Add("active");
                break;
            case "Logs":
                if (this.FindControl<StackPanel>("LogsSection") is StackPanel ls) ls.IsVisible = true;
                if (this.FindControl<Button>("LogsNavButton") is Button ln) ln.Classes.Add("active");
                break;
        }
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

    private void UpdatePriorityExtensionsUI() =>
        _controls.PriorityExtensionsListBox!.ItemsSource = new ObservableCollection<string>(_viewModel.GetPriorityExtensions());

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

    private void AddPriorityExtensionButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.AddPriorityExtensionTextBox?.Text is string text && !string.IsNullOrWhiteSpace(text))
        {
            string extension = text.Trim();
            if (!extension.StartsWith(".")) extension = "." + extension;
            
            var currentExtensions = _viewModel.GetPriorityExtensions();
            if (!currentExtensions.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)))
            {
                currentExtensions.Add(extension);
                _viewModel.UpdatePriorityExtensions(currentExtensions);
                UpdatePriorityExtensionsUI();
                SavePriorityExtensionsToSettings();
                _controls.AddPriorityExtensionTextBox.Text = string.Empty;
            }
        }
    }

    private void RemovePriorityExtensionButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.PriorityExtensionsListBox?.SelectedItem is string ext)
        {
            var currentExtensions = _viewModel.GetPriorityExtensions();
            currentExtensions.Remove(ext);
            _viewModel.UpdatePriorityExtensions(currentExtensions);
            UpdatePriorityExtensionsUI();
            SavePriorityExtensionsToSettings();
        }
    }

    private void SavePriorityExtensionsToSettings()
    {
        var extensions = _viewModel.GetPriorityExtensions();
        var logsPath = _fileSystemHandler.LogsPath;
        var configPath = _fileSystemHandler.ConfigPath;
        var statePath = _fileSystemHandler.StatePath;
        
        // Load current settings
        var settings = _settingsService.LoadSettings();
        settings["PriorityExtensions"] = string.Join(",", extensions);
        
        // Save with priority extensions
        var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json"), json);
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

    private void SaveIpButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.ServerIpTextBox != null && !string.IsNullOrWhiteSpace(_controls.ServerIpTextBox.Text))
        {
            _viewModel.SetServerIp(_controls.ServerIpTextBox.Text);
            _uiService.UpdateStatus($"IP Serveur mise à jour : {_controls.ServerIpTextBox.Text}", true);
        }
    }

    private void SaveThreadingSettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Validation et récupération des valeurs
            int maxJobs = 3; // Valeur par défaut
            int fileSizeThresholdMB = 10; // Valeur par défaut

            if (_controls.MaxJobsTextBox?.Text is string maxJobsText && !string.IsNullOrWhiteSpace(maxJobsText))
            {
                if (int.TryParse(maxJobsText, out int parsed) && parsed >= 1 && parsed <= 10)
                {
                    maxJobs = parsed;
                }
                else
                {
                    _uiService.UpdateStatus("Erreur: Le nombre de travaux doit être entre 1 et 10", false);
                    return;
                }
            }

            if (_controls.FileSizeThresholdTextBox?.Text is string thresholdText && !string.IsNullOrWhiteSpace(thresholdText))
            {
                if (int.TryParse(thresholdText, out int parsed) && parsed >= 1)
                {
                    fileSizeThresholdMB = parsed;
                }
                else
                {
                    _uiService.UpdateStatus("Erreur: Le seuil de taille doit être >= 1 MB", false);
                    return;
                }
            }

            // Sauvegarde des paramètres
            var logsPath = _fileSystemHandler.LogsPath;
            var configPath = _fileSystemHandler.ConfigPath;
            var statePath = _fileSystemHandler.StatePath;
            
            bool success = _settingsService.SaveSettings(logsPath, configPath, statePath, maxJobs, fileSizeThresholdMB);
            
            if (success)
            {
                // Appliquer immédiatement les nouveaux paramètres
                _viewModel.UpdateThreadingSettings(maxJobs, fileSizeThresholdMB);
                _uiService.UpdateStatus($"Paramètres multi-threading appliqués: {maxJobs} jobs max, seuil {fileSizeThresholdMB} MB", true);
            }
            else
            {
                _uiService.UpdateStatus("Erreur lors de la sauvegarde des paramètres", false);
            }
        }
        catch (Exception ex)
        {
            _uiService.UpdateStatus($"Erreur: {ex.Message}", false);
        }
    }
}