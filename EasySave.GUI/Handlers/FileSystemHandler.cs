using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using EasySave.Core.ViewModels;
using EasySave.GUI.Helpers;
using EasySave.GUI.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EasySave.GUI.Handlers;

/// <summary>
/// Handles file and folder interactions for path selection and configuration in the EasySave GUI.
/// </summary>
public class FileSystemHandler
{
    private readonly Window _window;
    private readonly ControlCache _controls;
    private readonly ViewModelConsole _viewModel;
    private readonly UIUpdateService _uiService;
    private readonly SettingsService _settingsService;
    
    private string _currentLogsPath;
    private string _currentConfigPath;
    private string _currentStatePath;

    public string LogsPath => _currentLogsPath;
    public string ConfigPath => _currentConfigPath;
    public string StatePath => _currentStatePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemHandler"/> class.
    /// </summary>
    /// <param name="window">The main application window.</param>
    /// <param name="controls">The cached UI controls.</param>
    /// <param name="viewModel">The view model for job operations.</param>
    /// <param name="uiService">The UI update service.</param>
    /// <param name="settingsService">The settings service.</param>
    /// <param name="logsPath">The initial logs directory path.</param>
    /// <param name="configPath">The initial config file path.</param>
    /// <param name="statePath">The initial state file path.</param>
    public FileSystemHandler(Window window, ControlCache controls, ViewModelConsole viewModel, 
        UIUpdateService uiService, SettingsService settingsService,
        string logsPath, string configPath, string statePath)
    {
        _window = window;
        _controls = controls;
        _viewModel = viewModel;
        _uiService = uiService;
        _settingsService = settingsService;
        _currentLogsPath = logsPath;
        _currentConfigPath = configPath;
        _currentStatePath = statePath;
    }

    /// <summary>
    /// Opens a folder picker dialog and sets the selected path in the specified TextBox.
    /// </summary>
    /// <param name="textBoxName">The name of the TextBox to update.</param>
    public async Task BrowseFolder(string textBoxName)
    {
        var storageProvider = _window.StorageProvider;
        
        var options = new FolderPickerOpenOptions
        {
            Title = "Sélectionner un dossier",
            AllowMultiple = false
        };

        var result = await storageProvider.OpenFolderPickerAsync(options);
        
        if (result.Count > 0)
        {
            var textBox = _window.FindControl<TextBox>(textBoxName);
            if (textBox != null)
                textBox.Text = result[0].Path.LocalPath;
        }
    }

    /// <summary>
    /// Opens a folder picker dialog to select the logs directory and updates the configuration.
    /// </summary>
    public async Task BrowseLogsFolder()
    {
        var storageProvider = _window.StorageProvider;
        
        var options = new FolderPickerOpenOptions
        {
            Title = "Sélectionner le dossier pour les logs",
            AllowMultiple = false
        };

        var result = await storageProvider.OpenFolderPickerAsync(options);
        
        if (result.Count > 0)
        {
            _currentLogsPath = result[0].Path.LocalPath;
            
            if (!Directory.Exists(_currentLogsPath))
            {
                try
                {
                    Directory.CreateDirectory(_currentLogsPath);
                }
                catch (Exception ex)
                {
                    _uiService.UpdateStatus($"Erreur lors de la création du dossier : {ex.Message}", false);
                    return;
                }
            }
            
            SaveSettingsAndReload();
            _uiService.UpdateStatus($"Dossier des logs configuré : {_currentLogsPath}", true);
        }
    }
    
    /// <summary>
    /// Opens a file picker dialog to select the configuration file and updates the configuration.
    /// </summary>
    public async Task BrowseConfigFile()
    {
        var storageProvider = _window.StorageProvider;
        
        var options = new FilePickerSaveOptions
        {
            Title = "Sélectionner l'emplacement du fichier de configuration",
            SuggestedFileName = "jobs_config.json",
            DefaultExtension = "json"
        };

        var result = await storageProvider.SaveFilePickerAsync(options);
        
        if (result != null)
        {
            _currentConfigPath = result.Path.LocalPath;
            
            if (!CreateParentDirectory(_currentConfigPath))
                return;
            
            SaveSettingsAndReload();
            _uiService.UpdateStatus($"Fichier de configuration configuré : {_currentConfigPath}", true);
        }
    }
    
    /// <summary>
    /// Opens a file picker dialog to select the state file and updates the configuration.
    /// </summary>
    public async Task BrowseStateFile()
    {
        var storageProvider = _window.StorageProvider;
        
        var options = new FilePickerSaveOptions
        {
            Title = "Sélectionner l'emplacement du fichier d'état",
            SuggestedFileName = "state.json",
            DefaultExtension = "json"
        };

        var result = await storageProvider.SaveFilePickerAsync(options);
        
        if (result != null)
        {
            _currentStatePath = result.Path.LocalPath;
            
            if (!CreateParentDirectory(_currentStatePath))
                return;
            
            SaveSettingsAndReload();
            _uiService.UpdateStatus($"Fichier d'état configuré : {_currentStatePath}", true);
        }
    }

    private bool CreateParentDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                _uiService.UpdateStatus($"Erreur lors de la création du dossier : {ex.Message}", false);
                return false;
            }
        }
        return true;
    }

    private void SaveSettingsAndReload()
    {
        _settingsService.SaveSettings(_currentLogsPath, _currentConfigPath, _currentStatePath);
        _uiService.UpdatePaths(_currentLogsPath, _currentConfigPath, _currentStatePath);
        ReloadViewModel();
    }

    private void ReloadViewModel()
    {
        _viewModel.UpdateLogsPath(_currentLogsPath);
        _viewModel.UpdateConfigPath(_currentConfigPath);
        _viewModel.UpdateStatePath(_currentStatePath);
    }
}
