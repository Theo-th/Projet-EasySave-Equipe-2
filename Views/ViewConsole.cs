using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Projet_EasySave.ViewModels;
using Projet_EasySave.Properties;

namespace Projet_EasySave
{
    public class ViewConsole
    {

        private ViewModelConsole _viewModel;

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
            bool quitter = false;
            int index = 0;

            while (!quitter)
            {
                string[] options = {
                    Lang.MenuOptionExecute,
                    Lang.MenuCreate,
                    Lang.MenuDelete,
                    Lang.SettingsTitle,
                    Lang.MenuQuit
                };

                AfficherMenuPrincipal(index, options);

                ConsoleKeyInfo touche = Console.ReadKey(true);

                if (touche.Key == ConsoleKey.UpArrow)
                {
                    index = (index == 0) ? options.Length - 1 : index - 1;
                }
                else if (touche.Key == ConsoleKey.DownArrow)
                {
                    index = (index + 1) % options.Length;
                }
                else if (touche.Key == ConsoleKey.Enter)
                {
                    switch (index)
                    {
                        case 0: MenuLancement(); break;
                        case 1: MenuCreation(); break;
                        case 2: MenuSuppression(); break;
                        case 3: MenuParametres(); break;
                        case 4: quitter = true; break;
                    }
                }
            }
        }

        // --- AFFICHAGE GENERAL ---

        private void AfficherMenuPrincipal(int index, string[] options)
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

        // --- SOUS-MENUS ---

        private void MenuLancement()
        {
            List<string> jobs = getJob();
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
                        executeJob(selectedIndices);
                        Console.WriteLine(Lang.MenuSaveLaunch);
                        Console.ReadKey();
                        back = true;
                    }
                }
                else if (key.Key == ConsoleKey.Escape) back = true;
            }
        }

        private void MenuCreation()
        {
            Console.Clear();
            Console.WriteLine(Lang.CreateSave);

            Console.Write(Lang.NameSave);
            string name = Console.ReadLine();

            Console.Write(Lang.SourcePath);
            string source = Console.ReadLine();

            Console.Write(Lang.DestPath);
            string dest = Console.ReadLine();

            Console.WriteLine(Lang.TypeSave);
            string type = Console.ReadLine();

            if (type == "1")
                type = "full";
            else if (type == "2")
                type = "diff";

            // Appel au ViewModel
            createJob(name, source, dest, type);

            Console.WriteLine(Lang.CreateFinish);
            Console.ReadKey();
        }

        private void MenuSuppression()
        {
            // Récupère la liste via le ViewModel
            List<string> jobs = getJob();
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
                    string confirm = Console.ReadLine();
                    if (confirm.ToUpper() == "O" || confirm.ToUpper() == "Y")
                    {
                        deleteJob(index);
                        Console.WriteLine(Lang.FinishDelete);
                        Console.ReadKey();

                        // Mise à jour de la liste locale pour l'affichage
                        jobs = getJob();
                        index = 0;
                    }
                }
            }
        }

        private void MenuParametres()
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

        private void SetLanguage(string cultureCode)
        {
            try
            {
                CultureInfo culture = new CultureInfo(cultureCode);
                Thread.CurrentThread.CurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;
            }
            catch (Exception e)
            {
                Console.WriteLine("Erreur de langue : " + e.Message);
            }
        }

        // --- LIAISON VIEWMODEL (REMPLACEMENT DES MOCKS) ---

        private List<string> getJob()
        {
            // Appel direct au ViewModel pour récupérer la liste réelle
            return _viewModel.GetAllJobs();
        }

        private void createJob(string name, string source, string dest, string type)
        {
            // Appel direct au ViewModel
            bool success = _viewModel.CreateJob(name, source, dest, type);
            if (!success)
            {
                Console.WriteLine(Lang.ErrorCreateJob);
            }
        }

        private void deleteJob(int index)
        {
            // Appel direct au ViewModel
            _viewModel.DeleteJob(index);
        }

        private void executeJob(List<int> indexes)
        {
            // Petit affichage pour l'utilisateur avant de lancer
            foreach (int i in indexes)
            {
                // On récupère le nom pour l'afficher joliment
                string jobName = _viewModel.GetJob(i) ?? "Inconnu";
                Console.WriteLine(string.Format(Lang.ExecuteJob, jobName));
            }

            // Appel direct au ViewModel pour lancer la liste
            _viewModel.ExecuteJobs(indexes);
        }
    }
}