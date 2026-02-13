namespace EasySave.GUI.Models;

public class JobItem
{
    public string Name { get; set; } = "";
    public int Index { get; set; }
    public bool IsSelected { get; set; }
    public string Type { get; set; } = "";
    public string Source { get; set; } = "";
    public string Target { get; set; } = "";
}
