using Avalonia.Controls;
using Avalonia.Media;
using EasySave.Core.Models;
using EasySave.Core.Properties;
using EasySave.GUI.Helpers;
using EasySave.GUI.Handlers;
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
    private JobEventHandler? _jobHandler;
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
    /// Sets the JobEventHandler reference for updating job progress texts.
    /// </summary>
    public void SetJobHandler(JobEventHandler jobHandler)
    {
        _jobHandler = jobHandler;
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
        var text = count == 0 ? Lang.JobsCountNone : count == 1 ? Lang.JobsCountOne : string.Format(Lang.JobsCountMany, count);

        if (_controls.ItemsCountText != null)
            _controls.ItemsCountText.Text = text;
    }

    /// <summary>
    /// Shows or hides the progress area and resets progress if hidden.
    /// </summary>
    public void ShowProgress(bool show, bool showGlobalControls = false)
    {
        if (_controls.ProgressArea != null)
            _controls.ProgressArea.IsVisible = show;

        if (_controls.GlobalControlsSection != null)
            _controls.GlobalControlsSection.IsVisible = show && showGlobalControls;

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
        UpdateSettingsSections();
        UpdateExecuteTab();
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
                : $"{Lang.FileLabel} : {Path.GetFileName(state.CurrentSourceFile)}";
    }

    /// <summary>
    /// Displays an alert for the business software.
    /// </summary>
    public void ShowBusinessProcessAlert(string processName)
    {
        UpdateStatus(string.Format(Lang.BusinessProcessAlert, processName), false);
    }


    private void UpdateTabHeaders()
    {
        var mainTabControl = _window.FindControl<TabControl>("MainTabControl");
        if (mainTabControl?.Items != null)
        {
            var tabItems = mainTabControl.Items.OfType<TabItem>().ToArray();
            if (tabItems.Length >= 4)
            {
                tabItems[0].Header = Lang.TabExecute;
                tabItems[1].Header = Lang.TabCreate;
                tabItems[2].Header = Lang.TabManage;
                tabItems[3].Header = Lang.TabSettings;
            }
        }
    }


    private void UpdateLabels()
    {
        SetTextBlock("ExecuteTitleText", Lang.ExecuteTitle);
        SetTextBlock("ExecuteDescText", Lang.ExecuteDescription);
        SetTextBlock("CreateTitleText", Lang.CreateTitle);
        SetTextBlock("ManageTitleText", Lang.ManageTitle);
        SetTextBlock("ManageDescText", Lang.ManageDesc);
        SetTextBlock("SettingsTitleText", Lang.SettingsAppTitle);
        SetTextBlock("LanguageSectionText", Lang.LanguageSection);
        SetTextBlock("LanguageInterfaceText", Lang.LanguageInterface);
        SetTextBlock("LanguageDescText", Lang.LanguageChoose);
        SetTextBlock("AboutSectionText", Lang.AboutSection);
        SetTextBlock("VersionLabelText", Lang.VersionLabel);
        SetTextBlock("LogsPathLabelText", Lang.LogsPathLabel);
        SetTextBlock("ConfigLabelText", Lang.ConfigLabel);
        SetTextBlock("StateLabelText", Lang.StateLabel);
        SetTextBlock("JobNameLabel", Lang.JobName);
        SetTextBlock("SourceLabel", Lang.SourceFolder);
        SetTextBlock("TargetLabel", Lang.DestinationFolder);
        SetTextBlock("TypeLabel", Lang.BackupType);
    }


    private void UpdateButtons()
    {
        // Execute tab
        SetButton("ExecuteButton", Lang.BtnExecute);
        SetTextBlock("PerPageText", Lang.LabelPerPage);
        // Create tab
        SetButton("CreateJobButton", Lang.BtnCreate);
        SetButton("BrowseSourceButton", Lang.BtnBrowse);
        SetButton("BrowseTargetButton", Lang.BtnBrowse);
        // Manage tab
        SetButton("DeleteJobButton", Lang.BtnDelete);
        SetButton("ViewDetailsButton", Lang.BtnViewDetails);
    }


    private void UpdateComboBoxes()
    {
        if (_controls.TypeComboBox != null)
        {
            var currentIndex = _controls.TypeComboBox.SelectedIndex;
            _controls.TypeComboBox.ItemsSource = new[] { Lang.BackupTypeFull, Lang.BackupTypeDifferential };
            _controls.TypeComboBox.SelectedIndex = currentIndex == -1 ? 0 : currentIndex;
        }

        if (_controls.LanguageComboBox != null)
        {
            var currentIndex = _controls.LanguageComboBox.SelectedIndex;
            _controls.LanguageComboBox.ItemsSource = new[] { Lang.LangFrench, Lang.LangEnglish };
            _controls.LanguageComboBox.SelectedIndex = currentIndex;
        }
    }


    private void UpdateHeaderAndFooter()
    {
        if (_controls.HeaderDescription != null)
            _controls.HeaderDescription.Text = Lang.AppDescription;

        if (_controls.FooterText != null)
            _controls.FooterText.Text = Lang.StatusReady;
    }


    private void SetTextBlock(string name, string value)
    {
        _window.FindControl<TextBlock>(name)?.SetValue(TextBlock.TextProperty, value);
    }


    private void SetButton(string name, string value)
    {
        _window.FindControl<Button>(name)?.SetValue(Button.ContentProperty, value);
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

    /// <summary>
    /// Updates all settings section labels and buttons.
    /// </summary>
    private void UpdateSettingsSections()
    {
        // Sidebar navigation buttons
        _window.FindControl<Button>("PathsNavButton")?.SetValue(Button.ContentProperty, Lang.NavPaths);
        _window.FindControl<Button>("SecurityNavButton")?.SetValue(Button.ContentProperty, Lang.NavSecurity);
        _window.FindControl<Button>("ThreadingNavButton")?.SetValue(Button.ContentProperty, Lang.NavMultiThreading);
        _window.FindControl<Button>("LogsNavButton")?.SetValue(Button.ContentProperty, Lang.NavLogging);

        // Update PathsSection
        SetTextBlock("PathsSectionTitle", Lang.SectionPaths);
        SetTextBlock("LabelLogsFolderText", Lang.LabelLogsFolder);
        SetTextBlock("LabelConfigFileText", Lang.LabelConfigFile);
        SetTextBlock("LabelStateFileText", Lang.LabelStateFile);
        _window.FindControl<Button>("BrowseLogsButton")?.SetValue(Button.ContentProperty, Lang.BtnBrowse);
        _window.FindControl<Button>("BrowseConfigButton")?.SetValue(Button.ContentProperty, Lang.BtnBrowse);
        _window.FindControl<Button>("BrowseStateButton")?.SetValue(Button.ContentProperty, Lang.BtnBrowse);

        // Update SecuritySection
        SetTextBlock("SecuritySectionTitle", Lang.SectionSecurity);
        SetTextBlock("LabelEncryptionKeyText", Lang.LabelEncryptionKey);
        SetTextBlock("LabelExtensionsToEncryptText", Lang.LabelExtensionsToEncrypt);
        SetTextBlock("LabelBusinessProcessText", Lang.LabelBusinessProcess);
        SetTextBlock("BusinessProcessDescText", Lang.BusinessProcessDescription);
        _window.FindControl<Button>("EditEncryptionKeyButton")?.SetValue(Button.ContentProperty, Lang.BtnModify);

        // Update ThreadingSection
        SetTextBlock("ThreadingSectionTitle", Lang.SectionMultiThreading);
        SetTextBlock("LabelPriorityExtensionsText", Lang.LabelPriorityExtensions);
        SetTextBlock("PriorityExtensionsDescText", Lang.PriorityExtensionsDescription);
        SetTextBlock("LabelMaxSimultaneousJobsText", Lang.LabelMaxSimultaneousJobs);
        SetTextBlock("MaxJobsRangeText", Lang.MaxJobsRange);
        SetTextBlock("LabelFileSizeThresholdText", Lang.LabelFileSizeThreshold);
        SetTextBlock("FileSizeThresholdDescText", Lang.FileSizeThresholdDescription);
        SetTextBlock("MegabytesText", Lang.Megabytes);
        _window.FindControl<Button>("SaveThreadingSettingsButton")?.SetValue(Button.ContentProperty, Lang.BtnSaveSettings);

        // Update LogsSection
        SetTextBlock("LogsSectionTitle", Lang.SectionLogging);
        SetTextBlock("LabelLogTargetText", Lang.LabelLogTarget);
        UpdateLogTargetComboBox();
        SetTextBlock("LabelServerIpText", Lang.LabelServerIp);
        _window.FindControl<Button>("SaveIpButton")?.SetValue(Button.ContentProperty, Lang.BtnSave);
    }

    /// <summary>
    /// Updates the log target ComboBox items.
    /// </summary>
    private void UpdateLogTargetComboBox()
    {
        var comboBox = _window.FindControl<ComboBox>("LogTargetComboBox");
        if (comboBox != null && comboBox.Items != null)
        {
            var items = comboBox.Items.OfType<ComboBoxItem>().ToArray();
            if (items.Length >= 3)
            {
                items[0].Content = Lang.LogTargetLocalOnly;
                items[1].Content = Lang.LogTargetServerOnly;
                items[2].Content = Lang.LogTargetBoth;
            }
        }
    }

    /// <summary>
    /// Updates the Execute tab controls (global buttons and control label).
    /// </summary>
    private void UpdateExecuteTab()
    {
        // Global control label
        SetTextBlock("GlobalControlLabel", Lang.GlobalControl);

        // Global control buttons
        _controls.PauseButton?.SetValue(Button.ContentProperty, Lang.BtnPauseAll);
        SetButton("ResumeButton", Lang.BtnResumeAll);
        _controls.StopButton?.SetValue(Button.ContentProperty, Lang.BtnStopAll);

        // Refresh all job progress item texts (badges + per-job buttons)
        _jobHandler?.RefreshJobProgressTexts();
    }
}
