using Avalonia.Controls;

namespace EasySave.GUI.Helpers;

// Manages UI control cache for performance optimization
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
    public TextBlock? PageInfoText { get; set; }
    public Button? PreviousPageButton { get; set; }
    public Button? NextPageButton { get; set; }
    public ComboBox? ItemsPerPageComboBox { get; set; }

    public TextBox? EncryptionKeyTextBox { get; set; }
    public Button? EditEncryptionKeyButton { get; set; }
    public ListBox? EncryptionExtensionsListBox { get; set; }
    public TextBox? AddExtensionTextBox { get; set; }
    public Button? AddExtensionButton { get; set; }
    public Button? RemoveExtensionButton { get; set; }

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
        PageInfoText = window.FindControl<TextBlock>("PageInfoText");
        PreviousPageButton = window.FindControl<Button>("PreviousPageButton");
        NextPageButton = window.FindControl<Button>("NextPageButton");
        ItemsPerPageComboBox = window.FindControl<ComboBox>("ItemsPerPageComboBox");

        EncryptionKeyTextBox = window.FindControl<TextBox>("EncryptionKeyTextBox");
        EditEncryptionKeyButton = window.FindControl<Button>("EditEncryptionKeyButton");
        EncryptionExtensionsListBox = window.FindControl<ListBox>("EncryptionExtensionsListBox");
        AddExtensionTextBox = window.FindControl<TextBox>("AddExtensionTextBox");
        AddExtensionButton = window.FindControl<Button>("AddExtensionButton");
        RemoveExtensionButton = window.FindControl<Button>("RemoveExtensionButton");
    }
}
