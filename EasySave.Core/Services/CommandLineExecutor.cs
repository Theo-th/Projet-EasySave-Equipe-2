using EasySave.Core.ViewModels;
using System;
using System.Collections.Generic;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Handles execution of backup jobs launched from the command line.
    /// </summary>
    public class CommandLineExecutor
    {
        private readonly ViewModelConsole _viewModel;

        public CommandLineExecutor(ViewModelConsole viewModel)
        {
            _viewModel = viewModel;
        }

        /// <summary>
        /// Executes the specified jobs and displays the results.
        /// </summary>
        public void Execute(List<int> jobIndices)
        {
            Console.WriteLine("EasySave - Command line execution mode");
            Console.WriteLine("-------------------------------------");

            var results = _viewModel.ExecuteJobs(jobIndices);

            foreach (var (index, message) in results)
            {
                string jobName = _viewModel.GetJob(index) ?? $"Job {index + 1}";
                Console.WriteLine($"Job {index + 1} ({jobName}): {message ?? "Success"}");
            }

            Console.WriteLine("-------------------------------------");
            Console.WriteLine("Execution completed.");
        }
    }
}