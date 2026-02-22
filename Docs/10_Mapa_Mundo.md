# 10 — Mapa del Mundo

> **Documento:** 10 de 14  
> **Versión:** 1.0  
> **Última actualización:** 22 de febrero de 2026  
> **Volver al índice:** [00_Indice.md](./00_Indice.md)  
> **Documento anterior:** [09 — Componentes Blazor](./09_Componentes_Blazor.md)

---

## 1. Visión General

El mapa del mundo es el componente visual central de MiniRisk. Se implementa como un **SVG interactivo** integrado en el componente `WorldMap.razor`. Representa los 42 territorios del Risk clásico organizados en 6 continentes, con conexiones (adyacencias) entre ellos.

### 1.1 Decisiones de Diseño

| Decisión | Elección | Justificación |
|----------|---------|---------------|
| **Formato** | SVG inline | Permite interactividad (click, hover), estilos CSS dinámicos y manipulación DOM vía Blazor |
| **Viewbox** | `0 0 1200 700` | Ratio ~16:9, se adapta al contenedor manteniendo proporciones |
| **Estilo** | Esquemático / simplificado | Más legible que un mapa realista; áreas de click claramente definidas |
| **Colores** | Color del jugador dueño | El territorio se tiñe del color del propietario; el continente tiene un tono de fondo sutil |
| **Ejércitos** | Badge circular sobre cada territorio | Fondo del color del dueño, número en blanco, fuente monoespaciada |

### 1.2 Estructura del SVG

```xml
<svg viewBox="0 0 1200 700" xmlns="http://www.w3.org/2000/svg">

    <!-- Capa 0: Fondo oceánico -->
    <rect width="1200" height="700" fill="#0a0e1a" />

    <!-- Capa 1: Fondos de continentes (formas agrupadas) -->
    <g id="continent-backgrounds" opacity="0.15"> ... </g>

    <!-- Capa 2: Líneas de adyacencia intercontinental -->
    <g id="connections" stroke="rgba(255,255,255,0.15)" stroke-dasharray="4,4"> ... </g>

    <!-- Capa 3: Territorios (paths interactivos) -->
    <g id="territories"> ... </g>

    <!-- Capa 4: Badges de ejércitos (círculos + texto) -->
    <g id="army-badges"> ... </g>

    <!-- Capa 5: Nombres de continentes (texto decorativo) -->
    <g id="continent-labels" opacity="0.4"> ... </g>

</svg>
```

---

## 2. Los 6 Continentes

### 2.1 Tabla de Continentes

| # | Continente | `ContinentName` | Territorios | Bonus | Color de fondo | Zona SVG (aprox.) |
|:-:|:----------:|:---------------:|:-----------:|:-----:|:--------------:|:-----------------:|
| 1 | América del Norte | `NorthAmerica` | 9 | +5 | `#1b3a4b` | x: 30–370, y: 30–320 |
| 2 | América del Sur | `SouthAmerica` | 4 | +2 | `#1b4332` | x: 170–350, y: 330–620 |
| 3 | Europa | `Europe` | 7 | +5 | `#3b1f2b` | x: 450–680, y: 30–280 |
| 4 | África | `Africa` | 6 | +3 | `#3d2c1e` | x: 450–680, y: 290–620 |
| 5 | Asia | `Asia` | 12 | +7 | `#2b2440` | x: 690–1100, y: 30–400 |
| 6 | Oceanía | `Oceania` | 4 | +2 | `#1a3342` | x: 900–1170, y: 410–650 |

### 2.2 Distribución Visual

```
    ┌──────────────────────────────────────────────────────────────────────┐
    │                          MAPA DEL MUNDO                              │
    │                                                                      │
    │   ┌───────────────┐   ┌─────────────┐   ┌──────────────────────┐    │
    │   │               │   │             │   │                      │    │
    │   │  AMÉRICA      │   │   EUROPA    │   │        ASIA          │    │
    │   │  DEL NORTE    │   │   (7)       │   │        (12)          │    │
    │   │  (9)          │   │   +5        │   │        +7            │    │
    │   │  +5           │   │             │   │                      │    │
    │   │               │   └──────┬──────┘   │                      │    │
    │   └───────┬───────┘          │          └──────────┬───────────┘    │
    │           │           ┌──────┴──────┐              │                │
    │   ┌───────┴───────┐   │             │      ┌───────┴───────┐       │
    │   │               │   │   ÁFRICA    │      │               │       │
    │   │  AMÉRICA      │   │   (6)       │      │   OCEANÍA     │       │
    │   │  DEL SUR      │   │   +3        │      │   (4)         │       │
    │   │  (4)          │   │             │      │   +2           │       │
    │   │  +2           │   │             │      │               │       │
    │   └───────────────┘   └─────────────┘      └───────────────┘       │
    │                                                                      │
    └──────────────────────────────────────────────────────────────────────┘
```

