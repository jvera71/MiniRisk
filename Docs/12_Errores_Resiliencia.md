# 12 — Manejo de Errores y Resiliencia

> **Documento:** 12 de 14  
> **Versión:** 1.0  
> **Última actualización:** 22 de febrero de 2026  
> **Volver al índice:** [00_Indice.md](./00_Indice.md)  
> **Documento anterior:** [11 — Servicios e Inyección de Dependencias](./11_Servicios_DI.md)

---

## 1. Estrategia General

MiniRisk maneja tres tipos de errores, cada uno con una estrategia diferente:

| Tipo de error | Ejemplo | Estrategia |
|:-------------|---------|:-----------|
| **Errores de lógica de juego** | Atacar un territorio propio, colocar más refuerzos de los disponibles | Retornar `GameResult.Fail(...)` al cliente. No es una excepción, es un flujo esperado. |
| **Errores de infraestructura** | Desconexión de SignalR, timeout de WebSocket | Reconexión automática, timers de tolerancia, degradación controlada. |
| **Errores inesperados** | `NullReferenceException`, bug no previsto | `ErrorBoundary` en Blazor, logging, notificación genérica al usuario. |

### 1.1 Principios

| Principio | Aplicación |
|-----------|-----------|
| **Fallar con gracia** | El usuario siempre recibe feedback claro. Nunca una pantalla en blanco o un error críptico. |
| **No perder estado** | Un error en una acción no corrompe el estado de la partida. Las operaciones son atómicas (dentro del `ExecuteWithLock`). |
| **Logging completo** | Todo error se registra con contexto suficiente para diagnosticarlo. |
| **Separar validación de excepción** | Las reglas de juego inválidas retornan `GameResult.Fail()`. Las excepciones se reservan para errores reales del sistema. |
| **Resilencia ante desconexión** | La pérdida de conexión es un evento esperado en red local. El sistema tolera desconexiones temporales. |

---

## 2. Errores del Motor de Juego (GameResult)

### 2.1 Patrón Result

El motor del juego **nunca lanza excepciones** para acciones inválidas. En su lugar, retorna un objeto `GameResult`:

```csharp
public class GameResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public static GameResult Ok() => new() { Success = true };
    public static GameResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}
```

**¿Por qué no excepciones?**

| Aspecto | Excepciones | GameResult |
|---------|:-----------:|:----------:|
| Performance | Costosas (stack trace) | Barato (un objeto) |
| Semántica | "Algo fue mal inesperadamente" | "La acción no es válida según las reglas" |
| Flujo de control | Interrumpen el flujo | Flujo lineal, if/else |
| Frecuencia | Raro | Frecuente (el jugador puede intentar acciones inválidas a menudo) |

### 2.2 Categorías de Error del Motor

| Categoría | Ejemplo | Código de error |
|-----------|---------|:---------------:|
| **No es tu turno** | Jugador intenta actuar fuera de su turno | `NOT_YOUR_TURN` |
| **Fase incorrecta** | Atacar durante fase de refuerzo | `WRONG_PHASE` |
| **Territorio inválido** | Atacar territorio propio, fortificar territorio enemigo | `INVALID_TERRITORY` |
| **Cantidad inválida** | Colocar más refuerzos de los disponibles | `INVALID_AMOUNT` |
| **No adyacente** | Atacar un territorio no adyacente | `NOT_ADJACENT` |
| **Sin camino** | Fortificar sin camino de territorios propios | `NO_PATH` |
| **Canje inválido** | Combinación de cartas incorrecta | `INVALID_TRADE` |
| **Ejércitos insuficientes** | Atacar con 1 ejército, dejar territorio a 0 | `INSUFFICIENT_ARMIES` |

### 2.3 Flujo de Error en el GameHub

