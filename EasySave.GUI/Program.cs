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

/// <summary>
/// Main entry point for the EasySave GUI application.
/// Handles both GUI and console modes, and configures Avalonia.
/// </summary>
class Program
{
    private const int ATTACH_PARENT_PROCESS = -1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    /// <summary>
    /// Application entry point. Avoid using Avalonia or third-party APIs before AppMain is called.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        // Check if the application should run in console mode
        bool consoleMode = args.Any(a => a.Equals("-console", StringComparison.OrdinalIgnoreCase));
        string[] filteredArgs = args
            .Where(a => !a.Equals("-console", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // Parse job indices from command-line arguments
        List<int>? jobIndices = CommandLineParser.ParseJobIndices(filteredArgs, 5);

        if (consoleMode || (jobIndices != null && jobIndices.Count > 0))
        {
            // Run in console mode
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
            // Run in GUI mode
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
    }

    /// <summary>
    /// Sets up the console for input/output redirection, especially on Windows.
    /// Attaches to parent console if available, otherwise allocates a new one.
    /// </summary>
    private static void SetupConsole()
    {
        if (OperatingSystem.IsWindows())
        {
            // Try to attach to parent console (cmd/PowerShell), otherwise create a new one.
            if (!AttachConsole(ATTACH_PARENT_PROCESS))
                AllocConsole();

            // Redirect standard streams to the attached/allocated console.
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            Console.SetIn(new StreamReader(Console.OpenStandardInput()));
        }
        // On Linux/macOS, standard streams are already available.
    }

    /// <summary>
    /// Avalonia configuration required for application and designer.
    /// </summary>
    /// <returns>Configured AppBuilder instance.</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