---

## 3. Los 42 Territorios

### 3.1 América del Norte (9 territorios)

| # | Territorio | `TerritoryName` | Nombre ES | Centro SVG (cx, cy) |
|:-:|-----------|:---------------:|-----------|:-------------------:|
| 1 | Alaska | `Alaska` | Alaska | (65, 75) |
| 2 | Northwest Territory | `NorthwestTerritory` | Terr. del Noroeste | (160, 65) |
| 3 | Greenland | `Greenland` | Groenlandia | (340, 45) |
| 4 | Alberta | `Alberta` | Alberta | (130, 130) |
| 5 | Ontario | `Ontario` | Ontario | (210, 130) |
| 6 | Quebec | `Quebec` | Quebec | (290, 120) |
| 7 | Western US | `WesternUnitedStates` | EE.UU. Occidental | (130, 200) |
| 8 | Eastern US | `EasternUnitedStates` | EE.UU. Oriental | (220, 210) |
| 9 | Central America | `CentralAmerica` | América Central | (160, 290) |

### 3.2 América del Sur (4 territorios)

| # | Territorio | `TerritoryName` | Nombre ES | Centro SVG |
|:-:|-----------|:---------------:|-----------|:----------:|
| 10 | Venezuela | `Venezuela` | Venezuela | (220, 365) |
| 11 | Peru | `Peru` | Perú | (210, 445) |
| 12 | Brazil | `Brazil` | Brasil | (290, 430) |
| 13 | Argentina | `Argentina` | Argentina | (240, 540) |

### 3.3 Europa (7 territorios)

| # | Territorio | `TerritoryName` | Nombre ES | Centro SVG |
|:-:|-----------|:---------------:|-----------|:----------:|
| 14 | Iceland | `Iceland` | Islandia | (465, 55) |
| 15 | Great Britain | `GreatBritain` | Gran Bretaña | (460, 130) |
| 16 | Scandinavia | `Scandinavia` | Escandinavia | (545, 65) |
| 17 | Western Europe | `WesternEurope` | Europa Occidental | (475, 220) |
| 18 | Northern Europe | `NorthernEurope` | Europa del Norte | (540, 150) |
| 19 | Southern Europe | `SouthernEurope` | Europa del Sur | (545, 230) |
| 20 | Ukraine | `Ukraine` | Ucrania | (620, 120) |

### 3.4 África (6 territorios)

| # | Territorio | `TerritoryName` | Nombre ES | Centro SVG |
|:-:|-----------|:---------------:|-----------|:----------:|
| 21 | North Africa | `NorthAfrica` | Norte de África | (490, 340) |
| 22 | Egypt | `Egypt` | Egipto | (570, 310) |
| 23 | East Africa | `EastAfrica` | África Oriental | (600, 410) |
| 24 | Congo | `Congo` | Congo | (545, 440) |
| 25 | South Africa | `SouthAfrica` | Sudáfrica | (560, 530) |
| 26 | Madagascar | `Madagascar` | Madagascar | (630, 510) |

### 3.5 Asia (12 territorios)

| # | Territorio | `TerritoryName` | Nombre ES | Centro SVG |
|:-:|-----------|:---------------:|-----------|:----------:|
| 27 | Middle East | `MiddleEast` | Oriente Medio | (650, 270) |
| 28 | Afghanistan | `Afghanistan` | Afganistán | (730, 180) |
| 29 | Ural | `Ural` | Urales | (740, 100) |
| 30 | Siberia | `Siberia` | Siberia | (820, 65) |
| 31 | Yakutsk | `Yakutsk` | Yakutsk | (920, 50) |
| 32 | Irkutsk | `Irkutsk` | Irkutsk | (890, 115) |
| 33 | Kamchatka | `Kamchatka` | Kamchatka | (1010, 60) |
| 34 | Mongolia | `Mongolia` | Mongolia | (890, 180) |
| 35 | Japan | `Japan` | Japón | (1000, 175) |
| 36 | China | `China` | China | (855, 245) |
| 37 | India | `India` | India | (770, 300) |
| 38 | Southeast Asia | `SoutheastAsia` | Sudeste Asiático | (880, 330) |

### 3.6 Oceanía (4 territorios)

