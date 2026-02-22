using MiniRisk.Models.Enums;

namespace MiniRisk.Models.Dtos;

public class TurnChangedDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public PlayerColor PlayerColor { get; set; }
    public GamePhase Phase { get; set; }
    public int TurnNumber { get; set; }
    public int Reinforcements { get; set; }
}

public class PhaseChangedDto
{
    public GamePhase NewPhase { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
}

public class CardsUpdatedDto
{
    public List<CardDto> Cards { get; set; } = [];
}

public class CardDto
{
    public string CardId { get; set; } = string.Empty;
    public CardType Type { get; set; }
    public TerritoryName? Territory { get; set; }
    public string? TerritoryDisplayName { get; set; }
}

public class CardsTradedDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public int ArmiesReceived { get; set; }
    public int TradeNumber { get; set; }
}
