using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using Avalonia.Layout;
using EasySave.Core.ViewModels;
using EasySave.Core.Models;
using EasySave.Core.Properties;
using EasySave.GUI.Services;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Globalization;
using System.Threading;
using System.Text.Json;

namespace EasySave.GUI;

public partial class MainWindow : Window
{
    private ViewModelConsole _viewModel;
    private ObservableCollection<JobItem> _jobs;
    private ProgressMonitorService? _progressMonitor;

    // Cache des contrôles pour optimisation
    private ListBox? _jobListBox;
    private ListBox? _manageJobListBox;
    private TextBlock? _statusText;
    private TextBlock? _footerText;
    private TextBlock? _headerStatus;
    private Border? _progressArea;
    private ComboBox? _typeComboBox;
    private ComboBox? _languageComboBox;
    private TextBlock? _logsPathValueText;
    private TextBox? _logsPathTextBox;
    private TextBlock? _configPathValueText;
    private TextBox? _configPathTextBox;
    private TextBlock? _statePathValueText;
    private TextBox? _statePathTextBox;
    private ProgressBar? _progressBar;
    private TextBlock? _progressText;
    private TextBlock? _currentFileText;
    private TextBlock? _itemsCountText;
    private TextBlock? _pageInfoText;
    private TextBlock? _currentPageText;
    private Button? _previousPageButton;
    private Button? _nextPageButton;
    private ComboBox? _itemsPerPageComboBox;

    private string _currentLogsPath;
    private string _currentConfigPath;
    private string _currentStatePath;