```
  Cliente                  GameHub                    GameEngine
  ──────                   ───────                    ──────────
    │                         │                           │
    │── Attack(args) ────────▶│                           │
    │                         │                           │
    │                         │── ExecuteWithLock ────────▶│
    │                         │                           │
    │                         │   Attack(game, ...)       │
    │                         │                           │
    │                         │◀── GameResult.Fail ───────│
    │                         │    "No es tu turno."      │
    │                         │                           │
    │                         │ (NO se modifica el estado)│
    │                         │                           │
    │◀── ActionError ─────────│                           │
    │    {                    │                           │
    │      Message: "No es   │                           │
    │        tu turno.",     │                           │
    │      Action: "Attack"  │                           │
    │    }                    │                           │
    │                         │                           │
    │ (UI muestra toast       │                           │
    │  de error rojo)         │                           │
```

```csharp
// En GameHub.cs
public async Task Attack(string gameId, string playerId,
    string from, string to, int diceCount)
{
    try
    {
        var result = await _gameManager.ExecuteWithLock(gameId, game =>
        {
            var fromTerritory = Enum.Parse<TerritoryName>(from);
            var toTerritory = Enum.Parse<TerritoryName>(to);
            return _gameEngine.Attack(game, playerId, fromTerritory, toTerritory, diceCount);
        });

        if (!result.Success)
        {
            // Error de validación → notificar solo al emisor
            await Clients.Caller.SendAsync("ActionError", new ActionErrorDto
            {
                Message = result.ErrorMessage!,
                ActionAttempted = "Attack"
            });
            return;
        }

        // Éxito → notificar a todo el grupo
        var state = _gameManager.GetGameState(gameId);
        await Clients.Group($"game-{gameId}").SendAsync("GameStateUpdated", state);

        if (result is AttackGameResult attackResult)
        {
            await Clients.Group($"game-{gameId}").SendAsync("DiceRolled",
                attackResult.AttackResult);

            if (attackResult.PlayerEliminated)
            {
                var eliminatedName = state.Players
                    .FirstOrDefault(p => p.PlayerId == attackResult.EliminatedPlayerId)
                    ?.PlayerName ?? "?";
                await Clients.Group($"game-{gameId}").SendAsync(
                    "PlayerEliminated", eliminatedName);
            }

            if (attackResult.GameOver)
            {
                var winnerName = _gameEngine.GetWinner(
                    _gameManager.GetGame(gameId)!)?.Name ?? "?";
                await Clients.Group($"game-{gameId}").SendAsync(
                    "GameOver", winnerName);
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in Attack: GameId={GameId}, PlayerId={PlayerId}",
            gameId, playerId);

        await Clients.Caller.SendAsync("ActionError", new ActionErrorDto
        {
            Message = "Ha ocurrido un error inesperado. Inténtalo de nuevo.",
            ActionAttempted = "Attack"
        });
    }
}
```

---

## 3. Errores de Infraestructura (SignalR)

### 3.1 Escenarios de Desconexión

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    ESCENARIOS DE DESCONEXIÓN                             │
│                                                                          │
│  1. WiFi inestable (pérdida momentánea)                                  │
│     ├── WebSocket se cierra                                              │
│     ├── SignalR intenta reconectar automáticamente                       │
│     └── Si reconecta en <30s → transparente para el usuario              │
│                                                                          │
│  2. Jugador cierra pestaña                                               │
│     ├── Circuito Blazor se destruye                                      │
│     ├── WebSocket se cierra                                              │
│     ├── OnDisconnectedAsync() se ejecuta                                 │
│     └── Timer de 60s (salto de turno) + 5min (abandono)                  │
│                                                                          │
│  3. PC se cuelga / apaga                                                 │
│     ├── WebSocket timeout (ClientTimeoutInterval: 60s)                   │
│     ├── Servidor detecta desconexión                                     │
│     ├── OnDisconnectedAsync() se ejecuta                                 │
│     └── Mismo manejo que escenario 2                                     │
│                                                                          │
│  4. Servidor se reinicia                                                 │
│     ├── TODO el estado se pierde (está en memoria)                       │
│     ├── TODAS las conexiones se cierran                                  │
│     ├── Los clientes ven error de conexión                               │
│     └── Deben refrescar la página y empezar de nuevo                     │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Reconexión Automática de SignalR

