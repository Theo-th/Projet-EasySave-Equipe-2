using Avalonia.Controls;
using Avalonia.Media;
using EasySave.Core.Models;
using EasySave.Core.Properties;
using EasySave.GUI.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace EasySave.GUI.Services;

/// <summary>
/// Service for managing UI updates in the EasySave GUI.
/// </summary>
public class UIUpdateService
{
    private readonly ControlCache _controls;
    private readonly Window _window;
    private string? _lastCurrentFileName = null;

    /// <summary>
    /// Initializes a new instance of UIUpdateService.
    /// </summary>
    public UIUpdateService(Window window, ControlCache controls)
    {
        _window = window;
        _controls = controls;
    }

    /// <summary>
    /// Updates the status message and footer color.
    /// </summary>
    public void UpdateStatus(string message, bool isSuccess)
    {
        if (_controls.StatusText != null)
        {
            _controls.StatusText.Text = message;
            _controls.StatusText.Foreground = isSuccess ? Brushes.Green : Brushes.Red;
        }

        if (_controls.FooterText != null)
            _controls.FooterText.Text = message;
    }

    /// <summary>
    /// Updates the jobs count display.
    /// </summary>
    public void UpdateJobsCount(int count)
    {
        var text = count == 0 ? "Aucune sauvegarde" : count == 1 ? "1 sauvegarde" : $"{count} sauvegardes";

        if (_controls.ItemsCountText != null)
            _controls.ItemsCountText.Text = text;
    }

    /// <summary>
    /// Shows or hides the progress area and resets progress if hidden.
    /// </summary>
    public void ShowProgress(bool show)
    {
        if (_controls.ProgressArea != null)
            _controls.ProgressArea.IsVisible = show;

        if (!show)
        {
            if (_controls.ProgressBar != null)
                _controls.ProgressBar.Value = 0;
            if (_controls.ProgressText != null)
                _controls.ProgressText.Text = "0%";
            if (_controls.CurrentFileText != null)
                _controls.CurrentFileText.Text = "";
        }
    }

    /// <summary>
    /// Updates all UI texts.
    /// </summary>
    public void UpdateAllTexts()
    {
        UpdateTabHeaders();
        UpdateLabels();
        UpdateButtons();
        UpdateComboBoxes();
        UpdateHeaderAndFooter();
        UpdateCurrentFileText();
    }

    /// <summary>
    /// Updates the current file text with the last known file, in the current language.
    /// </summary>
    public void UpdateCurrentFileText()
    {
        if (_controls.CurrentFileText != null && !string.IsNullOrEmpty(_lastCurrentFileName))
        {
            _controls.CurrentFileText.Text = string.Format(EasySave.Core.Properties.Lang.CurrentFile, _lastCurrentFileName);
        }
    }

    /// <summary>
    /// Stores the last file name for dynamic language refresh.
    /// </summary>
    public void SetCurrentFileName(string? fileName)
    {
        _lastCurrentFileName = fileName;
    }

    /// <summary>
    /// Updates the list of posted jobs.
    /// </summary>
    public void UpdateJobList(List<string> jobs)
    {
        if (_controls.JobListBox != null)
            _controls.JobListBox.ItemsSource = jobs;

        if (_controls.ManageJobListBox != null)
            _controls.ManageJobListBox.ItemsSource = jobs;

        UpdateJobsCount(jobs.Count);
    }

    /// <summary>
    /// Updates the progress bar with the current status.
    /// </summary>
    public void UpdateProgress(BackupJobState state)
    {
        ShowProgress(true);

        if (_controls.ProgressBar != null)
            _controls.ProgressBar.Value = state.ProgressPercentage;

        if (_controls.ProgressText != null)
            _controls.ProgressText.Text = $"{state.ProgressPercentage}%";

        if (_controls.CurrentFileText != null)
            _controls.CurrentFileText.Text = string.IsNullOrEmpty(state.CurrentSourceFile)
                ? "..."
                : $"Fichier : {Path.GetFileName(state.CurrentSourceFile)}";
    }

    /// <summary>
    /// Displays an alert for the business software.
    /// </summary>
    public void ShowBusinessProcessAlert(string processName)
    {
        UpdateStatus($" ALERTE : Logiciel métier '{processName}' détecté ! Sauvegarde en pause.", false);
    }


    private void UpdateTabHeaders()
    {
        var mainTabControl = _window.FindControl<TabControl>("MainTabControl");
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
    }


