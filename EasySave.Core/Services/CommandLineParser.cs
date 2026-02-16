using System;
using System.Collections.Generic;
using System.Linq;

namespace EasySave.Core.Services
{
    public static class CommandLineParser
    {
        public static List<int>? ParseJobIndices(string[]? args, int maxJobs)
        {
            if (args == null || args.Length == 0)
                return null;

            var indices = new HashSet<int>();

            foreach (var arg in args ?? Array.Empty<string>())
            {
                if (arg.Contains("-") && arg.Split('-') is [var start, var end] &&
                    int.TryParse(start, out int s) && int.TryParse(end, out int e))
                {
                    for (int i = s; i <= e; i++)
                        if (i >= 1 && i <= maxJobs)
                            indices.Add(i - 1);
                }
                else
                {
                    foreach (var part in arg.Split(';'))
                        if (int.TryParse(part.Trim(), out int idx) && idx >= 1 && idx <= maxJobs)
                            indices.Add(idx - 1);
                }
            }

            return indices.Count > 0 ? indices.OrderBy(x => x).ToList() : null;
        }
    }
}