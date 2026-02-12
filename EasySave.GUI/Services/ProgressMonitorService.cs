using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using EasySave.Core.Models;

namespace EasySave.GUI.Services;

public class ProgressMonitorService : IDisposable
{
    private Timer? _timer;
    private readonly string _statePath;
    private readonly Action<double, string> _onProgressUpdate;
    
    public ProgressMonitorService(string statePath, Action<double, string> onProgressUpdate)
    {
        _statePath = statePath;
        _onProgressUpdate = onProgressUpdate;
    }
    
    public void Start()
    {
        _timer = new Timer(UpdateProgress, null, 0, 200);
    }
    
    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }
    
    private void UpdateProgress(object? state)
    {
        try
        {
            if (!File.Exists(_statePath))
                return;
                
            string json = File.ReadAllText(_statePath);
            var states = JsonSerializer.Deserialize<List<BackupJobState>>(json);
            
            if (states == null || states.Count == 0)
                return;
                
            var activeState = states.FirstOrDefault(s => s.State == BackupState.Active);
            if (activeState == null)
                return;
                
            double progress = 0;
            if (activeState.TotalSize > 0)
            {
                long processedSize = activeState.TotalSize - activeState.RemainingSize;
                progress = (double)processedSize / activeState.TotalSize * 100.0;
            }
            
            string currentFile = !string.IsNullOrEmpty(activeState.CurrentSourceFile) 
                ? Path.GetFileName(activeState.CurrentSourceFile) 
                : "";
                
            _onProgressUpdate(progress, currentFile);
        }
        catch
        {
            // Silently ignore errors during monitoring
        }
    }
    
    public void Dispose()
    {
        Stop();
    }
}
