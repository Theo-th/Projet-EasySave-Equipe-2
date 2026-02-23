using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using EasySave.Core.ViewModels;
using EasySave.Core.Properties;
using EasySave.Core.Models;
using EasySave.ConsoleUI.Handlers;
using EasySave.ConsoleUI.Helpers;

namespace EasySave.ConsoleUI
{
    public class ViewConsole
    {
        private readonly ViewModelConsole _viewModel;
        private readonly JobMenuHandler _jobHandler;
        private readonly SettingsMenuHandler _settingsHandler;

        public ViewConsole(ViewModelConsole viewModel)
        {
            _viewModel = viewModel;
            _jobHandler = new JobMenuHandler(viewModel);
            _settingsHandler = new SettingsMenuHandler(viewModel);

            if (string.IsNullOrEmpty(Thread.CurrentThread.CurrentUICulture.Name))
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR");
                Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            }
        }

        public void ShowConsole()
        {
            bool quit = false;
            int index = 0;

            while (!quit)
            {
                string[] options = {
                    Lang.MenuOptionExecute,
                    Lang.MenuCreate,
                    Lang.MenuDelete,
                    Lang.SettingsTitle,
                    Lang.MenuQuit
                };

                MenuNavigator.ShowMenu(Lang.MsgWelcome, index, options);

                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                index = MenuNavigator.NavigateMenu(index, keyInfo, options.Length);

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    switch (index)
                    {
                        case 0: _jobHandler.ShowExecutionMenu(); break;
                        case 1: _jobHandler.ShowCreationMenu(); break;
                        case 2: _jobHandler.ShowDeletionMenu(); break;
                        case 3: ShowSettingsMenu(); break;
                        case 4: quit = true; break;
                    }
                }
            }
        }

        private void ShowSettingsMenu()
        {
            bool quit = false;
            int index = 0;

            while (!quit)
            {
                string[] options = {
                    Lang.MenuChangeLang,
                    Lang.TitleLog,
                    Lang.MenuLogTarget,
                    Lang.MenuConfigServerIp,
                    Lang.MenuQuit
                };

                MenuNavigator.ShowMenu(Lang.SettingsTitle, index, options);

                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                index = MenuNavigator.NavigateMenu(index, keyInfo, options.Length);

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    switch (index)
                    {
                        case 0: _settingsHandler.ShowLanguageSettings(); break;
                        case 1: _settingsHandler.ShowLogFormatMenu(); break;
                        case 2: _settingsHandler.ShowLogTargetMenu(); break;
                        case 3: _settingsHandler.ShowServerIpMenu(); break;
                        case 4: quit = true; break;
                    }
                }
            }
        }
    }
}