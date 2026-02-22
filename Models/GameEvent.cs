using MiniRisk.Models.Enums;

namespace MiniRisk.Models;

public class GameEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public GameEventType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PlayerId { get; set; }
    public string? PlayerName { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
