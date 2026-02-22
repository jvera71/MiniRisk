using System.Collections.Concurrent;
using MiniRisk.Models;
using MiniRisk.Models.Enums;
using MiniRisk.Services.Interfaces;

namespace MiniRisk.Services;

public class GameManager : IGameManager
{
    private readonly ConcurrentDictionary<string, Game> _games = new();
    private readonly ConcurrentDictionary<string, ConnectedPlayer> _connectedPlayers = new();
    private readonly ConcurrentDictionary<string, string> _connectionToPlayerId = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gameLocks = new();

    public record ConnectedPlayer(
        string PlayerId,
        string PlayerName,
        string? GameId,
        string? ConnectionId,
        DateTime ConnectedAt,
        DateTime? DisconnectedAt,
        bool IsConnected
    );

    // ═══════════════════════════════════════
    // GESTIÓN DE PARTIDAS
    // ═══════════════════════════════════════

    public GameSummary CreateGame(string name, string creatorPlayerId, string creatorPlayerName, GameSettings settings)
    {
        var game = new Game
        {
            Name = name,
            CreatorPlayerId = creatorPlayerId,
            Settings = settings
        };

        _games[game.Id] = game;
        _gameLocks[game.Id] = new SemaphoreSlim(1, 1);

        return new GameSummary
        {
            Id = game.Id,
            Name = game.Name,
            Status = game.Status,
            CurrentPlayers = 0,
            MaxPlayers = settings.MaxPlayers,
            CreatorName = creatorPlayerName,
            CreatedAt = game.CreatedAt
        };
    }

    public List<GameSummary> GetAvailableGames()
    {
        return _games.Values
            .Where(g => g.Status == GameStatus.WaitingForPlayers)
            .Select(g => new GameSummary
            {
                Id = g.Id,
                Name = g.Name,
                Status = g.Status,
                CurrentPlayers = g.Players.Count,
                MaxPlayers = g.Settings.MaxPlayers,
                CreatorName = GetPlayerName(g.CreatorPlayerId) ?? "Desconocido",
                CreatedAt = g.CreatedAt
            })
            .ToList();
    }

    public Game? GetGame(string gameId)
    {
        return _games.GetValueOrDefault(gameId);
    }

    public void RemoveGame(string gameId)
    {
        _games.TryRemove(gameId, out _);
        if (_gameLocks.TryRemove(gameId, out var semaphore))
        {
            semaphore.Dispose();
        }
    }

    // ═══════════════════════════════════════
    // GESTIÓN DE JUGADORES
    // ═══════════════════════════════════════

    public void RegisterPlayer(string playerId, string playerName)
    {
        _connectedPlayers[playerId] = new ConnectedPlayer(
            playerId, playerName, null, null, DateTime.UtcNow, null, true
        );
    }

    public (bool Success, PlayerColor AssignedColor, string? ErrorMessage) AddPlayer(string gameId, string playerId, string playerName, string connectionId)
    {
        if (!_games.TryGetValue(gameId, out var game))
        {
            return (false, PlayerColor.Neutral, "Partida no encontrada.");
        }

        // Simplicidad: Lock sobre el objeto Game, idealmente se usaría ExecuteWithLock pero AddPlayer es síncrono.
        lock (game)
        {
            if (game.Status != GameStatus.WaitingForPlayers)
                return (false, PlayerColor.Neutral, "La partida ya ha comenzado.");

            if (game.Players.Count >= game.Settings.MaxPlayers)
                return (false, PlayerColor.Neutral, "La partida está llena.");

            if (game.Players.Any(p => p.Id == playerId))
                return (false, PlayerColor.Neutral, "Ya estás en la partida.");

            // Asignar color (simple lógica: primero rojo, luego azul, etc)
            var availableColors = Enum.GetValues<PlayerColor>()
                                      .Where(c => c != PlayerColor.Neutral && !game.Players.Any(p => p.Color == c))
                                      .ToList();
            var color = availableColors.First();

            game.Players.Add(new Player
            {
                Id = playerId,
                Name = playerName,
                Color = color,
                IsConnected = true
            });

            UpdatePlayerConnection(gameId, playerId, connectionId);

            return (true, color, null);
        }
    }

