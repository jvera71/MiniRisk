# 07 — Gestión de Estado

> **Documento:** 07 de 14  
> **Versión:** 1.0  
> **Última actualización:** 22 de febrero de 2026  
> **Volver al índice:** [00_Indice.md](./00_Indice.md)  
> **Documento anterior:** [06 — Motor del Juego (Game Engine)](./06_Motor_Juego.md)

---

## 1. Principios de la Gestión de Estado

MiniRisk mantiene **todo su estado en la memoria RAM** del servidor. No hay base de datos, no hay persistencia en disco, no hay caché distribuida. Esta decisión, documentada en [01 — Visión General](./01_Vision_General.md) y [02 — Arquitectura General](./02_Arquitectura_General.md), se justifica por el contexto de uso: partidas efímeras entre amigos en red local.

### 1.1 Decisiones Clave

| Decisión | Elección | Justificación |
|----------|---------|---------------|
| **Almacenamiento** | Memoria RAM (sin BD) | Las partidas son efímeras; no se necesita recuperarlas tras reinicio |
| **Fuente de verdad** | Servidor (no el cliente) | El cliente solo renderiza; el estado vive en el servidor. Previene manipulación. |
| **Estado centralizado** | `GameManager` Singleton | Un único punto de acceso a todas las partidas activas |
| **Estado por jugador** | `PlayerSessionService` Scoped | Cada circuito Blazor tiene su propia sesión de jugador |
| **Concurrencia** | `ConcurrentDictionary` + `SemaphoreSlim` | Múltiples jugadores acceden al mismo estado simultáneamente |
| **Sincronización** | SignalR → `StateHasChanged()` | Los cambios se propagan a todos los clientes vía GameHub |

### 1.2 Capas del Estado

```
┌─────────────────────────────────────────────────────────────────────┐
│                    SERVIDOR (.NET 10 / Kestrel)                     │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                  ESTADO GLOBAL (Singleton)                    │  │
│  │                                                               │  │
│  │  GameManager                                                  │  │
│  │  ├── ActiveGames: ConcurrentDictionary<string, GameContext>   │  │
│  │  │   ├── "abc-123" → GameContext { Game, Lock, Timers... }    │  │
│  │  │   ├── "def-456" → GameContext { Game, Lock, Timers... }    │  │
│  │  │   └── "ghi-789" → GameContext { Game, Lock, Timers... }    │  │
│  │  │                                                            │  │
│  │  └── ConnectedPlayers: ConcurrentDictionary<string, string>   │  │
│  │      ├── "connectionId-1" → "playerId-1"                     │  │
│  │      ├── "connectionId-2" → "playerId-2"                     │  │
│  │      └── ...                                                  │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐    │
│  │ ESTADO SCOPED   │  │ ESTADO SCOPED   │  │ ESTADO SCOPED   │    │
│  │ (Circuito J1)   │  │ (Circuito J2)   │  │ (Circuito J3)   │    │
│  │                 │  │                 │  │                 │    │
│  │ PlayerSession   │  │ PlayerSession   │  │ PlayerSession   │    │
│  │ ┌─────────────┐ │  │ ┌─────────────┐ │  │ ┌─────────────┐ │    │
│  │ │Name:"Carlos"│ │  │ │Name: "Ana"  │ │  │ │Name: "Luis" │ │    │
│  │ │Id: "p1"     │ │  │ │Id: "p2"     │ │  │ │Id: "p3"     │ │    │
│  │ │Game:"abc"   │ │  │ │Game:"abc"   │ │  │ │Game:"abc"   │ │    │
│  │ │Color: Red   │ │  │ │Color: Blue  │ │  │ │Color: Green │ │    │
│  │ └─────────────┘ │  │ └─────────────┘ │  │ └─────────────┘ │    │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘    │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 2. GameManager: Orquestador Central

### 2.1 Interfaz

```csharp
public interface IGameManager
{
    // ── Gestión de partidas ──
    GameSummary CreateGame(string name, string creatorPlayerId, 
        string creatorPlayerName, GameSettings settings);
    List<GameSummary> GetAvailableGames();
    Game? GetGame(string gameId);
    GameStateDto GetGameState(string gameId);
    void RemoveGame(string gameId);

    // ── Gestión de jugadores en partida ──
    JoinResult AddPlayer(string gameId, string playerId, 
        string playerName, string connectionId);
    void RemovePlayer(string gameId, string playerId);
    void UpdatePlayerConnection(string gameId, string playerId, string connectionId);
    string? GetPlayerName(string playerId);

    // ── Gestión de conexiones globales ──
    void RegisterConnection(string connectionId, string playerId);
    void UnregisterConnection(string connectionId);
    string? GetPlayerIdByConnection(string connectionId);
    bool IsNameTaken(string name);

    // ── Estado de desconexión ──
    void MarkPlayerDisconnected(string gameId, string playerId);
    void MarkPlayerReconnected(string gameId, string playerId, string connectionId);
    ReconnectionInfo? GetReconnectionInfo(string playerName);

