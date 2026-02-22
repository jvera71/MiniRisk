using MiniRisk.Models;
using MiniRisk.Models.Enums;
using MiniRisk.Services.Interfaces;

namespace MiniRisk.Services;

public class MapService : IMapService
{
    private static readonly Dictionary<TerritoryName, List<TerritoryName>> Adjacencies = new()
    {
        // North America (9)
        [TerritoryName.Alaska] = [TerritoryName.NorthwestTerritory, TerritoryName.Alberta, TerritoryName.Kamchatka],
        [TerritoryName.NorthwestTerritory] = [TerritoryName.Alaska, TerritoryName.Alberta, TerritoryName.Ontario, TerritoryName.Greenland],
        [TerritoryName.Greenland] = [TerritoryName.NorthwestTerritory, TerritoryName.Ontario, TerritoryName.Quebec, TerritoryName.Iceland],
        [TerritoryName.Alberta] = [TerritoryName.Alaska, TerritoryName.NorthwestTerritory, TerritoryName.Ontario, TerritoryName.WesternUnitedStates],
        [TerritoryName.Ontario] = [TerritoryName.NorthwestTerritory, TerritoryName.Alberta, TerritoryName.Quebec, TerritoryName.Greenland, TerritoryName.WesternUnitedStates, TerritoryName.EasternUnitedStates],
        [TerritoryName.Quebec] = [TerritoryName.Ontario, TerritoryName.Greenland, TerritoryName.EasternUnitedStates],
        [TerritoryName.WesternUnitedStates] = [TerritoryName.Alberta, TerritoryName.Ontario, TerritoryName.EasternUnitedStates, TerritoryName.CentralAmerica],
        [TerritoryName.EasternUnitedStates] = [TerritoryName.Ontario, TerritoryName.Quebec, TerritoryName.WesternUnitedStates, TerritoryName.CentralAmerica],
        [TerritoryName.CentralAmerica] = [TerritoryName.WesternUnitedStates, TerritoryName.EasternUnitedStates, TerritoryName.Venezuela],

        // South America (4)
        [TerritoryName.Venezuela] = [TerritoryName.CentralAmerica, TerritoryName.Peru, TerritoryName.Brazil],
        [TerritoryName.Peru] = [TerritoryName.Venezuela, TerritoryName.Brazil, TerritoryName.Argentina],
        [TerritoryName.Brazil] = [TerritoryName.Venezuela, TerritoryName.Peru, TerritoryName.Argentina, TerritoryName.NorthAfrica],
        [TerritoryName.Argentina] = [TerritoryName.Peru, TerritoryName.Brazil],

        // Europe (7)
        [TerritoryName.Iceland] = [TerritoryName.Greenland, TerritoryName.GreatBritain, TerritoryName.Scandinavia],
        [TerritoryName.GreatBritain] = [TerritoryName.Iceland, TerritoryName.Scandinavia, TerritoryName.WesternEurope, TerritoryName.NorthernEurope],
        [TerritoryName.Scandinavia] = [TerritoryName.Iceland, TerritoryName.GreatBritain, TerritoryName.NorthernEurope, TerritoryName.Ukraine],
        [TerritoryName.WesternEurope] = [TerritoryName.GreatBritain, TerritoryName.NorthernEurope, TerritoryName.SouthernEurope, TerritoryName.NorthAfrica],
        [TerritoryName.NorthernEurope] = [TerritoryName.GreatBritain, TerritoryName.Scandinavia, TerritoryName.WesternEurope, TerritoryName.SouthernEurope, TerritoryName.Ukraine],
        [TerritoryName.SouthernEurope] = [TerritoryName.WesternEurope, TerritoryName.NorthernEurope, TerritoryName.Ukraine, TerritoryName.Egypt, TerritoryName.NorthAfrica, TerritoryName.MiddleEast],
        [TerritoryName.Ukraine] = [TerritoryName.Scandinavia, TerritoryName.NorthernEurope, TerritoryName.SouthernEurope, TerritoryName.Ural, TerritoryName.Afghanistan, TerritoryName.MiddleEast],

        // Africa (6)
        [TerritoryName.NorthAfrica] = [TerritoryName.Brazil, TerritoryName.WesternEurope, TerritoryName.SouthernEurope, TerritoryName.Egypt, TerritoryName.EastAfrica, TerritoryName.Congo],
        [TerritoryName.Egypt] = [TerritoryName.NorthAfrica, TerritoryName.SouthernEurope, TerritoryName.MiddleEast, TerritoryName.EastAfrica],
        [TerritoryName.EastAfrica] = [TerritoryName.NorthAfrica, TerritoryName.Egypt, TerritoryName.Congo, TerritoryName.SouthAfrica, TerritoryName.Madagascar],
        [TerritoryName.Congo] = [TerritoryName.NorthAfrica, TerritoryName.EastAfrica, TerritoryName.SouthAfrica],
        [TerritoryName.SouthAfrica] = [TerritoryName.Congo, TerritoryName.EastAfrica, TerritoryName.Madagascar],
        [TerritoryName.Madagascar] = [TerritoryName.EastAfrica, TerritoryName.SouthAfrica],

        // Asia (12)
        [TerritoryName.MiddleEast] = [TerritoryName.SouthernEurope, TerritoryName.Ukraine, TerritoryName.Egypt, TerritoryName.Afghanistan, TerritoryName.India],
        [TerritoryName.Afghanistan] = [TerritoryName.Ukraine, TerritoryName.MiddleEast, TerritoryName.Ural, TerritoryName.China, TerritoryName.India],
        [TerritoryName.Ural] = [TerritoryName.Ukraine, TerritoryName.Afghanistan, TerritoryName.Siberia, TerritoryName.China],
        [TerritoryName.Siberia] = [TerritoryName.Ural, TerritoryName.Yakutsk, TerritoryName.Irkutsk, TerritoryName.Mongolia, TerritoryName.China],
        [TerritoryName.Yakutsk] = [TerritoryName.Siberia, TerritoryName.Irkutsk, TerritoryName.Kamchatka],
        [TerritoryName.Irkutsk] = [TerritoryName.Siberia, TerritoryName.Yakutsk, TerritoryName.Kamchatka, TerritoryName.Mongolia],
        [TerritoryName.Kamchatka] = [TerritoryName.Yakutsk, TerritoryName.Irkutsk, TerritoryName.Mongolia, TerritoryName.Japan, TerritoryName.Alaska],
        [TerritoryName.Mongolia] = [TerritoryName.Siberia, TerritoryName.Irkutsk, TerritoryName.Kamchatka, TerritoryName.Japan, TerritoryName.China],
        [TerritoryName.Japan] = [TerritoryName.Kamchatka, TerritoryName.Mongolia],
        [TerritoryName.China] = [TerritoryName.Afghanistan, TerritoryName.Ural, TerritoryName.Siberia, TerritoryName.Mongolia, TerritoryName.India, TerritoryName.SoutheastAsia],
        [TerritoryName.India] = [TerritoryName.MiddleEast, TerritoryName.Afghanistan, TerritoryName.China, TerritoryName.SoutheastAsia],
        [TerritoryName.SoutheastAsia] = [TerritoryName.India, TerritoryName.China, TerritoryName.Indonesia],

        // Oceania (4)
        [TerritoryName.Indonesia] = [TerritoryName.SoutheastAsia, TerritoryName.NewGuinea, TerritoryName.WesternAustralia],
        [TerritoryName.NewGuinea] = [TerritoryName.Indonesia, TerritoryName.WesternAustralia, TerritoryName.EasternAustralia],
        [TerritoryName.WesternAustralia] = [TerritoryName.Indonesia, TerritoryName.NewGuinea, TerritoryName.EasternAustralia],
        [TerritoryName.EasternAustralia] = [TerritoryName.NewGuinea, TerritoryName.WesternAustralia]
    };

