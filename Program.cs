using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Projet_EasySave.Properties;

namespace Projet_EasySave
{
    class Program
    {
        // Variable de test il faut changer
        static List<string> mockJobs = new List<string> { "Sauvegarde_Travail", "Sauvegarde_Photos", "Sauvegarde_Systeme" };

        static void Main(string[] args)
        {
            // Initialisation langue par défaut (System ou FR)
            if (Thread.CurrentThread.CurrentUICulture.Name == "")
            {
                SetLanguage("fr-FR");
            }

            bool quitter = false;
            int index = 0;

            while (!quitter)
            {
                // A modifier avec les clés de langue
                string[] options = {
                    "Lancer plan de sauvegarde",
                    "Créer plan de sauvegarde",
                    "Supprimer plan de sauvegarde",
                    "Paramètres / Settings",
                    "Quitter"
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

        static void AfficherMenuPrincipal(int index, string[] options)
        {
            Console.Clear();
            Console.WriteLine("-------------------------------------");
            Console.WriteLine($"      EASY SAVE - MENU PRINCIPAL     ");
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

        static void MenuLancement()
        {
            // Récupération des jobs via la méthode future
            List<string> jobs = getJob();
            List<int> selectedIndices = new List<int>();
            int index = 0;
            bool back = false;

            while (!back)
            {
                Console.Clear();
                Console.WriteLine("=== LANCEMENT DES SAUVEGARDES ===");
                Console.WriteLine("Space: Sélectionner | Enter: Lancer | Echapp: Retour");
                Console.WriteLine("-------------------------------------");

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
                        // Appel de la méthode future executeJob
                        executeJob(selectedIndices);
                        Console.WriteLine("Sauvegardes lancées ! Appuyez pour continuer.");
                        Console.ReadKey();
                        back = true;
                    }
                }
                else if (key.Key == ConsoleKey.Escape) back = true;
            }
        }

        static void MenuCreation()
        {
            Console.Clear();
            Console.WriteLine("=== CREATION D'UNE SAUVEGARDE ===");

            Console.Write("Nom de la sauvegarde : ");
            string name = Console.ReadLine();

            Console.Write("Chemin source : ");
            string source = Console.ReadLine();

            Console.Write("Chemin destination : ");
            string dest = Console.ReadLine();

            Console.WriteLine("Type (1: Complet, 2: Différentiel) : ");
            string type = Console.ReadLine();

            // Appel de la méthode future createJob
            createJob(name, source, dest, type);

            Console.WriteLine("\nSauvegarde créée avec succès !");
            Console.ReadKey();
        }

        static void MenuSuppression()
        {
            List<string> jobs = getJob();
            int index = 0;
            bool back = false;

            while (!back)
            {
                Console.Clear();
                Console.WriteLine("=== SUPPRESSION D'UNE SAUVEGARDE ===");
                Console.WriteLine("Enter: Supprimer | Echapp: Retour");
                Console.WriteLine("-------------------------------------");

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
                    Console.Write($"\nÊtes-vous sûr de vouloir supprimer '{jobs[index]}' ? (O/N) : ");
                    string confirm = Console.ReadLine();
                    if (confirm.ToUpper() == "O" || confirm.ToUpper() == "Y")
                    {
                        // Appel de la méthode future deleteJob
                        deleteJob(index);
                        Console.WriteLine("Suppression effectuée.");
                        Console.ReadKey();

                        jobs = getJob();
                        if (jobs.Count == 0) back = true;
                        else index = 0;
                    }
                }
            }
        }

        static void MenuParametres()
        {
            int index = 0;
            bool back = false;

            while (!back)
            {
                // A modifier 
                string[] options = {
            Lang.LangFrench ?? "Français",
            Lang.LangEnglish ?? "English",
            Lang.BtnReturn ?? "Retour"
        };

                Console.Clear();
                // A modifier
                Console.WriteLine($"=== {Lang.SettingsTitle ?? "Paramètres"} ===");
                Console.WriteLine("-------------------------------------");

                // Affichage des options avec surbrillance
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
                        // On affiche un petit indicateur visuel de la langue active actuelle
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

                // Navigation
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
                        case 0: 
                            SetLanguage("fr-FR");
                            break;
                        case 1:
                            SetLanguage("en-US");
                            break;
                        case 2: 
                            back = true;
                            break;
                    }
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    back = true;
                }
            }
        }

        static void SetLanguage(string cultureCode)
        {
            try
            {
                CultureInfo culture = new CultureInfo(cultureCode);
                // Change la langue des ressources (.resx)
                Thread.CurrentThread.CurrentUICulture = culture;
                // Change la langue des formats
                Thread.CurrentThread.CurrentCulture = culture;
            }
            catch (Exception e)
            {
                Console.WriteLine("Erreur de langue : " + e.Message);
            }
        }

        // Ces méthodes ne font rien de concret pour l'instant, juste simuler le comportement
        // On remplacera le contenu plus tard.

        static List<string> getJob()
        {
            // Simulation : retourne la liste mockée
            return mockJobs;
        }

        static void createJob(string name, string source, string dest, string type)
        {
            // Simulation : Ajoute à la liste temporaire
            mockJobs.Add(name);
        }

        static void deleteJob(int index)
        {
            // Simulation
            if (index >= 0 && index < mockJobs.Count) mockJobs.RemoveAt(index);
        }

        static void executeJob(List<int> indexes)
        {
            // Simulation
            foreach (int i in indexes)
            {
                Console.WriteLine($"Exécution de {mockJobs[i]}...");
            }
        }
    }
}