    public void RemovePlayer(string gameId, string playerId)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
            if (player != null)
            {
                game.Players.Remove(player);
                if (_connectedPlayers.TryGetValue(playerId, out var cp))
                {
                    _connectedPlayers[playerId] = cp with { GameId = null };
                }
            }
        }
    }

    public ReconnectionInfo? GetReconnectionInfo(string playerName)
    {
        var game = _games.Values.FirstOrDefault(g => 
            g.Status != GameStatus.Finished && 
            g.Players.Any(p => p.Name == playerName && !p.IsConnected));

        if (game != null)
        {
            return new ReconnectionInfo
            {
                GameId = game.Id,
                GameName = game.Name
            };
        }
        return null;
    }

    public void UpdatePlayerConnection(string gameId, string playerId, string connectionId)
    {
        if (_connectedPlayers.TryGetValue(playerId, out var cp))
        {
            _connectedPlayers[playerId] = cp with { ConnectionId = connectionId, IsConnected = true, DisconnectedAt = null };
            _connectionToPlayerId[connectionId] = playerId;
        }
    }

    public string? GetPlayerName(string playerId)
    {
        return _connectedPlayers.TryGetValue(playerId, out var cp) ? cp.PlayerName : null;
    }

    public bool IsNameTaken(string name)
    {
        return _connectedPlayers.Values
            .Any(p => p.IsConnected && p.PlayerName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    // ═══════════════════════════════════════
    // CONEXIONES
    // ═══════════════════════════════════════

    public void RegisterConnection(string connectionId, string playerId)
    {
        _connectionToPlayerId[connectionId] = playerId;
        if (_connectedPlayers.TryGetValue(playerId, out var cp))
        {
            _connectedPlayers[playerId] = cp with { ConnectionId = connectionId, IsConnected = true, DisconnectedAt = null };
        }
    }

    public void UnregisterConnection(string connectionId)
    {
        if (_connectionToPlayerId.TryRemove(connectionId, out var playerId))
        {
            if (_connectedPlayers.TryGetValue(playerId, out var cp))
            {
                _connectedPlayers[playerId] = cp with { ConnectionId = null, IsConnected = false, DisconnectedAt = DateTime.UtcNow };
            }
        }
    }

    public string? GetPlayerIdByConnection(string connectionId)
    {
        return _connectionToPlayerId.GetValueOrDefault(connectionId);
    }

    public void MarkPlayerDisconnected(string gameId, string playerId)
    {
        if (_connectedPlayers.TryGetValue(playerId, out var cp))
        {
            _connectedPlayers[playerId] = cp with { IsConnected = false, DisconnectedAt = DateTime.UtcNow };
        }
    }

    public void MarkPlayerReconnected(string gameId, string playerId, string connectionId)
    {
        _connectionToPlayerId[connectionId] = playerId;
        if (_connectedPlayers.TryGetValue(playerId, out var cp))
        {
            _connectedPlayers[playerId] = cp with { ConnectionId = connectionId, IsConnected = true, DisconnectedAt = null, GameId = gameId };
        }
    }

    // ═══════════════════════════════════════
    // ACCESO CON LOCK
    // ═══════════════════════════════════════

    public async Task<T> ExecuteWithLock<T>(string gameId, Func<Game, T> action)
    {
        if (!_games.TryGetValue(gameId, out var game))
            throw new ArgumentException("Game not found", nameof(gameId));

        var semaphore = _gameLocks.GetOrAdd(gameId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            return action(game);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task ExecuteWithLock(string gameId, Func<Game, Task> action)
    {
        if (!_games.TryGetValue(gameId, out var game))
            throw new ArgumentException("Game not found", nameof(gameId));

        var semaphore = _gameLocks.GetOrAdd(gameId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            await action(game);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