El `HubConnection` se configura con reconexión automática:

```csharp
// En Game.razor
_hubConnection = new HubConnectionBuilder()
    .WithUrl(Navigation.ToAbsoluteUri("/gamehub"))
    .WithAutomaticReconnect(new[] {
        TimeSpan.Zero,             // Intento inmediato
        TimeSpan.FromSeconds(2),   // 2 segundos
        TimeSpan.FromSeconds(5),   // 5 segundos
        TimeSpan.FromSeconds(10),  // 10 segundos
        TimeSpan.FromSeconds(30),  // 30 segundos
        // Después de esto, se rinde
    })
    .Build();
```

### 3.3 Eventos de Reconexión en el Cliente

```csharp
// Estado de conexión visible en la UI
_hubConnection.Reconnecting += error =>
{
    _connectionState = ConnectionState.Reconnecting;
    AddToast("⚠️ Conexión perdida. Reconectando...", ToastType.Warning, persistent: true);
    InvokeAsync(StateHasChanged);
    return Task.CompletedTask;
};

_hubConnection.Reconnected += connectionId =>
{
    _connectionState = ConnectionState.Connected;
    RemoveToast("⚠️ Conexión perdida. Reconectando...");
    AddToast("✅ Conexión restablecida", ToastType.Success);

    // Re-unirse al grupo del juego con el nuevo ConnectionId
    _hubConnection!.SendAsync("RejoinGame", GameId, PlayerSession.PlayerId, connectionId);

    InvokeAsync(StateHasChanged);
    return Task.CompletedTask;
};

_hubConnection.Closed += error =>
{
    _connectionState = ConnectionState.Disconnected;
    RemoveToast("⚠️ Conexión perdida. Reconectando...");
    AddToast("❌ Se perdió la conexión. Recarga la página.", ToastType.Error, persistent: true);
    InvokeAsync(StateHasChanged);
    return Task.CompletedTask;
};
```

### 3.4 Manejo en el Servidor

```csharp
// En GameHub.cs
public override async Task OnDisconnectedAsync(Exception? exception)
{
    var playerId = _gameManager.GetPlayerIdByConnection(Context.ConnectionId);

    if (playerId != null)
    {
        var playerInfo = _gameManager.GetPlayerInfo(playerId);

        if (playerInfo?.CurrentGameId != null)
        {
            var gameId = playerInfo.CurrentGameId;
            _gameManager.MarkPlayerDisconnected(gameId, playerId);

            var playerName = playerInfo.PlayerName;

            // Notificar al grupo
            await Clients.Group($"game-{gameId}").SendAsync(
                "PlayerDisconnected", playerName);

            _logger.LogWarning(
                "Player {Name} disconnected from game {GameId}. Reason: {Reason}",
                playerName, gameId,
                exception?.Message ?? "Client closed connection");
        }

        _gameManager.UnregisterConnection(Context.ConnectionId);
    }

    await base.OnDisconnectedAsync(exception);
}
```

### 3.5 Indicador de Conexión en la UI

```razor
<div class="connection-indicator connection-@_connectionState.ToString().ToLower()">
    @switch (_connectionState)
    {
        case ConnectionState.Connected:
            <span class="dot dot--green"></span>
            <span>Conectado</span>
            break;
        case ConnectionState.Reconnecting:
            <span class="dot dot--yellow dot--pulse"></span>
            <span>Reconectando...</span>
            break;
        case ConnectionState.Disconnected:
            <span class="dot dot--red"></span>
            <span>Desconectado</span>
            break;
    }
</div>

@code {
    public enum ConnectionState { Connected, Reconnecting, Disconnected }
}
```

```css
.dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    display: inline-block;
}
.dot--green { background: var(--color-success); }
.dot--yellow { background: var(--color-warning); }
.dot--red { background: var(--color-danger); }
.dot--pulse { animation: pulse 1.5s infinite; }
```

---

## 4. Errores en Componentes Blazor (ErrorBoundary)

### 4.1 ErrorBoundary Global

