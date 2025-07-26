namespace SpelunQ.Models;

public class QueueInfo
{
    public string Name { get; set; } = string.Empty;
    public int Messages { get; set; }
    public int MessagesReady { get; set; }
    public int MessagesUnacknowledged { get; set; }
    public bool Durable { get; set; }
    public bool AutoDelete { get; set; }
    public string State { get; set; } = string.Empty;
        
    public string DisplayInfo => $"{Name} ({Messages} messages)";
}