| # | Territorio | `TerritoryName` | Nombre ES | Centro SVG |
|:-:|-----------|:---------------:|-----------|:----------:|
| 39 | Indonesia | `Indonesia` | Indonesia | (910, 430) |
| 40 | New Guinea | `NewGuinea` | Nueva Guinea | (1020, 410) |
| 41 | Western Australia | `WesternAustralia` | Australia Occ. | (960, 540) |
| 42 | Eastern Australia | `EasternAustralia` | Australia Or. | (1060, 520) |

---

## 4. Adyacencias Completas

### 4.1 Tabla de Adyacencias

Cada fila muestra un territorio y sus vecinos directos. Las conexiones son **bidireccionales**: si A es adyacente a B, entonces B es adyacente a A.

#### América del Norte

| Territorio | Adyacente a |
|-----------|:------------|
| **Alaska** | Northwest Territory, Alberta, **Kamchatka** 🌊 |
| **Northwest Territory** | Alaska, Alberta, Ontario, Greenland |
| **Greenland** | Northwest Territory, Ontario, Quebec, **Iceland** 🌊 |
| **Alberta** | Alaska, Northwest Territory, Ontario, Western US |
| **Ontario** | Northwest Territory, Alberta, Quebec, Greenland, Western US, Eastern US |
| **Quebec** | Ontario, Greenland, Eastern US |
| **Western US** | Alberta, Ontario, Eastern US, Central America |
| **Eastern US** | Ontario, Quebec, Western US, Central America |
| **Central America** | Western US, Eastern US, **Venezuela** 🌊 |

#### América del Sur

| Territorio | Adyacente a |
|-----------|:------------|
| **Venezuela** | **Central America** 🌊, Peru, Brazil |
| **Peru** | Venezuela, Brazil, Argentina |
| **Brazil** | Venezuela, Peru, Argentina, **North Africa** 🌊 |
| **Argentina** | Peru, Brazil |

#### Europa

| Territorio | Adyacente a |
|-----------|:------------|
| **Iceland** | **Greenland** 🌊, Great Britain, Scandinavia |
| **Great Britain** | Iceland, Scandinavia, Western Europe, Northern Europe |
| **Scandinavia** | Iceland, Great Britain, Northern Europe, Ukraine |
| **Western Europe** | Great Britain, Northern Europe, Southern Europe, **North Africa** 🌊 |
| **Northern Europe** | Great Britain, Scandinavia, Western Europe, Southern Europe, Ukraine |
| **Southern Europe** | Western Europe, Northern Europe, Ukraine, **North Africa** 🌊, **Egypt** 🌊, **Middle East** 🌊 |
| **Ukraine** | Scandinavia, Northern Europe, Southern Europe, **Ural**, **Afghanistan**, **Middle East** |

#### África

| Territorio | Adyacente a |
|-----------|:------------|
| **North Africa** | **Brazil** 🌊, **Western Europe** 🌊, **Southern Europe** 🌊, Egypt, East Africa, Congo |
| **Egypt** | **Southern Europe** 🌊, North Africa, East Africa, **Middle East** |
| **East Africa** | North Africa, Egypt, Congo, South Africa, Madagascar, **Middle East** |
| **Congo** | North Africa, East Africa, South Africa |
| **South Africa** | Congo, East Africa, Madagascar |
| **Madagascar** | East Africa, South Africa |

#### Asia

| Territorio | Adyacente a |
|-----------|:------------|
| **Middle East** | **Southern Europe** 🌊, **Egypt**, **East Africa**, Afghanistan, India |
| **Afghanistan** | **Ukraine**, Middle East, Ural, China, India |
| **Ural** | **Ukraine**, Afghanistan, Siberia, China |
| **Siberia** | Ural, Yakutsk, Irkutsk, Mongolia, China |
| **Yakutsk** | Siberia, Irkutsk, Kamchatka |
| **Irkutsk** | Siberia, Yakutsk, Kamchatka, Mongolia |
| **Kamchatka** | Yakutsk, Irkutsk, Mongolia, Japan, **Alaska** 🌊 |
| **Mongolia** | Siberia, Irkutsk, Kamchatka, Japan, China |
| **Japan** | Kamchatka, Mongolia |
| **China** | Afghanistan, Ural, Siberia, Mongolia, India, Southeast Asia |
| **India** | Middle East, Afghanistan, China, Southeast Asia |
| **Southeast Asia** | China, India, **Indonesia** 🌊 |

#### Oceanía