    // ── Acceso con lock para operaciones del motor ──
    Task<T> ExecuteWithLock<T>(string gameId, Func<Game, T> action);
    Task ExecuteWithLock(string gameId, Func<Game, Task> action);
}
```

### 2.2 Implementación

```csharp
public class GameManager : IGameManager
{
    // ═══════════════════════════════════════
    // ESTADO INTERNO
    // ═══════════════════════════════════════
    
    /// <summary>
    /// Partidas activas indexadas por ID.
    /// </summary>
    private readonly ConcurrentDictionary<string, GameContext> _games = new();

    /// <summary>
    /// Mapeo ConnectionId → PlayerId para identificar jugadores por conexión.
    /// </summary>
    private readonly ConcurrentDictionary<string, string> _connections = new();

    /// <summary>
    /// Mapeo PlayerId → PlayerInfo para búsqueda rápida por nombre.
    /// </summary>
    private readonly ConcurrentDictionary<string, PlayerInfo> _players = new();

    private readonly ILogger<GameManager> _logger;

    public GameManager(ILogger<GameManager> logger)
    {
        _logger = logger;
    }
}
```

### 2.3 GameContext: Envoltorio de Partida

Cada partida se envuelve en un `GameContext` que añade mecanismos de concurrencia y metadatos operacionales:

```csharp
/// <summary>
/// Contexto operacional de una partida.
/// Envuelve la entidad Game con mecanismos de concurrencia y timers.
/// </summary>
public class GameContext : IDisposable
{
    /// <summary>
    /// La entidad de dominio con todo el estado del juego.
    /// </summary>
    public Game Game { get; }

    /// <summary>
    /// Semáforo para garantizar acceso exclusivo al estado de la partida.
    /// </summary>
    public SemaphoreSlim Lock { get; } = new(1, 1);

    /// <summary>
    /// Timers de desconexión por jugador.
    /// Key: PlayerId, Value: Timer que se activa al desconectar.
    /// </summary>
    public ConcurrentDictionary<string, Timer> DisconnectionTimers { get; } = new();

    public GameContext(Game game)
    {
        Game = game;
    }

    public void Dispose()
    {
        Lock.Dispose();
        foreach (var timer in DisconnectionTimers.Values)
        {
            timer.Dispose();
        }
        DisconnectionTimers.Clear();
    }
}
```

### 2.4 PlayerInfo: Registro de Jugador Conectado

```csharp
/// <summary>
/// Información básica de un jugador conectado al sistema.
/// </summary>
public class PlayerInfo
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string? CurrentGameId { get; set; }
    public string? ConnectionId { get; set; }
    public DateTime ConnectedAt { get; set; }
}
```

---

## 3. Operaciones del GameManager

### 3.1 Crear Partida

```csharp
public GameSummary CreateGame(string name, string creatorPlayerId,
    string creatorPlayerName, GameSettings settings)
{
    var game = new Game
    {
        Name = name.Trim(),
        Status = GameStatus.WaitingForPlayers,
        Settings = settings,
        CreatorPlayerId = creatorPlayerId,
        CreatedAt = DateTime.UtcNow
    };

    var context = new GameContext(game);
    
    if (!_games.TryAdd(game.Id, context))
    {
        context.Dispose();
        throw new InvalidOperationException("Failed to create game. ID collision.");
    }

    _logger.LogInformation("Game '{GameName}' ({GameId}) created by {Creator}",
        name, game.Id, creatorPlayerName);

    return new GameSummary
    {
        Id = game.Id,
        Name = game.Name,
        Status = game.Status,
        CurrentPlayers = 0,
        MaxPlayers = settings.MaxPlayers,
        CreatorName = creatorPlayerName,
        CreatedAt = game.CreatedAt
    };
}
```

### 3.2 Añadir Jugador a Partida

```csharp
public class JoinResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public PlayerColor AssignedColor { get; set; }
}

