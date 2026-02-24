using System;
using System.Collections.Generic;
using EasySave.Core.ViewModels;
using EasySave.Core.Properties;
using EasySave.Core.Models;

namespace EasySave.ConsoleUI.Handlers
{
    /// <summary>
    /// Handles job creation, deletion and execution menus.
    /// </summary>
    public class JobMenuHandler
    {
        private readonly ViewModelConsole _viewModel;

        public JobMenuHandler(ViewModelConsole viewModel)
        {
            _viewModel = viewModel;
        }

        public void ShowCreationMenu()
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

        public void ShowDeletionMenu()
        {
            List<string> jobs = _viewModel.GetAllJobs();
            int index = 0;
            bool back = false;

            while (!back)
            {
                Helpers.MenuNavigator.ShowSingleSelectMenu(Lang.DeleteSave + "\n" + Lang.EnterReturn, jobs, index);

                if (jobs.Count == 0)
                {
                    Console.ReadKey();
                    return;
                }

                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Escape)
                {
                    back = true;
                }
                else if (key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.DownArrow)
                {
                    index = Helpers.MenuNavigator.NavigateMenu(index, key, jobs.Count);
                }
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

        public void ShowExecutionMenu()
        {
            List<string> jobs = _viewModel.GetAllJobs();
            List<int> selectedIndices = new List<int>();
            int index = 0;
            bool back = false;

            while (!back)
            {
                Helpers.MenuNavigator.ShowMultiSelectMenu(Lang.MenuLaunch + "\n" + Lang.MenuOptionSave, jobs, selectedIndices, index);

                if (jobs.Count == 0)
                {
                    Console.WriteLine(Lang.MenuAucunTravail);
                    Console.ReadKey();
                    return;
                }

                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Escape)
                {
                    back = true;
                }
                else if (key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.DownArrow)
                {
                    index = Helpers.MenuNavigator.NavigateMenu(index, key, jobs.Count);
                }
                else if (key.Key == ConsoleKey.Spacebar)
                {
                    if (selectedIndices.Contains(index))
                        selectedIndices.Remove(index);
                    else
                        selectedIndices.Add(index);
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
            }
        }
    }
}
