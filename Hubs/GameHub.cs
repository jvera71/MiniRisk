using Microsoft.AspNetCore.SignalR;
using MiniRisk.Models;
using MiniRisk.Models.Dtos;
using MiniRisk.Models.Enums;
using MiniRisk.Services.Interfaces;

namespace MiniRisk.Hubs;

public class GameHub : Hub
{
    private readonly IGameManager _gameManager;
    private readonly IGameEngine _gameEngine;
    private readonly IMapService _mapService;

    public GameHub(IGameManager gameManager, IGameEngine gameEngine, IMapService mapService)
    {
        _gameManager = gameManager;
        _gameEngine = gameEngine;
        _mapService = mapService;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var playerId = _gameManager.GetPlayerIdByConnection(Context.ConnectionId);
        if (playerId != null)
        {
            _gameManager.UnregisterConnection(Context.ConnectionId);
            // Ideally notify games where this player was active
        }
        await base.OnDisconnectedAsync(exception);
    }

    // ═══════════════════════════════════════
    // GESTIÓN DE PARTIDAS
    // ═══════════════════════════════════════

    public async Task JoinGame(string gameId, string playerId, string playerName)
    {
        await _gameManager.ExecuteWithLock(gameId, async (game) =>
        {
            if (game.Status != GameStatus.WaitingForPlayers)
            {
                await Clients.Caller.SendAsync("ActionError", new ActionErrorDto { Message = "La partida ya ha comenzado.", ActionAttempted = "JoinGame" });
                return;
            }

            if (game.Players.Count >= game.Settings.MaxPlayers)
            {
                await Clients.Caller.SendAsync("ActionError", new ActionErrorDto { Message = "La partida está llena.", ActionAttempted = "JoinGame" });
                return;
            }

            if (game.Players.Any(p => p.Id == playerId))
            {
                // Re-unirse si ya estaba (ej: recarga de página rápida)
                await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(gameId));
                await Clients.Caller.SendAsync("GameStateUpdated", game.ToDto(_mapService));
                return;
            }

            var newPlayer = new Player
            {
                Id = playerId,
                Name = playerName,
                Color = (PlayerColor)game.Players.Count // Asignación automática de color
            };

            game.Players.Add(newPlayer);
            _gameManager.UpdatePlayerConnection(gameId, playerId, Context.ConnectionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(gameId));

            await Clients.Group(GetGroupName(gameId)).SendAsync("PlayerJoined", new PlayerJoinedDto
            {
                PlayerId = playerId,
                PlayerName = playerName,
                Color = newPlayer.Color
            });

            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated", game.ToDto(_mapService));
        });
    }

    public async Task StartGame(string gameId, string playerId)
    {
        await _gameManager.ExecuteWithLock(gameId, async (game) =>
        {
            if (game.CreatorPlayerId != playerId)
            {
                await Clients.Caller.SendAsync("ActionError", new ActionErrorDto { Message = "Solo el creador puede iniciar la partida.", ActionAttempted = "StartGame" });
                return;
            }

            if (game.Players.Count < 2)
            {
                await Clients.Caller.SendAsync("ActionError", new ActionErrorDto { Message = "Se necesitan al menos 2 jugadores.", ActionAttempted = "StartGame" });
                return;
            }

            _gameEngine.InitializeGame(game);
            _gameEngine.DistributeTerritoriesRandomly(game);

            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated", game.ToDto(_mapService));
            
            // Notificar a cada jugador sobre sus cartas (aunque al inicio no suelen tener)
            foreach (var player in game.Players)
            {
                // Note: Requeriría saber el ConnectionId de cada uno. GameManager lo tienne.
            }
        });
    }

    // ═══════════════════════════════════════
    // ACCIONES DE JUEGO
    // ═══════════════════════════════════════

    public async Task PlaceReinforcements(string gameId, string playerId, TerritoryName territory, int count)
    {
        await _gameManager.ExecuteWithLock(gameId, async (game) =>
        {
            var result = _gameEngine.PlaceReinforcements(game, playerId, territory, count);
            if (!result.Success)
            {
                await Clients.Caller.SendAsync("ActionError", new ActionErrorDto { Message = result.ErrorMessage!, ActionAttempted = "PlaceReinforcements" });
                return;
            }

            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated", game.ToDto(_mapService));
        });
    }

    public async Task ConfirmReinforcements(string gameId, string playerId)
    {
        await _gameManager.ExecuteWithLock(gameId, async (game) =>
        {
            var result = _gameEngine.ConfirmReinforcements(game, playerId);
            if (!result.Success)
            {
                await Clients.Caller.SendAsync("ActionError", new ActionErrorDto { Message = result.ErrorMessage!, ActionAttempted = "ConfirmReinforcements" });
                return;
            }

            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated", game.ToDto(_mapService));
        });
    }

    public async Task Attack(string gameId, string playerId, TerritoryName from, TerritoryName to, int diceCount)
    {
        await _gameManager.ExecuteWithLock(gameId, async (game) =>
        {
            var result = _gameEngine.Attack(game, playerId, from, to, diceCount);
            if (!result.Success)
            {
                await Clients.Caller.SendAsync("ActionError", new ActionErrorDto { Message = result.ErrorMessage!, ActionAttempted = "Attack" });
                return;
            }

            // Notificar el resultado de los dados primero
            var diceDto = new DiceResultDto
            {
                AttackerDice = result.AttackResult!.AttackerDice,
                DefenderDice = result.AttackResult.DefenderDice,
                AttackerLosses = result.AttackResult.AttackerLosses,
                DefenderLosses = result.AttackResult.DefenderLosses,
                FromTerritoryId = from.ToString(),
                ToTerritoryId = to.ToString(),
                AttackerName = game.GetPlayerById(playerId)?.Name ?? "Atacante",
                DefenderName = game.GetPlayerById(game.Territories[to].OwnerId)?.Name ?? "Defensor",
                TerritoryConquered = result.AttackResult.TerritoryConquered
            };

            await Clients.Group(GetGroupName(gameId)).SendAsync("DiceRolled", diceDto);

            // Si hubo conquista, se actualiza el estado (el Engine ya cambió el owner)
            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated", game.ToDto(_mapService));

            if (result.GameOver)
            {
                var winner = _gameEngine.GetWinner(game);
                await Clients.Group(GetGroupName(gameId)).SendAsync("GameOver", new GameOverDto
                {
                    WinnerId = winner?.Id ?? "",
                    WinnerName = winner?.Name ?? "",
                    WinnerColor = winner?.Color ?? PlayerColor.Neutral,
                    TotalTurns = game.TurnNumber,
                    Duration = DateTime.UtcNow - (game.StartedAt ?? DateTime.UtcNow)
                });
            }
        });
    }

    public async Task Fortify(string gameId, string playerId, TerritoryName from, TerritoryName to, int count)
    {
        await _gameManager.ExecuteWithLock(gameId, async (game) =>
        {
            var result = _gameEngine.Fortify(game, playerId, from, to, count);
            if (!result.Success)
            {
                await Clients.Caller.SendAsync("ActionError", new ActionErrorDto { Message = result.ErrorMessage!, ActionAttempted = "Fortify" });
                return;
            }

            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated", game.ToDto(_mapService));
        });
    }

    public async Task SkipFortification(string gameId, string playerId)
    {
        await _gameManager.ExecuteWithLock(gameId, async (game) =>
        {
            var result = _gameEngine.SkipFortification(game, playerId);
            if (!result.Success)
            {
                await Clients.Caller.SendAsync("ActionError", new ActionErrorDto { Message = result.ErrorMessage!, ActionAttempted = "SkipFortification" });
                return;
            }

            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated", game.ToDto(_mapService));
        });
    }

    // ═══════════════════════════════════════
    // AUXILIARES
    // ═══════════════════════════════════════

    private string GetGroupName(string gameId) => $"game-{gameId}";
}
