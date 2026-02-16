using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using EasySave.Core.ViewModels;
using EasySave.Core.Models;
using EasySave.GUI.Helpers;
using EasySave.GUI.Services;
using EasySave.GUI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EasySave.Core.Properties;

namespace EasySave.GUI.Handlers;

/// <summary>
/// Handles backup job-related events and actions in the EasySave GUI.
/// </summary>
public class JobEventHandler
{
    private readonly Window _window;
    private readonly ControlCache _controls;
    private readonly ViewModelConsole _viewModel;
    private readonly UIUpdateService _uiService;
    private readonly ObservableCollection<JobItem> _jobs;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobEventHandler"/> class.
    /// </summary>
    /// <param name="window">The main application window.</param>
    /// <param name="controls">The cached UI controls.</param>
    /// <param name="viewModel">The view model for job operations.</param>
    /// <param name="uiService">The UI update service.</param>
    /// <param name="jobs">The observable collection of jobs.</param>
    public JobEventHandler(Window window, ControlCache controls, ViewModelConsole viewModel, 
        UIUpdateService uiService, ObservableCollection<JobItem> jobs)
    {
        _window = window;
        _controls = controls;
        _viewModel = viewModel;
        _uiService = uiService;
        _jobs = jobs;
    }

    /// <summary>
    /// Loads all jobs from the view model and updates the UI lists.
    /// </summary>
    public void LoadJobs()
    {
        _jobs.Clear();
        var jobNames = _viewModel.GetAllJobs();
        var jobCount = jobNames.Count;
        var jobNamesList = new List<string>(jobCount);
        
        for (int i = 0; i < jobCount; i++)
        {
            var jobInfo = _viewModel.GetJob(i);
            if (jobInfo != null)
            {
                var parts = jobInfo.Split(" -- ");
                _jobs.Add(new JobItem 
                { 
                    Name = parts[0],
                    Index = i,
                    IsSelected = false,
                    Type = parts.Length > 1 ? parts[1] : "Unknown",
                    Source = parts.Length > 2 ? parts[2] : "",
                    Target = parts.Length > 3 ? parts[3] : ""
                });
                jobNamesList.Add(parts[0]);
            }
        }
        
        if (_controls.JobListBox != null)
            _controls.JobListBox.ItemsSource = jobNamesList;
            
        if (_controls.ManageJobListBox != null)
            _controls.ManageJobListBox.ItemsSource = jobNamesList;
            
        _uiService.UpdateJobsCount(_jobs.Count);
    }

    /// <summary>
    /// Handles the execution of selected backup jobs when the execute button is clicked.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    public async void ExecuteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.JobListBox?.SelectedItems == null || _controls.JobListBox.SelectedItems.Count == 0)
        {
            _uiService.UpdateStatus("Aucun travail sélectionné", false);
            return;
        }

        var selectedCount = _controls.JobListBox.SelectedItems.Count;
        var selectedIndices = new List<int>(selectedCount);
        
        var jobDict = new Dictionary<string, int>(_jobs.Count);
        for (int i = 0; i < _jobs.Count; i++)
        {
            jobDict[_jobs[i].Name] = _jobs[i].Index;
        }
        
        foreach (var item in _controls.JobListBox.SelectedItems)
        {
            var name = item?.ToString();
            if (name != null && jobDict.TryGetValue(name, out var index))
            {
                selectedIndices.Add(index);
            }
        }
        
        if (selectedIndices.Count == 0)
        {
            _uiService.UpdateStatus("Aucun travail valide sélectionné", false);
            return;
        }
        
        _uiService.UpdateStatus($"Exécution de {selectedIndices.Count} sauvegarde(s)...", true);
        _uiService.ShowProgress(true);
        
