using Projet_EasySave.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Projet_EasySave.Properties;

namespace Projet_EasySave
{
    /// <summary>
    /// Point d'entrée principal de l'application EasySave
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            ViewModelConsole viewModel = new ViewModelConsole();
            ViewConsole view = new ViewConsole(viewModel);
            view.ShowConsole();
        }
    }
}