public JoinResult AddPlayer(string gameId, string playerId,
    string playerName, string connectionId)
{
    if (!_games.TryGetValue(gameId, out var context))
        return new JoinResult { Success = false, ErrorMessage = "La partida no existe." };

    var game = context.Game;

    if (game.Status != GameStatus.WaitingForPlayers)
        return new JoinResult { Success = false, ErrorMessage = "La partida ya ha comenzado." };

    if (game.Players.Count >= game.Settings.MaxPlayers)
        return new JoinResult { Success = false, ErrorMessage = "La partida está llena." };

    if (game.Players.Any(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase)))
        return new JoinResult { Success = false, ErrorMessage = "Ya hay un jugador con ese nombre." };

    // Asignar el primer color disponible
    var usedColors = game.Players.Select(p => p.Color).ToHashSet();
    var availableColor = Enum.GetValues<PlayerColor>()
        .Where(c => c != PlayerColor.Neutral && !usedColors.Contains(c))
        .First();

    var player = new Player
    {
        Id = playerId,
        Name = playerName,
        Color = availableColor,
        ConnectionId = connectionId,
        IsConnected = true
    };

    game.Players.Add(player);

    // Actualizar registro global
    _players.AddOrUpdate(playerId,
        new PlayerInfo
        {
            PlayerId = playerId,
            PlayerName = playerName,
            CurrentGameId = gameId,
            ConnectionId = connectionId,
            ConnectedAt = DateTime.UtcNow
        },
        (_, existing) =>
        {
            existing.CurrentGameId = gameId;
            existing.ConnectionId = connectionId;
            return existing;
        });

    _logger.LogInformation("Player {PlayerName} ({PlayerId}) joined game {GameId}",
        playerName, playerId, gameId);

    return new JoinResult { Success = true, AssignedColor = availableColor };
}
```

### 3.3 Obtener Partidas Disponibles

```csharp
public List<GameSummary> GetAvailableGames()
{
    return _games.Values
        .Where(ctx => ctx.Game.Status == GameStatus.WaitingForPlayers)
        .Select(ctx => new GameSummary
        {
            Id = ctx.Game.Id,
            Name = ctx.Game.Name,
            Status = ctx.Game.Status,
            CurrentPlayers = ctx.Game.Players.Count,
            MaxPlayers = ctx.Game.Settings.MaxPlayers,
            CreatorName = ctx.Game.Players
                .FirstOrDefault(p => p.Id == ctx.Game.CreatorPlayerId)?.Name ?? "?",
            CreatedAt = ctx.Game.CreatedAt
        })
        .OrderByDescending(g => g.CreatedAt)
        .ToList();
}
```

### 3.4 Obtener Estado para SignalR (DTO)

```csharp
public GameStateDto GetGameState(string gameId)
{
    if (!_games.TryGetValue(gameId, out var context))
        throw new KeyNotFoundException($"Game {gameId} not found.");

    var game = context.Game;

    return new GameStateDto
    {
        GameId = game.Id,
        GameName = game.Name,
        Status = game.Status,
        Phase = game.Phase,
        CurrentPlayerId = game.GetCurrentPlayer()?.Id ?? string.Empty,
        CurrentPlayerName = game.GetCurrentPlayer()?.Name ?? string.Empty,
        TurnNumber = game.TurnNumber,
        TradeCount = game.TradeCount,
        RemainingReinforcements = game.RemainingReinforcements,
        Players = game.Players.Select(p => new PlayerDto
        {
            PlayerId = p.Id,
            PlayerName = p.Name,
            Color = p.Color,
            TerritoryCount = p.GetOwnedTerritories(game).Count(),
            TotalArmies = p.GetTotalArmies(game),
            CardCount = p.Cards.Count,
            IsConnected = p.IsConnected,
            IsEliminated = p.IsEliminated
        }).ToList(),
        Territories = game.Territories.Values.Select(t => new TerritoryDto
        {
            TerritoryId = t.Name.ToString(),
            TerritoryName = t.Name.ToString(),
            ContinentId = t.Continent.ToString(),
            OwnerId = t.OwnerId,
            OwnerName = game.GetPlayerById(t.OwnerId)?.Name ?? "?",
            OwnerColor = game.GetPlayerById(t.OwnerId)?.Color ?? PlayerColor.Neutral,
            Armies = t.Armies
        }).ToList(),
        RecentEvents = game.EventLog
            .TakeLast(20)
            .Select(e => new GameEventDto
            {
                Type = e.Type.ToString(),
                Message = e.Message,
                PlayerName = e.PlayerName,
                Timestamp = e.Timestamp
            }).ToList(),
        StartedAt = game.StartedAt ?? game.CreatedAt
    };
}
```

---

## 4. Concurrencia y Thread-Safety

### 4.1 El Problema

En Blazor Server, múltiples circuitos (jugadores) pueden invocar métodos del `GameHub` simultáneamente. Todos estos métodos acceden al **mismo objeto `Game`** a través del `GameManager` Singleton:

```
  Jugador 1                    Jugador 2                    Jugador 3
  (Circuito 1)                 (Circuito 2)                 (Circuito 3)
       │                            │                            │
       │── Attack(...) ────────────▶│                            │
       │                            │── PlaceReinforcements() ──▶│
       │                            │                            │── SendChat() ──▶
       │                            │                            │
       ▼                            ▼                            ▼
  ┌──────────────────────────────────────────────────────────────────┐
  │                         GameManager                              │
  │                                                                  │
  │  _games["abc-123"] → GameContext                                 │
  │                       ├── Game  ◀──── ¡ACCESO CONCURRENTE!      │
  │                       └── Lock (SemaphoreSlim)                   │
  └──────────────────────────────────────────────────────────────────┘
