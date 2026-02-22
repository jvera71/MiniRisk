using MiniRisk.Models.Enums;

namespace MiniRisk.Models.Dtos;

public class GameOverDto
{
    public string WinnerId { get; set; } = string.Empty;
    public string WinnerName { get; set; } = string.Empty;
    public PlayerColor WinnerColor { get; set; }
    public int TotalTurns { get; set; }
    public TimeSpan Duration { get; set; }
    public List<PlayerFinalStatsDto> PlayerStats { get; set; } = [];
}

public class PlayerFinalStatsDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public PlayerColor Color { get; set; }
    public int TerritoriesConquered { get; set; }
    public int TerritoriesLost { get; set; }
    public int ArmiesDestroyed { get; set; }
    public int ArmiesLost { get; set; }
    public int CardsTraded { get; set; }
    public bool IsWinner { get; set; }
    public int? EliminatedOnTurn { get; set; }
}
