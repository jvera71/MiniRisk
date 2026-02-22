using MiniRisk.Models;
using MiniRisk.Models.Enums;

namespace MiniRisk.Services.Interfaces;

public interface IMapService
{
    /// <summary>
    /// Crea los 42 territorios con sus adyacencias.
    /// </summary>
    Dictionary<TerritoryName, Territory> CreateTerritories();

    /// <summary>
    /// Crea los 6 continentes con sus territorios y bonificaciones.
    /// </summary>
    Dictionary<ContinentName, Continent> CreateContinents();

    /// <summary>
    /// Crea el mazo completo de 44 cartas (sin barajar).
    /// </summary>
    List<Card> CreateCardDeck();

    /// <summary>
    /// Obtiene los territorios adyacentes a uno dado.
    /// </summary>
    List<TerritoryName> GetAdjacentTerritories(TerritoryName territory);

    /// <summary>
    /// Obtiene el continente al que pertenece un territorio.
    /// </summary>
    ContinentName GetContinent(TerritoryName territory);

    /// <summary>
    /// Obtiene la bonificación de un continente.
    /// </summary>
    int GetContinentBonus(ContinentName continent);

    /// <summary>
    /// Obtiene el nombre localizado (español) de un territorio.
    /// </summary>
    string GetTerritoryDisplayName(TerritoryName territory);

    /// <summary>
    /// Obtiene el nombre localizado (español) de un continente.
    /// </summary>
    string GetContinentDisplayName(ContinentName continent);
}