```

Sin protección, dos jugadores podrían modificar el mismo `Game` simultáneamente, causando estados inconsistentes.

### 4.2 Estrategia de Concurrencia

| Capa | Mecanismo | Protege |
|------|-----------|---------|
| **Colecciones del GameManager** | `ConcurrentDictionary` | Acceso a `_games`, `_connections`, `_players` |
| **Estado de cada partida** | `SemaphoreSlim` (1 hilo) | Acceso exclusivo al objeto `Game` dentro de un `GameContext` |
| **Operaciones del motor** | `ExecuteWithLock<T>()` | Garantiza que solo una acción modifica la partida a la vez |

### 4.3 Patrón ExecuteWithLock

Todos los métodos del `GameHub` que modifican el estado de una partida deben usar este patrón:

```csharp
public async Task<T> ExecuteWithLock<T>(string gameId, Func<Game, T> action)
{
    if (!_games.TryGetValue(gameId, out var context))
        throw new KeyNotFoundException($"Game {gameId} not found.");

    await context.Lock.WaitAsync();
    try
    {
        return action(context.Game);
    }
    finally
    {
        context.Lock.Release();
    }
}

public async Task ExecuteWithLock(string gameId, Func<Game, Task> action)
{
    if (!_games.TryGetValue(gameId, out var context))
        throw new KeyNotFoundException($"Game {gameId} not found.");

    await context.Lock.WaitAsync();
    try
    {
        await action(context.Game);
    }
    finally
    {
        context.Lock.Release();
    }
}
```

**Uso en el GameHub:**

```csharp
// En GameHub.cs
public async Task Attack(string gameId, string playerId,
    string fromTerritoryId, string toTerritoryId, int attackDiceCount)
{
    var from = Enum.Parse<TerritoryName>(fromTerritoryId);
    var to = Enum.Parse<TerritoryName>(toTerritoryId);

    var result = await _gameManager.ExecuteWithLock(gameId, game =>
    {
        return _gameEngine.Attack(game, playerId, from, to, attackDiceCount);
    });

    if (!result.Success)
    {
        await Clients.Caller.SendAsync("ActionError", new ActionErrorDto
        {
            Message = result.ErrorMessage!,
            ActionAttempted = "Attack"
        });
        return;
    }

    // Notificar a todos (fuera del lock, ya que no modifica estado)
    await Clients.Group($"game-{gameId}").SendAsync("DiceRolled", result.AttackResult);
    
    var gameState = _gameManager.GetGameState(gameId);
    await Clients.Group($"game-{gameId}").SendAsync("GameStateUpdated", gameState);
}
```

### 4.4 Diagrama de Secuencia con Lock

```
  Jugador 1 (Attack)              GameManager                  Jugador 2 (Fortify)
  ──────────────────              ───────────                  ───────────────────
       │                              │                              │
  1. ──── ExecuteWithLock ──────────▶│                              │
       │                              │                              │
       │                    Lock.WaitAsync()                         │
       │                    ┌──── LOCK ADQUIRIDO ────┐              │
       │                    │                        │              │
       │              2. ───┤ Execute Attack()       │              │
       │                    │ (modifica Game)        │              │
       │                    │                        │   3. ──── ExecuteWithLock ──▶
       │                    │                        │              │
       │                    │                        │     Lock.WaitAsync()
       │                    │                        │     ┌── ESPERANDO... ──┐
       │                    │                        │     │                  │
       │              4. ◀──┤ return result          │     │                  │
       │                    │                        │     │                  │
       │                    └──── Lock.Release() ────┘     │                  │
       │                              │                    │                  │
       │                              │              ┌──── LOCK ADQUIRIDO ───┘
       │                              │              │
       │                        5. ───┤ Execute Fortify()
       │                              │ (modifica Game)
       │                              │              │
       │                        6. ◀──┤ return result
       │                              │              │
       │                              │              └── Lock.Release()
       │                              │                              │
