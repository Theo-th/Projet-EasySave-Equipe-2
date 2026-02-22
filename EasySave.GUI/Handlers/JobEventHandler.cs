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

public class JobEventHandler
{
    private readonly Window _window;
    private readonly ControlCache _controls;
    private readonly ViewModelConsole _viewModel;
    private readonly UIUpdateService _uiService;
    private readonly ObservableCollection<JobItem> _jobs;
    private readonly ObservableCollection<JobProgressItem> _jobProgressItems;
    private readonly Dictionary<string, JobProgressItem> _progressByJobName;

    public JobEventHandler(Window window, ControlCache controls, ViewModelConsole viewModel,
        UIUpdateService uiService, ObservableCollection<JobItem> jobs)
    {
        _window = window;
        _controls = controls;
        _viewModel = viewModel;
        _uiService = uiService;
        _jobs = jobs;
        _jobProgressItems = new ObservableCollection<JobProgressItem>();
        _progressByJobName = new Dictionary<string, JobProgressItem>();

        if (_window.FindControl<ItemsControl>("JobProgressList") is ItemsControl progressList)
            progressList.ItemsSource = _jobProgressItems;
    }

    public void LoadJobs()
    {
        _jobs.Clear();
        var jobNames = _viewModel.GetAllJobs();
        var jobNamesList = new List<string>(jobNames.Count);

        for (int i = 0; i < jobNames.Count; i++)
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

    public async void ExecuteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.JobListBox?.SelectedItems == null || _controls.JobListBox.SelectedItems.Count == 0)
        {
            _uiService.UpdateStatus("Aucun travail sélectionné", false);
            return;
        }

        var selectedIndices = new List<int>();

        // Vider les précédents items de progression
        _jobProgressItems.Clear();
        _progressByJobName.Clear();

        var jobDict = new Dictionary<string, int>(_jobs.Count);
        for (int i = 0; i < _jobs.Count; i++)
            jobDict[_jobs[i].Name] = _jobs[i].Index;

        foreach (var item in _controls.JobListBox.SelectedItems)
        {
            var name = item?.ToString();
            if (name == null || !jobDict.TryGetValue(name, out var index)) continue;

            selectedIndices.Add(index);

            // Capture du nom pour les lambdas (closure-safe)
            string capturedName = name;

            var progressItem = new JobProgressItem(
                pauseAction:  () => _viewModel.PauseJob(capturedName),
                resumeAction: () => _viewModel.ResumeJob(capturedName),
                stopAction:   () => _viewModel.StopJob(capturedName)
            )
            {
                JobName        = name,
                Progress       = 0,
                ProgressText   = "En attente...",
                FilesCountText = "En attente...",
                CurrentFile    = "",
                State          = BackupState.Inactive   // ← était Active, corrigé en Inactive
            };

            _jobProgressItems.Add(progressItem);
            _progressByJobName[name] = progressItem;
        }

        if (selectedIndices.Count == 0)
        {
            _uiService.UpdateStatus("Aucun travail valide sélectionné", false);
            return;
        }

        _uiService.UpdateStatus(string.Format(Lang.StatusExecutingBackups, selectedIndices.Count), true);
        _uiService.ShowProgress(true);