Blazor Server incluye `ErrorBoundary` para capturar excepciones en el renderizado de componentes:

```razor
@* MainLayout.razor *@
<div class="app-layout">
    <ErrorBoundary @ref="_errorBoundary">
        <ChildContent>
            @Body
        </ChildContent>
        <ErrorContent Context="exception">
            <div class="error-panel">
                <h2>😵 ¡Ups! Algo salió mal</h2>
                <p>Ha ocurrido un error inesperado.</p>
                <p class="error-detail">@exception.Message</p>
                <button class="btn btn-primary" @onclick="Recover">
                    Intentar de nuevo
                </button>
            </div>
        </ErrorContent>
    </ErrorBoundary>
</div>

@code {
    private ErrorBoundary? _errorBoundary;

    private void Recover()
    {
        _errorBoundary?.Recover();
    }
}
```

### 4.2 ErrorBoundary Local (por componente)

Cada sección del juego puede tener su propio ErrorBoundary para aislar fallos:

```razor
@* Game.razor *@
<div class="game-layout">
    <GameHeader GameState="_gameState" MyPlayerId="@PlayerSession.PlayerId" />

    <ErrorBoundary>
        <ChildContent>
            <WorldMap GameState="_gameState" ... />
        </ChildContent>
        <ErrorContent>
            <div class="panel error-fallback">
                ⚠️ Error al cargar el mapa. Recarga la página.
            </div>
        </ErrorContent>
    </ErrorBoundary>

    <ErrorBoundary>
        <ChildContent>
            <TurnControls GameState="_gameState" ... />
        </ChildContent>
        <ErrorContent>
            <div class="panel error-fallback">
                ⚠️ Error en los controles. Recarga la página.
            </div>
        </ErrorContent>
    </ErrorBoundary>

    <EventLog Events="_gameState?.RecentEvents ?? []" />
</div>
```

De esta forma, un error en el mapa SVG no deja sin controles al jugador, y viceversa.

---

## 5. Gestión de Timeouts de Turno

### 5.1 Flujo de Timeout

```
  Jugador se desconecta durante su turno
       │
       ▼
  Timer de 60s (salto de turno)
       │
       ├── ¿Reconectó? → SÍ → Cancelar timer, continúa
       │
       └── NO → Ejecutar salto de turno
                  │
                  ▼
           ┌──────────────────────────────────────┐
           │  SkipDisconnectedPlayerTurn()         │
           │                                      │
           │  1. Si está en fase de refuerzo:     │
           │     → Colocar refuerzos automát.     │
           │       (aleatorio en sus territorios) │
           │                                      │
           │  2. Si está en fase de ataque:       │
           │     → EndAttackPhase()               │
           │                                      │
           │  3. Si está en fase de fortificación  │
           │     → SkipFortification()            │
           │                                      │
           │  4. EndTurn() → siguiente jugador    │
           │                                      │
           │  5. Notificar al grupo:              │
           │     "Se saltó el turno de Carlos     │
           │      (desconectado)"                 │
           └──────────────────────────────────────┘
```

### 5.2 Implementación

