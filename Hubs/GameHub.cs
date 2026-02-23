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
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task RegisterPlayer(string playerId, string playerName)
    {
        _gameManager.RegisterConnection(Context.ConnectionId, playerId);
        var games = _gameManager.GetAvailableGames();
        await Clients.Caller.SendAsync("LobbyUpdated", games);
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

            if (game.Players.Count >= game.Settings.MaxPlayers && !game.Players.Any(p => p.Id == playerId))
            {
                await Clients.Caller.SendAsync("ActionError", new ActionErrorDto { Message = "La partida está llena.", ActionAttempted = "JoinGame" });
                return;
            }

            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
            {
                var result = _gameManager.AddPlayer(gameId, playerId, playerName, Context.ConnectionId);
                if (!result.Success)
                {
                    await Clients.Caller.SendAsync("ActionError", new ActionErrorDto { Message = result.ErrorMessage!, ActionAttempted = "JoinGame" });
                    return;
                }
                player = game.Players.FirstOrDefault(p => p.Id == playerId);
            }
            else
            {
                _gameManager.UpdatePlayerConnection(gameId, playerId, Context.ConnectionId);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(gameId));

            if (player != null)
            {
                await Clients.Group(GetGroupName(gameId)).SendAsync("PlayerJoined", new PlayerJoinedDto
                {
                    PlayerId = playerId,
                    PlayerName = player.Name,
                    Color = player.Color
                });
            }

            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated", game.ToDto(_mapService));
            
            // Notificar al lobby que una partida ha cambiado (ej: número de jugadores)
            await Clients.All.SendAsync("LobbyUpdated", _gameManager.GetAvailableGames());
        });
    }

    public async Task JoinGameGroup(string gameId)
    {
        var playerId = _gameManager.GetPlayerIdByConnection(Context.ConnectionId);
        if (playerId == null) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(gameId));
        
        var game = _gameManager.GetGame(gameId);
        if (game != null)
        {
            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
            if (player != null)
            {
                await Clients.Group(GetGroupName(gameId)).SendAsync("PlayerJoined", new PlayerJoinedDto
                {
                    PlayerId = playerId,
                    PlayerName = player.Name,
                    Color = player.Color
                });
            }
            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated", game.ToDto(_mapService));
            await Clients.All.SendAsync("LobbyUpdated", _gameManager.GetAvailableGames());
        }
    }

    public async Task LeaveGameGroup(string gameId)
    {
        var playerId = _gameManager.GetPlayerIdByConnection(Context.ConnectionId);
        if (playerId == null) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(gameId));
        
        var playerName = _gameManager.GetPlayerName(playerId) ?? "Jugador";
        await Clients.Group(GetGroupName(gameId)).SendAsync("PlayerLeft", new PlayerLeftDto
        {
            PlayerId = playerId,
            PlayerName = playerName
        });
        
        var game = _gameManager.GetGame(gameId);
        if (game != null)
        {
            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated", game.ToDto(_mapService));
            await Clients.All.SendAsync("LobbyUpdated", _gameManager.GetAvailableGames());
        }
    }


    public async Task LeaveGame(string gameId, string playerId)
    {
        _gameManager.RemovePlayer(gameId, playerId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(gameId));
        
        var playerName = _gameManager.GetPlayerName(playerId) ?? "Jugador";
        await Clients.Group(GetGroupName(gameId)).SendAsync("PlayerLeft", new PlayerLeftDto
        {
            PlayerId = playerId,
            PlayerName = playerName
        });
        
        var game = _gameManager.GetGame(gameId);
        if (game != null)
        {
            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated", game.ToDto(_mapService));
        }
    }

    public async Task RejoinGame(string gameId, string playerId)
    {
        _gameManager.UpdatePlayerConnection(gameId, playerId, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(gameId));
        
        var playerName = _gameManager.GetPlayerName(playerId) ?? "Jugador";
        await Clients.Group(GetGroupName(gameId)).SendAsync("PlayerReconnected", new PlayerReconnectedDto
        {
            PlayerId = playerId,
            PlayerName = playerName
        });
        
        var game = _gameManager.GetGame(gameId);
        if (game != null)
        {
            await Clients.Caller.SendAsync("GameStateUpdated", game.ToDto(_mapService));
        }
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

            var firstPlayer = game.GetCurrentPlayer();
            var gameStateDto = game.ToDto(_mapService);

            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated",    gameStateDto);
            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStarting", gameId);
            await Clients.Group(GetGroupName(gameId)).SendAsync("TurnChanged", new TurnChangedDto
            {
                PlayerId = firstPlayer.Id,
                PlayerName = firstPlayer.Name,
                PlayerColor = firstPlayer.Color,
                Phase = game.Phase,
                TurnNumber = game.TurnNumber,
                Reinforcements = game.RemainingReinforcements
            });

            await Clients.Group(GetGroupName(gameId)).SendAsync("SystemMessage", new SystemMessageDto { 
                Message = "¡La partida ha comenzado! Distribuyendo territorios...",
                Type = SystemMessageType.Info,
                Timestamp = DateTime.UtcNow
            });
            await Clients.All.SendAsync("LobbyUpdated", _gameManager.GetAvailableGames());
            
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

    public async Task PlaceInitialArmies(string gameId, string playerId, TerritoryName territory, int count)
    {
        await _gameManager.ExecuteWithLock(gameId, async (game) =>
        {
            var result = _gameEngine.PlaceInitialArmies(game, playerId, territory, count);
            if (!result.Success)
            {
                await Clients.Caller.SendAsync("ActionError", new ActionErrorDto { Message = result.ErrorMessage!, ActionAttempted = "PlaceInitialArmies" });
                return;
            }

            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated", game.ToDto(_mapService));
        });
    }

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
            await Clients.Group(GetGroupName(gameId)).SendAsync("PhaseChanged", new PhaseChangedDto
            {
                NewPhase = game.Phase,
                PlayerId = playerId,
                PlayerName = game.GetPlayerById(playerId)?.Name ?? ""
            });
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

            if (result.PlayerEliminated)
            {
                var eliminated = game.GetPlayerById(result.EliminatedPlayerId!);
                await Clients.Group(GetGroupName(gameId)).SendAsync("PlayerEliminated", new PlayerEliminatedDto
                {
                    EliminatedPlayerId = result.EliminatedPlayerId!,
                    EliminatedPlayerName = eliminated?.Name ?? "Jugador",
                    EliminatedByPlayerId = playerId,
                    EliminatedByPlayerName = game.GetPlayerById(playerId)?.Name ?? "Atacante"
                });
            }

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

    public async Task MoveArmiesAfterConquest(string gameId, string playerId, TerritoryName from, TerritoryName to, int count)
    {
        await _gameManager.ExecuteWithLock(gameId, async (game) =>
        {
            var result = _gameEngine.MoveArmiesAfterConquest(game, playerId, from, to, count);
            if (!result.Success)
            {
                await Clients.Caller.SendAsync("ActionError", new ActionErrorDto { Message = result.ErrorMessage!, ActionAttempted = "MoveArmiesAfterConquest" });
                return;
            }

            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated", game.ToDto(_mapService));
        });
    }

    public async Task EndAttackPhase(string gameId, string playerId)
    {
        await _gameManager.ExecuteWithLock(gameId, async (game) =>
        {
            var result = _gameEngine.EndAttackPhase(game, playerId);
            if (!result.Success)
            {
                await Clients.Caller.SendAsync("ActionError", new ActionErrorDto { Message = result.ErrorMessage!, ActionAttempted = "EndAttackPhase" });
                return;
            }

            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated", game.ToDto(_mapService));
            await Clients.Group(GetGroupName(gameId)).SendAsync("PhaseChanged", new PhaseChangedDto
            {
                NewPhase = game.Phase,
                PlayerId = playerId,
                PlayerName = game.GetPlayerById(playerId)?.Name ?? ""
            });
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
            await BroadcastTurnChange(gameId, game);
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
            await BroadcastTurnChange(gameId, game);
        });
    }

    public async Task EndTurn(string gameId, string playerId)
    {
        await _gameManager.ExecuteWithLock(gameId, async (game) =>
        {
            var result = _gameEngine.EndTurn(game, playerId);
            if (!result.Success)
            {
                await Clients.Caller.SendAsync("ActionError", new ActionErrorDto { Message = result.ErrorMessage!, ActionAttempted = "EndTurn" });
                return;
            }

            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated", game.ToDto(_mapService));
            await BroadcastTurnChange(gameId, game);
        });
    }

    public async Task TradeCards(string gameId, string playerId, string[] cardIds)
    {
        await _gameManager.ExecuteWithLock(gameId, async (game) =>
        {
            var result = _gameEngine.TradeCards(game, playerId, cardIds);
            if (!result.Success)
            {
                await Clients.Caller.SendAsync("ActionError", new ActionErrorDto { Message = result.ErrorMessage!, ActionAttempted = "TradeCards" });
                return;
            }

            var player = game.GetPlayerById(playerId);
            if (player != null)
            {
                await Clients.Group(GetGroupName(gameId)).SendAsync("CardsTraded", new CardsTradedDto
                {
                    PlayerId = playerId,
                    PlayerName = player.Name,
                    TradeNumber = game.TradeCount,
                    ArmiesReceived = 0 // En una versión más sofisticada, lo devolvería el Engine en el Result 
                });
            }

            await Clients.Group(GetGroupName(gameId)).SendAsync("GameStateUpdated", game.ToDto(_mapService));
        });
    }

    public async Task SendChatMessage(string gameId, string playerId, string message)
    {
        var game = _gameManager.GetGame(gameId);
        if (game == null) return;
        
        var player = game.GetPlayerById(playerId);
        if (player == null) return;

        await Clients.Group(GetGroupName(gameId)).SendAsync("ChatMessageReceived", new ChatMessageDto
        {
            PlayerId = playerId,
            PlayerName = player.Name,
            PlayerColor = player.Color,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }

    // ═══════════════════════════════════════
    // AUXILIARES
    // ═══════════════════════════════════════

    private async Task BroadcastTurnChange(string gameId, Game game)
    {
        var nextPlayer = game.GetCurrentPlayer();
        await Clients.Group(GetGroupName(gameId)).SendAsync("TurnChanged", new TurnChangedDto
        {
            PlayerId = nextPlayer.Id,
            PlayerName = nextPlayer.Name,
            PlayerColor = nextPlayer.Color,
            Phase = game.Phase,
            TurnNumber = game.TurnNumber,
            Reinforcements = game.RemainingReinforcements
        });
    }

    private string GetGroupName(string gameId) => $"game-{gameId}";
}
