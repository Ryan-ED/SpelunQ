namespace SpelunQ_wpf.Models;

public class RabbitMessage
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Content { get; init; } = string.Empty;
    public DateTime ReceivedAt { get; init; } = DateTime.Now;
    public string Queue { get; init; } = string.Empty;
    public string Exchange { get; init; } = string.Empty;
    public string RoutingKey { get; init; } = string.Empty;
    public Dictionary<string, object> Headers { get; init; } = new();
        
    public string DisplayContent => Content.Length > 100 ?
        string.Concat(Content.AsSpan(0, 100), "...") : Content;
}