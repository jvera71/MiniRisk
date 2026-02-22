using MiniRisk.Models;

namespace MiniRisk.Services.Interfaces;

public interface ICardService
{
    /// <summary>
    /// Verifica si una combinación de 3 cartas es un canje válido.
    /// </summary>
    bool IsValidTrade(List<Card> cards);

    /// <summary>
    /// Calcula los ejércitos otorgados por el N-ésimo canje global.
    /// </summary>
    int GetArmiesForTrade(int tradeNumber);
}
