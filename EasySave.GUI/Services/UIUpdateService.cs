using Avalonia.Controls;
using Avalonia.Media;
using EasySave.Core.Properties;
using EasySave.GUI.Helpers;
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