| Territorio | Adyacente a |
|-----------|:------------|
| **Indonesia** | **Southeast Asia** 🌊, New Guinea, Western Australia |
| **New Guinea** | Indonesia, Eastern Australia, Western Australia |
| **Western Australia** | Indonesia, New Guinea, Eastern Australia |
| **Eastern Australia** | New Guinea, Western Australia |

### 4.2 Conexiones Intercontinentales

Las conexiones 🌊 cruzan agua (mares/océanos). Visualmente se representan con líneas discontinuas en el SVG:

| Conexión | Continentes | Representa |
|----------|:-----------:|-----------|
| Alaska ↔ Kamchatka | N.América ↔ Asia | Estrecho de Bering |
| Greenland ↔ Iceland | N.América ↔ Europa | Atlántico Norte |
| Central America ↔ Venezuela | N.América ↔ S.América | Mar Caribe |
| Brazil ↔ North Africa | S.América ↔ África | Atlántico Sur |
| Western Europe ↔ North Africa | Europa ↔ África | Estrecho de Gibraltar |
| Southern Europe ↔ North Africa | Europa ↔ África | Mar Mediterráneo |
| Southern Europe ↔ Egypt | Europa ↔ África | Mar Mediterráneo |
| Southern Europe ↔ Middle East | Europa ↔ Asia | Mar Mediterráneo / Turquía |
| Ukraine ↔ Ural | Europa ↔ Asia | Montes Urales |
| Ukraine ↔ Afghanistan | Europa ↔ Asia | Asia Central |
| Ukraine ↔ Middle East | Europa ↔ Asia | Mar Negro / Cáucaso |
| Egypt ↔ Middle East | África ↔ Asia | Canal de Suez |
| East Africa ↔ Middle East | África ↔ Asia | Mar Rojo |
| Southeast Asia ↔ Indonesia | Asia ↔ Oceanía | Estrecho de Malaca |

### 4.3 Verificación de Adyacencias

**Total de conexiones únicas:** Las 42 regiones comparten un total de **83 conexiones** bidireccionales. Cada territorio tiene entre 2 y 6 vecinos.

| Territorio | Nº vecinos |
|-----------|:----------:|
| Mín: Argentina, Japan | 2 |
| Máx: China, Northern Europe, Ontario | 6 |
| Media | ~3.95 |

---

## 5. Implementación del MapService (Datos Estáticos)

### 5.1 Coordenadas y Paths

Los datos del mapa se almacenan en una clase estática `TerritoryPaths` que sirve como fuente de datos para el SVG:

```csharp
/// <summary>
/// Datos estáticos de los paths SVG de cada territorio.
/// Incluye el path de la forma, las coordenadas del centro
/// (para el badge de ejércitos) y el nombre localizado.
/// </summary>
public static class TerritoryPaths
{
    private static readonly Dictionary<string, TerritoryPathData> Paths = new()
    {
        ["Alaska"] = new(
            Path: "M30,55 L75,40 L105,55 L100,90 L85,105 L50,100 L30,80 Z",
            CenterX: 65,
            CenterY: 75
        ),
        ["NorthwestTerritory"] = new(
            Path: "M105,35 L175,25 L225,40 L210,80 L160,90 L105,55 Z",
            CenterX: 160,
            CenterY: 65
        ),
        ["Greenland"] = new(
            Path: "M280,15 L350,10 L395,25 L390,65 L340,80 L290,60 Z",
            CenterX: 340,
            CenterY: 45
        ),
        // ... (39 territorios más)
    };

    /// <summary>Obtiene el path SVG de un territorio.</summary>
    public static string Get(string territoryId)
        => Paths.TryGetValue(territoryId, out var data) ? data.Path : "";

    /// <summary>Obtiene la coordenada X del centro para el badge.</summary>
    public static float GetCenterX(string territoryId)
        => Paths.TryGetValue(territoryId, out var data) ? data.CenterX : 0;

    /// <summary>Obtiene la coordenada Y del centro para el badge.</summary>
    public static float GetCenterY(string territoryId)
        => Paths.TryGetValue(territoryId, out var data) ? data.CenterY : 0;

    public record TerritoryPathData(string Path, float CenterX, float CenterY);
}
```

### 5.2 Adyacencias en MapService (Extracto)