    private void UpdateLabels()
    {
        UpdateTextBlock("ExecuteTitleText", "ExecuteTitle");
        UpdateTextBlock("ExecuteDescText", "ExecuteDescription");
        UpdateTextBlock("CreateTitleText", "CreateTitle");
        UpdateTextBlock("ManageTitleText", "ManageTitle");
        UpdateTextBlock("ManageDescText", "ManageDesc");
        UpdateTextBlock("SettingsTitleText", "SettingsAppTitle");
        UpdateTextBlock("LanguageSectionText", "LanguageSection");
        UpdateTextBlock("LanguageInterfaceText", "LanguageInterface");
        UpdateTextBlock("LanguageDescText", "LanguageChoose");
        UpdateTextBlock("AboutSectionText", "AboutSection");
        UpdateTextBlock("VersionLabelText", "VersionLabel");
        UpdateTextBlock("LogsPathLabelText", "LogsPathLabel");
        UpdateTextBlock("ConfigLabelText", "ConfigLabel");
        UpdateTextBlock("StateLabelText", "StateLabel");
        UpdateTextBlock("JobNameLabel", "JobName");
        UpdateTextBlock("SourceLabel", "SourceFolder");
        UpdateTextBlock("TargetLabel", "DestinationFolder");
        UpdateTextBlock("TypeLabel", "BackupType");
    }


    private void UpdateButtons()
    {
        UpdateButton("ExecuteButton", "BtnExecute");
        UpdateButton("CreateJobButton", "BtnCreate");
        UpdateButton("DeleteJobButton", "BtnDelete");
        UpdateButton("ViewDetailsButton", "BtnViewDetails");
        UpdateButton("BrowseSourceButton", "BtnModify");
        UpdateButton("BrowseTargetButton", "BtnModify");
        UpdateButton("BrowseLogsButton", "BtnModify");
        UpdateButton("BrowseConfigButton", "BtnModify");
        UpdateButton("BrowseStateButton", "BtnModify");
        var resumeBtn = _window.FindControl<Button>("ResumeButton");
        if (resumeBtn != null) resumeBtn.Content = $"â¶  {LocalizationManager.GetString("BtnResume")}";
        var stopBtn = _window.FindControl<Button>("StopButton");
        if (stopBtn != null) stopBtn.Content = $"â¹  {LocalizationManager.GetString("BtnStop")}";
    }


    private void UpdateComboBoxes()
    {
        if (_controls.TypeComboBox != null)
        {
            var currentIndex = _controls.TypeComboBox.SelectedIndex;
            _controls.TypeComboBox.ItemsSource = new[]
            {
                LocalizationManager.GetString("BackupTypeFull"),
                LocalizationManager.GetString("BackupTypeDifferential")
            };
            _controls.TypeComboBox.SelectedIndex = currentIndex == -1 ? 0 : currentIndex;
        }

        if (_controls.LanguageComboBox != null)
        {
            var currentIndex = _controls.LanguageComboBox.SelectedIndex;
            _controls.LanguageComboBox.ItemsSource = new[]
            {
                LocalizationManager.GetString("LangFrench"),
                LocalizationManager.GetString("LangEnglish")
            };
            _controls.LanguageComboBox.SelectedIndex = currentIndex;
        }
    }


    private void UpdateHeaderAndFooter()
    {
        if (_controls.HeaderDescription != null)
            _controls.HeaderDescription.Text = LocalizationManager.GetString("AppDescription");

        if (_controls.FooterText != null)
            _controls.FooterText.Text = LocalizationManager.GetString("StatusReady");
    }


    private void UpdateTextBlock(string name, string key)
    {
        _window.FindControl<TextBlock>(name)?.SetValue(TextBlock.TextProperty, LocalizationManager.GetString(key));
    }


    private void UpdateButton(string name, string key)
    {
        _window.FindControl<Button>(name)?.SetValue(Button.ContentProperty, LocalizationManager.GetString(key));
    }

    /// <summary>
    /// Updates the displayed paths for logs, config, and state files.
    /// </summary>
    public void UpdatePaths(string logsPath, string configPath, string statePath)
    {
        if (_controls.LogsPathValueText != null)
            _controls.LogsPathValueText.Text = logsPath;
        if (_controls.LogsPathTextBox != null)
            _controls.LogsPathTextBox.Text = logsPath;

        if (_controls.ConfigPathValueText != null)
            _controls.ConfigPathValueText.Text = configPath;
        if (_controls.ConfigPathTextBox != null)
            _controls.ConfigPathTextBox.Text = configPath;

        if (_controls.StatePathValueText != null)
            _controls.StatePathValueText.Text = statePath;
        if (_controls.StatePathTextBox != null)
            _controls.StatePathTextBox.Text = statePath;
    }
}
