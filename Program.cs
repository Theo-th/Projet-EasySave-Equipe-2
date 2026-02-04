using Projet_EasySave.Services;
using Projet_EasySave.ViewModels;

namespace Projet_EasySave
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ViewModelConsole service = new ViewModelConsole();

            
            // Test 5: Récupérer un travail spécifique
            Console.WriteLine("Test 5: Récupération travail par index");
            string? job0 = service.GetJob(0);
            Console.WriteLine($"Travail 0: {job0}\n");
        }
    }
}
