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

        // Initialisation des contrôles de chiffrement
        UpdateEncryptionKeyUI();
        UpdateEncryptionExtensionsUI();
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

        // Gestion clé de cryptage
        if (_controls.EditEncryptionKeyButton != null)
            _controls.EditEncryptionKeyButton.Click += EditEncryptionKeyButton_Click;
        // Gestion extensions à chiffrer
        if (_controls.AddExtensionButton != null)
            _controls.AddExtensionButton.Click += AddExtensionButton_Click;
        if (_controls.RemoveExtensionButton != null)
            _controls.RemoveExtensionButton.Click += RemoveExtensionButton_Click;
    }

    // Met à jour la clé de cryptage affichée
    private void UpdateEncryptionKeyUI()
    {
        if (_controls.EncryptionKeyTextBox != null)
            _controls.EncryptionKeyTextBox.Text = _viewModel.GetEncryptionKey();
    }

    // Met à jour la liste des extensions à chiffrer
    private void UpdateEncryptionExtensionsUI()
    {
        if (_controls.EncryptionExtensionsListBox != null)
        {
            _controls.EncryptionExtensionsListBox.ItemsSource = new ObservableCollection<string>(_viewModel.GetEncryptionExtensions());
        }
    }

    // Handler pour modifier la clé de cryptage
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

    // Handler pour ajouter une extension
    private void AddExtensionButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.AddExtensionTextBox == null || string.IsNullOrWhiteSpace(_controls.AddExtensionTextBox.Text))
            return;
        var ext = _controls.AddExtensionTextBox.Text.Trim();
        _viewModel.AddEncryptionExtension(ext);
        UpdateEncryptionExtensionsUI();
        _controls.AddExtensionTextBox.Text = string.Empty;
    }

    // Handler pour supprimer une extension sélectionnée
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