    public MainWindow()
    {
        InitializeComponent();
        
        // Charger les chemins sauvegardés ou utiliser les défauts
        var settings = LoadSettings();
        _currentLogsPath = settings.GetValueOrDefault("LogsPath", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs"));
        _currentConfigPath = settings.GetValueOrDefault("ConfigPath", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jobs_config.json"));
        _currentStatePath = settings.GetValueOrDefault("StatePath", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "state.json"));
        
        _viewModel = new ViewModelConsole(customLogPath: _currentLogsPath, customConfigPath: _currentConfigPath, customStatePath: _currentStatePath);
        _jobs = new ObservableCollection<JobItem>();
        
        // Initialiser le cache des contrôles
        CacheControls();
        
        LoadJobs();
        SetupEventHandlers();
        UpdateAllTexts();
        UpdateAllPaths();
    }

    private void CacheControls()
    {
        _jobListBox = this.FindControl<ListBox>("JobListBox");
        _manageJobListBox = this.FindControl<ListBox>("ManageJobListBox");
        _statusText = this.FindControl<TextBlock>("StatusText");
        _footerText = this.FindControl<TextBlock>("FooterText");
        _headerStatus = this.FindControl<TextBlock>("HeaderStatusText");
        _progressArea = this.FindControl<Border>("ProgressArea");
        _typeComboBox = this.FindControl<ComboBox>("TypeComboBox");
        _languageComboBox = this.FindControl<ComboBox>("LanguageComboBox");
        _logsPathValueText = this.FindControl<TextBlock>("LogsPathValueText");
        _logsPathTextBox = this.FindControl<TextBox>("LogsPathTextBox");
        _configPathValueText = this.FindControl<TextBlock>("ConfigPathValueText");
        _configPathTextBox = this.FindControl<TextBox>("ConfigPathTextBox");
        _statePathValueText = this.FindControl<TextBlock>("StatePathValueText");
        _statePathTextBox = this.FindControl<TextBox>("StatePathTextBox");
        _progressBar = this.FindControl<ProgressBar>("ProgressBar");
        _progressText = this.FindControl<TextBlock>("ProgressText");
        _currentFileText = this.FindControl<TextBlock>("CurrentFileText");
        _itemsCountText = this.FindControl<TextBlock>("ItemsCountText");
        _pageInfoText = this.FindControl<TextBlock>("PageInfoText");
        _currentPageText = this.FindControl<TextBlock>("CurrentPageText");
        _previousPageButton = this.FindControl<Button>("PreviousPageButton");
        _nextPageButton = this.FindControl<Button>("NextPageButton");
        _itemsPerPageComboBox = this.FindControl<ComboBox>("ItemsPerPageComboBox");
    }

    private void LoadJobs()
    {
        _jobs.Clear();
        var jobNames = _viewModel.GetAllJobs();
        var jobCount = jobNames.Count;
        
        // Pré-allouer une liste pour les noms de jobs
        var jobNamesList = new List<string>(jobCount);
        
        for (int i = 0; i < jobCount; i++)
        {
            var job = _viewModel.GetJobDetails(i);
            if (job != null)
            {
                _jobs.Add(new JobItem 
                { 
                    Name = job.Name,
                    Index = i,
                    IsSelected = false,
                    Type = job.Type.ToString(),
                    Source = job.SourceDirectory,
                    Target = job.TargetDirectory
                });
                jobNamesList.Add(job.Name);
            }
        }
        
        // Utiliser le cache des contrôles
        if (_jobListBox != null)
            _jobListBox.ItemsSource = jobNamesList;
            
        if (_manageJobListBox != null)
            _manageJobListBox.ItemsSource = jobNamesList;
            
        // Mettre à jour le compteur
        UpdateJobsCount();
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

        if (executeButton != null)
            executeButton.Click += ExecuteButton_Click;
            
        if (createJobButton != null)
            createJobButton.Click += CreateJobButton_Click;
            
        if (deleteJobButton != null)
            deleteJobButton.Click += DeleteJobButton_Click;
            
        if (viewDetailsButton != null)
            viewDetailsButton.Click += ViewDetailsButton_Click;
            
        if (browseSourceButton != null)
            browseSourceButton.Click += async (s, e) => await BrowseFolder("SourcePathTextBox");
            
        if (browseTargetButton != null)
            browseTargetButton.Click += async (s, e) => await BrowseFolder("TargetPathTextBox");
            
        if (browseLogsButton != null)
            browseLogsButton.Click += async (s, e) => await BrowseLogsFolder();
            
        var browseConfigButton = this.FindControl<Button>("BrowseConfigButton");
        if (browseConfigButton != null)
            browseConfigButton.Click += async (s, e) => await BrowseConfigFile();
            
        var browseStateButton = this.FindControl<Button>("BrowseStateButton");
        if (browseStateButton != null)
            browseStateButton.Click += async (s, e) => await BrowseStateFile();
            
        if (_languageComboBox != null)
            _languageComboBox.SelectionChanged += LanguageComboBox_SelectionChanged;
    }

    private async void ExecuteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_jobListBox?.SelectedItems == null || _jobListBox.SelectedItems.Count == 0)
        {
            UpdateStatus("Aucun travail sélectionné", false);
            return;
        }

        var selectedCount = _jobListBox.SelectedItems.Count;
        var selectedIndices = new List<int>(selectedCount);
        
        // Créer un dictionnaire pour éviter les recherches répétées
        var jobDict = new Dictionary<string, int>(_jobs.Count);
        for (int i = 0; i < _jobs.Count; i++)
        {
            jobDict[_jobs[i].Name] = _jobs[i].Index;
        }
        
        foreach (var item in _jobListBox.SelectedItems)
        {
            var name = item?.ToString();
            if (name != null && jobDict.TryGetValue(name, out var index))
            {
                selectedIndices.Add(index);
            }
        }
        
        if (selectedIndices.Count == 0)
        {
            UpdateStatus("Aucun travail valide sélectionné", false);
            return;
        }
        
        UpdateStatus($"Exécution de {selectedIndices.Count} sauvegarde(s)...", true);
        ShowProgress(true);
        
        // Démarrer la surveillance de progression
        _progressMonitor = new ProgressMonitorService(_currentStatePath, UpdateProgressUI);
        _progressMonitor.Start();
        