```

### 4.5 ¿Qué No Necesita Lock?

| Operación | ¿Necesita lock? | Motivo |
|-----------|:---------------:|--------|
| Leer lista de partidas disponibles | ❌ | `ConcurrentDictionary` es thread-safe para lectura |
| Verificar si un nombre está en uso | ❌ | Lectura de `ConcurrentDictionary` |
| Enviar mensaje de chat | ❌ | No modifica el estado del juego (solo añade al log) |
| Atacar, fortificar, reforzar | ✅ | Modifica `Game.Territories`, `Game.Players`, etc. |
| Canjear cartas | ✅ | Modifica `Player.Cards`, `Game.TradeCount`, `Game.RemainingReinforcements` |
| Crear partida | ❌ | `TryAdd` en `ConcurrentDictionary` es atómico |
| Unirse a partida | ✅ | Modifica `Game.Players` |

---

## 5. Ciclo de Vida del Estado

### 5.1 Diagrama Completo

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                        CICLO DE VIDA DEL ESTADO                              │
│                                                                              │
│  ┌───────────────────┐                                                       │
│  │  1. CREACIÓN      │  Jugador crea partida desde el Lobby                  │
│  │                   │                                                       │
│  │  GameManager      │  → new Game()                                         │
│  │  .CreateGame()    │  → new GameContext(game)                              │
│  │                   │  → _games.TryAdd(id, context)                         │
│  └─────────┬─────────┘                                                       │
│            │                                                                 │
│            ▼                                                                 │
│  ┌───────────────────┐                                                       │
│  │  2. LOBBY         │  Status: WaitingForPlayers                            │
│  │                   │                                                       │
│  │  Jugadores        │  → AddPlayer() × N                                    │
│  │  se unen          │  → Colores asignados automáticamente                  │
│  │  (2–6)            │  → Visible en GetAvailableGames()                     │
│  └─────────┬─────────┘                                                       │
│            │  Creador pulsa "Iniciar" (min. 2 jugadores)                     │
│            ▼                                                                 │
│  ┌───────────────────┐                                                       │
│  │  3. SETUP         │  Status: Playing, Phase: Setup                        │
│  │                   │                                                       │
│  │  GameEngine       │  → InitializeGame(): crear territorios, mazo          │
│  │  .InitializeGame()│  → DistributeTerritoriesRandomly()                    │
│  │                   │  → PlaceInitialArmies() por turnos                    │
│  │                   │  → StartPlaying(): transición a juego activo          │
│  └─────────┬─────────┘                                                       │
│            │                                                                 │
│            ▼                                                                 │
│  ┌───────────────────┐                                                       │
│  │  4. EN CURSO      │  Status: Playing, Phase: Reinforcement/Attack/Fort.   │
│  │                   │                                                       │
│  │  Ciclo de turnos  │  → CalculateReinforcements()                          │
│  │  (el grueso de    │  → PlaceReinforcements() / TradeCards()               │
│  │   la partida)     │  → Attack() / MoveArmiesAfterConquest()               │
│  │                   │  → Fortify() / SkipFortification()                    │
│  │                   │  → EndTurn() → siguiente jugador                      │
│  │                   │                                                       │
│  │  Estado se        │  Tras cada acción:                                    │
│  │  sincroniza       │  → GetGameState() → GameStateDto                      │
│  │  constantemente   │  → SignalR → "GameStateUpdated" → todos los clientes  │
│  └─────────┬─────────┘                                                       │
│            │  Un jugador controla los 42 territorios                         │
│            │  (o todos abandonan)                                            │
│            ▼                                                                 │
│  ┌───────────────────┐                                                       │
│  │  5. FINALIZACIÓN  │  Status: Finished                                     │
│  │                   │                                                       │
│  │  game.FinishedAt  │  → SignalR: "GameOver" con estadísticas               │
│  │  = DateTime.UtcNow│  → Los jugadores ven pantalla de victoria/derrota     │
│  │                   │  → La partida sigue en memoria (consultable)          │
│  └─────────┬─────────┘                                                       │
│            │  Todos los jugadores abandonan / timeout                        │
│            ▼                                                                 │
│  ┌───────────────────┐                                                       │
│  │  6. LIMPIEZA      │                                                       │
│  │                   │  → _games.TryRemove(gameId)                           │
│  │  RemoveGame()     │  → context.Dispose() (libera Lock y Timers)           │
│  │                   │  → GC recoge la memoria                               │
│  └───────────────────┘                                                       │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 5.2 Limpieza de Partidas

Las partidas finalizadas o abandonadas deben limpiarse para liberar memoria. Se implementa un mecanismo de limpieza periódica:

```csharp
public class GameCleanupService : BackgroundService
{
    private readonly IGameManager _gameManager;
    private readonly ILogger<GameCleanupService> _logger;