```csharp
// En MapService.cs
private static readonly Dictionary<TerritoryName, List<TerritoryName>> Adjacencies = new()
{
    // ═══ AMÉRICA DEL NORTE ═══
    [TerritoryName.Alaska] = [
        TerritoryName.NorthwestTerritory,
        TerritoryName.Alberta,
        TerritoryName.Kamchatka  // 🌊 Intercontinental
    ],
    [TerritoryName.NorthwestTerritory] = [
        TerritoryName.Alaska,
        TerritoryName.Alberta,
        TerritoryName.Ontario,
        TerritoryName.Greenland
    ],
    [TerritoryName.Greenland] = [
        TerritoryName.NorthwestTerritory,
        TerritoryName.Ontario,
        TerritoryName.Quebec,
        TerritoryName.Iceland  // 🌊 Intercontinental
    ],
    [TerritoryName.Alberta] = [
        TerritoryName.Alaska,
        TerritoryName.NorthwestTerritory,
        TerritoryName.Ontario,
        TerritoryName.WesternUnitedStates
    ],
    [TerritoryName.Ontario] = [
        TerritoryName.NorthwestTerritory,
        TerritoryName.Alberta,
        TerritoryName.Quebec,
        TerritoryName.Greenland,
        TerritoryName.WesternUnitedStates,
        TerritoryName.EasternUnitedStates
    ],
    [TerritoryName.Quebec] = [
        TerritoryName.Ontario,
        TerritoryName.Greenland,
        TerritoryName.EasternUnitedStates
    ],
    [TerritoryName.WesternUnitedStates] = [
        TerritoryName.Alberta,
        TerritoryName.Ontario,
        TerritoryName.EasternUnitedStates,
        TerritoryName.CentralAmerica
    ],
    [TerritoryName.EasternUnitedStates] = [
        TerritoryName.Ontario,
        TerritoryName.Quebec,
        TerritoryName.WesternUnitedStates,
        TerritoryName.CentralAmerica
    ],
    [TerritoryName.CentralAmerica] = [
        TerritoryName.WesternUnitedStates,
        TerritoryName.EasternUnitedStates,
        TerritoryName.Venezuela  // 🌊 Intercontinental
    ],

    // ═══ AMÉRICA DEL SUR ═══
    [TerritoryName.Venezuela] = [
        TerritoryName.CentralAmerica,  // 🌊
        TerritoryName.Peru,
        TerritoryName.Brazil
    ],
    [TerritoryName.Peru] = [
        TerritoryName.Venezuela,
        TerritoryName.Brazil,
        TerritoryName.Argentina
    ],
    [TerritoryName.Brazil] = [
        TerritoryName.Venezuela,
        TerritoryName.Peru,
        TerritoryName.Argentina,
        TerritoryName.NorthAfrica  // 🌊 Intercontinental
    ],
    [TerritoryName.Argentina] = [
        TerritoryName.Peru,
        TerritoryName.Brazil
    ],

    // ═══ EUROPA ═══
    [TerritoryName.Iceland] = [
        TerritoryName.Greenland,  // 🌊
        TerritoryName.GreatBritain,
        TerritoryName.Scandinavia
    ],
    [TerritoryName.GreatBritain] = [
        TerritoryName.Iceland,
        TerritoryName.Scandinavia,
        TerritoryName.WesternEurope,
        TerritoryName.NorthernEurope
    ],
    [TerritoryName.Scandinavia] = [
        TerritoryName.Iceland,
        TerritoryName.GreatBritain,
        TerritoryName.NorthernEurope,
        TerritoryName.Ukraine
    ],
    [TerritoryName.WesternEurope] = [
        TerritoryName.GreatBritain,
        TerritoryName.NorthernEurope,
        TerritoryName.SouthernEurope,
        TerritoryName.NorthAfrica  // 🌊
    ],
    [TerritoryName.NorthernEurope] = [
        TerritoryName.GreatBritain,
        TerritoryName.Scandinavia,
        TerritoryName.WesternEurope,
        TerritoryName.SouthernEurope,
        TerritoryName.Ukraine
    ],
    [TerritoryName.SouthernEurope] = [
        TerritoryName.WesternEurope,
        TerritoryName.NorthernEurope,
        TerritoryName.Ukraine,
        TerritoryName.NorthAfrica,  // 🌊
        TerritoryName.Egypt,        // 🌊
        TerritoryName.MiddleEast    // 🌊
    ],
    [TerritoryName.Ukraine] = [
        TerritoryName.Scandinavia,
        TerritoryName.NorthernEurope,
        TerritoryName.SouthernEurope,
        TerritoryName.Ural,         // Europa ↔ Asia
        TerritoryName.Afghanistan,  // Europa ↔ Asia
        TerritoryName.MiddleEast    // Europa ↔ Asia
    ],

    // ═══ ÁFRICA ═══
    [TerritoryName.NorthAfrica] = [
        TerritoryName.Brazil,          // 🌊
        TerritoryName.WesternEurope,   // 🌊
        TerritoryName.SouthernEurope,  // 🌊
        TerritoryName.Egypt,
        TerritoryName.EastAfrica,
        TerritoryName.Congo
    ],
    [TerritoryName.Egypt] = [
        TerritoryName.SouthernEurope,  // 🌊
        TerritoryName.NorthAfrica,
        TerritoryName.EastAfrica,
        TerritoryName.MiddleEast
    ],
    [TerritoryName.EastAfrica] = [
        TerritoryName.NorthAfrica,
        TerritoryName.Egypt,
        TerritoryName.Congo,
        TerritoryName.SouthAfrica,
        TerritoryName.Madagascar,
        TerritoryName.MiddleEast
    ],
    [TerritoryName.Congo] = [
        TerritoryName.NorthAfrica,
        TerritoryName.EastAfrica,
        TerritoryName.SouthAfrica
    ],
    [TerritoryName.SouthAfrica] = [
        TerritoryName.Congo,
        TerritoryName.EastAfrica,
        TerritoryName.Madagascar
    ],
    [TerritoryName.Madagascar] = [
        TerritoryName.EastAfrica,
        TerritoryName.SouthAfrica
    ],

    // ═══ ASIA ═══
    [TerritoryName.MiddleEast] = [
        TerritoryName.SouthernEurope,  // 🌊
        TerritoryName.Egypt,
        TerritoryName.EastAfrica,
        TerritoryName.Ukraine,
        TerritoryName.Afghanistan,
        TerritoryName.India
    ],
    [TerritoryName.Afghanistan] = [
        TerritoryName.Ukraine,
        TerritoryName.MiddleEast,
        TerritoryName.Ural,
        TerritoryName.China,
        TerritoryName.India
    ],
    [TerritoryName.Ural] = [
        TerritoryName.Ukraine,
        TerritoryName.Afghanistan,
        TerritoryName.Siberia,
        TerritoryName.China
    ],
    [TerritoryName.Siberia] = [
        TerritoryName.Ural,
        TerritoryName.Yakutsk,
        TerritoryName.Irkutsk,
        TerritoryName.Mongolia,
        TerritoryName.China
    ],
    [TerritoryName.Yakutsk] = [
        TerritoryName.Siberia,
        TerritoryName.Irkutsk,
        TerritoryName.Kamchatka
    ],
    [TerritoryName.Irkutsk] = [
        TerritoryName.Siberia,
        TerritoryName.Yakutsk,
        TerritoryName.Kamchatka,
        TerritoryName.Mongolia
    ],
    [TerritoryName.Kamchatka] = [
        TerritoryName.Yakutsk,
        TerritoryName.Irkutsk,
        TerritoryName.Mongolia,
        TerritoryName.Japan,
        TerritoryName.Alaska  // 🌊 Intercontinental
    ],
    [TerritoryName.Mongolia] = [
        TerritoryName.Siberia,
        TerritoryName.Irkutsk,
        TerritoryName.Kamchatka,
        TerritoryName.Japan,
        TerritoryName.China
    ],
    [TerritoryName.Japan] = [
        TerritoryName.Kamchatka,
        TerritoryName.Mongolia
    ],
    [TerritoryName.China] = [
        TerritoryName.Afghanistan,
        TerritoryName.Ural,
        TerritoryName.Siberia,
        TerritoryName.Mongolia,
        TerritoryName.India,
        TerritoryName.SoutheastAsia
    ],
    [TerritoryName.India] = [
        TerritoryName.MiddleEast,
        TerritoryName.Afghanistan,
        TerritoryName.China,
        TerritoryName.SoutheastAsia
    ],
    [TerritoryName.SoutheastAsia] = [
        TerritoryName.China,
        TerritoryName.India,
        TerritoryName.Indonesia  // 🌊
    ],

    // ═══ OCEANÍA ═══
    [TerritoryName.Indonesia] = [
        TerritoryName.SoutheastAsia,  // 🌊
        TerritoryName.NewGuinea,
        TerritoryName.WesternAustralia
    ],
    [TerritoryName.NewGuinea] = [
        TerritoryName.Indonesia,
        TerritoryName.EasternAustralia,
        TerritoryName.WesternAustralia
    ],
    [TerritoryName.WesternAustralia] = [
        TerritoryName.Indonesia,
        TerritoryName.NewGuinea,
        TerritoryName.EasternAustralia
    ],
    [TerritoryName.EasternAustralia] = [
        TerritoryName.NewGuinea,
        TerritoryName.WesternAustralia
    ],
};
```

