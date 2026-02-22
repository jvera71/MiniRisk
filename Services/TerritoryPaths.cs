using MiniRisk.Models.Enums;

namespace MiniRisk.Services;

public static class TerritoryPaths
{
    private static readonly Dictionary<TerritoryName, TerritoryPathData> Paths = new()
    {
        [TerritoryName.Alaska] = new("M35,45 L95,45 L95,105 L35,105 Z", 65, 75),
        [TerritoryName.NorthwestTerritory] = new("M130,35 L190,35 L190,95 L130,95 Z", 160, 65),
        [TerritoryName.Greenland] = new("M310,15 L370,15 L370,75 L310,75 Z", 340, 45),
        [TerritoryName.Alberta] = new("M100,100 L160,100 L160,160 L100,160 Z", 130, 130),
        [TerritoryName.Ontario] = new("M180,100 L240,100 L240,160 L180,160 Z", 210, 130),
        [TerritoryName.Quebec] = new("M260,90 L320,90 L320,150 L260,150 Z", 290, 120),
        [TerritoryName.WesternUnitedStates] = new("M100,170 L160,170 L160,230 L100,230 Z", 130, 200),
        [TerritoryName.EasternUnitedStates] = new("M190,180 L250,180 L250,240 L190,240 Z", 220, 210),
        [TerritoryName.CentralAmerica] = new("M130,260 L190,260 L190,320 L130,320 Z", 160, 290),
        
        [TerritoryName.Venezuela] = new("M190,335 L250,335 L250,395 L190,395 Z", 220, 365),
        [TerritoryName.Peru] = new("M180,415 L240,415 L240,475 L180,475 Z", 210, 445),
        [TerritoryName.Brazil] = new("M260,400 L320,400 L320,460 L260,460 Z", 290, 430),
        [TerritoryName.Argentina] = new("M210,510 L270,510 L270,570 L210,570 Z", 240, 540),

        [TerritoryName.Iceland] = new("M435,25 L495,25 L495,85 L435,85 Z", 465, 55),
        [TerritoryName.GreatBritain] = new("M430,100 L490,100 L490,160 L430,160 Z", 460, 130),
        [TerritoryName.Scandinavia] = new("M515,35 L575,35 L575,95 L515,95 Z", 545, 65),
        [TerritoryName.WesternEurope] = new("M445,190 L505,190 L505,250 L445,250 Z", 475, 220),
        [TerritoryName.NorthernEurope] = new("M510,120 L570,120 L570,180 L510,180 Z", 540, 150),
        [TerritoryName.SouthernEurope] = new("M515,200 L575,200 L575,260 L515,260 Z", 545, 230),
        [TerritoryName.Ukraine] = new("M590,90 L650,90 L650,150 L590,150 Z", 620, 120),

        [TerritoryName.NorthAfrica] = new("M460,310 L520,310 L520,370 L460,370 Z", 490, 340),
        [TerritoryName.Egypt] = new("M540,280 L600,280 L600,340 L540,340 Z", 570, 310),
        [TerritoryName.EastAfrica] = new("M570,380 L630,380 L630,440 L570,440 Z", 600, 410),
        [TerritoryName.Congo] = new("M515,410 L575,410 L575,470 L515,470 Z", 545, 440),
        [TerritoryName.SouthAfrica] = new("M530,500 L590,500 L590,560 L530,560 Z", 560, 530),
        [TerritoryName.Madagascar] = new("M600,480 L660,480 L660,540 L600,540 Z", 630, 510),

        [TerritoryName.MiddleEast] = new("M620,240 L680,240 L680,300 L620,300 Z", 650, 270),
        [TerritoryName.Afghanistan] = new("M700,150 L760,150 L760,210 L700,210 Z", 730, 180),
        [TerritoryName.Ural] = new("M710,70 L770,70 L770,130 L710,130 Z", 740, 100),
        [TerritoryName.Siberia] = new("M790,35 L850,35 L850,95 L790,95 Z", 820, 65),
        [TerritoryName.Yakutsk] = new("M890,20 L950,20 L950,80 L890,80 Z", 920, 50),
        [TerritoryName.Irkutsk] = new("M860,85 L920,85 L920,145 L860,145 Z", 890, 115),
        [TerritoryName.Kamchatka] = new("M980,30 L1040,30 L1040,90 L980,90 Z", 1010, 60),
        [TerritoryName.Mongolia] = new("M860,150 L920,150 L920,210 L860,210 Z", 890, 180),
        [TerritoryName.Japan] = new("M970,145 L1030,145 L1030,205 L970,205 Z", 1000, 175),
        [TerritoryName.China] = new("M825,215 L885,215 L885,275 L825,275 Z", 855, 245),
        [TerritoryName.India] = new("M740,270 L800,270 L800,330 L740,330 Z", 770, 300),
        [TerritoryName.SoutheastAsia] = new("M850,300 L910,300 L910,360 L850,360 Z", 880, 330),

        [TerritoryName.Indonesia] = new("M880,400 L940,400 L940,460 L880,460 Z", 910, 430),
        [TerritoryName.NewGuinea] = new("M990,380 L1050,380 L1050,440 L990,440 Z", 1020, 410),
        [TerritoryName.WesternAustralia] = new("M930,510 L990,510 L990,570 L930,570 Z", 960, 540),
        [TerritoryName.EasternAustralia] = new("M1030,490 L1090,490 L1090,550 L1030,550 Z", 1060, 520),
    };

    public static string Get(TerritoryName name)
        => Paths.TryGetValue(name, out var data) ? data.Path : "";

    public static string Get(string nameStr)
    {
        if (Enum.TryParse<TerritoryName>(nameStr, out var name))
            return Get(name);
        return "";
    }

    public static float GetCenterX(TerritoryName name)
        => Paths.TryGetValue(name, out var data) ? data.CenterX : 0;

    public static float GetCenterX(string nameStr)
    {
        if (Enum.TryParse<TerritoryName>(nameStr, out var name))
            return GetCenterX(name);
        return 0;
    }

    public static float GetCenterY(TerritoryName name)
        => Paths.TryGetValue(name, out var data) ? data.CenterY : 0;

    public static float GetCenterY(string nameStr)
    {
        if (Enum.TryParse<TerritoryName>(nameStr, out var name))
            return GetCenterY(name);
        return 0;
    }

    public record TerritoryPathData(string Path, float CenterX, float CenterY);
}
