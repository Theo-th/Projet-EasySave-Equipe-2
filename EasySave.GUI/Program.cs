using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using EasySave.Core.Services;
using EasySave.Core.ViewModels;
using EasySave.ConsoleUI;

namespace EasySave.GUI;

class Program
{
    private const int ATTACH_PARENT_PROCESS = -1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    // Entry point for the application. Avoid using Avalonia or third-party APIs before AppMain is called.
    [STAThread]
    public static void Main(string[] args)
    {
        bool consoleMode = args.Any(a => a.Equals("-console", StringComparison.OrdinalIgnoreCase));
        string[] filteredArgs = args
            .Where(a => !a.Equals("-console", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        List<int>? jobIndices = CommandLineParser.ParseJobIndices(filteredArgs, 5);

        if (consoleMode || (jobIndices != null && jobIndices.Count > 0))
        {
            SetupConsole();

            ViewModelConsole viewModel = new ViewModelConsole();

            if (jobIndices != null && jobIndices.Count > 0)
            {
                new CommandLineExecutor(viewModel).Execute(jobIndices);
            }
            else
            {
                new ViewConsole(viewModel).ShowConsole();
            }

            if (OperatingSystem.IsWindows())
                FreeConsole();
        }
        else
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
    }

    private static void SetupConsole()
    {
        if (OperatingSystem.IsWindows())
        {
            // Tente de s'attacher à la console parente (cmd/PowerShell),
            // sinon en crée une nouvelle (double-clic avec -console).
            if (!AttachConsole(ATTACH_PARENT_PROCESS))
                AllocConsole();

            // Redirige les flux standard vers la console attachée/allouée.
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            Console.SetIn(new StreamReader(Console.OpenStandardInput()));
        }
        // Sur Linux/macOS, les flux standard sont déjà disponibles.
    }

    // Avalonia configuration required for application and designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