---

## 6. Conexiones Intercontinentales (Líneas SVG)

Las conexiones que cruzan océanos se representan con líneas discontinuas:

```xml
<g id="connections" stroke="rgba(255,255,255,0.15)" stroke-width="1.5"
   stroke-dasharray="6,4" fill="none">

    <!-- Alaska ↔ Kamchatka (Estrecho de Bering) -->
    <!-- Se dibuja como dos líneas: Alaska hasta el borde izquierdo,
         y Kamchatka hasta el borde derecho (cruce del Pacífico) -->
    <line x1="65" y1="75" x2="30" y2="60" />     <!-- Alaska → borde izq -->
    <line x1="1170" y1="60" x2="1010" y2="60" />  <!-- borde der → Kamchatka -->

    <!-- Greenland ↔ Iceland (Atlántico Norte) -->
    <line x1="340" y1="45" x2="465" y2="55" />

    <!-- Central America ↔ Venezuela -->
    <line x1="160" y1="290" x2="220" y2="365" />

    <!-- Brazil ↔ North Africa (Atlántico Sur) -->
    <line x1="290" y1="430" x2="490" y2="340" />

    <!-- Western Europe ↔ North Africa -->
    <line x1="475" y1="220" x2="490" y2="340" />

    <!-- Southern Europe ↔ North Africa -->
    <line x1="545" y1="230" x2="490" y2="340" />

    <!-- Southern Europe ↔ Egypt -->
    <line x1="545" y1="230" x2="570" y2="310" />

    <!-- Southern Europe ↔ Middle East -->
    <line x1="545" y1="230" x2="650" y2="270" />

    <!-- East Africa ↔ Middle East -->
    <line x1="600" y1="410" x2="650" y2="270" />

    <!-- Southeast Asia ↔ Indonesia -->
    <line x1="880" y1="330" x2="910" y2="430" />

</g>
```

