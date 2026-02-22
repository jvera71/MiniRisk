using MiniRisk.Models.Enums;

namespace MiniRisk.Models.Dtos;

public class ChatMessageDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public PlayerColor PlayerColor { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class SystemMessageDto
{
    public string Message { get; set; } = string.Empty;
    public SystemMessageType Type { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum SystemMessageType
{
    Info,
    Warning,
    Error
}

public class ActionErrorDto
{
    public string Message { get; set; } = string.Empty;
    public string ActionAttempted { get; set; } = string.Empty;
}
