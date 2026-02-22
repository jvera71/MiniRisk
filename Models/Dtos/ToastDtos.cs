namespace MiniRisk.Models.Dtos;

public enum ToastType
{
    Info,
    Success,
    Warning,
    Error,
    Conquest,
    Elimination,
    YourTurn
}

public class ToastMessage
{
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; }
    public bool Persistent { get; set; }
    public DateTime CreatedAt { get; set; }
}