    private static readonly Dictionary<ContinentName, (int Bonus, List<TerritoryName> Territories)> ContinentData = new()
    {
        [ContinentName.NorthAmerica] = (5, [
            TerritoryName.Alaska, TerritoryName.NorthwestTerritory, TerritoryName.Greenland, TerritoryName.Alberta,
            TerritoryName.Ontario, TerritoryName.Quebec, TerritoryName.WesternUnitedStates, TerritoryName.EasternUnitedStates,
            TerritoryName.CentralAmerica
        ]),
        [ContinentName.SouthAmerica] = (2, [
            TerritoryName.Venezuela, TerritoryName.Peru, TerritoryName.Brazil, TerritoryName.Argentina
        ]),
        [ContinentName.Europe] = (5, [
            TerritoryName.Iceland, TerritoryName.GreatBritain, TerritoryName.Scandinavia, TerritoryName.WesternEurope,
            TerritoryName.NorthernEurope, TerritoryName.SouthernEurope, TerritoryName.Ukraine
        ]),
        [ContinentName.Africa] = (3, [
            TerritoryName.NorthAfrica, TerritoryName.Egypt, TerritoryName.EastAfrica, TerritoryName.Congo,
            TerritoryName.SouthAfrica, TerritoryName.Madagascar
        ]),
        [ContinentName.Asia] = (7, [
            TerritoryName.MiddleEast, TerritoryName.Afghanistan, TerritoryName.Ural, TerritoryName.Siberia,
            TerritoryName.Yakutsk, TerritoryName.Irkutsk, TerritoryName.Kamchatka, TerritoryName.Mongolia,
            TerritoryName.Japan, TerritoryName.China, TerritoryName.India, TerritoryName.SoutheastAsia
        ]),
        [ContinentName.Oceania] = (2, [
            TerritoryName.Indonesia, TerritoryName.NewGuinea, TerritoryName.WesternAustralia, TerritoryName.EasternAustralia
        ]),
    };

