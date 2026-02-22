namespace MiniRisk.Services.Interfaces;

public interface IDiceService
{
    /// <summary>
    /// Tira N dados de 6 caras (1-6).
    /// Retorna los resultados ordenados de mayor a menor.
    /// </summary>
    int[] Roll(int numberOfDice);
}
