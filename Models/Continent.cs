using MiniRisk.Models.Enums;

namespace MiniRisk.Models;

public class Continent
{
    /// <summary>
    /// Nombre del continente (enum ContinentName).
    /// </summary>
    public ContinentName Name { get; set; }
    
    /// <summary>
    /// Ejércitos de bonificación por controlar todos los territorios del continente.
    /// </summary>
    public int BonusArmies { get; set; }
    
    /// <summary>
    /// Lista de territorios que componen este continente.
    /// Inmutable tras la inicialización.
    /// </summary>
    public List<TerritoryName> Territories { get; set; } = [];
    
    /// <summary>
    /// Verifica si un jugador controla todos los territorios del continente.
    /// </summary>
    public bool IsControlledBy(string playerId, Dictionary<TerritoryName, Territory> allTerritories)
        => Territories.All(t => allTerritories[t].OwnerId == playerId);
}
