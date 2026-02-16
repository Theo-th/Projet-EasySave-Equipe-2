using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Core.Services
{
    public class ProcessStatusChangedEventArgs : EventArgs
    {
        public DetectedProcess Process { get; set; } = null!;
        public bool IsRunning { get; set; }
        public DateTime ChangeTime { get; set; } = DateTime.Now;
    }

    public class DetectedProcess
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime DetectionTime { get; set; } = DateTime.Now;

        public override bool Equals(object? obj) =>
            obj is DetectedProcess other && ProcessId == other.ProcessId && ProcessName == other.ProcessName;

        public override int GetHashCode() => HashCode.Combine(ProcessId, ProcessName);
    }

    /// <summary>
    /// Detects and monitors business processes during backup operations.
    /// </summary>
    public class ProcessDetector : IDisposable
    {
        private readonly string _jsonFilePath;
        private List<string> _watchedProcessNames = new();
        private Dictionary<string, bool> _processStatus = new();
        private CancellationTokenSource? _cancellationToken;
        private Task? _monitoringTask;

        public event EventHandler<ProcessStatusChangedEventArgs>? ProcessStatusChanged;

        public ProcessDetector(string? jsonFilePath = null)
        {
            _jsonFilePath = jsonFilePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "watched_processes.json");

            Directory.CreateDirectory(Path.GetDirectoryName(_jsonFilePath)!);
            LoadFromJson();
        }

        /// <summary>
        /// Adds a process to the watch list
        /// </summary>
        public void AddWatchedProcess(string processName)
        {
            if (!_watchedProcessNames.Contains(processName))
            {
                _watchedProcessNames.Add(processName);
                _processStatus[processName] = IsProcessRunning(processName);
                SaveToJson();
            }
        }

        /// <summary>
        /// Removes a process from the watch list
        /// </summary>
        public void RemoveWatchedProcess(string processName)
        {
            if (_watchedProcessNames.Remove(processName))
            {
                _processStatus.Remove(processName);
                SaveToJson();
            }
        }

        /// <summary>
        /// Gets the list of watched processes
        /// </summary>
        public List<string> GetWatchedProcesses() => new(_watchedProcessNames);

        /// <summary>
        /// Checks if any watched process is currently running.
        /// Returns the name of the first detected running process, or null if none.
        /// </summary>
        public string? IsAnyWatchedProcessRunning()
        {
            foreach (var processName in _watchedProcessNames)
            {
                if (IsProcessRunning(processName))
                    return processName;
            }
            return null;
        }

        /// <summary>
        /// Checks the current status of all watched processes
        /// </summary>
        public void CheckProcessesNow()
        {
            foreach (var processName in _watchedProcessNames)
            {
                bool isRunning = IsProcessRunning(processName);
                bool wasRunning = _processStatus.GetValueOrDefault(processName, false);

                if (isRunning != wasRunning)
                {
                    _processStatus[processName] = isRunning;
                    OnProcessStatusChanged(processName, isRunning);
                }
            }
        }

        /// <summary>
        /// Starts continuous monitoring of watched processes
        /// </summary>
        public void StartContinuousMonitoring(int intervalMs = 2000)
        {
            if (_monitoringTask != null)
                throw new InvalidOperationException("Monitoring already in progress.");

            _cancellationToken = new CancellationTokenSource();

            _monitoringTask = Task.Run(async () =>
            {
                while (!_cancellationToken.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(intervalMs, _cancellationToken.Token);
                        CheckProcessesNow();
                    }
                    catch (OperationCanceledException) { break; }
                    catch { }
                }
            });
        }

        /// <summary>
        /// Stops the continuous monitoring
        /// </summary>
        public async Task StopContinuousMonitoring()
        {
            if (_cancellationToken == null) return;

            _cancellationToken.Cancel();
            if (_monitoringTask != null)
                try { await _monitoringTask; }
                catch (OperationCanceledException) { }

            _monitoringTask = null;
        }

        /// <summary>
        /// Checks if a process is currently running
        /// </summary>
        private bool IsProcessRunning(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                var isRunning = processes.Length > 0;
                foreach (var p in processes) p.Dispose();
                return isRunning;
            }
            catch
            {
                return false;
            }
        }

        private void OnProcessStatusChanged(string processName, bool isRunning)
        {
            try
            {
                var process = Process.GetProcessesByName(processName).FirstOrDefault();
                var detected = new DetectedProcess
                {
                    ProcessId = process?.Id ?? -1,
                    ProcessName = processName,
                    FilePath = process?.MainModule?.FileName ?? "Unknown",
                    DetectionTime = DateTime.Now
                };
                process?.Dispose();

                ProcessStatusChanged?.Invoke(this, new ProcessStatusChangedEventArgs
                {
                    Process = detected,
                    IsRunning = isRunning,
                    ChangeTime = DateTime.Now
                });
            }
            catch { }
        }

        public void LoadFromJson()
        {
            try
            {
                if (File.Exists(_jsonFilePath))
                {
                    var json = File.ReadAllText(_jsonFilePath);
                    _watchedProcessNames = JsonSerializer.Deserialize<List<string>>(json) ?? new();
                    foreach (var name in _watchedProcessNames)
                        _processStatus[name] = IsProcessRunning(name);
                }
            }
            catch { _watchedProcessNames = new(); }
        }

        private void SaveToJson()
        {
            try
            {
                var json = JsonSerializer.Serialize(_watchedProcessNames, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_jsonFilePath, json);
            }
            catch { }
        }

        public void Dispose()
        {
            StopContinuousMonitoring().Wait();
            _cancellationToken?.Dispose();
        }
    }
}