    private static readonly Dictionary<TerritoryName, string> DisplayNames = new()
    {
        [TerritoryName.Alaska] = "Alaska",
        [TerritoryName.NorthwestTerritory] = "Territorio del Noroeste",
        [TerritoryName.Greenland] = "Groenlandia",
        [TerritoryName.Alberta] = "Alberta",
        [TerritoryName.Ontario] = "Ontario",
        [TerritoryName.Quebec] = "Quebec",
        [TerritoryName.WesternUnitedStates] = "EE.UU. Occidental",
        [TerritoryName.EasternUnitedStates] = "EE.UU. Oriental",
        [TerritoryName.CentralAmerica] = "América Central",
        [TerritoryName.Venezuela] = "Venezuela",
        [TerritoryName.Peru] = "Perú",
        [TerritoryName.Brazil] = "Brasil",
        [TerritoryName.Argentina] = "Argentina",
        [TerritoryName.Iceland] = "Islandia",
        [TerritoryName.GreatBritain] = "Gran Bretaña",
        [TerritoryName.Scandinavia] = "Escandinavia",
        [TerritoryName.WesternEurope] = "Europa Occidental",
        [TerritoryName.NorthernEurope] = "Europa del Norte",
        [TerritoryName.SouthernEurope] = "Europa del Sur",
        [TerritoryName.Ukraine] = "Ucrania",
        [TerritoryName.NorthAfrica] = "Norte de África",
        [TerritoryName.Egypt] = "Egipto",
        [TerritoryName.EastAfrica] = "África Oriental",
        [TerritoryName.Congo] = "Congo",
        [TerritoryName.SouthAfrica] = "Sudáfrica",
        [TerritoryName.Madagascar] = "Madagascar",
        [TerritoryName.MiddleEast] = "Oriente Medio",
        [TerritoryName.Afghanistan] = "Afganistán",
        [TerritoryName.Ural] = "Ural",
        [TerritoryName.Siberia] = "Siberia",
        [TerritoryName.Yakutsk] = "Yakutsk",
        [TerritoryName.Irkutsk] = "Irkutsk",
        [TerritoryName.Kamchatka] = "Kamchatka",
        [TerritoryName.Mongolia] = "Mongolia",
        [TerritoryName.Japan] = "Japón",
        [TerritoryName.China] = "China",
        [TerritoryName.India] = "India",
        [TerritoryName.SoutheastAsia] = "Sureste Asiático",
        [TerritoryName.Indonesia] = "Indonesia",
        [TerritoryName.NewGuinea] = "Nueva Guinea",
        [TerritoryName.WesternAustralia] = "Australia Occidental",
        [TerritoryName.EasternAustralia] = "Australia Oriental"
    };

    public Dictionary<TerritoryName, Territory> CreateTerritories()
    {
        var territories = new Dictionary<TerritoryName, Territory>();

        foreach (var (name, adjacencies) in Adjacencies)
        {
            var continent = ContinentData
                .First(c => c.Value.Territories.Contains(name)).Key;

            territories[name] = new Territory
            {
                Name = name,
                Continent = continent,
                AdjacentTerritories = new List<TerritoryName>(adjacencies),
                Armies = 0,
                OwnerId = string.Empty
            };
        }

        return territories;
    }

    public Dictionary<ContinentName, Continent> CreateContinents()
    {
        return ContinentData.ToDictionary(
            kvp => kvp.Key,
            kvp => new Continent
            {
                Name = kvp.Key,
                BonusArmies = kvp.Value.Bonus,
                Territories = new List<TerritoryName>(kvp.Value.Territories)
            }
        );
    }

    public List<Card> CreateCardDeck()
    {
        var cards = new List<Card>();
        var allTerritories = Enum.GetValues<TerritoryName>().ToList();
        var types = new[] { CardType.Infantry, CardType.Cavalry, CardType.Artillery };

        for (int i = 0; i < allTerritories.Count; i++)
        {
            cards.Add(new Card
            {
                Type = types[i % 3],
                Territory = allTerritories[i],
                Id = Guid.NewGuid().ToString() // IDs unique per card
            });
        }

        cards.Add(new Card { Type = CardType.Wildcard, Territory = null, Id = Guid.NewGuid().ToString() });
        cards.Add(new Card { Type = CardType.Wildcard, Territory = null, Id = Guid.NewGuid().ToString() });

        return cards;
    }

    public List<TerritoryName> GetAdjacentTerritories(TerritoryName territory)
        => new(Adjacencies[territory]);

    public ContinentName GetContinent(TerritoryName territory)
        => ContinentData.First(c => c.Value.Territories.Contains(territory)).Key;

    public int GetContinentBonus(ContinentName continent)
        => ContinentData[continent].Bonus;

    public string GetTerritoryDisplayName(TerritoryName territory)
        => DisplayNames.GetValueOrDefault(territory, territory.ToString());

    public string GetContinentDisplayName(ContinentName continent)
        => continent switch
        {
            ContinentName.NorthAmerica => "América del Norte",
            ContinentName.SouthAmerica => "América del Sur",
            ContinentName.Europe => "Europa",
            ContinentName.Africa => "África",
            ContinentName.Asia => "Asia",
            ContinentName.Oceania => "Oceanía",
            _ => continent.ToString()
        };
}
