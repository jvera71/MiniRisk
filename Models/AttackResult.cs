using MiniRisk.Models.Enums;

namespace MiniRisk.Models;

public class AttackResult
{
    /// <summary>Dados del atacante, ordenados de mayor a menor.</summary>
    public int[] AttackerDice { get; set; } = [];
    
    /// <summary>Dados del defensor, ordenados de mayor a menor.</summary>
    public int[] DefenderDice { get; set; } = [];
    
    /// <summary>Ejércitos perdidos por el atacante.</summary>
    public int AttackerLosses { get; set; }
    
    /// <summary>Ejércitos perdidos por el defensor.</summary>
    public int DefenderLosses { get; set; }
    
    /// <summary>Territorio desde el que se atacó.</summary>
    public TerritoryName FromTerritory { get; set; }
    
    /// <summary>Territorio atacado.</summary>
    public TerritoryName ToTerritory { get; set; }
    
    /// <summary>True si el defensor perdió todos sus ejércitos → conquista.</summary>
    public bool TerritoryConquered { get; set; }
}
