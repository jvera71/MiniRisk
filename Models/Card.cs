using MiniRisk.Models.Enums;

namespace MiniRisk.Models;

public class Card
{
    /// <summary>
    /// Identificador único de la carta.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Tipo de la carta: Infantry, Cavalry, Artillery o Wildcard.
    /// </summary>
    public CardType Type { get; set; }
    
    /// <summary>
    /// Territorio asociado a la carta. Null para comodines.
    /// </summary>
    public TerritoryName? Territory { get; set; }
}
