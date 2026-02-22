using MiniRisk.Models.Enums;

namespace MiniRisk.Services.Interfaces;

public interface IPlayerSessionService
{
    bool IsIdentified { get; }
    string PlayerId { get; }
    string PlayerName { get; }
    PlayerColor? AssignedColor { get; }
    string? CurrentGameId { get; }
    DateTime ConnectedAt { get; }

    void SetPlayer(string name);
    void SetColor(PlayerColor color);
    void JoinGame(string gameId);
    void LeaveGame();
    void Clear();
}
