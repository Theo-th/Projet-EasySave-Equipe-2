using Avalonia.Controls;

namespace EasySave.GUI.Helpers;

/// <summary>
/// Manages a cache of UI controls for performance optimization in the EasySave GUI.
/// </summary>

public class ControlCache
{
    public ListBox? JobListBox { get; set; }
    public ListBox? ManageJobListBox { get; set; }
    public TextBlock? StatusText { get; set; }
    public TextBlock? FooterText { get; set; }
    public TextBlock? HeaderDescription { get; set; }
    public Border? ProgressArea { get; set; }
    public ComboBox? TypeComboBox { get; set; }
    public ComboBox? LanguageComboBox { get; set; }
    public ComboBox? LogTargetComboBox { get; set; }

    public TextBlock? LogsPathValueText { get; set; }
    public TextBox? LogsPathTextBox { get; set; }
    public TextBlock? ConfigPathValueText { get; set; }
    public TextBox? ConfigPathTextBox { get; set; }
    public TextBlock? StatePathValueText { get; set; }
    public TextBox? StatePathTextBox { get; set; }
    public ProgressBar? ProgressBar { get; set; }
    public TextBlock? ProgressText { get; set; }
    public TextBlock? CurrentFileText { get; set; }
    public TextBlock? ItemsCountText { get; set; }
    public ComboBox? ItemsPerPageComboBox { get; set; }

    public TextBox? EncryptionKeyTextBox { get; set; }
    public Button? EditEncryptionKeyButton { get; set; }
    public ListBox? EncryptionExtensionsListBox { get; set; }
    public TextBox? AddExtensionTextBox { get; set; }
    public Button? AddExtensionButton { get; set; }
    public Button? RemoveExtensionButton { get; set; }

    public ListBox? PriorityExtensionsListBox { get; set; }
    public TextBox? AddPriorityExtensionTextBox { get; set; }
    public Button? AddPriorityExtensionButton { get; set; }
    public Button? RemovePriorityExtensionButton { get; set; }

    public ListBox? WatchedProcessesListBox { get; set; }
    public TextBox? AddProcessTextBox { get; set; }
    public Button? AddProcessButton { get; set; }
    public Button? RemoveProcessButton { get; set; }

    public TextBox? ServerIpTextBox { get; set; }
    public Button? SaveIpButton { get; set; }

    // Multi-threading settings
    public TextBox? MaxJobsTextBox { get; set; }
    public TextBox? FileSizeThresholdTextBox { get; set; }
    public Button? SaveThreadingSettingsButton { get; set; }


    // Backup control buttons (Pause / Resume / Stop)
    public Border? GlobalControlsSection { get; set; }
    public Button? PlayButton { get; set; }
    public Button? PauseButton { get; set; }
    public Button? ResumeButton { get; set; }
    public Button? StopButton { get; set; }


    /// <summary>
    /// Initializes all cached controls by finding them in the specified window.
    /// </summary>
    /// <param name="window">The main application window containing the controls.</param>
    public void InitializeFrom(Window window)
    {
        JobListBox = window.FindControl<ListBox>("JobListBox");
        ManageJobListBox = window.FindControl<ListBox>("ManageJobListBox");
        StatusText = window.FindControl<TextBlock>("StatusText");
        FooterText = window.FindControl<TextBlock>("FooterText");
        HeaderDescription = window.FindControl<TextBlock>("HeaderDescriptionText");
        ProgressArea = window.FindControl<Border>("ProgressArea");
        TypeComboBox = window.FindControl<ComboBox>("TypeComboBox");
        LanguageComboBox = window.FindControl<ComboBox>("LanguageComboBox");
        LogTargetComboBox = window.FindControl<ComboBox>("LogTargetComboBox"); // AJOUT

        LogsPathValueText = window.FindControl<TextBlock>("LogsPathValueText");
        LogsPathTextBox = window.FindControl<TextBox>("LogsPathTextBox");
        ConfigPathValueText = window.FindControl<TextBlock>("ConfigPathValueText");
        ConfigPathTextBox = window.FindControl<TextBox>("ConfigPathTextBox");
        StatePathValueText = window.FindControl<TextBlock>("StatePathValueText");
        StatePathTextBox = window.FindControl<TextBox>("StatePathTextBox");
        ProgressBar = window.FindControl<ProgressBar>("ProgressBar");
        ProgressText = window.FindControl<TextBlock>("ProgressText");
        CurrentFileText = window.FindControl<TextBlock>("CurrentFileText");
        ItemsCountText = window.FindControl<TextBlock>("ItemsCountText");
        ItemsPerPageComboBox = window.FindControl<ComboBox>("ItemsPerPageComboBox");

        EncryptionKeyTextBox = window.FindControl<TextBox>("EncryptionKeyTextBox");
        EditEncryptionKeyButton = window.FindControl<Button>("EditEncryptionKeyButton");
        EncryptionExtensionsListBox = window.FindControl<ListBox>("EncryptionExtensionsListBox");
        AddExtensionTextBox = window.FindControl<TextBox>("AddExtensionTextBox");
        AddExtensionButton = window.FindControl<Button>("AddExtensionButton");
        RemoveExtensionButton = window.FindControl<Button>("RemoveExtensionButton");

        // Priority extensions
        PriorityExtensionsListBox = window.FindControl<ListBox>("PriorityExtensionsListBox");
        AddPriorityExtensionTextBox = window.FindControl<TextBox>("AddPriorityExtensionTextBox");
        AddPriorityExtensionButton = window.FindControl<Button>("AddPriorityExtensionButton");
        RemovePriorityExtensionButton = window.FindControl<Button>("RemovePriorityExtensionButton");


        // Process Detector controls
        WatchedProcessesListBox = window.FindControl<ListBox>("WatchedProcessesListBox");
        AddProcessTextBox = window.FindControl<TextBox>("AddProcessTextBox");
        AddProcessButton = window.FindControl<Button>("AddProcessButton");
        RemoveProcessButton = window.FindControl<Button>("RemoveProcessButton");

        ServerIpTextBox = window.FindControl<TextBox>("ServerIpTextBox");
        SaveIpButton = window.FindControl<Button>("SaveIpButton");

        // Multi-threading settings
        MaxJobsTextBox = window.FindControl<TextBox>("MaxJobsTextBox");
        FileSizeThresholdTextBox = window.FindControl<TextBox>("FileSizeThresholdTextBox");
        SaveThreadingSettingsButton = window.FindControl<Button>("SaveThreadingSettingsButton");


        // Backup control buttons
        PlayButton = window.FindControl<Button>("PlayButton");
        GlobalControlsSection = window.FindControl<Border>("GlobalControlsSection");
        PauseButton = window.FindControl<Button>("PauseButton");
        ResumeButton = window.FindControl<Button>("ResumeButton");
        StopButton = window.FindControl<Button>("StopButton");
    }
}