```csharp
private async Task HandleTurnSkipAsync(string gameId, string playerId)
{
    try
    {
        await _gameManager.ExecuteWithLock(gameId, async game =>
        {
            // Verificar que sigue siendo el turno del jugador desconectado
            if (game.GetCurrentPlayer()?.Id != playerId) return;
            if (game.GetPlayerById(playerId)?.IsConnected == true) return;

            var player = game.GetPlayerById(playerId)!;

            // Auto-resolver según la fase
            switch (game.Phase)
            {
                case GamePhase.Reinforcement:
                    // Colocar refuerzos aleatoriamente en sus territorios
                    AutoPlaceReinforcements(game, player);
                    game.Phase = GamePhase.Attack;
                    goto case GamePhase.Attack;

                case GamePhase.Attack:
                    game.Phase = GamePhase.Fortification;
                    goto case GamePhase.Fortification;

                case GamePhase.Fortification:
                    // No fortificar, pasar turno
                    if (game.ConqueredThisTurn)
                    {
                        var card = game.DrawCard();
                        if (card != null) player.AddCard(card);
                    }
                    game.AdvanceTurn();
                    var next = game.GetCurrentPlayer();
                    game.RemainingReinforcements =
                        _gameEngine.CalculateReinforcements(game, next);
                    break;
            }

            game.AddEvent(new GameEvent
            {
                Type = GameEventType.TurnEnded,
                Message = $"Se saltó el turno de {player.Name} (desconectado).",
                PlayerId = playerId,
                PlayerName = player.Name
            });
        });

        // Notificar al grupo
        var state = _gameManager.GetGameState(gameId);
        await _hubContext.Clients.Group($"game-{gameId}")
            .SendAsync("GameStateUpdated", state);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error skipping turn for disconnected player {PlayerId}", playerId);
    }
}

private void AutoPlaceReinforcements(Game game, Player player)
{
    var ownedTerritories = player.GetOwnedTerritories(game).ToList();
    if (ownedTerritories.Count == 0) return;

    while (game.RemainingReinforcements > 0)
    {
        // Distribuir uniformemente
        foreach (var territory in ownedTerritories)
        {
            if (game.RemainingReinforcements <= 0) break;
            territory.AddArmies(1);
            game.RemainingReinforcements--;
        }
    }
}
```

### 5.3 Timer de Abandono (5 minutos)

```csharp
private async Task HandleAbandonAsync(string gameId, string playerId)
{
    try
    {
        await _gameManager.ExecuteWithLock(gameId, async game =>
        {
            var player = game.GetPlayerById(playerId);
            if (player == null || player.IsConnected || player.IsEliminated) return;

            // Marcar como eliminado
            player.IsEliminated = true;

            // Si era su turno, avanzar
            if (game.GetCurrentPlayer()?.Id == playerId)
            {
                game.AdvanceTurn();
                var next = game.GetCurrentPlayer();
                game.RemainingReinforcements =
                    _gameEngine.CalculateReinforcements(game, next);
            }

            // Devolver territorios a "neutral" o redistribuir
            foreach (var territory in game.Territories.Values
                .Where(t => t.OwnerId == playerId))
            {
                territory.OwnerId = "neutral";
                territory.Armies = 1;
            }

            // Devolver cartas al mazo
            var cards = player.SurrenderAllCards();
            game.DiscardPile.AddRange(cards);

            game.AddEvent(new GameEvent
            {
                Type = GameEventType.PlayerEliminated,
                Message = $"{player.Name} ha abandonado la partida (desconectado 5 min).",
                PlayerId = playerId,
                PlayerName = player.Name
            });

            // ¿Queda solo 1 jugador?
            if (_gameEngine.IsGameOver(game))
            {
                game.Status = GameStatus.Finished;
                game.FinishedAt = DateTime.UtcNow;
            }
        });

        var state = _gameManager.GetGameState(gameId);
        await _hubContext.Clients.Group($"game-{gameId}")
            .SendAsync("GameStateUpdated", state);
        await _hubContext.Clients.Group($"game-{gameId}")
            .SendAsync("PlayerEliminated", _gameManager.GetPlayerName(playerId));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error handling abandon for player {PlayerId}", playerId);
    }
}
```

---

## 6. Logging y Diagnóstico

### 6.1 Niveles de Log

| Nivel | Uso en MiniRisk | Ejemplo |
|:-----:|----------------|---------|
| **Trace** | Detalles internos del motor (solo en dev) | Cada comparación de dados |
| **Debug** | Flow de acciones del juego | "PlaceReinforcements: Alaska +3" |
| **Information** | Eventos del ciclo de vida | "Game created", "Player joined", "Turn started" |
| **Warning** | Situaciones recuperables | "Player disconnected", "Turn skipped" |
| **Error** | Errores inesperados | Excepciones no controladas |
| **Critical** | Fallos del sistema | "GameManager state corruption detected" |

### 6.2 Logging Estructurado

