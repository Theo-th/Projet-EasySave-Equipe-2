using EasySave.Core.ViewModels;
using EasySave.Core.Services;
using System;
using System.Collections.Generic;

namespace EasySave.ConsoleUI
{
    class Program
    {
        static void Main(string[] args)
        {
            ViewModelConsole viewModel = new ViewModelConsole();
            List<int>? jobIndices = CommandLineParser.ParseJobIndices(args, 5);

            if (jobIndices != null && jobIndices.Count > 0)
            {
                new CommandLineExecutor(viewModel).Execute(jobIndices);
            }
            else
            {
                new ViewConsole(viewModel).ShowConsole();
            }
        }
    }
}
