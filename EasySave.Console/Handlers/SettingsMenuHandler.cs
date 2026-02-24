using System;
using System.Globalization;
using System.Threading;
using EasySave.Core.ViewModels;
using EasySave.Core.Properties;

namespace EasySave.ConsoleUI.Handlers
{
    /// <summary>
    /// Handles settings menus (language, log format, log target, server IP).
    /// </summary>
    public class SettingsMenuHandler
    {
        private readonly ViewModelConsole _viewModel;

        public SettingsMenuHandler(ViewModelConsole viewModel)
        {
            _viewModel = viewModel;
        }

        public void ShowLanguageSettings()
        {
            int index = 0;
            bool back = false;

            while (!back)
            {
                string[] options = {
                    Lang.LangFrench,
                    Lang.LangEnglish,
                    Lang.BtnReturn
                };

                string[] activeMarkers = new string[options.Length];
                if (Thread.CurrentThread.CurrentUICulture.Name == "fr-FR")
                    activeMarkers[0] = " [Active]";
                else if (Thread.CurrentThread.CurrentUICulture.Name.StartsWith("en"))
                    activeMarkers[1] = " [Active]";

                Helpers.MenuNavigator.ShowMenu(Lang.SettingsTitle, index, options, activeMarkers);

                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Escape)
                {
                    back = true;
                }
                else if (key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.DownArrow)
                {
                    index = Helpers.MenuNavigator.NavigateMenu(index, key, options.Length);
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    switch (index)
                    {
                        case 0: SetLanguage("fr-FR"); break;
                        case 1: SetLanguage("en-US"); break;
                        case 2: back = true; break;
                    }
                }
            }
        }

        public void ShowLogFormatMenu()
        {
            int index = 0;
            bool back = false;
            string[] formats = { "JSON", "XML" };

            while (!back)
            {
                string[] activeMarkers = new string[formats.Length];
                for (int i = 0; i < formats.Length; i++)
                {
                    if (_viewModel.CurrentLogFormat() == formats[i])
                        activeMarkers[i] = " [Active]";
                }

                Helpers.MenuNavigator.ShowMenu(Lang.TitleLogFormat + "\n" + Lang.BtnReturn + " (Escape)", index, formats, activeMarkers);

                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Escape)
                {
                    back = true;
                }
                else if (key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.DownArrow)
                {
                    index = Helpers.MenuNavigator.NavigateMenu(index, key, formats.Length);
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    _viewModel.ChangeLogFormat(formats[index]);
                    back = true;
                }
            }
        }

        public void ShowLogTargetMenu()
        {
            int index = 0;
            bool back = false;
            string[] targets = { "Local", "Server", "Both" };
            string[] displayNames = { Lang.LogTargetLocalOnly, Lang.LogTargetServerOnly, Lang.LogTargetBoth };

            while (!back)
            {
                Helpers.MenuNavigator.ShowMenu(Lang.LogTargetMenuTitle + "\n" + Lang.BtnReturn + " (Escape)", index, displayNames);

                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Escape)
                {
                    back = true;
                }
                else if (key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.DownArrow)
                {
                    index = Helpers.MenuNavigator.NavigateMenu(index, key, targets.Length);
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    _viewModel.SetLogTarget(targets[index]);
                    Console.WriteLine($"\n{Lang.LogTargetModified} {displayNames[index]}");
                    Thread.Sleep(1000);
                    back = true;
                }
            }
        }

        public void ShowServerIpMenu()
        {
            Console.Clear();
            Console.WriteLine(Lang.ServerIpMenuTitle);
            Console.WriteLine($"{Lang.ServerIpCurrent} {_viewModel.GetServerIp()}");
            Console.WriteLine("-------------------------------------");
            Console.Write($"{Lang.ServerIpNewPrompt} ");

            string? input = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(input))
            {
                _viewModel.SetServerIp(input);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(Lang.ServerIpSaved);
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine(Lang.ServerIpCancelled);
            }
            Thread.Sleep(1000);
        }

        private static void SetLanguage(string culture)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
        }
    }
}
