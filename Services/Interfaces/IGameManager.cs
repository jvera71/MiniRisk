using MiniRisk.Models;

namespace MiniRisk.Services.Interfaces;

public interface IGameManager
{
    // Gestión de partidas
    GameSummary CreateGame(string name, string creatorPlayerId,
        string creatorPlayerName, GameSettings settings);
    List<GameSummary> GetAvailableGames();
    Game? GetGame(string gameId);
    // GameStateDto GetGameState(string gameId); // DTOs might be in Phase 4, but let's see.
    void RemoveGame(string gameId);

    // Gestión de jugadores en partida
    // JoinResult AddPlayer(...); // JoinResult check Doc 07 or 11
    void RemovePlayer(string gameId, string playerId);
    void UpdatePlayerConnection(string gameId, string playerId, string connectionId);
    string? GetPlayerName(string playerId);

    // Conexiones globales
    void RegisterConnection(string connectionId, string playerId);
    void UnregisterConnection(string connectionId);
    string? GetPlayerIdByConnection(string connectionId);
    bool IsNameTaken(string name);

    // Desconexión / Reconexión
    void MarkPlayerDisconnected(string gameId, string playerId);
    void MarkPlayerReconnected(string gameId, string playerId, string connectionId);
    // ReconnectionInfo? GetReconnectionInfo(string playerName);

    // Acceso con lock
    Task<T> ExecuteWithLock<T>(string gameId, Func<Game, T> action);
    Task ExecuteWithLock(string gameId, Func<Game, Task> action);
    
    // Additional methods from RegisterPlayer as seen in Doc 03 section 7.5
    void RegisterPlayer(string playerId, string playerName);
}