        await Task.Run(() =>
        {
            var result = _viewModel.ExecuteJobs(selectedIndices);
            if (result != null)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    _uiService.ShowProgress(false);
                    _uiService.UpdateStatus(result, false);
                });
            }
        });

        _uiService.ShowProgress(false);
        if (selectedIndices.Count == 1)
            _uiService.UpdateStatus(Lang.StatusBackupCompleted, true);
        else
            _uiService.UpdateStatus(string.Format(Lang.StatusBackupsCompleted, selectedIndices.Count), true);
    }

    public void CreateJobButton_Click(object? sender, RoutedEventArgs e)
    {
        var nameBox   = _window.FindControl<TextBox>("JobNameTextBox");
        var sourceBox = _window.FindControl<TextBox>("SourcePathTextBox");
        var targetBox = _window.FindControl<TextBox>("TargetPathTextBox");
        var typeBox   = _controls.TypeComboBox;

        if (nameBox == null || sourceBox == null || targetBox == null || typeBox == null) return;

        var name   = nameBox.Text;
        var source = sourceBox.Text;
        var target = targetBox.Text;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            _uiService.UpdateStatus("Veuillez remplir tous les champs", false);
            return;
        }

        var type   = typeBox.SelectedIndex == 1 ? BackupType.Differential : BackupType.Complete;
        var result = _viewModel.CreateJob(name, source, target, type);

        if (!result.Success)
        {
            _uiService.UpdateStatus($"Erreur: {result.ErrorMessage}", false);
            return;
        }

        nameBox.Text = ""; sourceBox.Text = ""; targetBox.Text = "";
        typeBox.SelectedIndex = 0;
        LoadJobs();
        _uiService.UpdateStatus($"Plan '{name}' créé avec succès", true);
    }

    public void DeleteJobButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.ManageJobListBox?.SelectedIndex >= 0 &&
            _controls.ManageJobListBox.SelectedIndex < _jobs.Count)
        {
            var jobIndex = _controls.ManageJobListBox.SelectedIndex;
            var jobName  = _jobs[jobIndex].Name;
            if (_viewModel.DeleteJob(jobIndex))
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

    public async void ViewDetailsButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_controls.ManageJobListBox?.SelectedItem != null &&
            _controls.ManageJobListBox.SelectedIndex >= 0 &&
            _controls.ManageJobListBox.SelectedIndex < _jobs.Count)
        {
            await ShowJobDetailsDialog(_jobs[_controls.ManageJobListBox.SelectedIndex]);
        }
        else
        {
            _uiService.UpdateStatus("⚠️ Veuillez sélectionner un plan pour voir les détails", false);
        }
    }

    /// <summary>
    /// Met à jour la progression et l'état d'un travail dans l'UI.
    /// </summary>
    public void OnBackupProgressChanged(BackupJobState state)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (!_progressByJobName.TryGetValue(state.Name, out var progressItem)) return;

            // Mise à jour de l'état (badge couleur + visibilité boutons)
            progressItem.State = state.State;

            // Calcul de la progression
            double progress = state.TotalSize > 0
                ? Math.Min(100, Math.Max(0, (double)(state.TotalSize - state.RemainingSize) / state.TotalSize * 100.0))
                : 0;

            progressItem.Progress = progress;

            // Compteur fichiers
            int filesDone = state.TotalFiles - state.RemainingFiles;
            progressItem.FilesCountText = $"{filesDone} / {state.TotalFiles} fichiers";

            // Texte progression + temps restant estimé
            string timeText = string.Empty;
            if (filesDone > 0 && state.RemainingFiles > 0)
            {
                double elapsed = (DateTime.Now - state.StartTimestamp).TotalSeconds;
                int secondsLeft = (int)(elapsed / filesDone * state.RemainingFiles);
                timeText = $"  ~{secondsLeft / 60:D2}:{secondsLeft % 60:D2} restant";
            }

            progressItem.ProgressText = state.State switch
            {
                BackupState.Completed => "100% ✓",
                BackupState.Paused    => $"{(int)progress}% ⏸",
                BackupState.Inactive  => $"{(int)progress}% ⏹",
                BackupState.Error     => $"{(int)progress}% ✕",
                _                    => $"{(int)progress}%{timeText}"
            };

            // Fichier en cours
            if (!string.IsNullOrEmpty(state.CurrentSourceFile))
                progressItem.CurrentFile = $"↳  {Path.GetFileName(state.CurrentSourceFile)}";
        });
    }

    // ----------------------------------------------------------------

    private async Task ShowJobDetailsDialog(JobItem job)
    {
        var dialog = new Window
        {
            Title = $"Détails - {job.Name}",
            Width = 600, Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel { Spacing = 20, Margin = new Thickness(30) };
        panel.Children.Add(new TextBlock
        {
            Text = "📋 Détails du plan de sauvegarde",
            FontSize = 20, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#1F2937")),
            Margin = new Thickness(0, 0, 0, 20)
        });

        panel.Children.Add(CreateDetailBlock("Nom", job.Name));
        panel.Children.Add(CreateDetailBlock("Type", job.Type));
        panel.Children.Add(CreateDetailBlock("Chemin source", job.Source));
        panel.Children.Add(CreateDetailBlock("Chemin de destination", job.Target));

        var closeBtn = new Button
        {
            Content = "Fermer",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0),
            Padding = new Thickness(40, 12),
            Background = new SolidColorBrush(Color.Parse("#3B82F6")),
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(6)
        };
        closeBtn.Click += (s, ev) => dialog.Close();
        panel.Children.Add(closeBtn);

        dialog.Content = new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        await dialog.ShowDialog(_window);
    }

    private StackPanel CreateDetailBlock(string label, string value)
    {
        var block = new StackPanel { Spacing = 5 };
        block.Children.Add(new TextBlock
        {
            Text = label, FontSize = 13, FontWeight = FontWeight.SemiBold,
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
                Text = value, FontSize = 14,
                Foreground = new SolidColorBrush(Color.Parse("#1F2937")),
                TextWrapping = TextWrapping.Wrap
            }
        });
        return block;
    }
}