    // Limpiar cada 5 minutos
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);

    // Partidas finalizadas se eliminan tras 10 minutos
    private static readonly TimeSpan FinishedGameRetention = TimeSpan.FromMinutes(10);

    // Partidas en lobby sin jugadores se eliminan tras 15 minutos
    private static readonly TimeSpan EmptyGameRetention = TimeSpan.FromMinutes(15);

    public GameCleanupService(IGameManager gameManager, 
        ILogger<GameCleanupService> logger)
    {
        _gameManager = gameManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CleanupInterval, stoppingToken);
            CleanupStaleGames();
        }
    }

    private void CleanupStaleGames()
    {
        var now = DateTime.UtcNow;
        var games = _gameManager.GetAllGames(); // método interno

        foreach (var game in games)
        {
            bool shouldRemove = false;
            string reason = string.Empty;

            // Partida finalizada hace más de 10 minutos
            if (game.Status == GameStatus.Finished &&
                game.FinishedAt.HasValue &&
                now - game.FinishedAt.Value > FinishedGameRetention)
            {
                shouldRemove = true;
                reason = "finished and expired";
            }

            // Partida en lobby sin jugadores
            if (game.Status == GameStatus.WaitingForPlayers &&
                game.Players.Count == 0 &&
                now - game.CreatedAt > EmptyGameRetention)
            {
                shouldRemove = true;
                reason = "empty lobby expired";
            }

            // Partida en curso sin jugadores conectados
            if (game.Status == GameStatus.Playing &&
                game.Players.All(p => !p.IsConnected) &&
                game.Players.All(p => p.DisconnectedAt.HasValue &&
                    now - p.DisconnectedAt.Value > TimeSpan.FromMinutes(5)))
            {
                shouldRemove = true;
                reason = "all players disconnected";
            }

            if (shouldRemove)
            {
                _gameManager.RemoveGame(game.Id);
                _logger.LogInformation(
                    "Cleaned up game '{GameName}' ({GameId}): {Reason}",
                    game.Name, game.Id, reason);
            }
        }
    }
}
```

**Registro en `Program.cs`:**

```csharp
builder.Services.AddHostedService<GameCleanupService>();
```

---

## 6. Estado por Jugador: PlayerSessionService

Como se documentó en detalle en [03 — Identificación de Jugadores](./03_Identificacion_Jugadores.md), cada jugador tiene un servicio **Scoped** que vive durante su circuito Blazor:

### 6.1 Relación entre PlayerSessionService y GameManager

```
┌──────────────────────────────────────────────────────────────────┐
│                         SERVIDOR                                  │
│                                                                   │
│  GameManager (Singleton)                                          │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │  Game "abc"                                               │    │
│  │  ├── Player { Id:"p1", Name:"Carlos", Color:Red }        │    │
│  │  ├── Player { Id:"p2", Name:"Ana", Color:Blue }          │    │
│  │  └── Player { Id:"p3", Name:"Luis", Color:Green }        │    │
│  └──────────────────────────────────────────────────────────┘    │
│        ▲               ▲               ▲                         │
│        │               │               │                         │
│   referencia      referencia      referencia                     │
│   por Id          por Id          por Id                         │
│        │               │               │                         │
│  ┌─────┴─────┐   ┌─────┴─────┐   ┌─────┴─────┐                │
│  │PlayerSess.│   │PlayerSess.│   │PlayerSess.│                │
│  │(Scoped)   │   │(Scoped)   │   │(Scoped)   │                │
│  │           │   │           │   │           │                │
│  │Id: "p1"   │   │Id: "p2"   │   │Id: "p3"   │                │
│  │Name:Carlos│   │Name: Ana  │   │Name: Luis │                │
│  │Game: "abc"│   │Game: "abc"│   │Game: "abc"│                │
│  └───────────┘   └───────────┘   └───────────┘                │
│   Circuito 1      Circuito 2      Circuito 3                   │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

**Flujo de datos:**

| Dato | Dónde se lee | Dónde se escribe |
|------|:------------:|:----------------:|
| Nombre del jugador | `PlayerSessionService` (local al circuito) | `PlayerSessionService.SetPlayer()` |
| Estado de la partida | `GameManager.GetGameState()` (Singleton) | `GameEngine` vía `ExecuteWithLock()` |
| Cartas del jugador | `GameManager` → `Player.Cards` | `GameEngine.TradeCards()` / `DrawCard()` |
| Color del jugador | `PlayerSessionService.AssignedColor` | `GameManager.AddPlayer()` |
| ¿Es mi turno? | `GameStateDto.CurrentPlayerId == PlayerSession.PlayerId` | — |

---

## 7. Sincronización del Estado con los Clientes

### 7.1 Patrón: Modificar → Serializar → Difundir

Después de cada acción que modifica el estado, se sigue este patrón:

```
  Acción del jugador
       │
       ▼
  ┌──────────────────┐
  │ ExecuteWithLock() │
  │ GameEngine.X()    │───── Modifica Game en memoria
  └────────┬─────────┘
           │
           ▼
  ┌──────────────────┐
  │ GetGameState()    │───── Serializa Game → GameStateDto
  └────────┬─────────┘
           │
           ▼
  ┌──────────────────┐
  │ SignalR Broadcast │───── Envía a todos los jugadores del grupo
  │ "GameStateUpdated"│
  └────────┬─────────┘
           │
           ▼
  ┌──────────────────┐
  │ Cada cliente      │───── Actualiza estado local
  │ StateHasChanged() │───── Re-renderiza componentes Blazor
  └──────────────────┘
```

### 7.2 ¿Estado Completo o Diffs?

| Enfoque | Ventajas | Desventajas |
|---------|----------|-------------|
| **Estado completo** (elegido) | Simple, sin desincronización, fácil de depurar | Más datos por mensaje (~2-5 KB) |
| **Diffs incrementales** | Menos datos por mensaje | Complejo, riesgo de desincronización, necesita reconciliación |

**Decisión:** Se envía el **estado completo** (`GameStateDto`) tras cada acción. Con solo 2–6 jugadores en LAN, el tamaño del mensaje (~2-5 KB JSON) es insignificante.

### 7.3 Tamaño Estimado del GameStateDto

| Componente | Tamaño aprox. |
|------------|:-------------:|
| Metadatos de partida | ~200 bytes |
| 6 jugadores × ~100 bytes | ~600 bytes |
| 42 territorios × ~80 bytes | ~3.360 bytes |
| 20 eventos recientes × ~100 bytes | ~2.000 bytes |
| **Total estimado** | **~6 KB** |

En una red local con ~1 ms de latencia, transmitir 6 KB es imperceptible.

