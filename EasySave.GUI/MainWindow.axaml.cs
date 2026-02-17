using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using EasySave.Core.Models;
using EasySave.Core.ViewModels;
using EasySave.GUI.Helpers;
using EasySave.GUI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasySave.GUI
{
    public partial class MainWindow : Window
    {
        private readonly ViewModelConsole _viewModel;
        private readonly ControlCache _controls;
        private readonly UIUpdateService _uiService;
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize helpers
            _controls = new ControlCache();
            _controls.InitializeFrom(this);


            _uiService = new UIUpdateService(this, _controls);

            // Initialize ViewModel
            _viewModel = new ViewModelConsole();

            // Load initial data
            RefreshJobList();
            LoadSettings();

            // Setup event handlers
            SetupEventHandlers();

            // Setup UI update timer (polling)
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += (s, e) => UpdateUiState();
            _timer.Start();

            // Subscribe to ViewModel events
            _viewModel.OnProgressChanged += (state) => Dispatcher.UIThread.InvokeAsync(() => _uiService.UpdateProgress(state));
            _viewModel.OnBusinessProcessDetected += (processName) => Dispatcher.UIThread.InvokeAsync(() => _uiService.ShowBusinessProcessAlert(processName));
        }

        private void SetupEventHandlers()
        {
            // --- Backup Actions ---
            if (_controls.SaveIpButton != null) _controls.SaveIpButton.Click += SaveIpButton_Click;
            if (_controls.PlayButton != null) _controls.PlayButton.Click += PlayButton_Click;
            if (_controls.PauseButton != null) _controls.PauseButton.Click += PauseButton_Click;
            if (_controls.ResumeButton != null) _controls.ResumeButton.Click += ResumeButton_Click;
            if (_controls.StopButton != null) _controls.StopButton.Click += StopButton_Click;

            // --- Job Management ---
            var createJobBtn = this.FindControl<Button>("CreateJobButton");
            if (createJobBtn != null) createJobBtn.Click += CreateJobButton_Click;

            var deleteJobBtn = this.FindControl<Button>("DeleteJobButton");
            if (deleteJobBtn != null) deleteJobBtn.Click += DeleteJobButton_Click;

            // --- Settings: General ---
            if (_controls.LanguageComboBox != null) _controls.LanguageComboBox.SelectionChanged += LanguageComboBox_SelectionChanged;

            // Log Format (JSON/XML)
            var logFormatCombo = this.FindControl<ComboBox>("LogFormatComboBox");
            if (logFormatCombo != null) logFormatCombo.SelectionChanged += LogFormatComboBox_SelectionChanged;

            // Log Target (Local/Docker) - NEW
            if (_controls.LogTargetComboBox != null) _controls.LogTargetComboBox.SelectionChanged += LogTargetComboBox_SelectionChanged;

            // --- Settings: Encryption ---
            if (_controls.EditEncryptionKeyButton != null) _controls.EditEncryptionKeyButton.Click += EditEncryptionKeyButton_Click;
            if (_controls.AddExtensionButton != null) _controls.AddExtensionButton.Click += AddExtensionButton_Click;
            if (_controls.RemoveExtensionButton != null) _controls.RemoveExtensionButton.Click += RemoveExtensionButton_Click;

            // --- Settings: Process Monitoring ---
            if (_controls.AddProcessButton != null) _controls.AddProcessButton.Click += AddProcessButton_Click;
            if (_controls.RemoveProcessButton != null) _controls.RemoveProcessButton.Click += RemoveProcessButton_Click;
        }

        private void LoadSettings()
        {
            // Load Job List
            RefreshJobList();

            // Load Paths
            if (_controls.LogsPathTextBox != null) _controls.LogsPathTextBox.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (_controls.ConfigPathTextBox != null) _controls.ConfigPathTextBox.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jobs_config.json");

            // Load Encryption Data
            if (_controls.EncryptionKeyTextBox != null) _controls.EncryptionKeyTextBox.Text = _viewModel.GetEncryptionKey();
            RefreshExtensionsList();

            if (_controls.ServerIpTextBox != null) _controls.ServerIpTextBox.Text = _viewModel.GetServerIp();

            // Load Processes
            RefreshProcessesList();

            // Load Log Format
            var logFormatCombo = this.FindControl<ComboBox>("LogFormatComboBox");
            if (logFormatCombo != null)
            {
                var currentFormat = _viewModel.CurrentLogFormat();
                logFormatCombo.SelectedIndex = currentFormat == "XML" ? 1 : 0;
            }
        }

        private void RefreshJobList()
        {
            var jobs = _viewModel.GetAllJobs();
            _uiService.UpdateJobList(jobs);
        }

        private void RefreshExtensionsList()
        {
            if (_controls.EncryptionExtensionsListBox != null)
                _controls.EncryptionExtensionsListBox.ItemsSource = _viewModel.GetEncryptionExtensions();
        }

        private void RefreshProcessesList()
        {
            if (_controls.WatchedProcessesListBox != null)
                _controls.WatchedProcessesListBox.ItemsSource = _viewModel.GetWatchedProcesses();
        }

        private void UpdateUiState()
        {
            // Optional: Periodic UI refresh if needed outside events
        }

        // ==================== EVENT HANDLERS ====================

        // --- Backup Execution ---
        private void PlayButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_controls.JobListBox != null && _controls.JobListBox.SelectedItems != null && _controls.JobListBox.SelectedItems.Count > 0)
            {
                var selectedIndices = new List<int>();
                var allJobs = _viewModel.GetAllJobs();

                foreach (var item in _controls.JobListBox.SelectedItems)
                {
                    int index = allJobs.IndexOf(item.ToString() ?? "");
                    if (index >= 0) selectedIndices.Add(index);
                }

                if (selectedIndices.Count > 0)
                {
                    _uiService.UpdateStatus("Démarrage de la sauvegarde...", false);
                    // Running in a separate thread to avoid freezing UI
                    System.Threading.Tasks.Task.Run(() => _viewModel.ExecuteJobs(selectedIndices));
                }
            }
            else
            {
                _uiService.UpdateStatus("Veuillez sélectionner un travail.", true);
            }
        }

        private void PauseButton_Click(object? sender, RoutedEventArgs e) => _viewModel.PauseBackup();
        private void ResumeButton_Click(object? sender, RoutedEventArgs e) => _viewModel.ResumeBackup();
        private void StopButton_Click(object? sender, RoutedEventArgs e) => _viewModel.StopBackup();

        // --- Job Creation / Deletion ---
        private void CreateJobButton_Click(object? sender, RoutedEventArgs e)
        {
            var nameBox = this.FindControl<TextBox>("JobNameTextBox");
            var sourceBox = this.FindControl<TextBox>("SourcePathTextBox");
            var destBox = this.FindControl<TextBox>("DestPathTextBox");
            var typeCombo = this.FindControl<ComboBox>("TypeComboBox");

            if (nameBox == null || sourceBox == null || destBox == null || typeCombo == null) return;

            string name = nameBox.Text ?? "";
            string source = sourceBox.Text ?? "";
            string dest = destBox.Text ?? "";
            BackupType type = typeCombo.SelectedIndex == 1 ? BackupType.Differential : BackupType.Complete;

            var result = _viewModel.CreateJob(name, source, dest, type);
            if (result.Success)
            {
                _uiService.UpdateStatus($"Travail '{name}' créé avec succès.", false);
                RefreshJobList();
                // Clear fields
                nameBox.Text = ""; sourceBox.Text = ""; destBox.Text = "";
            }
            else
            {
                _uiService.UpdateStatus($"Erreur : {result.ErrorMessage}", true);
            }
        }

        private void DeleteJobButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_controls.ManageJobListBox != null && _controls.ManageJobListBox.SelectedItem != null)
            {
                var selectedName = _controls.ManageJobListBox.SelectedItem.ToString();
                var allJobs = _viewModel.GetAllJobs();
                int index = allJobs.IndexOf(selectedName ?? "");

                if (index >= 0)
                {
                    _viewModel.DeleteJob(index);
                    RefreshJobList();
                    _uiService.UpdateStatus("Travail supprimé.", false);
                }
            }
        }

        // --- Settings Handlers ---

        private void LanguageComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            // TODO: Implement Language Service
        }

        private void LogFormatComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo != null && combo.SelectedItem is ComboBoxItem item)
            {
                string format = item.Content?.ToString() ?? "JSON";
                _viewModel.ChangeLogFormat(format);
            }
        }

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

        private void EditEncryptionKeyButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_controls.EncryptionKeyTextBox != null)
            {
                _viewModel.SetEncryptionKey(_controls.EncryptionKeyTextBox.Text ?? "");
                _uiService.UpdateStatus("Clé de chiffrement mise à jour.", false);
            }
        }

        private void AddExtensionButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_controls.AddExtensionTextBox != null && !string.IsNullOrWhiteSpace(_controls.AddExtensionTextBox.Text))
            {
                _viewModel.AddEncryptionExtension(_controls.AddExtensionTextBox.Text);
                _controls.AddExtensionTextBox.Text = "";
                RefreshExtensionsList();
            }
        }

        private void RemoveExtensionButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_controls.EncryptionExtensionsListBox != null && _controls.EncryptionExtensionsListBox.SelectedItem != null)
            {
                _viewModel.RemoveEncryptionExtension(_controls.EncryptionExtensionsListBox.SelectedItem.ToString() ?? "");
                RefreshExtensionsList();
            }
        }

        private void AddProcessButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_controls.AddProcessTextBox != null && !string.IsNullOrWhiteSpace(_controls.AddProcessTextBox.Text))
            {
                _viewModel.AddWatchedProcess(_controls.AddProcessTextBox.Text);
                _controls.AddProcessTextBox.Text = "";
                RefreshProcessesList();
            }
        }

        private void RemoveProcessButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_controls.WatchedProcessesListBox != null && _controls.WatchedProcessesListBox.SelectedItem != null)
            {
                _viewModel.RemoveWatchedProcess(_controls.WatchedProcessesListBox.SelectedItem.ToString() ?? "");
                RefreshProcessesList();
            }
        }

        private void SaveIpButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_controls.ServerIpTextBox != null && !string.IsNullOrWhiteSpace(_controls.ServerIpTextBox.Text))
            {
                _viewModel.SetServerIp(_controls.ServerIpTextBox.Text);
                _uiService.UpdateStatus($"IP Server Update : {_controls.ServerIpTextBox.Text}", true);
            }
        }
    }
}