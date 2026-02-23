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
    private readonly EncryptionHandler _encryptionHandler;
    private readonly ProcessPriorityHandler _processPriorityHandler;

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
        _uiService.SetJobHandler(_jobHandler);
        _fileSystemHandler = new FileSystemHandler(this, _controls, _viewModel, _uiService, _settingsService, logsPath, configPath, statePath);
        _encryptionHandler = new EncryptionHandler(this, _controls, _viewModel);
        _processPriorityHandler = new ProcessPriorityHandler(this, _controls, _viewModel, _settingsService);

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
        _encryptionHandler.Initialize();
        _processPriorityHandler.Initialize();
        LoadThreadingSettings(settings);
        LoadMiscSettings(settings);
    }

    private void LoadThreadingSettings(Dictionary<string, string> settings)
    {
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

    private void LoadMiscSettings(Dictionary<string, string> settings)
    {
        // Langue : appliquer avant de changer l'index pour éviter le double-déclenchement
        if (settings.TryGetValue("Language", out string? lang) && _controls.LanguageComboBox != null)
        {
            LocalizationManager.SetLanguage(lang);
            _controls.LanguageComboBox.SelectedIndex = lang == "en-US" ? 1 : 0;
        }

        // Cible des logs
        if (settings.TryGetValue("LogTarget", out string? logTarget) && _controls.LogTargetComboBox != null)
        {
            _viewModel.SetLogTarget(logTarget);
            _controls.LogTargetComboBox.SelectedIndex = logTarget switch { "Server" => 1, "Both" => 2, _ => 0 };
        }

        // IP serveur
        if (settings.TryGetValue("ServerIp", out string? serverIp) && !string.IsNullOrWhiteSpace(serverIp) && _controls.ServerIpTextBox != null)
        {
            _controls.ServerIpTextBox.Text = serverIp;
            _viewModel.SetServerIp(serverIp);
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

        // Pagination
        if (_controls.ItemsPerPageComboBox != null) _controls.ItemsPerPageComboBox.SelectionChanged += _jobHandler.ItemsPerPage_Changed;

        // File system navigation buttons
        if (this.FindControl<Button>("BrowseSourceButton") is Button bSrc) bSrc.Click += async (s, e) => await _fileSystemHandler.BrowseFolder("SourcePathTextBox");
        if (this.FindControl<Button>("BrowseTargetButton") is Button bTrg) bTrg.Click += async (s, e) => await _fileSystemHandler.BrowseFolder("TargetPathTextBox");
        if (this.FindControl<Button>("BrowseLogsButton") is Button bLog) bLog.Click += async (s, e) => await _fileSystemHandler.BrowseLogsFolder();
        if (this.FindControl<Button>("BrowseConfigButton") is Button bCfg) bCfg.Click += async (s, e) => await _fileSystemHandler.BrowseConfigFile();
        if (this.FindControl<Button>("BrowseStateButton") is Button bSt) bSt.Click += async (s, e) => await _fileSystemHandler.BrowseStateFile();

        // Global settings
        if (_controls.LanguageComboBox != null) _controls.LanguageComboBox.SelectionChanged += LanguageComboBox_SelectionChanged;
        if (_controls.LogTargetComboBox != null) _controls.LogTargetComboBox.SelectionChanged += LogTargetComboBox_SelectionChanged;

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

    private void OnBusinessProcessDetected(string processName)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            _uiService.UpdateStatus($"Sauvegarde interrompue : processus métier '{processName}' détecté !", false));
    }

    // --- Application settings ---

    private void LanguageComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedIndex >= 0)
        {
            string culture = cb.SelectedIndex == 1 ? "en-US" : "fr-FR";
            if (Thread.CurrentThread.CurrentUICulture.Name == culture) return;

            LocalizationManager.SetLanguage(culture);
            _settingsService.UpdateSetting("Language", culture);
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
            _settingsService.UpdateSetting("LogTarget", target);
            _uiService.UpdateStatus($"Cible des logs : {target}", true);
        }
    }

    private void SaveIpButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.ServerIpTextBox != null && !string.IsNullOrWhiteSpace(_controls.ServerIpTextBox.Text))
        {
            _viewModel.SetServerIp(_controls.ServerIpTextBox.Text);
            _settingsService.UpdateSetting("ServerIp", _controls.ServerIpTextBox.Text);
            _uiService.UpdateStatus($"IP Serveur mise à jour : {_controls.ServerIpTextBox.Text}", true);
        }
    }

    private void SaveThreadingSettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Validation et récupération des valeurs
            if (!TryParseMaxJobs(out int maxJobs) || !TryParseFileSizeThreshold(out int fileSizeThresholdMB))
                return;

            // 1. Appliquer immédiatement (sans attendre la sauvegarde)
            _viewModel.UpdateThreadingSettings(maxJobs, fileSizeThresholdMB);

            // 2. Sauvegarder chaque paramètre indépendamment via UpdateSetting
            _settingsService.UpdateSetting("MaxSimultaneousJobs", maxJobs.ToString());
            _settingsService.UpdateSetting("FileSizeThresholdMB", fileSizeThresholdMB.ToString());

            _uiService.UpdateStatus(
                $"Paramètres multi-threading appliqués : {maxJobs} travaux max, seuil {fileSizeThresholdMB} MB", true);
        }
        catch (Exception ex)
        {
            _uiService.UpdateStatus($"Erreur : {ex.Message}", false);
        }
    }

    private bool TryParseMaxJobs(out int maxJobs)
    {
        maxJobs = 3;
        if (_controls.MaxJobsTextBox?.Text is not string text || string.IsNullOrWhiteSpace(text))
            return true; // valeur par défaut acceptée

        if (int.TryParse(text, out int parsed) && parsed >= 1 && parsed <= 10)
        {
            maxJobs = parsed;
            return true;
        }
        _uiService.UpdateStatus("Erreur : Le nombre de travaux doit être entre 1 et 10", false);
        return false;
    }

    private bool TryParseFileSizeThreshold(out int fileSizeThresholdMB)
    {
        fileSizeThresholdMB = 10;
        if (_controls.FileSizeThresholdTextBox?.Text is not string text || string.IsNullOrWhiteSpace(text))
            return true; // valeur par défaut acceptée

        if (int.TryParse(text, out int parsed) && parsed >= 1)
        {
            fileSizeThresholdMB = parsed;
            return true;
        }
        _uiService.UpdateStatus("Erreur : Le seuil de taille doit être >= 1 MB", false);
        return false;
    }
}