---

## 8. Recuperación ante Desconexión

### 8.1 Escenarios de Desconexión

| Escenario | Causa | Impacto |
|-----------|-------|---------|
| WiFi se cae momentáneamente | Red inestable | El circuito Blazor intenta reconectar; el Hub se reconecta automáticamente |
| Jugador cierra pestaña accidentalmente | Acción del usuario | Circuito destruido; necesita reabrir y re-identificarse |
| PC del jugador se cuelga | Hardware/SO | Circuito perdido; el servidor detecta desconexión por timeout de WebSocket |
| Servidor se reinicia | Mantenimiento | Todo el estado se pierde; todas las partidas terminan |

### 8.2 Flujo de Detección y Manejo

```
  Jugador pierde conexión
       │
       ▼
  ┌───────────────────────────────────┐
  │  GameHub.OnDisconnectedAsync()    │
  │                                   │
  │  1. Obtener playerId por          │
  │     connectionId                  │
  │  2. Obtener gameId del jugador    │
  └──────────┬────────────────────────┘
             │
             ▼
  ┌───────────────────────────────────┐
  │  GameManager                      │
  │  .MarkPlayerDisconnected()        │
  │                                   │
  │  1. Player.IsConnected = false    │
  │  2. Player.DisconnectedAt = now   │
  │  3. Iniciar timer de 60s          │
  │     (salto de turno)              │
  │  4. Iniciar timer de 5min         │
  │     (abandono)                    │
  └──────────┬────────────────────────┘
             │
             ▼
  ┌───────────────────────────────────┐
  │  Notificar al grupo               │
  │  "PlayerDisconnected"             │
  │  { PlayerId, PlayerName }         │
  └───────────────────────────────────┘
             │
      ┌──────┴───────────┐
      │                  │
  Reconecta           No reconecta
  en < 60s            en 60s
      │                  │
      ▼                  ▼
  ┌──────────┐   ┌──────────────────────┐
  │ Rejoin   │   │ Timer de 60s expira  │
  │ (ver 8.3)│   │                      │
  │          │   │ Si es su turno:      │
  └──────────┘   │ → Saltar turno       │
                 │ → AdvanceTurn()      │
                 │ → Notificar grupo    │
                 └──────────┬───────────┘
                            │
                    No reconecta en 5 min
                            │
                            ▼
                 ┌──────────────────────┐
                 │ Timer de 5 min expira│
                 │                      │
                 │ → Marcar jugador     │
                 │   como abandonado    │
                 │ → Territorios pasan  │
                 │   a neutral          │
                 │ → IsEliminated=true  │
                 │ → Notificar grupo    │
                 └──────────────────────┘
```

### 8.3 Implementación de Desconexión

```csharp
public void MarkPlayerDisconnected(string gameId, string playerId)
{
    if (!_games.TryGetValue(gameId, out var context)) return;

    var player = context.Game.GetPlayerById(playerId);
    if (player == null) return;

    player.IsConnected = false;
    player.DisconnectedAt = DateTime.UtcNow;

    // Timer para saltar turno (60 segundos)
    if (context.Game.GetCurrentPlayer()?.Id == playerId)
    {
        var skipTimer = new Timer(_ =>
        {
            _ = HandleTurnSkipAsync(gameId, playerId);
        }, null, TimeSpan.FromSeconds(60), Timeout.InfiniteTimeSpan);

        context.DisconnectionTimers.AddOrUpdate(
            $"{playerId}_skip", skipTimer, (_, old) => { old.Dispose(); return skipTimer; });
    }

    // Timer para abandono (5 minutos)
    var abandonTimer = new Timer(_ =>
    {
        _ = HandleAbandonAsync(gameId, playerId);
    }, null, TimeSpan.FromMinutes(5), Timeout.InfiniteTimeSpan);

    context.DisconnectionTimers.AddOrUpdate(
        $"{playerId}_abandon", abandonTimer, (_, old) => { old.Dispose(); return abandonTimer; });

    _logger.LogWarning("Player {PlayerName} ({PlayerId}) disconnected from game {GameId}",
        player.Name, playerId, gameId);
}
```

### 8.4 Reconexión

```csharp
public void MarkPlayerReconnected(string gameId, string playerId, string connectionId)
{
    if (!_games.TryGetValue(gameId, out var context)) return;

    var player = context.Game.GetPlayerById(playerId);
    if (player == null) return;

    player.IsConnected = true;
    player.DisconnectedAt = null;
    player.ConnectionId = connectionId;

    // Cancelar timers de desconexión
    if (context.DisconnectionTimers.TryRemove($"{playerId}_skip", out var skipTimer))
        skipTimer.Dispose();

    if (context.DisconnectionTimers.TryRemove($"{playerId}_abandon", out var abandonTimer))
        abandonTimer.Dispose();

    // Actualizar registro global
    _connections.AddOrUpdate(connectionId, playerId, (_, _) => playerId);

    _logger.LogInformation("Player {PlayerName} ({PlayerId}) reconnected to game {GameId}",
        player.Name, playerId, gameId);
}
```

