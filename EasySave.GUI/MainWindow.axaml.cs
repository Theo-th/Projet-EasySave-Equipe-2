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

namespace EasySave.GUI;

// Main window of the EasySave application
// Refactored architecture with separation of concerns
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
        
        // Initialize interface
        SetupEventHandlers();
        _jobHandler.LoadJobs();
        _uiService.UpdateAllTexts();
        _uiService.UpdatePaths(_fileSystemHandler.LogsPath, _fileSystemHandler.ConfigPath, _fileSystemHandler.StatePath);
    }

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
    }

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
}
