using MiniRisk.Models.Enums;

namespace MiniRisk.Models;

public class GameSettings
{
    /// <summary>Número máximo de jugadores (2–6).</summary>
    public int MaxPlayers { get; set; } = 6;
    
    /// <summary>Modo de distribución de territorios.</summary>
    public TerritoryDistributionMode DistributionMode { get; set; } 
        = TerritoryDistributionMode.Random;
}