### 8.5 Información de Reconexión

Cuando un jugador recarga la página y se re-identifica con el mismo nombre, el sistema detecta si estaba en una partida:

```csharp
public class ReconnectionInfo
{
    public string GameId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public PlayerColor Color { get; set; }
}

public ReconnectionInfo? GetReconnectionInfo(string playerName)
{
    // Buscar si existe un jugador desconectado con ese nombre en alguna partida
    foreach (var context in _games.Values)
    {
        var player = context.Game.Players.FirstOrDefault(p =>
            p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase) &&
            !p.IsConnected &&
            !p.IsEliminated);

        if (player != null)
        {
            return new ReconnectionInfo
            {
                GameId = context.Game.Id,
                PlayerId = player.Id,
                GameName = context.Game.Name,
                Color = player.Color
            };
        }
    }

    return null;
}
```

**Flujo en la UI:**

```
  Jugador recarga la página
       │
       ▼
  Home.razor: introduce nombre "Carlos"
       │
       ▼
  GameManager.GetReconnectionInfo("Carlos")
       │
  ┌────┴────────┐
  │             │
  null       ReconnectionInfo
  │             │
  ▼             ▼
  Ir al       Mostrar diálogo:
  Lobby       "Estabas en la partida
  normal      'Los viernes'. ¿Reconectar?"
                  │
           ┌──────┴──────┐
           │             │
          Sí            No
           │             │
           ▼             ▼
        RejoinGame    Ir al Lobby
        (volver a     (la partida sigue
         la partida)   sin el jugador)
```

---

## 9. Diagrama de Flujo del Estado Completo

```
┌────────────────────────────────────────────────────────────────────────┐
│                 FLUJO DE ESTADO: ACCIÓN → RESULTADO                    │
│                                                                        │
│  1. CLIENTE                                                            │
│     Jugador hace click en "Atacar Alaska → Kamchatka"                  │
│     │                                                                  │
│     ▼                                                                  │
│  2. COMPONENTE BLAZOR (Game.razor)                                     │
│     hubConnection.SendAsync("Attack", gameId, playerId, ...)           │
│     │                                                                  │
│     ▼                                                                  │
│  3. SIGNALR (GameHub)                                                  │
│     Recibe la invocación, obtiene game y player                        │
│     │                                                                  │
│     ▼                                                                  │
│  4. GAME MANAGER (Singleton)                                           │
│     ExecuteWithLock(gameId, game => ...)                                │
│     → Adquiere SemaphoreSlim                                           │
│     │                                                                  │
│     ▼                                                                  │
│  5. GAME ENGINE (Transient)                                            │
│     Attack(game, playerId, from, to, dice)                             │
│     → Valida precondiciones                                            │
│     → DiceService.Roll()                                               │
│     → ResolveCombat()                                                  │
│     → Modifica game.Territories[].Armies                               │
│     → Retorna AttackGameResult                                         │
│     │                                                                  │
│     ▼                                                                  │
│  6. GAME MANAGER                                                       │
│     → Libera SemaphoreSlim                                             │
│     │                                                                  │
│     ▼                                                                  │
│  7. SIGNALR (GameHub)                                                  │
│     → GetGameState(gameId) → GameStateDto                              │
│     → Clients.Group("game-xxx").SendAsync("DiceRolled", result)        │
│     → Clients.Group("game-xxx").SendAsync("GameStateUpdated", state)   │
│     │                                                                  │
│     ▼                                                                  │
│  8. TODOS LOS CLIENTES                                                 │
│     → Handler recibe GameStateDto                                      │
│     → Actualiza estado local                                           │
│     → InvokeAsync(StateHasChanged)                                     │
│     → Blazor re-renderiza los componentes afectados                    │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 10. Resumen de Servicios y Ciclos de Vida

| Servicio | Ciclo de Vida | Estado que gestiona | Thread-Safety |
|----------|:------------:|---------------------|:-------------:|
| `GameManager` | **Singleton** | Todas las partidas activas, conexiones, jugadores | `ConcurrentDictionary` + `SemaphoreSlim` por partida |
| `GameEngine` | **Transient** | Ninguno propio (recibe `Game` como parámetro) | No necesita (acceso protegido por `ExecuteWithLock`) |
| `DiceService` | **Transient** | Ninguno | `Random.Shared` es thread-safe |
| `CardService` | **Transient** | Ninguno | No necesita |
| `MapService` | **Singleton** | Datos estáticos del mapa (inmutables) | Inmutable → inherentemente thread-safe |
| `PlayerSessionService` | **Scoped** | Nombre, ID, color del jugador del circuito | Un solo circuito → no hay concurrencia |
| `GameCleanupService` | **Hosted** | Ninguno (limpia partidas vía `GameManager`) | Acceso vía `GameManager` (thread-safe) |

---

> **Siguiente documento:** [08 — Diseño de la Interfaz de Usuario](./08_Diseno_UI.md)
