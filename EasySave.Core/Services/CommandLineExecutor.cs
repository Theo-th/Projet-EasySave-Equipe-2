using EasySave.Core.ViewModels;
using EasySave.Core.Interfaces;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineExecutor"/> class.
        /// </summary>
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

            var result = _viewModel.ExecuteJobs(jobIndices);

            if (string.IsNullOrEmpty(result))
            {
                Console.WriteLine("No job specified.");
            }
            else
            {
                Console.WriteLine(result);
            }

            Console.WriteLine("-------------------------------------");
            Console.WriteLine("Execution completed.");
        }
    }
}