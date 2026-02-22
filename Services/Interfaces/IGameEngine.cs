using MiniRisk.Models;
using MiniRisk.Models.Enums;

namespace MiniRisk.Services.Interfaces;

public interface IGameEngine
{
    // ── Inicialización ──
    GameResult InitializeGame(Game game);
    GameResult DistributeTerritoriesRandomly(Game game);
    GameResult PlaceInitialArmies(Game game, string playerId, TerritoryName territory, int count);
    GameResult StartPlaying(Game game);

    // ── Refuerzo ──
    int CalculateReinforcements(Game game, Player player);
    GameResult PlaceReinforcements(Game game, string playerId, TerritoryName territory, int count);
    GameResult ConfirmReinforcements(Game game, string playerId);

    // ── Ataque ──
    AttackGameResult Attack(Game game, string playerId,
        TerritoryName from, TerritoryName to, int attackDiceCount);
    GameResult MoveArmiesAfterConquest(Game game, string playerId,
        TerritoryName from, TerritoryName to, int armyCount);
    GameResult EndAttackPhase(Game game, string playerId);

    // ── Fortificación ──
    GameResult Fortify(Game game, string playerId,
        TerritoryName from, TerritoryName to, int armyCount);
    GameResult SkipFortification(Game game, string playerId);
    bool AreConnected(Game game, string playerId, TerritoryName from, TerritoryName to);

    // ── Cartas ──
    GameResult TradeCards(Game game, string playerId, string[] cardIds);

    // ── Turno ──
    GameResult EndTurn(Game game, string playerId);

    // ── Consultas ──
    bool IsGameOver(Game game);
    Player? GetWinner(Game game);
}

/// <summary>
/// Resultado genérico de una acción del motor del juego.
/// </summary>
public class GameResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public static GameResult Ok() => new() { Success = true };
    public static GameResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}

/// <summary>
/// Resultado de un ataque, incluye el resultado de los dados.
/// </summary>
public class AttackGameResult : GameResult
{
    public AttackResult? AttackResult { get; set; }
    public bool PlayerEliminated { get; set; }
    public string? EliminatedPlayerId { get; set; }
    public bool GameOver { get; set; }

    public static AttackGameResult OkAttack(AttackResult result) => new()
    {
        Success = true,
        AttackResult = result
    };
    
    public static new AttackGameResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}
