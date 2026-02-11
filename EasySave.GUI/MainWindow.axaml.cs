using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using EasySave.Core.ViewModels;
using EasySave.Core.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EasySave.GUI;

public partial class MainWindow : Window
{
    private readonly ViewModelConsole _viewModel;
    private ObservableCollection<JobItem> _jobs;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new ViewModelConsole();
        _jobs = new ObservableCollection<JobItem>();
        
        LoadJobs();
        SetupEventHandlers();
    }

    private void LoadJobs()
    {
        _jobs.Clear();
        var jobNames = _viewModel.GetAllJobs();
        
        for (int i = 0; i < jobNames.Count; i++)
        {
            var jobInfo = _viewModel.GetJob(i);
            _jobs.Add(new JobItem 
            { 
                Name = jobInfo ?? jobNames[i],
                Index = i,
                IsSelected = false 
            });
        }
        
        var jobListBox = this.FindControl<ListBox>("JobListBox");
        var manageListBox = this.FindControl<ListBox>("ManageJobListBox");
        
        if (jobListBox != null)
        {
            var items = _jobs.Select(j => j.Name).ToList();
            jobListBox.ItemsSource = items;
        }
            
        if (manageListBox != null)
            manageListBox.ItemsSource = _jobs.Select(j => j.Name).ToList();
    }

    private void SetupEventHandlers()
    {
        var executeButton = this.FindControl<Button>("ExecuteButton");
        var createJobButton = this.FindControl<Button>("CreateJobButton");
        var deleteJobButton = this.FindControl<Button>("DeleteJobButton");
        var browseSourceButton = this.FindControl<Button>("BrowseSourceButton");
        var browseTargetButton = this.FindControl<Button>("BrowseTargetButton");

        if (executeButton != null)
            executeButton.Click += ExecuteButton_Click;
            
        if (createJobButton != null)
            createJobButton.Click += CreateJobButton_Click;
            
        if (deleteJobButton != null)
            deleteJobButton.Click += DeleteJobButton_Click;
            
        if (browseSourceButton != null)
            browseSourceButton.Click += async (s, e) => await BrowseFolder("SourcePathTextBox");
            
        if (browseTargetButton != null)
            browseTargetButton.Click += async (s, e) => await BrowseFolder("TargetPathTextBox");
    }

    private async void ExecuteButton_Click(object? sender, RoutedEventArgs e)
    {
        var jobListBox = this.FindControl<ListBox>("JobListBox");
        
        if (jobListBox?.SelectedItems == null || jobListBox.SelectedItems.Count == 0)
        {
            UpdateStatus("Aucun travail sélectionné", false);
            return;
        }

        var selectedIndices = new List<int>();
        foreach (var item in jobListBox.SelectedItems)
        {
            var name = item?.ToString();
            var job = _jobs.FirstOrDefault(j => j.Name == name);
            if (job != null)
                selectedIndices.Add(job.Index);
        }
        
        UpdateStatus($"Exécution de {selectedIndices.Count} sauvegarde(s)...", true);
        
        foreach (var index in selectedIndices)
        {
            var result = _viewModel.ExecuteJob(index);
            if (result != null)
            {
                UpdateStatus($"Erreur: {result}", false);
                return;
            }
        }
        
        UpdateStatus($"{selectedIndices.Count} sauvegarde(s) terminée(s) !", true);
    }

    private void CreateJobButton_Click(object? sender, RoutedEventArgs e)
    {
        var nameBox = this.FindControl<TextBox>("JobNameTextBox");
        var sourceBox = this.FindControl<TextBox>("SourcePathTextBox");
        var targetBox = this.FindControl<TextBox>("TargetPathTextBox");
        var typeCombo = this.FindControl<ComboBox>("BackupTypeComboBox");

        if (nameBox == null || sourceBox == null || targetBox == null || typeCombo == null)
            return;

        var name = nameBox.Text;
        var source = sourceBox.Text;
        var target = targetBox.Text;
        var typeIndex = typeCombo.SelectedIndex;

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
        typeCombo.SelectedIndex = 0;
        
        LoadJobs();
        UpdateStatus($"Plan '{name}' créé avec succès", true);
    }

    private void DeleteJobButton_Click(object? sender, RoutedEventArgs e)
    {
        var manageListBox = this.FindControl<ListBox>("ManageJobListBox");
        
        if (manageListBox?.SelectedIndex >= 0 && manageListBox.SelectedIndex < _jobs.Count)
        {
            var jobIndex = manageListBox.SelectedIndex;
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
        var statusText = this.FindControl<TextBlock>("StatusText");
        var footerText = this.FindControl<TextBlock>("FooterText");
        
        if (statusText != null)
        {
            statusText.Text = message;
            statusText.Foreground = isSuccess ? Avalonia.Media.Brushes.Green : Avalonia.Media.Brushes.Red;
        }
        
        if (footerText != null)
            footerText.Text = message;
    }
}

public class JobItem
{
    public string Name { get; set; } = "";
    public int Index { get; set; }
    public bool IsSelected { get; set; }
}