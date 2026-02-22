using MiniRisk.Models.Enums;

namespace MiniRisk.Models.Dtos;

public class GameStateDto
{
    public string GameId { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public GameStatus Status { get; set; }
    public GamePhase Phase { get; set; }
    public string CurrentPlayerId { get; set; } = string.Empty;
    public string CurrentPlayerName { get; set; } = string.Empty;
    public int TurnNumber { get; set; }
    public int TradeCount { get; set; }
    public int RemainingReinforcements { get; set; }
    public List<PlayerDto> Players { get; set; } = [];
    public List<TerritoryDto> Territories { get; set; } = [];
    public List<GameEventDto> RecentEvents { get; set; } = [];
    public DateTime StartedAt { get; set; }
}

public class PlayerDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public PlayerColor Color { get; set; }
    public int TerritoryCount { get; set; }
    public int TotalArmies { get; set; }
    public int CardCount { get; set; }
    public bool IsConnected { get; set; }
    public bool IsEliminated { get; set; }
}

public class TerritoryDto
{
    public string TerritoryId { get; set; } = string.Empty;
    public string TerritoryName { get; set; } = string.Empty;
    public ContinentName Continent { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public PlayerColor OwnerColor { get; set; }
    public int Armies { get; set; }
}

public class GameEventDto
{
    public string Message { get; set; } = string.Empty;
    public GameEventType Type { get; set; }
    public string? PlayerId { get; set; }
    public string? PlayerName { get; set; }
    public DateTime Timestamp { get; set; }
}
