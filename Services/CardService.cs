using MiniRisk.Models;
using MiniRisk.Models.Enums;
using MiniRisk.Services.Interfaces;

namespace MiniRisk.Services;

public class CardService : ICardService
{
    public bool IsValidTrade(List<Card> cards)
    {
        if (cards.Count != 3) return false;

        var types = cards.Select(c => c.Type).ToList();
        int wildcards = types.Count(t => t == CardType.Wildcard);

        // Comodín + 2 cualesquiera
        if (wildcards >= 1) return true;

        // 3 del mismo tipo
        if (types.All(t => t == types[0])) return true;

        // 1 de cada tipo (Infantry + Cavalry + Artillery)
        if (types.Distinct().Count() == 3) return true;

        return false;
    }

    public int GetArmiesForTrade(int tradeNumber)
    {
        return tradeNumber switch
        {
            1 => 4,
            2 => 6,
            3 => 8,
            4 => 10,
            5 => 12,
            6 => 15,
            _ => 15 + (tradeNumber - 6) * 5
        };
    }
}