```csharp
// Bueno: logging estructurado con parámetros nombrados
_logger.LogInformation(
    "Player {PlayerName} ({PlayerId}) attacked {From} → {To} with {Dice} dice. Result: Atk-{AtkLoss}, Def-{DefLoss}",
    playerName, playerId, from, to, diceCount,
    result.AttackerLosses, result.DefenderLosses);

// Malo: concatenación de strings
_logger.LogInformation($"Player {playerName} attacked {from} to {to}"); // ❌
```

### 6.3 Configuración

```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.AspNetCore.SignalR": "Debug",
      "MiniRisk.Services": "Debug",
      "MiniRisk.Hubs": "Debug"
    }
  }
}

// appsettings.json (producción)
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.AspNetCore.SignalR": "Warning",
      "MiniRisk.Services": "Information",
      "MiniRisk.Hubs": "Information"
    }
  }
}
```

### 6.4 Log de Diagnóstico del GameManager

El `GameManager` incluye métodos para inspección del estado:

```csharp
public void LogDiagnostics()
{
    _logger.LogInformation("=== GameManager Diagnostics ===");
    _logger.LogInformation("Active games: {Count}", _games.Count);
    _logger.LogInformation("Connected players: {Count}", _connections.Count);

    foreach (var (gameId, context) in _games)
    {
        var game = context.Game;
        _logger.LogInformation(
            "Game {GameId} ({Name}): Status={Status}, Phase={Phase}, " +
            "Players={PlayerCount}, Turn={Turn}",
            gameId, game.Name, game.Status, game.Phase,
            game.Players.Count, game.TurnNumber);

        foreach (var player in game.Players)
        {
            _logger.LogInformation(
                "  Player {Name}: Color={Color}, Connected={Connected}, " +
                "Eliminated={Eliminated}, Territories={Territories}",
                player.Name, player.Color, player.IsConnected,
                player.IsEliminated, player.GetOwnedTerritories(game).Count());
        }
    }
}
```

---

## 7. Protección contra Corrupción del Estado

### 7.1 Atomicidad

Todas las operaciones que modifican el estado se ejecutan dentro de `ExecuteWithLock`, que garantiza:

1. **Exclusión mutua**: Solo una operación modifica el estado a la vez.
2. **Atomicidad**: Si la operación falla (excepción), el estado puede quedar parcialmente modificado. Para prevenirlo:

```csharp
// Patrón: validar todo ANTES de modificar
public AttackGameResult Attack(Game game, string playerId,
    TerritoryName from, TerritoryName to, int diceCount)
{
    // ═══ FASE 1: VALIDACIÓN (no modifica nada) ═══
    if (game.Phase != GamePhase.Attack)
        return AttackGameResult.Fail("Wrong phase");

    var attacker = game.Territories[from];
    var defender = game.Territories[to];

    if (attacker.OwnerId != playerId) return AttackGameResult.Fail("...");
    if (!attacker.IsAdjacentTo(to)) return AttackGameResult.Fail("...");
    if (!attacker.CanAttackFrom()) return AttackGameResult.Fail("...");

    // ═══ FASE 2: EJECUCIÓN (solo si toda validación pasó) ═══
    var dice = _diceService.Roll(diceCount);
    var defDice = _diceService.Roll(Math.Min(2, defender.Armies));
    var result = ResolveCombat(dice, defDice, from, to);

    // ═══ FASE 3: APLICACIÓN (modificar estado) ═══
    attacker.Armies -= result.AttackerLosses;
    defender.Armies -= result.DefenderLosses;
    // ...
}
```

### 7.2 Verificación de Invariantes

Periódicamente (o en modo debug), verificar que las invariantes del dominio se cumplen:

