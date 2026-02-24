using Avalonia.Controls;
using Avalonia.Interactivity;
using EasySave.Core.ViewModels;
using EasySave.GUI.Helpers;
using EasySave.GUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace EasySave.GUI.Handlers
{
    /// <summary>
    /// Handles priority extensions and watched processes UI operations.
    /// </summary>
    public class ProcessPriorityHandler
    {
        private readonly Window _window;
        private readonly ControlCache _controls;
        private readonly ViewModelConsole _viewModel;
        private readonly SettingsService _settingsService;

        public ProcessPriorityHandler(Window window, ControlCache controls, ViewModelConsole viewModel, SettingsService settingsService)
        {
            _window = window;
            _controls = controls;
            _viewModel = viewModel;
            _settingsService = settingsService;
        }

        public void Initialize()
        {
            // Priority extensions
            if (_controls.AddPriorityExtensionButton != null)
                _controls.AddPriorityExtensionButton.Click += AddPriorityExtensionButton_Click;
            if (_controls.RemovePriorityExtensionButton != null)
                _controls.RemovePriorityExtensionButton.Click += RemovePriorityExtensionButton_Click;

            // Watched processes
            if (_controls.AddProcessButton != null)
                _controls.AddProcessButton.Click += AddProcessButton_Click;
            if (_controls.RemoveProcessButton != null)
                _controls.RemoveProcessButton.Click += RemoveProcessButton_Click;

            UpdatePriorityExtensionsUI();
            UpdateWatchedProcessesUI();
        }

        public void UpdatePriorityExtensionsUI() =>
            _controls.PriorityExtensionsListBox!.ItemsSource = new ObservableCollection<string>(_viewModel.GetPriorityExtensions());

        public void UpdateWatchedProcessesUI() =>
            _controls.WatchedProcessesListBox!.ItemsSource = new ObservableCollection<string>(_viewModel.GetWatchedProcesses());

        private void AddPriorityExtensionButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_controls.AddPriorityExtensionTextBox?.Text is string text && !string.IsNullOrWhiteSpace(text))
            {
                string extension = text.Trim();
                if (!extension.StartsWith(".")) extension = "." + extension;

                var currentExtensions = _viewModel.GetPriorityExtensions();
                if (!currentExtensions.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                {
                    currentExtensions.Add(extension);
                    _viewModel.UpdatePriorityExtensions(currentExtensions);
                    UpdatePriorityExtensionsUI();
                    SavePriorityExtensionsToSettings();
                    _controls.AddPriorityExtensionTextBox.Text = string.Empty;
                }
            }
        }

        private void RemovePriorityExtensionButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_controls.PriorityExtensionsListBox?.SelectedItem is string ext)
            {
                var currentExtensions = _viewModel.GetPriorityExtensions();
                currentExtensions.Remove(ext);
                _viewModel.UpdatePriorityExtensions(currentExtensions);
                UpdatePriorityExtensionsUI();
                SavePriorityExtensionsToSettings();
            }
        }

        private void AddProcessButton_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_controls.AddProcessTextBox?.Text)) return;
            _viewModel.AddWatchedProcess(_controls.AddProcessTextBox.Text.Trim());
            UpdateWatchedProcessesUI();
            _controls.AddProcessTextBox.Text = string.Empty;
        }

        private void RemoveProcessButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_controls.WatchedProcessesListBox?.SelectedItem is string processName)
            {
                _viewModel.RemoveWatchedProcess(processName);
                UpdateWatchedProcessesUI();
            }
        }

        private void SavePriorityExtensionsToSettings()
        {
            var extensions = _viewModel.GetPriorityExtensions();
            _settingsService.UpdateSetting("PriorityExtensions", string.Join(",", extensions));
        }
    }
}
