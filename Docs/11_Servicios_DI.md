# 11 — Servicios e Inyección de Dependencias

> **Documento:** 11 de 14  
> **Versión:** 1.0  
> **Última actualización:** 22 de febrero de 2026  
> **Volver al índice:** [00_Indice.md](./00_Indice.md)  
> **Documento anterior:** [10 — Mapa del Mundo](./10_Mapa_Mundo.md)

---

## 1. Visión General

MiniRisk utiliza el **contenedor de inyección de dependencias (DI)** nativo de ASP.NET Core para gestionar la creación, la vida y las dependencias entre servicios. No se usan contenedores de terceros (Autofac, Ninject, etc.): el contenedor integrado cubre todas las necesidades.

### 1.1 Principios

| Principio | Aplicación |
|-----------|-----------|
| **Programar contra interfaces** | Todos los servicios se exponen como interfaces (`IGameManager`, `IGameEngine`, etc.) |
| **Inversión de dependencias** | Las clases de alto nivel (Hub, componentes) dependen de abstracciones, no de implementaciones |
| **Ciclo de vida explícito** | Cada servicio tiene un ciclo de vida justificado: Singleton, Scoped o Transient |
| **Composición en la raíz** | Toda la configuración de DI se concentra en `Program.cs` |
| **Sin service locator** | No se usa `IServiceProvider` directamente en código de negocio; solo inyección por constructor |

### 1.2 Estructura de Archivos

```
MiniRisk/
├── Services/
│   ├── Interfaces/
│   │   ├── IGameManager.cs
│   │   ├── IGameEngine.cs
│   │   ├── IDiceService.cs
│   │   ├── ICardService.cs
│   │   ├── IMapService.cs
│   │   └── IPlayerSessionService.cs
│   │
│   ├── GameManager.cs
│   ├── GameEngine.cs
│   ├── DiceService.cs
│   ├── CardService.cs
│   ├── MapService.cs
│   ├── PlayerSessionService.cs
│   └── GameCleanupService.cs
│
├── Hubs/
│   └── GameHub.cs
│
└── Program.cs                ← Composición raíz (registro de DI)
```

---