```csharp
#if DEBUG
public void VerifyInvariants(Game game)
{
    // INV-01: Todo territorio tiene ≥1 ejército
    foreach (var t in game.Territories.Values)
    {
        if (t.Armies < 1 && !string.IsNullOrEmpty(t.OwnerId) && t.OwnerId != "neutral")
        {
            _logger.LogCritical(
                "INVARIANT VIOLATION: Territory {Territory} has {Armies} armies!",
                t.Name, t.Armies);
        }
    }

    // INV-05: Total de cartas = 44
    int totalCards = game.CardDeck.Count
                   + game.DiscardPile.Count
                   + game.Players.Sum(p => p.Cards.Count);
    if (totalCards != 44)
    {
        _logger.LogCritical(
            "INVARIANT VIOLATION: Total cards = {Total} (expected 44)!",
            totalCards);
    }

    // INV-06: Jugador actual no eliminado
    if (game.Status == GameStatus.Playing && game.GetCurrentPlayer().IsEliminated)
    {
        _logger.LogCritical(
            "INVARIANT VIOLATION: Current player {Player} is eliminated!",
            game.GetCurrentPlayer().Name);
    }
}
#endif
```

---

## 8. Manejo de la Caída del Servidor

### 8.1 Impacto

| Aspecto | Impacto |
|---------|---------|
| Estado de las partidas | **Perdido completamente** (todo en memoria) |
| Conexiones SignalR | Cerradas |
| Circuitos Blazor | Destruidos |
| Sesiones de jugador | Perdidas |

### 8.2 Experiencia del Usuario

```
  Servidor se reinicia
       │
       ▼
  Cliente detecta pérdida de conexión
       │
       ▼
  SignalR intenta reconectar (WithAutomaticReconnect)
       │
       ├── Servidor está de vuelta
       │   │
       │   ▼
       │   Conexión restablecida, PERO:
       │   - No hay partidas en memoria
       │   - No hay sesión del jugador
       │   │
       │   ▼
       │   Servidor envía: "Session expired"
       │   │
       │   ▼
       │   Cliente redirige a Home.razor
       │   con mensaje: "El servidor se ha reiniciado.
       │                  Las partidas en curso se han perdido."
       │
       └── Servidor sigue caído
           │
           ▼
           Closed event → UI muestra:
           "No se puede conectar al servidor.
            Comprueba que el servidor está ejecutándose."
```

### 8.3 Mejoras Futuras (Fuera de v1.0)

| Mejora | Descripción |
|--------|-------------|
| **Persistencia periódica** | Serializar el estado a disco cada N turnos (JSON) |
| **Checkpoint** | Guardar un snapshot al inicio de cada turno |
| **Recuperación** | Al reiniciar, cargar el último snapshot y notificar "partida recuperada" |

Estas mejoras están **fuera del alcance de la v1.0** pero se documentan como referencia.

---

## 9. Tabla Resumen de Errores y Manejo

| Error | Dónde | Detección | Manejo | Notificación al usuario |
|-------|-------|-----------|--------|------------------------|
| Acción inválida del juego | GameEngine | `GameResult.Fail()` | Retornar error, no modificar estado | Toast rojo con mensaje |
| Excepción en GameHub | GameHub | `try/catch` | Logging + error genérico | Toast "Error inesperado" |
| Excepción en render | Componente Blazor | `ErrorBoundary` | Mostrar fallback, botón "Reintentar" | Panel de error inline |
| Desconexión WiFi temporal | SignalR | `Reconnecting` event | Reconexión automática | Toast amarillo "Reconectando..." |
| Desconexión prolongada | SignalR | `OnDisconnectedAsync` | Timer 60s (skip) + 5min (abandon) | Banner "Jugador desconectado" |
| Cierre pestaña + reapertura | Blazor Circuit | `GetReconnectionInfo()` | Ofrecer reconexión | Diálogo "¿Reconectar?" |
| Servidor se reinicia | Infraestructura | Conexión fallida | Redirigir a Home | Mensaje "Servidor reiniciado" |
| Timeout de turno (desconexión) | GameManager timer | Timer callback | Auto-resolver fase y pasar turno | Toast "Turno saltado" |
| Estado corrupto (invariante) | GameEngine (debug) | `VerifyInvariants()` | Log critical | — (solo diagnóstico) |

---

> **Siguiente documento:** [13 — Testing](./13_Testing.md)
