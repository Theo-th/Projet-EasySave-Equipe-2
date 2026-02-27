using System.Collections.Generic;
using EasySave.Core.Models;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Utility service for sorting and categorizing files to be backed up by priority and size.
    /// </summary>
    public static class FileJobCategorizer
    {
        /// <summary>
        /// Categorizes a collection of backup files into four lists based on priority and size threshold.
        /// </summary>
        public static void CategorizeFiles(IEnumerable<FileJob> files, long sizeThreshold,
            out List<FileJob> priorityLight, out List<FileJob> nonPriorityLight,
            out List<FileJob> priorityHeavy, out List<FileJob> nonPriorityHeavy)
        {
            priorityLight = new List<FileJob>();
            nonPriorityLight = new List<FileJob>();
            priorityHeavy = new List<FileJob>();
            nonPriorityHeavy = new List<FileJob>();

            foreach (var file in files)
            {
                if (file.IsPriority && file.FileSize <= sizeThreshold)
                    priorityLight.Add(file);
                else if (!file.IsPriority && file.FileSize <= sizeThreshold)
                    nonPriorityLight.Add(file);
                else if (file.IsPriority && file.FileSize > sizeThreshold)
                    priorityHeavy.Add(file);
                else
                    nonPriorityHeavy.Add(file);
            }
        }
    }
}
