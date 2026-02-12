using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using EasySave.Core.ViewModels;
using EasySave.Core.Properties;
using EasySave.Core.Models;

namespace EasySave.ConsoleUI
{
    public class ViewConsole
    {
        private readonly ViewModelConsole _viewModel;

        public ViewConsole(ViewModelConsole viewModel)
        {
            _viewModel = viewModel;

            if (string.IsNullOrEmpty(Thread.CurrentThread.CurrentUICulture.Name))
            {
                SetLanguage("fr-FR");
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

                ShowMainMenu(index, options);

                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    index = (index == 0) ? options.Length - 1 : index - 1;
                }
                else if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    index = (index + 1) % options.Length;
                }
                else if (keyInfo.Key == ConsoleKey.Enter)
                {
                    switch (index)
                    {
                        case 0: LaunchMenu(); break;
                        case 1: CreationMenu(); break;
                        case 2: DeleteMenu(); break;
                        case 3: MenuSettings(); break;
                        case 4: quit = true; break;
                    }
                }
            }
        }



        private void ShowSettings(int index, string[] options)
        {
            Console.Clear();
            Console.WriteLine("-------------------------------------");
            Console.WriteLine(Lang.SettingsTitle);
            Console.WriteLine("-------------------------------------");

            for (int i = 0; i < options.Length; i++)
            {
                if (i == index)
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"> {options[i]}");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"  {options[i]}");
                }
            }
            Console.WriteLine("-------------------------------------");
        }

        private void ShowMainMenu(int index, string[] options)
        {
            Console.Clear();
            Console.WriteLine("-------------------------------------");
            Console.WriteLine(Lang.MsgWelcome);
            Console.WriteLine("-------------------------------------");

            for (int i = 0; i < options.Length; i++)
            {
                if (i == index)
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"> {options[i]}");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"  {options[i]}");
                }
            }
            Console.WriteLine("-------------------------------------");
        }

        private void LaunchMenu()
        {
            List<string> jobs = _viewModel.GetAllJobs();
            List<int> selectedIndices = new List<int>();
            int index = 0;
            bool back = false;

            while (!back)
            {
                Console.Clear();
                Console.WriteLine(Lang.MenuLaunch);
                Console.WriteLine(Lang.MenuOptionSave);
                Console.WriteLine("-------------------------------------");

                if (jobs.Count == 0)
                {
                    Console.WriteLine(Lang.MenuAucunTravail);
                    Console.ReadKey();
                    back = true;
                    continue;
                }

                for (int i = 0; i < jobs.Count; i++)
                {
                    string checkbox = selectedIndices.Contains(i) ? "[X]" : "[ ]";
                    string prefix = (i == index) ? ">" : " ";

                    if (i == index) Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"{prefix} {checkbox} {jobs[i]}");
                    Console.ResetColor();
                }

                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.UpArrow) index = (index == 0) ? jobs.Count - 1 : index - 1;
                else if (key.Key == ConsoleKey.DownArrow) index = (index + 1) % jobs.Count;
                else if (key.Key == ConsoleKey.Spacebar)
                {
                    if (selectedIndices.Contains(index)) selectedIndices.Remove(index);
                    else selectedIndices.Add(index);
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    if (selectedIndices.Count > 0)
                    {
                        foreach (int i in selectedIndices)
                        {
                            string? jobName = _viewModel.GetJob(i);
                            Console.WriteLine(string.Format(Lang.ExecuteJob, jobName ?? "Inconnu"));
                        }

                        _viewModel.ExecuteJobs(selectedIndices);

                        Console.WriteLine(Lang.MenuSaveLaunch);
                        Console.ReadKey();
                        back = true;
                    }
                }
                else if (key.Key == ConsoleKey.Escape) back = true;
            }
        }

        private void CreationMenu()
        {
            Console.Clear();
            Console.WriteLine(Lang.CreateSave);

            Console.Write(Lang.NameSave);
            string? name = Console.ReadLine();

            Console.Write(Lang.SourcePath);
            string? source = Console.ReadLine();

            Console.Write(Lang.DestPath);
            string? dest = Console.ReadLine();

            Console.WriteLine(Lang.TypeSave);
            string? typeInput = Console.ReadLine();

            BackupType type = typeInput switch
            {
                "1" => BackupType.Complete,
                "2" => BackupType.Differential,
                _ => BackupType.Complete
            };

            var (success, errorMessage) = _viewModel.CreateJob(name, source, dest, type);

            if (!success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{Lang.ErrorCreateJob} {errorMessage}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(Lang.CreateFinish);
                Console.ResetColor();
            }

            Console.ReadKey();
        }


        public void MenuSettings()
        {
            bool quit = false;
            int index = 0;

            while (!quit)
            {
                string[] options = {
                    "Changer la Langue",
                    Lang.TitleLog,
                    Lang.MenuQuit
                };

                ShowSettings(index, options);

                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    index = (index == 0) ? options.Length - 1 : index - 1;
                }
                else if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    index = (index + 1) % options.Length;
                }
                else if (keyInfo.Key == ConsoleKey.Enter)
                {
                    switch (index)
                    {
                        case 0: LangSettings(); break;
                        case 1: MenuLogFormat(); break;
                        case 2: quit = true; break;
                    }
                }
            }
        }














        private void MenuLogFormat()
        {
            int index = 0;
            bool back = false;
            string[] formats = { "JSON", "XML" };

            while (!back)
            {
                Console.Clear();
                Console.WriteLine(Lang.TitleLogFormat);
                Console.WriteLine("-------------------------------------");

                for (int i = 0; i < formats.Length; i++)
                {
                    string activeTag = (_viewModel.CurrentLogFormat() == formats[i]) ? " [Active]" : "";

                    if (i == index)
                    {
                        Console.BackgroundColor = ConsoleColor.Blue;
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"> {formats[i]}{activeTag}");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"  {formats[i]}{activeTag}");
                    }
                }
                Console.WriteLine("-------------------------------------");
                Console.WriteLine(Lang.BtnReturn + " (Escape)");

                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.UpArrow) index = (index == 0) ? formats.Length - 1 : index - 1;
                else if (key.Key == ConsoleKey.DownArrow) index = (index + 1) % formats.Length;
                else if (key.Key == ConsoleKey.Escape) back = true;
                else if (key.Key == ConsoleKey.Enter)
                {
                    _viewModel.ChangeLogFormat(formats[index]);
                    back = true;
                }
            }
        }


        private void DeleteMenu()
        {
            List<string> jobs = _viewModel.GetAllJobs();
            int index = 0;
            bool back = false;

            while (!back)
            {
                Console.Clear();
                Console.WriteLine(Lang.DeleteSave);
                Console.WriteLine(Lang.EnterReturn);
                Console.WriteLine("-------------------------------------");

                if (jobs.Count == 0)
                {
                    Console.WriteLine(Lang.NoSaveDelete);
                    Console.ReadKey();
                    back = true;
                    continue;
                }

                for (int i = 0; i < jobs.Count; i++)
                {
                    string prefix = (i == index) ? "> " : "  ";
                    if (i == index) Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{prefix}{jobs[i]}");
                    Console.ResetColor();
                }

                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.UpArrow) index = (index == 0) ? jobs.Count - 1 : index - 1;
                else if (key.Key == ConsoleKey.DownArrow) index = (index + 1) % jobs.Count;
                else if (key.Key == ConsoleKey.Escape) back = true;
                else if (key.Key == ConsoleKey.Enter)
                {
                    Console.Write(string.Format(Lang.SureToDelete, jobs[index]));
                    string? confirm = Console.ReadLine();

                    if (!string.IsNullOrEmpty(confirm) &&
                        (confirm.Equals("O", StringComparison.OrdinalIgnoreCase) ||
                         confirm.Equals("Y", StringComparison.OrdinalIgnoreCase)))
                    {
                        _viewModel.DeleteJob(index);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(Lang.FinishDelete);
                        Console.ResetColor();
                        Console.ReadKey();

                        jobs = _viewModel.GetAllJobs();
                        index = Math.Min(index, Math.Max(0, jobs.Count - 1));
                    }
                }
            }
        }

        private void LangSettings()
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

                Console.Clear();
                Console.WriteLine(Lang.SettingsTitle);
                Console.WriteLine("-------------------------------------");

                for (int i = 0; i < options.Length; i++)
                {
                    if (i == index)
                    {
                        Console.BackgroundColor = ConsoleColor.Blue;
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"> {options[i]}");
                        Console.ResetColor();
                    }
                    else
                    {
                        string currentLangCheck = "";
                        if ((i == 0 && Thread.CurrentThread.CurrentUICulture.Name == "fr-FR") ||
                            (i == 1 && Thread.CurrentThread.CurrentUICulture.Name.StartsWith("en")))
                        {
                            currentLangCheck = " [Active]";
                        }

                        Console.WriteLine($"  {options[i]}{currentLangCheck}");
                    }
                }
                Console.WriteLine("-------------------------------------");

                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.UpArrow)
                {
                    index = (index == 0) ? options.Length - 1 : index - 1;
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    index = (index + 1) % options.Length;
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
                else if (key.Key == ConsoleKey.Escape)
                {
                    back = true;
                }
            }
        }

        private void SetLanguage(string culture)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
        }
    }
}