using MiniRisk.Services.Interfaces;

namespace MiniRisk.Services;

public class DiceService : IDiceService
{
    public int[] Roll(int numberOfDice)
    {
        if (numberOfDice < 1 || numberOfDice > 3)
            throw new ArgumentOutOfRangeException(nameof(numberOfDice),
                "Number of dice must be between 1 and 3.");

        return Enumerable.Range(0, numberOfDice)
            .Select(_ => Random.Shared.Next(1, 7))
            .OrderByDescending(d => d)
            .ToArray();
    }
}