        await Task.Run(() =>
        {
            var result = _viewModel.ExecuteJobs(selectedIndices);
            if (result != null)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    _uiService.ShowProgress(false);
                    bool isSuccess = result.Contains("completed successfully");
                    _uiService.UpdateStatus(result, isSuccess);
                });
                return;
            }
        });
        
        _uiService.ShowProgress(false);
        _uiService.UpdateStatus($"{selectedIndices.Count} sauvegarde(s) terminée(s) !", true);
    }

    /// <summary>
    /// Handles the creation of a new backup job when the create button is clicked.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    public void CreateJobButton_Click(object? sender, RoutedEventArgs e)
    {
        var nameBox = _window.FindControl<TextBox>("JobNameTextBox");
        var sourceBox = _window.FindControl<TextBox>("SourcePathTextBox");
        var targetBox = _window.FindControl<TextBox>("TargetPathTextBox");
        var typeBox = _controls.TypeComboBox;

        if (nameBox == null || sourceBox == null || targetBox == null || typeBox == null)
            return;

        var name = nameBox.Text;
        var source = sourceBox.Text;
        var target = targetBox.Text;
        var typeIndex = typeBox.SelectedIndex;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            _uiService.UpdateStatus("Veuillez remplir tous les champs", false);
            return;
        }

        var type = typeIndex == 1 ? BackupType.Differential : BackupType.Complete;
        var result = _viewModel.CreateJob(name, source, target, type);
        
        if (!result.Success)
        {
            _uiService.UpdateStatus($"Erreur: {result.ErrorMessage}", false);
            return;
        }
        
        nameBox.Text = "";
        sourceBox.Text = "";
        targetBox.Text = "";
        typeBox.SelectedIndex = 0;
        
        LoadJobs();
        _uiService.UpdateStatus($"Plan '{name}' créé avec succès", true);
    }

    /// <summary>
    /// Handles the deletion of a selected backup job when the delete button is clicked.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    public void DeleteJobButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.ManageJobListBox?.SelectedIndex >= 0 && _controls.ManageJobListBox.SelectedIndex < _jobs.Count)
        {
            var jobIndex = _controls.ManageJobListBox.SelectedIndex;
            var jobName = _jobs[jobIndex].Name;
            var success = _viewModel.DeleteJob(jobIndex);
            
            if (success)
            {
                LoadJobs();
                _uiService.UpdateStatus($"Plan '{jobName}' supprimé", true);
            }
            else
            {
                _uiService.UpdateStatus("Erreur lors de la suppression", false);
            }
        }
        else
        {
            _uiService.UpdateStatus("Veuillez sélectionner un plan à supprimer", false);
        }
    }

    /// <summary>
    /// Displays the details of the selected backup job in a dialog.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    public async void ViewDetailsButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.ManageJobListBox?.SelectedItem != null && 
            _controls.ManageJobListBox.SelectedIndex >= 0 && 
            _controls.ManageJobListBox.SelectedIndex < _jobs.Count)
        {
            var job = _jobs[_controls.ManageJobListBox.SelectedIndex];
            await ShowJobDetailsDialog(job);
        }
        else
        {
            _uiService.UpdateStatus("⚠️ Veuillez sélectionner un plan pour voir les détails", false);
        }
    }

    private async Task ShowJobDetailsDialog(JobItem job)
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

        panel.Children.Add(new TextBlock
        {
            Text = $"📋 Détails du plan de sauvegarde",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#1F2937")),
            Margin = new Thickness(0, 0, 0, 20)
        });

        panel.Children.Add(CreateDetailBlock("Nom", job.Name));
        panel.Children.Add(CreateDetailBlock("Type", job.Type));
        panel.Children.Add(CreateDetailBlock("Chemin source", job.Source));
        panel.Children.Add(CreateDetailBlock("Chemin de destination", job.Target));

        var closeButton = new Button
        {
            Content = "Fermer",
            HorizontalAlignment = HorizontalAlignment.Center,
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

        await dialog.ShowDialog(_window);
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

    /// <summary>
    /// Updates the UI to reflect the progress of a running backup job.
    /// </summary>
    /// <param name="state">The current state of the backup job.</param>
    public void OnBackupProgressChanged(BackupJobState state)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            double progress = 0;
            if (state.TotalSize > 0)
            {
                long processedSize = state.TotalSize - state.RemainingSize;
                progress = (double)processedSize / state.TotalSize * 100.0;
            }

            if (_controls.ProgressBar != null)
                _controls.ProgressBar.Value = Math.Min(100, Math.Max(0, progress));

            // Calcul du temps restant estimé (multilingue)
            string timeLeftText = string.Empty;
            int filesDone = state.TotalFiles - state.RemainingFiles;
            if (filesDone > 0 && state.RemainingFiles > 0)
            {
                var elapsed = (DateTime.Now - state.StartTimestamp).TotalSeconds;
                double avgPerFile = elapsed / filesDone;
                int secondsLeft = (int)(avgPerFile * state.RemainingFiles);
                int min = secondsLeft / 60;
                int sec = secondsLeft % 60;
                string timeValue = $"{min:D2}:{sec:D2}";
                timeLeftText = $" | {Lang.TimeLeft.Replace("{0}", timeValue)}";
            }

            if (_controls.ProgressText != null)
                _controls.ProgressText.Text = $"{(int)progress}%{timeLeftText}";

            if (_controls.CurrentFileText != null && !string.IsNullOrEmpty(state.CurrentSourceFile))
            {
                string fileName = Path.GetFileName(state.CurrentSourceFile);
                _controls.CurrentFileText.Text = string.Format(Lang.CurrentFile, fileName);
                _uiService.SetCurrentFileName(fileName);
            }
        });
    }
}
