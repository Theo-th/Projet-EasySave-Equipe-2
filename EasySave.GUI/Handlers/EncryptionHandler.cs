using Avalonia.Controls;
using Avalonia.Interactivity;
using EasySave.Core.Properties;
using EasySave.Core.ViewModels;
using EasySave.GUI.Helpers;
using EasySave.GUI.Services;
using System;
using System.Collections.ObjectModel;

namespace EasySave.GUI.Handlers
{
    /// <summary>
    /// Handles encryption-related UI operations (key, extensions).
    /// </summary>
    public class EncryptionHandler
    {
        private readonly Window _window;
        private readonly ControlCache _controls;
        private readonly ViewModelConsole _viewModel;

        public EncryptionHandler(Window window, ControlCache controls, ViewModelConsole viewModel)
        {
            _window = window;
            _controls = controls;
            _viewModel = viewModel;
        }

        public void Initialize()
        {
            if (_controls.EditEncryptionKeyButton != null)
                _controls.EditEncryptionKeyButton.Click += EditEncryptionKeyButton_Click;
            if (_controls.AddExtensionButton != null)
                _controls.AddExtensionButton.Click += AddExtensionButton_Click;
            if (_controls.RemoveExtensionButton != null)
                _controls.RemoveExtensionButton.Click += RemoveExtensionButton_Click;

            UpdateEncryptionKeyUI();
            UpdateEncryptionExtensionsUI();
        }

        public void UpdateEncryptionKeyUI() =>
            _controls.EncryptionKeyTextBox!.Text = _viewModel.GetEncryptionKey();

        public void UpdateEncryptionExtensionsUI() =>
            _controls.EncryptionExtensionsListBox!.ItemsSource = new ObservableCollection<string>(_viewModel.GetEncryptionExtensions());

        private async void EditEncryptionKeyButton_Click(object? sender, RoutedEventArgs e)
        {
            var newKeyBox = new TextBox { Width = 200 };
            var validateBtn = new Button { Content = Lang.BtnValidate, Margin = new Avalonia.Thickness(0, 10, 0, 0) };
            var dialog = new Window
            {
                Title = Lang.EncryptionKeyTitle,
                Width = 250,
                Height = 120,
                Content = new StackPanel { Margin = new Avalonia.Thickness(10), Children = { newKeyBox, validateBtn } }
            };
            validateBtn.Click += (s, ev) =>
            {
                _viewModel.SetEncryptionKey(newKeyBox.Text ?? "");
                UpdateEncryptionKeyUI();
                dialog.Close();
            };
            await dialog.ShowDialog(_window);
        }

        private void AddExtensionButton_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_controls.AddExtensionTextBox?.Text)) return;
            _viewModel.AddEncryptionExtension(_controls.AddExtensionTextBox.Text.Trim());
            UpdateEncryptionExtensionsUI();
            _controls.AddExtensionTextBox.Text = string.Empty;
        }

        private void RemoveExtensionButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_controls.EncryptionExtensionsListBox?.SelectedItem is string ext)
            {
                _viewModel.RemoveEncryptionExtension(ext);
                UpdateEncryptionExtensionsUI();
            }
        }
    }
}