---

## 7. Interactividad

### 7.1 Resumen de Interacciones

| Acción del usuario | Fase | Resultado visual | Resultado funcional |
|-------------------|:----:|------------------|---------------------|
| **Hover** sobre territorio | Todas | Brillo, borde blanco, tooltip | — |
| **Click** en territorio propio | Refuerzo | Se selecciona (borde pulsante) | Se puede colocar refuerzos ahí |
| **Click** en territorio propio con ≥2 ej. | Ataque | Se selecciona como atacante (borde blanco pulsante) | — |
| **Click** en territorio enemigo adyacente | Ataque | Se selecciona como defensor (borde rojo) | Se habilita botón "Atacar" |
| **Click** en territorio propio (1º) | Fortificación | Se selecciona como origen (borde azul) | — |
| **Click** en territorio propio (2º) | Fortificación | Se selecciona como destino (borde verde) | Se habilita botón "Fortificar" |
| **Click** en territorio no seleccionable | Cualquiera | Nada (cursor `not-allowed`) | — |

### 7.2 Tooltip de Territorio

```
┌──────────────────────────────┐
│  🗺️ Alaska                   │
│  Dueño: Carlos 🔴            │
│  Ejércitos: 5                │
│  Continente: América del Norte│
└──────────────────────────────┘
```

Implementado con CSS puro (`:hover` + `::after`) o con un componente `Tooltip.razor` ligero.

### 7.3 Estilos de Interactividad

```css
/* Territorio hover */
.territory--selectable .territory-shape {
    cursor: pointer;
    transition: filter 0.2s ease, stroke 0.2s ease;
}

.territory--selectable .territory-shape:hover {
    filter: brightness(1.3);
    stroke: rgba(255, 255, 255, 0.6);
    stroke-width: 2;
}

/* No seleccionable */
.territory--disabled .territory-shape {
    cursor: not-allowed;
    opacity: 0.5;
}

/* Territorio seleccionado (atacante / origen) */
.territory--selected .territory-shape {
    stroke: white;
    stroke-width: 3;
    animation: pulse 1.5s ease-in-out infinite;
}

/* Territorio objetivo (defensor / destino) */
.territory--target .territory-shape {
    stroke: var(--color-danger);
    stroke-width: 2.5;
    animation: pulse 1.5s ease-in-out infinite;
}

/* Territorio de fortificación */
.territory--fortify-from .territory-shape {
    stroke: var(--color-info);
    stroke-width: 2.5;
}

.territory--fortify-to .territory-shape {
    stroke: var(--color-success);
    stroke-width: 2.5;
}

/* Animación de conquista */
.territory--conquered .territory-shape {
    animation: conquest-flash 0.8s ease-out;
}

@keyframes conquest-flash {
    0% { fill: white; filter: brightness(2); }
    50% { filter: brightness(1.5); }
    100% { filter: brightness(1); }
}
```

