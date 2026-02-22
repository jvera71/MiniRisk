using MiniRisk.Models.Enums;
using MiniRisk.Services.Interfaces;

namespace MiniRisk.Services;

public class PlayerSessionService : IPlayerSessionService
{
    public bool IsIdentified { get; private set; }
    public string PlayerId { get; private set; } = string.Empty;
    public string PlayerName { get; private set; } = string.Empty;
    public PlayerColor? AssignedColor { get; private set; }
    public string? CurrentGameId { get; private set; }
    public DateTime ConnectedAt { get; private set; }

    public void SetPlayer(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre no puede estar vacío.", nameof(name));

        PlayerId = Guid.NewGuid().ToString();
        PlayerName = name.Trim();
        ConnectedAt = DateTime.UtcNow;
        IsIdentified = true;
    }

    public void SetColor(PlayerColor color)
    {
        AssignedColor = color;
    }

    public void JoinGame(string gameId)
    {
        CurrentGameId = gameId ?? throw new ArgumentNullException(nameof(gameId));
    }

    public void LeaveGame()
    {
        CurrentGameId = null;
        AssignedColor = null;
    }

    public void Clear()
    {
        IsIdentified = false;
        PlayerId = string.Empty;
        PlayerName = string.Empty;
        AssignedColor = null;
        CurrentGameId = null;
    }
}