## 2. Registro de Servicios en Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════
// 1. BLAZOR SERVER + SIGNALR
// ═══════════════════════════════════════════════════════════
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR(options =>
{
    // Tamaño máximo de mensaje (para GameStateDto grandes)
    options.MaximumReceiveMessageSize = 64 * 1024; // 64 KB

    // Timeout de conexión
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

// ═══════════════════════════════════════════════════════════
// 2. SERVICIOS — SINGLETON (estado compartido, viven toda la app)
// ═══════════════════════════════════════════════════════════
builder.Services.AddSingleton<IGameManager, GameManager>();
builder.Services.AddSingleton<IMapService, MapService>();

// ═══════════════════════════════════════════════════════════
// 3. SERVICIOS — SCOPED (uno por circuito Blazor / jugador)
// ═══════════════════════════════════════════════════════════
builder.Services.AddScoped<IPlayerSessionService, PlayerSessionService>();

// ═══════════════════════════════════════════════════════════
// 4. SERVICIOS — TRANSIENT (sin estado, nueva instancia por uso)
// ═══════════════════════════════════════════════════════════
builder.Services.AddTransient<IGameEngine, GameEngine>();
builder.Services.AddTransient<IDiceService, DiceService>();
builder.Services.AddTransient<ICardService, CardService>();

// ═══════════════════════════════════════════════════════════
// 5. SERVICIOS DE INFRAESTRUCTURA
// ═══════════════════════════════════════════════════════════
builder.Services.AddHostedService<GameCleanupService>();

// ═══════════════════════════════════════════════════════════
// LOGGING
// ═══════════════════════════════════════════════════════════
builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

// ═══════════════════════════════════════════════════════════
// PIPELINE HTTP
// ═══════════════════════════════════════════════════════════
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Mapear el hub de SignalR del juego
app.MapHub<GameHub>("/gamehub");

app.Run();
```

---

## 3. Catálogo de Servicios

### 3.1 Tabla Resumen

| Servicio | Interfaz | Ciclo de Vida | Estado | Dependencias |
|----------|----------|:------------:|--------|-------------|
| `GameManager` | `IGameManager` | **Singleton** | Partidas activas, conexiones, jugadores | `ILogger<GameManager>` |
| `MapService` | `IMapService` | **Singleton** | Datos estáticos del mapa (inmutable) | — |
| `PlayerSessionService` | `IPlayerSessionService` | **Scoped** | Nombre, ID, color del jugador actual | — |
| `GameEngine` | `IGameEngine` | **Transient** | Ninguno | `IDiceService`, `ICardService`, `IMapService` |
| `DiceService` | `IDiceService` | **Transient** | Ninguno | — |
| `CardService` | `ICardService` | **Transient** | Ninguno | — |
| `GameHub` | *(Hub)* | **Transient** | Ninguno | `IGameManager`, `IGameEngine` |
| `GameCleanupService` | *(Hosted)* | **Singleton** | Ninguno | `IGameManager`, `ILogger` |

### 3.2 Diagrama de Dependencias

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      DIAGRAMA DE DEPENDENCIAS                           │
│                                                                         │
│                                                                         │
│  ┌──────────────────────┐         ┌──────────────────────┐             │
│  │   Componentes Blazor  │         │      GameHub         │             │
│  │   (Pages/Shared)      │         │   (SignalR Hub)      │             │
│  │                       │         │                      │             │
│  │ Home.razor            │         │  Transient           │             │
│  │ Lobby.razor           │         │  (1 instancia por    │             │
│  │ Game.razor            │         │   invocación)        │             │
│  └───┬──────────┬────────┘         └───┬─────────┬───────┘             │
│      │          │                      │         │                      │
│      │          │                      │         │                      │
│      ▼          ▼                      ▼         ▼                      │
│  ┌────────┐ ┌──────────────┐   ┌────────────┐ ┌──────────────┐        │
│  │Player  │ │ Navigation   │   │GameManager │ │ GameEngine   │        │
│  │Session │ │ Manager      │   │            │ │              │        │
│  │Service │ │ (framework)  │   │ Singleton  │ │ Transient    │        │
│  │        │ │              │   │            │ │              │        │
│  │Scoped  │ └──────────────┘   └──────┬─────┘ └──┬───┬───┬──┘        │
│  │        │                           │          │   │   │            │
│  └────────┘                           │          │   │   │            │
│                                       │          ▼   │   ▼            │
│                                       │    ┌───────┐ │ ┌──────────┐  │
│                                       │    │ Dice  │ │ │  Card    │  │
│                            ┌──────────┘    │Service│ │ │ Service  │  │
│                            │               │       │ │ │          │  │
│                            │               │Transi.│ │ │ Transient│  │
│                            │               └───────┘ │ └──────────┘  │
│                            │                         │                │
│                            ▼                         ▼                │
│                    ┌──────────────────────────────────────┐           │
│                    │            MapService                 │           │
│                    │                                      │           │
│                    │  Singleton (datos estáticos inmut.)   │           │
│                    │                                      │           │
│                    │  • 42 territorios + adyacencias      │           │
│                    │  • 6 continentes + bonificaciones    │           │
│                    │  • 44 cartas (plantilla del mazo)    │           │
│                    └──────────────────────────────────────┘           │
│                                                                       │
│          ┌───────────────────────────────────────┐                    │
│          │        GameCleanupService              │                    │
│          │        (BackgroundService / Hosted)     │                    │
│          │                                        │                    │
│          │  Limpia partidas expiradas cada 5 min   │                    │
│          │  Depende de: IGameManager, ILogger      │                    │
│          └───────────────────────────────────────┘                    │
│                                                                       │
│  LEYENDA:                                                             │
│  ──▶  = depende de (inyección por constructor)                        │
│  Singleton  = una instancia para toda la aplicación                   │
│  Scoped     = una instancia por circuito Blazor (por jugador)         │
│  Transient  = nueva instancia cada vez que se solicita                │
│                                                                       │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Detalle de Cada Servicio

### 4.1 IGameManager / GameManager — Singleton

**Responsabilidad:** Orquestador central de todas las partidas activas. Almacena el estado global del sistema.

> Documentado en detalle en [07 — Gestión de Estado](./07_Gestion_Estado.md).

```csharp
public interface IGameManager
{
    // Gestión de partidas
    GameSummary CreateGame(string name, string creatorPlayerId,
        string creatorPlayerName, GameSettings settings);
    List<GameSummary> GetAvailableGames();
    Game? GetGame(string gameId);
    GameStateDto GetGameState(string gameId);
    void RemoveGame(string gameId);

    // Gestión de jugadores en partida
    JoinResult AddPlayer(string gameId, string playerId,
        string playerName, string connectionId);
    void RemovePlayer(string gameId, string playerId);
    void UpdatePlayerConnection(string gameId, string playerId, string connectionId);
    string? GetPlayerName(string playerId);

    // Conexiones globales
    void RegisterConnection(string connectionId, string playerId);
    void UnregisterConnection(string connectionId);
    string? GetPlayerIdByConnection(string connectionId);
    bool IsNameTaken(string name);

    // Desconexión / Reconexión
    void MarkPlayerDisconnected(string gameId, string playerId);
    void MarkPlayerReconnected(string gameId, string playerId, string connectionId);
    ReconnectionInfo? GetReconnectionInfo(string playerName);

    // Acceso con lock
    Task<T> ExecuteWithLock<T>(string gameId, Func<Game, T> action);
    Task ExecuteWithLock(string gameId, Func<Game, Task> action);
}
```

| Aspecto | Detalle |
|---------|--------|
| **¿Por qué Singleton?** | Debe sobrevivir a todos los circuitos. Es el repositorio central de todas las partidas. Todos los jugadores y hubs acceden al mismo `GameManager`. |
| **Thread-Safety** | `ConcurrentDictionary` para colecciones + `SemaphoreSlim` por partida para operaciones de modificación. |
| **Dependencias** | `ILogger<GameManager>` |
| **Dónde se inyecta** | `GameHub`, `GameCleanupService`, y opcionalmente en componentes Blazor (Lobby). |

---

### 4.2 IGameEngine / GameEngine — Transient

**Responsabilidad:** Implementa todas las reglas del juego. Valida acciones, resuelve combates, calcula refuerzos.

> Documentado en detalle en [06 — Motor del Juego](./06_Motor_Juego.md).

```csharp
public interface IGameEngine
{
    // Inicialización
    GameResult InitializeGame(Game game);
    GameResult DistributeTerritoriesRandomly(Game game);
    GameResult PlaceInitialArmies(Game game, string playerId,
        TerritoryName territory, int count);
    GameResult StartPlaying(Game game);

    // Refuerzo
    int CalculateReinforcements(Game game, Player player);
    GameResult PlaceReinforcements(Game game, string playerId,
        TerritoryName territory, int count);
    GameResult ConfirmReinforcements(Game game, string playerId);

    // Ataque
    AttackGameResult Attack(Game game, string playerId,
        TerritoryName from, TerritoryName to, int attackDiceCount);
    GameResult MoveArmiesAfterConquest(Game game, string playerId,
        TerritoryName from, TerritoryName to, int armyCount);
    GameResult EndAttackPhase(Game game, string playerId);

    // Fortificación
    GameResult Fortify(Game game, string playerId,
        TerritoryName from, TerritoryName to, int armyCount);
    GameResult SkipFortification(Game game, string playerId);
    bool AreConnected(Game game, string playerId,
        TerritoryName from, TerritoryName to);

    // Cartas
    GameResult TradeCards(Game game, string playerId, string[] cardIds);

    // Turno
    GameResult EndTurn(Game game, string playerId);

    // Consultas
    bool IsGameOver(Game game);
    Player? GetWinner(Game game);
}
```

| Aspecto | Detalle |
|---------|--------|
| **¿Por qué Transient?** | No tiene estado propio. Recibe `Game` como parámetro y lo modifica. Una nueva instancia por invocación es óptima. No hay razón para mantener la instancia viva. |
| **Thread-Safety** | No necesita. El acceso siempre está protegido por `GameManager.ExecuteWithLock()`. |
| **Dependencias** | `IDiceService`, `ICardService`, `IMapService` |
| **Dónde se inyecta** | `GameHub` |

---

### 4.3 IDiceService / DiceService — Transient

**Responsabilidad:** Generar tiradas de dados aleatorias.

```csharp
public interface IDiceService
{
    /// <summary>
    /// Tira N dados de 6 caras (1-6).
    /// Retorna los resultados ordenados de mayor a menor.
    /// </summary>
    int[] Roll(int numberOfDice);
}

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
```

| Aspecto | Detalle |
|---------|--------|
| **¿Por qué Transient?** | Sin estado. Cada tirada es independiente. `Random.Shared` es thread-safe (estático, .NET 6+). |
| **Thread-Safety** | `Random.Shared` es thread-safe por diseño. |
| **Dependencias** | Ninguna |
| **Dónde se inyecta** | `GameEngine` |
| **Testing** | Se inyecta un mock que retorna dados predefinidos para testing determinista. |

---

### 4.4 ICardService / CardService — Transient

**Responsabilidad:** Validar combinaciones de canje de cartas y calcular ejércitos por canje.

```csharp
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
```

| Aspecto | Detalle |
|---------|--------|
| **¿Por qué Transient?** | Sin estado. Lógica pura de validación y cálculo. |
| **Dependencias** | Ninguna |
| **Dónde se inyecta** | `GameEngine` |

---

### 4.5 IMapService / MapService — Singleton

**Responsabilidad:** Proveer los datos estáticos del mapa: territorios, adyacencias, continentes y plantilla del mazo de cartas. Estos datos son **inmutables** y se cargan una sola vez.

```csharp
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
```

**Implementación (extracto):**

```csharp
public class MapService : IMapService
{
    // ═══════════════════════════════════════
    // DATOS ESTÁTICOS (cargados una sola vez)
    // ═══════════════════════════════════════

    private static readonly Dictionary<TerritoryName, List<TerritoryName>> Adjacencies = new()
    {
        [TerritoryName.Alaska] = [
            TerritoryName.NorthwestTerritory,
            TerritoryName.Alberta,
            TerritoryName.Kamchatka   // Conexión intercontinental
        ],
        [TerritoryName.NorthwestTerritory] = [
            TerritoryName.Alaska,
            TerritoryName.Alberta,
            TerritoryName.Ontario,
            TerritoryName.Greenland
        ],
        // ... (42 territorios con todas sus adyacencias)
    };

    private static readonly Dictionary<ContinentName, (int Bonus, List<TerritoryName> Territories)>
        ContinentData = new()
    {
        [ContinentName.NorthAmerica] = (5, [
            TerritoryName.Alaska, TerritoryName.NorthwestTerritory,
            TerritoryName.Greenland, TerritoryName.Alberta,
            TerritoryName.Ontario, TerritoryName.Quebec,
            TerritoryName.WesternUnitedStates, TerritoryName.EasternUnitedStates,
            TerritoryName.CentralAmerica
        ]),
        [ContinentName.SouthAmerica] = (2, [
            TerritoryName.Venezuela, TerritoryName.Peru,
            TerritoryName.Brazil, TerritoryName.Argentina
        ]),
        [ContinentName.Europe] = (5, [
            TerritoryName.Iceland, TerritoryName.GreatBritain,
            TerritoryName.Scandinavia, TerritoryName.WesternEurope,
            TerritoryName.NorthernEurope, TerritoryName.SouthernEurope,
            TerritoryName.Ukraine
        ]),
        [ContinentName.Africa] = (3, [
            TerritoryName.NorthAfrica, TerritoryName.Egypt,
            TerritoryName.EastAfrica, TerritoryName.Congo,
            TerritoryName.SouthAfrica, TerritoryName.Madagascar
        ]),
        [ContinentName.Asia] = (7, [
            TerritoryName.MiddleEast, TerritoryName.Afghanistan,
            TerritoryName.Ural, TerritoryName.Siberia,
            TerritoryName.Yakutsk, TerritoryName.Irkutsk,
            TerritoryName.Kamchatka, TerritoryName.Mongolia,
            TerritoryName.Japan, TerritoryName.China,
            TerritoryName.India, TerritoryName.SoutheastAsia
        ]),
        [ContinentName.Oceania] = (2, [
            TerritoryName.Indonesia, TerritoryName.NewGuinea,
            TerritoryName.WesternAustralia, TerritoryName.EasternAustralia
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
        // ... (42 territorios)
    };

    // ═══════════════════════════════════════
    // MÉTODOS PÚBLICOS
    // ═══════════════════════════════════════

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

        // 42 cartas de territorio (14 de cada tipo)
        for (int i = 0; i < allTerritories.Count; i++)
        {
            cards.Add(new Card
            {
                Type = types[i % 3],
                Territory = allTerritories[i]
            });
        }

        // 2 comodines
        cards.Add(new Card { Type = CardType.Wildcard, Territory = null });
        cards.Add(new Card { Type = CardType.Wildcard, Territory = null });

        return cards; // Total: 44
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
```

| Aspecto | Detalle |
|---------|--------|
| **¿Por qué Singleton?** | Los datos del mapa son estáticos e inmutables. No tiene sentido recrearlos. Se cargan una sola vez y se reutilizan. |
| **Thread-Safety** | Los datos internos son `static readonly` e inmutables. `CreateTerritories()` y `CreateCardDeck()` crean nuevas instancias cada vez (para cada partida). |
| **Dependencias** | Ninguna |
| **Dónde se inyecta** | `GameEngine` |

---

### 4.6 IPlayerSessionService / PlayerSessionService — Scoped

**Responsabilidad:** Almacenar la identidad del jugador actual durante la vida de su circuito Blazor.

> Documentado en detalle en [03 — Identificación de Jugadores](./03_Identificacion_Jugadores.md).

```csharp
public interface IPlayerSessionService
{
    bool IsIdentified { get; }
    string PlayerId { get; }
    string PlayerName { get; }
    PlayerColor? AssignedColor { get; }
    string? CurrentGameId { get; }
    DateTime ConnectedAt { get; }

    void SetPlayer(string name);
    void SetColor(PlayerColor color);
    void JoinGame(string gameId);
    void LeaveGame();
    void Clear();
}
```

| Aspecto | Detalle |
|---------|--------|
| **¿Por qué Scoped?** | En Blazor Server, Scoped = una instancia por circuito. Cada pestaña del navegador (cada jugador) tiene su propia sesión aislada. Si fuera Singleton, todos compartirían el mismo nombre. Si fuera Transient, el nombre se perdería entre componentes. |
| **Thread-Safety** | No necesita. Un circuito Blazor procesa eventos secuencialmente (single-threaded por diseño). |
| **Dependencias** | Ninguna |
| **Dónde se inyecta** | Componentes Blazor (`Home.razor`, `Lobby.razor`, `Game.razor`) |

---

### 4.7 GameHub — Transient (por diseño de SignalR)

**Responsabilidad:** Punto de entrada de comunicación en tiempo real. Recibe invocaciones de los clientes, delega al `GameEngine` y difunde resultados.

> Documentado en detalle en [04 — SignalR](./04_SignalR.md).

```csharp
[AllowAnonymous]
public class GameHub : Hub
{
    private readonly IGameManager _gameManager;
    private readonly IGameEngine _gameEngine;

    public GameHub(IGameManager gameManager, IGameEngine gameEngine)
    {
        _gameManager = gameManager;
        _gameEngine = gameEngine;
    }

    // Métodos del servidor: JoinGame, Attack, Fortify, TradeCards, etc.
    // Métodos de ciclo de vida: OnConnectedAsync, OnDisconnectedAsync
}
```

| Aspecto | Detalle |
|---------|--------|
| **¿Por qué Transient?** | Los Hubs de SignalR son transient por diseño de ASP.NET Core. Se crea una instancia por cada invocación de método. No se debe almacenar estado en el Hub. |
| **Thread-Safety** | No aplica; cada invocación usa su propia instancia. |
| **Dependencias** | `IGameManager`, `IGameEngine` |
| **Restricción importante** | ⚠️ No se pueden inyectar servicios **Scoped** en un Hub. El Hub no tiene circuito Blazor asociado. Para identificar al jugador, se usa el `ConnectionId` y el `GameManager`. |

---

### 4.8 GameCleanupService — Hosted (Singleton)

**Responsabilidad:** Limpieza periódica de partidas expiradas para liberar memoria.

> Documentado en [07 — Gestión de Estado](./07_Gestion_Estado.md).

| Aspecto | Detalle |
|---------|--------|
| **Ciclo de vida** | `BackgroundService` registrado como `AddHostedService`. Se ejecuta como Singleton durante toda la vida de la aplicación. |
| **Intervalo** | Cada 5 minutos |
| **Dependencias** | `IGameManager`, `ILogger<GameCleanupService>` |

---

## 5. Ciclos de Vida en Detalle

### 5.1 Singleton vs Scoped vs Transient

```
  ┌──────────────────────────────────────────────────────────────────┐
  │                    CICLOS DE VIDA EN ASP.NET CORE                 │
  │                                                                  │
  │  SINGLETON ───────────────────────────────────────────────────── │
  │  │  Creado una sola vez, al inicio de la aplicación.            │
  │  │  Compartido por todos los circuitos, hubs y servicios.       │
  │  │  Vive hasta que la aplicación se cierra.                     │
  │  │                                                              │
  │  │  En MiniRisk: GameManager, MapService                        │
  │  │                                                              │
  │  │  ┌──────────────────────────────────────────────────┐       │
  │  │  │              VIDA DE LA APLICACIÓN               │       │
  │  │  │  ┌─────┐  ┌─────┐  ┌─────┐                     │       │
  │  │  │  │Circ1│  │Circ2│  │Circ3│   ← todos comparten  │       │
  │  │  │  └─────┘  └─────┘  └─────┘     la misma         │       │
  │  │  │                                 instancia        │       │
  │  │  └──────────────────────────────────────────────────┘       │
  │                                                                  │
  │  SCOPED ─────────────────────────────────────────────────────── │
  │  │  Creado una vez por circuito Blazor (por pestaña/jugador).   │
  │  │  Todos los componentes del mismo circuito comparten          │
  │  │  la misma instancia. Se destruye al cerrar la pestaña.      │
  │  │                                                              │
  │  │  En MiniRisk: PlayerSessionService                           │
  │  │                                                              │
  │  │  ┌───────────────┐  ┌───────────────┐                      │
  │  │  │  Circuito 1   │  │  Circuito 2   │                      │
  │  │  │  ┌──────────┐ │  │  ┌──────────┐ │                      │
  │  │  │  │Instancia │ │  │  │Instancia │ │  ← cada circuito     │
  │  │  │  │ propia   │ │  │  │ propia   │ │    tiene la suya     │
  │  │  │  └──────────┘ │  │  └──────────┘ │                      │
  │  │  └───────────────┘  └───────────────┘                      │
  │                                                                  │
  │  TRANSIENT ──────────────────────────────────────────────────── │
  │  │  Creado cada vez que se solicita al contenedor.              │
  │  │  No se reutiliza. Se destruye al terminar la operación.     │
  │  │                                                              │
  │  │  En MiniRisk: GameEngine, DiceService, CardService, GameHub  │
  │  │                                                              │
  │  │  Petición 1 → nueva instancia                               │
  │  │  Petición 2 → nueva instancia (diferente)                   │
  │  │  Petición 3 → nueva instancia (diferente)                   │
  │                                                                  │
  └──────────────────────────────────────────────────────────────────┘
```

### 5.2 Reglas de Compatibilidad de Ciclos de Vida

| Consumidor ↓ / Dependencia → | Singleton | Scoped | Transient |
|:----------------------------:|:---------:|:------:|:---------:|
| **Singleton** | ✅ | ❌ **Captive dependency** | ⚠️ Cuidado (captura) |
| **Scoped** | ✅ | ✅ | ✅ |
| **Transient** | ✅ | ✅ (si está en scope) | ✅ |
| **Hub (Transient)** | ✅ | ❌ **No hay scope** | ✅ |

**Restricciones en MiniRisk:**

| Regla | Aplicación |
|-------|-----------|
| `GameManager` (Singleton) no puede inyectar `PlayerSessionService` (Scoped) | ✅ `GameManager` no depende de `PlayerSessionService` |
| `GameHub` (Transient sin scope) no puede inyectar `PlayerSessionService` (Scoped) | ✅ `GameHub` identifica al jugador por `ConnectionId` vía `GameManager` |
| `GameEngine` (Transient) puede inyectar `MapService` (Singleton) | ✅ Sin problemas |
| `GameEngine` (Transient) puede inyectar `DiceService` (Transient) | ✅ Sin problemas |

---

## 6. Inyección en Componentes Blazor

Los componentes Blazor inyectan servicios con la directiva `@inject`:

```razor
@* Home.razor *@
@inject IPlayerSessionService PlayerSession
@inject NavigationManager Navigation
@inject IGameManager GameManager

@* Lobby.razor *@
@inject IPlayerSessionService PlayerSession
@inject NavigationManager Navigation
@inject IGameManager GameManager

@* Game.razor *@
@inject IPlayerSessionService PlayerSession
@inject NavigationManager Navigation
@inject IGameManager GameManager
```

### 6.1 ¿Qué servicios inyectan los componentes?

| Componente | Servicios inyectados | Uso |
|-----------|---------------------|-----|
| `Home.razor` | `IPlayerSessionService`, `NavigationManager`, `IGameManager` | Identificar al jugador, verificar nombre, redirigir al lobby |
| `Lobby.razor` | `IPlayerSessionService`, `NavigationManager`, `IGameManager` | Verificar sesión, listar/crear/unirse a partidas |
| `Game.razor` | `IPlayerSessionService`, `NavigationManager`, `IGameManager` | Verificar sesión, crear `HubConnection`, obtener estado |
| Componentes hijos | Ningún servicio | Reciben datos por `[Parameter]` desde `Game.razor` |

> **Importante:** Los componentes hijos (`WorldMap`, `DiceRoller`, `PlayerPanel`, etc.) **no inyectan servicios directamente**. Reciben datos y callbacks del componente padre `Game.razor` vía `[Parameter]` y `[EventCallback]`. Esto mantiene los componentes simples y reutilizables.

---

## 7. Inyección en el GameHub

El `GameHub` recibe servicios por inyección de constructor, como cualquier otra clase:

```csharp
public class GameHub : Hub
{
    private readonly IGameManager _gameManager;
    private readonly IGameEngine _gameEngine;
    // ⚠️ NO se puede inyectar IPlayerSessionService aquí (es Scoped)

    public GameHub(IGameManager gameManager, IGameEngine gameEngine)
    {
        _gameManager = gameManager;
        _gameEngine = gameEngine;
    }
}
```

**¿Por qué no se puede inyectar `IPlayerSessionService` en el Hub?**

El Hub no tiene un circuito Blazor asociado. El Hub tiene su propio scope transitorio por invocación, y no coincide con el scope del circuito del jugador. Para identificar al jugador en el Hub se usa:

```csharp
// Identificar al jugador por su ConnectionId
var playerId = _gameManager.GetPlayerIdByConnection(Context.ConnectionId);
```

---

## 8. Testing y Mocks

Al programar contra interfaces, cada servicio es fácilmente sustituible por mocks en los tests:

```csharp
// Ejemplo: test del GameEngine con dados predecibles
[Fact]
public void Attack_WhenAttackerRollsHigher_DefenderLosesArmy()
{
    // Arrange
    var mockDice = new Mock<IDiceService>();
    mockDice.Setup(d => d.Roll(3)).Returns([6, 5, 4]);  // Atacante
    mockDice.Setup(d => d.Roll(2)).Returns([3, 2]);      // Defensor

    var mockCards = new Mock<ICardService>();
    var mapService = new MapService();  // Datos reales del mapa

    var engine = new GameEngine(mockDice.Object, mockCards.Object, mapService);

    var game = CreateTestGame();  // Helper para crear partida de prueba
    // ... setup territories ...

    // Act
    var result = engine.Attack(game, "player1",
        TerritoryName.Alaska, TerritoryName.Kamchatka, 3);

    // Assert
    Assert.True(result.Success);
    Assert.Equal(0, result.AttackResult!.AttackerLosses);
    Assert.Equal(2, result.AttackResult.DefenderLosses);
}
```

| Servicio | ¿Mock en tests? | Motivo |
|----------|:----------------:|--------|
| `IDiceService` | ✅ Siempre | Para resultados deterministas |
| `ICardService` | ✅ A veces | Para forzar canjes válidos/inválidos |
| `IMapService` | ❌ Usar real | Los datos del mapa son correctos y ligeros |
| `IGameManager` | ✅ En tests de Hub | Para aislar la lógica del Hub |
| `IGameEngine` | ✅ En tests de Hub | Para aislar la lógica del Hub |
| `IPlayerSessionService` | ✅ En tests de componentes (bUnit) | Para simular un jugador identificado |

---

## 9. Resumen Visual

```
┌──────────────────────────────────────────────────────────────────┐
│                      Program.cs                                   │
│                                                                   │
│  // Singleton                                                     │
│  services.AddSingleton<IGameManager, GameManager>();              │
│  services.AddSingleton<IMapService, MapService>();                │
│                                                                   │
│  // Scoped                                                        │
│  services.AddScoped<IPlayerSessionService, PlayerSessionService>();│
│                                                                   │
│  // Transient                                                     │
│  services.AddTransient<IGameEngine, GameEngine>();                │
│  services.AddTransient<IDiceService, DiceService>();              │
│  services.AddTransient<ICardService, CardService>();              │
│                                                                   │
│  // Hosted                                                        │
│  services.AddHostedService<GameCleanupService>();                 │
│                                                                   │
│  // Framework                                                     │
│  services.AddRazorComponents().AddInteractiveServerComponents();  │
│  services.AddSignalR();                                           │
│                                                                   │
│  // Endpoints                                                     │
│  app.MapRazorComponents<App>().AddInteractiveServerRenderMode();  │
│  app.MapHub<GameHub>("/gamehub");                                 │
└──────────────────────────────────────────────────────────────────┘
```

---

> **Siguiente documento:** [12 — Manejo de Errores y Resiliencia](./12_Errores_Resiliencia.md)