---

## 8. Responsive y Zoom

### 8.1 Ajuste al Contenedor

El SVG se ajusta automáticamente al contenedor manteniendo el ratio de aspecto:

```css
.world-map-container {
    width: 100%;
    height: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    overflow: hidden;
    background: #0a0e1a;
    border-radius: 12px;
    border: 1px solid var(--border-subtle);
}

.world-map {
    width: 100%;
    height: 100%;
    max-width: 100%;
    max-height: 100%;
}
```

### 8.2 Zoom (Futuro)

Para partidas en pantallas pequeñas, se podría implementar zoom:

```css
.world-map-container.zoomable {
    cursor: grab;
    overflow: auto;
}

.world-map.zoomed {
    width: 150%;
    height: 150%;
    cursor: grab;
}

.world-map.zoomed:active {
    cursor: grabbing;
}
```

Esto queda fuera del alcance de la v1.0 pero se menciona como mejora futura.

---

## 9. Validación de Integridad del Mapa

Para garantizar la correctitud del mapa, se incluyen tests:

```csharp
[Fact]
public void Map_ShouldHave42Territories()
{
    var mapService = new MapService();
    var territories = mapService.CreateTerritories();
    Assert.Equal(42, territories.Count);
}

[Fact]
public void Map_ShouldHave6Continents()
{
    var mapService = new MapService();
    var continents = mapService.CreateContinents();
    Assert.Equal(6, continents.Count);
}

[Fact]
public void Map_AllTerritoriesBelongToAContinent()
{
    var mapService = new MapService();
    var territories = mapService.CreateTerritories();
    var continents = mapService.CreateContinents();

    var allContinentTerritories = continents.Values
        .SelectMany(c => c.Territories)
        .ToHashSet();

    Assert.Equal(42, allContinentTerritories.Count);
    foreach (var territory in territories.Keys)
    {
        Assert.Contains(territory, allContinentTerritories);
    }
}

[Fact]
public void Map_AdjacenciesAreBidirectional()
{
    var mapService = new MapService();
    var territories = mapService.CreateTerritories();

    foreach (var (name, territory) in territories)
    {
        foreach (var neighbor in territory.AdjacentTerritories)
        {
            Assert.Contains(name, territories[neighbor].AdjacentTerritories);
        }
    }
}

[Fact]
public void Map_AllTerritoriesAreReachable()
{
    // Verifica que el grafo es conexo (no hay territorios aislados)
    var mapService = new MapService();
    var territories = mapService.CreateTerritories();

    var visited = new HashSet<TerritoryName>();
    var queue = new Queue<TerritoryName>();
    var start = territories.Keys.First();

    queue.Enqueue(start);
    visited.Add(start);

    while (queue.Count > 0)
    {
        var current = queue.Dequeue();
        foreach (var neighbor in territories[current].AdjacentTerritories)
        {
            if (visited.Add(neighbor))
            {
                queue.Enqueue(neighbor);
            }
        }
    }

    Assert.Equal(42, visited.Count);
}

[Fact]
public void Map_CardDeckHas44Cards()
{
    var mapService = new MapService();
    var deck = mapService.CreateCardDeck();

    Assert.Equal(44, deck.Count);
    Assert.Equal(14, deck.Count(c => c.Type == CardType.Infantry));
    Assert.Equal(14, deck.Count(c => c.Type == CardType.Cavalry));
    Assert.Equal(14, deck.Count(c => c.Type == CardType.Artillery));
    Assert.Equal(2, deck.Count(c => c.Type == CardType.Wildcard));
}

[Fact]
public void Map_ContinentBonusesAreCorrect()
{
    var mapService = new MapService();
    var continents = mapService.CreateContinents();

    Assert.Equal(5, continents[ContinentName.NorthAmerica].BonusArmies);
    Assert.Equal(2, continents[ContinentName.SouthAmerica].BonusArmies);
    Assert.Equal(5, continents[ContinentName.Europe].BonusArmies);
    Assert.Equal(3, continents[ContinentName.Africa].BonusArmies);
    Assert.Equal(7, continents[ContinentName.Asia].BonusArmies);
    Assert.Equal(2, continents[ContinentName.Oceania].BonusArmies);
}
```

---

> **Siguiente documento:** [11 — Servicios e Inyección de Dependencias](./11_Servicios_DI.md)