        // Exécuter les sauvegardes dans un thread séparé
        await System.Threading.Tasks.Task.Run(() =>
        {
            foreach (var index in selectedIndices)
            {
                var result = _viewModel.ExecuteJob(index);
                if (result != null)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        _progressMonitor?.Stop();
                        ShowProgress(false);
                        UpdateStatus($"Erreur: {result}", false);
                    });
                    return;
                }
            }
        });
        
        _progressMonitor?.Stop();
        ShowProgress(false);
        UpdateStatus($"{selectedIndices.Count} sauvegarde(s) terminée(s) !", true);
    }

    private void CreateJobButton_Click(object? sender, RoutedEventArgs e)
    {
        var nameBox = this.FindControl<TextBox>("JobNameTextBox");
        var sourceBox = this.FindControl<TextBox>("SourcePathTextBox");
        var targetBox = this.FindControl<TextBox>("TargetPathTextBox");
        var typeBox = this.FindControl<ComboBox>("TypeComboBox");

        if (nameBox == null || sourceBox == null || targetBox == null || typeBox == null)
            return;

        var name = nameBox.Text;
        var source = sourceBox.Text;
        var target = targetBox.Text;
        var typeIndex = typeBox.SelectedIndex;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            UpdateStatus("Veuillez remplir tous les champs", false);
            return;
        }

        var type = typeIndex == 1 ? BackupType.Differential : BackupType.Complete;
        var result = _viewModel.CreateJob(name, source, target, type);
        
        if (!result.Success)
        {
            UpdateStatus($"Erreur: {result.ErrorMessage}", false);
            return;
        }
        
        // Clear fields
        nameBox.Text = "";
        sourceBox.Text = "";
        targetBox.Text = "";
        typeBox.SelectedIndex = 0;
        
        LoadJobs();
        UpdateStatus($"Plan '{name}' créé avec succès", true);
    }

    private void DeleteJobButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_manageJobListBox?.SelectedIndex >= 0 && _manageJobListBox.SelectedIndex < _jobs.Count)
        {
            var jobIndex = _manageJobListBox.SelectedIndex;
            var jobName = _jobs[jobIndex].Name;
            var success = _viewModel.DeleteJob(jobIndex);
            
            if (success)
            {
                LoadJobs();
                UpdateStatus($"Plan '{jobName}' supprimé", true);
            }
            else
            {
                UpdateStatus("Erreur lors de la suppression", false);
            }
        }
        else
        {
            UpdateStatus("Veuillez sélectionner un plan à supprimer", false);
        }
    }

    private async System.Threading.Tasks.Task BrowseFolder(string textBoxName)
    {
        var storageProvider = StorageProvider;
        
        var options = new FolderPickerOpenOptions
        {
            Title = "Sélectionner un dossier",
            AllowMultiple = false
        };

        var result = await storageProvider.OpenFolderPickerAsync(options);
        
        if (result.Count > 0)
        {
            var textBox = this.FindControl<TextBox>(textBoxName);
            if (textBox != null)
                textBox.Text = result[0].Path.LocalPath;
        }
    }

    private void UpdateStatus(string message, bool isSuccess)
    {
        if (_statusText != null)
        {
            _statusText.Text = message;
            _statusText.Foreground = isSuccess ? Avalonia.Media.Brushes.Green : Avalonia.Media.Brushes.Red;
        }
        
        if (_footerText != null)
            _footerText.Text = message;
            
        if (_headerStatus != null)
            _headerStatus.Text = isSuccess ? "✓ En cours" : "⚠ Attention";
    }
    
    private void UpdateJobsCount()
    {
        var count = _jobs.Count;
        var text = count == 0 ? "Aucune sauvegarde" : count == 1 ? "1 sauvegarde" : $"{count} sauvegardes";
        
        if (_itemsCountText != null)
            _itemsCountText.Text = text;
    }
    
    private void ShowProgress(bool show)
    {
        if (_progressArea != null)
            _progressArea.IsVisible = show;
            
        if (!show)
        {
            // Réinitialiser la barre de progression
            if (_progressBar != null)
                _progressBar.Value = 0;
            if (_progressText != null)
                _progressText.Text = "0%";
            if (_currentFileText != null)
                _currentFileText.Text = "";
        }
    }
    
    private void UpdateProgressUI(double progress, string currentFile)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_progressBar != null)
                _progressBar.Value = Math.Min(100, Math.Max(0, progress));
                
            if (_progressText != null)
                _progressText.Text = $"{(int)progress}%";
                
            if (_currentFileText != null && !string.IsNullOrEmpty(currentFile))
            {
                string fileName = Path.GetFileName(currentFile);
                _currentFileText.Text = $"Fichier en cours : {fileName}";
            }
        });
    }
    
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "o", "Ko", "Mo", "Go", "To" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
    
    private async void ViewDetailsButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_manageJobListBox?.SelectedItem != null && _manageJobListBox.SelectedIndex >= 0 && _manageJobListBox.SelectedIndex < _jobs.Count)
        {
            // Accès direct par index au lieu de recherche LINQ
            var job = _jobs[_manageJobListBox.SelectedIndex];
            await ShowJobDetailsDialog(job);
        }
        else
        {
            UpdateStatus("⚠️ Veuillez sélectionner un plan pour voir les détails", false);
        }
    }

    private async System.Threading.Tasks.Task ShowJobDetailsDialog(JobItem job)
    {
        var dialog = new Window
        {
            Title = $"Détails - {job.Name}",
            Width = 600,
            Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var scrollViewer = new ScrollViewer
        {
            Padding = new Thickness(30),
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var panel = new StackPanel { Spacing = 20 };

        // Titre
        panel.Children.Add(new TextBlock
        {
            Text = $"📋 Détails du plan de sauvegarde",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#1F2937")),
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Nom
        panel.Children.Add(CreateDetailBlock("Nom", job.Name));

        // Type
        panel.Children.Add(CreateDetailBlock("Type", job.Type));

        // Source
        panel.Children.Add(CreateDetailBlock("Chemin source", job.Source));

        // Destination
        panel.Children.Add(CreateDetailBlock("Chemin de destination", job.Target));

        // Bouton Fermer
        var closeButton = new Button
        {
            Content = "Fermer",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0),
            Padding = new Thickness(40, 12),
            Background = new SolidColorBrush(Color.Parse("#3B82F6")),
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(6),
            FontSize = 14,
            FontWeight = FontWeight.SemiBold
        };
        closeButton.Click += (s, e) => dialog.Close();

        panel.Children.Add(closeButton);

        scrollViewer.Content = panel;
        dialog.Content = scrollViewer;

        await dialog.ShowDialog(this);
    }

    private StackPanel CreateDetailBlock(string label, string value)
    {
        var block = new StackPanel { Spacing = 5 };

        block.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#6B7280"))
        });

        block.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.Parse("#F9FAFB")),
            BorderBrush = new SolidColorBrush(Color.Parse("#E5E7EB")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(15, 10),
            Child = new TextBlock
            {
                Text = value,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.Parse("#1F2937")),
                TextWrapping = TextWrapping.Wrap
            }
        });

        return block;
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
            
            // Ne rien faire si la langue est déjà la bonne
            if (Thread.CurrentThread.CurrentUICulture.Name == culture)
                return;
            
            LocalizationManager.SetLanguage(culture);
            
            // Mettre à jour tous les textes de l'interface
            UpdateAllTexts();
            
            // Recharger les jobs pour mettre à jour les types
            LoadJobs();
            
            // Message de confirmation
            string message = Thread.CurrentThread.CurrentUICulture.Name == "fr-FR" 
                ? "Langue changée avec succès" 
                : "Language changed successfully";
            UpdateStatus(message, true);
        }
    }

    private void UpdateAllTexts()
    {
        // Mettre à jour les onglets
        var mainTabControl = this.FindControl<TabControl>("MainTabControl");
        if (mainTabControl?.Items != null)
        {
            var tabItems = mainTabControl.Items.OfType<TabItem>().ToArray();
            if (tabItems.Length >= 4)
            {
                tabItems[0].Header = LocalizationManager.GetString("TabExecute");
                tabItems[1].Header = LocalizationManager.GetString("TabCreate");
                tabItems[2].Header = LocalizationManager.GetString("TabManage");
                tabItems[3].Header = LocalizationManager.GetString("TabSettings");
            }
        }

        // Méthode helper pour réduire le code répétitif
        void UpdateTextBlock(string name, string key) =>
            (this.FindControl<TextBlock>(name))?.SetValue(TextBlock.TextProperty, LocalizationManager.GetString(key));
            
        void UpdateButton(string name, string key) =>
            (this.FindControl<Button>(name))?.SetValue(Button.ContentProperty, LocalizationManager.GetString(key));

        // Onglet Exécuter
        UpdateTextBlock("ExecuteTitleText", "ExecuteTitle");
        UpdateTextBlock("ExecuteDescText", "ExecuteDescription");

        // Onglet Créer
        UpdateTextBlock("CreateTitleText", "CreateTitle");

        // Onglet Gérer
        UpdateTextBlock("ManageTitleText", "ManageTitle");
        UpdateTextBlock("ManageDescText", "ManageDesc");

        // Onglet Paramètres
        UpdateTextBlock("SettingsTitleText", "SettingsAppTitle");
        UpdateTextBlock("LanguageSectionText", "LanguageSection");
        UpdateTextBlock("LanguageInterfaceText", "LanguageInterface");
        UpdateTextBlock("LanguageDescText", "LanguageChoose");
        UpdateTextBlock("AboutSectionText", "AboutSection");
        UpdateTextBlock("VersionLabelText", "VersionLabel");
        UpdateTextBlock("LogsPathLabelText", "LogsPathLabel");
        UpdateTextBlock("ConfigLabelText", "ConfigLabel");
        UpdateTextBlock("StateLabelText", "StateLabel");

        // Boutons
        UpdateButton("ExecuteButton", "BtnExecute");
        UpdateButton("CreateJobButton", "BtnCreate");
        UpdateButton("DeleteJobButton", "BtnDelete");
        UpdateButton("ViewDetailsButton", "BtnViewDetails");
        UpdateButton("BrowseSourceButton", "BtnBrowse");
        UpdateButton("BrowseTargetButton", "BtnBrowse");

        // Labels de formulaire
        UpdateTextBlock("JobNameLabel", "JobName");
        UpdateTextBlock("SourceLabel", "SourceFolder");
        UpdateTextBlock("TargetLabel", "DestinationFolder");
        UpdateTextBlock("TypeLabel", "BackupType");

        // Mettre à jour ComboBox de type de sauvegarde (utiliser cache)
        if (_typeComboBox != null)
        {
            var currentIndex = _typeComboBox.SelectedIndex;
            _typeComboBox.ItemsSource = new[] 
            {
                LocalizationManager.GetString("BackupTypeFull"),
                LocalizationManager.GetString("BackupTypeDifferential")
            };
            _typeComboBox.SelectedIndex = currentIndex == -1 ? 0 : currentIndex;
        }

        // Mettre à jour ComboBox de langue (utiliser cache)
        if (_languageComboBox != null)
        {
            var currentIndex = _languageComboBox.SelectedIndex;
            _languageComboBox.ItemsSource = new[] 
            {
                LocalizationManager.GetString("LangFrench"),
                LocalizationManager.GetString("LangEnglish")
            };
            _languageComboBox.SelectedIndex = currentIndex;
        }
    }

    private void UpdateAllPaths()
    {
        if (_logsPathValueText != null)
            _logsPathValueText.Text = _currentLogsPath;
        if (_logsPathTextBox != null)
            _logsPathTextBox.Text = _currentLogsPath;
            
        if (_configPathValueText != null)
            _configPathValueText.Text = _currentConfigPath;
        if (_configPathTextBox != null)
            _configPathTextBox.Text = _currentConfigPath;
            
        if (_statePathValueText != null)
            _statePathValueText.Text = _currentStatePath;
        if (_statePathTextBox != null)
            _statePathTextBox.Text = _currentStatePath;
    }

    private Dictionary<string, string> LoadSettings()
    {
        string settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        
        try
        {
            if (File.Exists(settingsFile))
            {
                string json = File.ReadAllText(settingsFile);
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
        }
        catch { }
        
        return new Dictionary<string, string>();
    }

    private void SaveSettings()
    {
        string settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        
        try
        {
            var settings = new Dictionary<string, string>
            {
                ["LogsPath"] = _currentLogsPath,
                ["ConfigPath"] = _currentConfigPath,
                ["StatePath"] = _currentStatePath
            };
            
            string json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFile, json);
        }
        catch (Exception ex)
        {
            UpdateStatus($"Erreur lors de la sauvegarde des préférences : {ex.Message}", false);
        }
    }

    private async System.Threading.Tasks.Task BrowseLogsFolder()
    {
        var storageProvider = StorageProvider;
        
        var options = new FolderPickerOpenOptions
        {
            Title = "Sélectionner le dossier pour les logs",
            AllowMultiple = false
        };

        var result = await storageProvider.OpenFolderPickerAsync(options);
        
        if (result.Count > 0)
        {
            _currentLogsPath = result[0].Path.LocalPath;
            
            // Créer le dossier s'il n'existe pas
            if (!Directory.Exists(_currentLogsPath))
            {
                try
                {
                    Directory.CreateDirectory(_currentLogsPath);
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Erreur lors de la création du dossier : {ex.Message}", false);
                    return;
                }
            }
            
            // Sauvegarder les préférences
            SaveSettings();
            
            // Mettre à jour l'affichage
            UpdateAllPaths();
            
            // Recréer le ViewModel avec le nouveau chemin
            _viewModel = new ViewModelConsole(customLogPath: _currentLogsPath, customConfigPath: _currentConfigPath, customStatePath: _currentStatePath);
            LoadJobs();
            
            UpdateStatus($"Dossier des logs configuré : {_currentLogsPath}", true);
        }
    }
    
    private async System.Threading.Tasks.Task BrowseConfigFile()
    {
        var storageProvider = StorageProvider;
        
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
            
            // Créer le dossier parent s'il n'existe pas
            var directory = Path.GetDirectoryName(_currentConfigPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Erreur lors de la création du dossier : {ex.Message}", false);
                    return;
                }
            }
            
            // Sauvegarder les préférences
            SaveSettings();
            
            // Mettre à jour l'affichage
            UpdateAllPaths();
            
            // Recréer le ViewModel avec le nouveau chemin
            _viewModel = new ViewModelConsole(customLogPath: _currentLogsPath, customConfigPath: _currentConfigPath, customStatePath: _currentStatePath);
            LoadJobs();
            
            UpdateStatus($"Fichier de configuration configuré : {_currentConfigPath}", true);
        }
    }
    
    private async System.Threading.Tasks.Task BrowseStateFile()
    {
        var storageProvider = StorageProvider;
        
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
            
            // Créer le dossier parent s'il n'existe pas
            var directory = Path.GetDirectoryName(_currentStatePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Erreur lors de la création du dossier : {ex.Message}", false);
                    return;
                }
            }
            
            // Sauvegarder les préférences
            SaveSettings();
            
            // Mettre à jour l'affichage
            UpdateAllPaths();
            
            // Recréer le ViewModel avec le nouveau chemin
            _viewModel = new ViewModelConsole(customLogPath: _currentLogsPath, customConfigPath: _currentConfigPath, customStatePath: _currentStatePath);
            LoadJobs();
            
            UpdateStatus($"Fichier d'état configuré : {_currentStatePath}", true);
        }
    }
}


public class JobItem
{
    public string Name { get; set; } = "";
    public int Index { get; set; }
    public bool IsSelected { get; set; }
    public string Type { get; set; } = "";
    public string Source { get; set; } = "";
    public string Target { get; set; } = "";
}