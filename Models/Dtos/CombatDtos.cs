namespace MiniRisk.Models.Dtos;

public class DiceResultDto
{
    public int[] AttackerDice { get; set; } = [];
    public int[] DefenderDice { get; set; } = [];
    public int AttackerLosses { get; set; }
    public int DefenderLosses { get; set; }
    public string FromTerritoryId { get; set; } = string.Empty;
    public string ToTerritoryId { get; set; } = string.Empty;
    public string AttackerId { get; set; } = string.Empty;
    public string AttackerName { get; set; } = string.Empty;
    public string DefenderId { get; set; } = string.Empty;
    public string DefenderName { get; set; } = string.Empty;
    public bool TerritoryConquered { get; set; }
}

public class TerritoryConqueredDto
{
    public string TerritoryId { get; set; } = string.Empty;
    public string TerritoryName { get; set; } = string.Empty;
    public string PreviousOwnerId { get; set; } = string.Empty;
    public string PreviousOwnerName { get; set; } = string.Empty;
    public string NewOwnerId { get; set; } = string.Empty;
    public string NewOwnerName { get; set; } = string.Empty;
    public int ArmiesMoved { get; set; }
}
