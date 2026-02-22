# 04 — Comunicación en Tiempo Real — SignalR

> **Documento:** 04 de 14  
> **Versión:** 1.0  
> **Última actualización:** 22 de febrero de 2026  
> **Volver al índice:** [00_Indice.md](./00_Indice.md)  
> **Documento anterior:** [03 — Identificación de Jugadores](./03_Identificacion_Jugadores.md)

---

## 1. ¿Por qué SignalR?

### 1.1 Comparativa de Alternativas

| Mecanismo | Descripción | ¿Adecuado para MiniRisk? |
|-----------|-------------|:------------------------:|
| **Polling HTTP** | El cliente hace peticiones periódicas al servidor para obtener actualizaciones | ❌ Latencia alta (segundo entre polls), desperdicio de recursos |
| **Long Polling** | El servidor mantiene la petición abierta hasta que hay datos nuevos | ⚠️ Menor latencia pero complejo de implementar y mantener |
| **Server-Sent Events (SSE)** | El servidor envía eventos al cliente por una conexión unidireccional | ⚠️ Solo servidor→cliente; el cliente necesitaría REST para enviar acciones |
| **WebSocket directo** | Conexión bidireccional persistente con protocolo personalizado | ⚠️ Funcional, pero requiere implementar protocolo, serialización y gestión de grupos manualmente |
| **SignalR** | Abstracción de alto nivel sobre WebSocket con soporte para invocación de métodos, grupos, reconexión automática y serialización | ✅ **Ideal** |

### 1.2 Ventajas de SignalR en el Contexto de MiniRisk

| Ventaja | Detalle |
|---------|---------|
| **Ya integrado en Blazor Server** | Blazor Server usa SignalR internamente para los circuitos. Añadir un hub adicional no introduce ninguna dependencia nueva. |
| **Invocación de métodos tipada** | En lugar de enviar mensajes crudos por WebSocket, se invocan métodos con nombre y parámetros tipados (`hub.SendAsync("Attack", from, to, dice)`). |
| **Grupos nativos** | SignalR permite agrupar conexiones por partida (`Groups.AddToGroupAsync`). Un solo `Clients.Group("game-xyz").SendAsync(...)` notifica a todos los jugadores de la partida. |
| **Reconexión automática** | `WithAutomaticReconnect()` gestiona la reconexión por red inestable sin código manual. |
| **Serialización automática** | Los objetos C# se serializan/deserializan automáticamente a JSON. No hay que construir ni parsear mensajes. |
| **Escalabilidad suficiente** | Un servidor con SignalR soporta fácilmente cientos de conexiones. Para 2–6 jugadores en LAN, sobra capacidad. |
| **Misma tecnología cliente-servidor** | El cliente (Blazor) y el servidor comparten los mismos modelos C#. No hay desajuste de tipos. |

---

## 2. Arquitectura de Comunicación

### 2.1 Dos Canales: Circuito Blazor + GameHub

Como se explicó en el [documento 02](./02_Arquitectura_General.md), MiniRisk tiene dos canales SignalR simultáneos:

```
  Navegador del Jugador
  ┌────────────────────────────────────────────────┐
  │                                                │
  │   blazor.web.js                                │
  │   ├── WebSocket 1: Circuito Blazor             │
  │   │   (automático, gestiona render de UI)      │
  │   │                                            │
  │   └── WebSocket 2: HubConnection al GameHub    │
  │       (explícito, creado en Game.razor)         │
  │       URL: /gamehub                            │
  │                                                │
  └────────────────────┬───────────┬───────────────┘
                       │           │
              Circuito │           │ Hub
              (render) │           │ (juego)
                       │           │
                       ▼           ▼
  ┌────────────────────────────────────────────────┐
  │              SERVIDOR ASP.NET Core              │
  │                                                │
  │   Blazor Circuit Handler     GameHub            │
  │   (UI del jugador)           (lógica del juego) │
  │                                                │
  └────────────────────────────────────────────────┘
```

### 2.2 ¿Cuándo se usa cada canal?

| Acción | Canal utilizado | Motivo |
|--------|:--------------:|--------|
| Render de la UI del jugador | Circuito Blazor | Automático, gestionado por Blazor |
| Click en un botón que solo afecta a la UI local | Circuito Blazor | No involucra a otros jugadores |
| Atacar un territorio | GameHub | Todos los jugadores deben ver el resultado |
| Mover ejércitos (fortificar) | GameHub | Todos deben actualizar su mapa |
| Enviar mensaje de chat | GameHub | Todos deben recibir el mensaje |
| Ver mis cartas | Circuito Blazor | Solo me afecta a mí |
| Canjear cartas | GameHub | El canje afecta al refuerzo y al estado global |
| Pasar de fase / terminar turno | GameHub | Todos deben saber que cambió el turno |

---

## 3. Diseño del GameHub

### 3.1 Visión General

```csharp
// Hubs/GameHub.cs
[AllowAnonymous]  // No hay autenticación
public class GameHub : Hub
{
    private readonly IGameManager _gameManager;
    private readonly IGameEngine _gameEngine;

    public GameHub(IGameManager gameManager, IGameEngine gameEngine)
    {
        _gameManager = gameManager;
        _gameEngine = gameEngine;
    }

    // ... métodos del servidor (ver sección 3.2)

    public override async Task OnConnectedAsync() { ... }
    public override async Task OnDisconnectedAsync(Exception? exception) { ... }
}
```

### 3.2 Métodos del Servidor (Client → Server)

Estos son los métodos que los clientes (componentes Blazor) invocan en el servidor:

#### Gestión de Partida

```csharp
/// <summary>
/// Un jugador se une a una partida existente.
/// Lo añade al grupo de SignalR y a la lista de jugadores de la partida.
/// </summary>
public async Task JoinGame(string gameId, string playerId, string playerName)

/// <summary>
/// Un jugador abandona la partida voluntariamente.
/// Lo elimina del grupo de SignalR.
/// </summary>
public async Task LeaveGame(string gameId, string playerId)

/// <summary>
/// Un jugador que se desconectó se reconecta a su partida.
/// Reasigna su ConnectionId y lo vuelve a añadir al grupo.
/// </summary>
public async Task RejoinGame(string gameId, string playerId)

/// <summary>
/// El creador de la partida indica que se inicie el juego.
/// Distribuye territorios y pasa a la fase de colocación inicial.
/// </summary>
public async Task StartGame(string gameId, string playerId)
```

#### Fase de Colocación Inicial

```csharp
/// <summary>
/// El jugador coloca ejércitos iniciales en sus territorios
/// durante la fase de configuración.
/// </summary>
public async Task PlaceInitialArmies(string gameId, string playerId, 
    string territoryId, int count)
```

#### Fase de Refuerzo

```csharp
/// <summary>
/// El jugador coloca ejércitos de refuerzo en un territorio propio.
/// Solo válido durante la fase de refuerzo del turno del jugador.
/// </summary>
public async Task PlaceReinforcements(string gameId, string playerId, 
    string territoryId, int count)

/// <summary>
/// El jugador confirma que ha terminado de colocar todos sus refuerzos.
/// Avanza a la fase de ataque.
/// </summary>
public async Task ConfirmReinforcements(string gameId, string playerId)
```

#### Fase de Ataque

```csharp
/// <summary>
/// El jugador ataca un territorio adyacente enemigo.
/// Resuelve la tirada de dados y aplica el resultado.
/// </summary>
public async Task Attack(string gameId, string playerId, 
    string fromTerritoryId, string toTerritoryId, int attackDiceCount)

/// <summary>
/// Tras conquistar un territorio, el jugador mueve ejércitos al nuevo territorio.
/// </summary>
public async Task MoveArmiesAfterConquest(string gameId, string playerId,
    string fromTerritoryId, string toTerritoryId, int armyCount)

/// <summary>
/// El jugador decide dejar de atacar y pasar a la fase de fortificación.
/// </summary>
public async Task EndAttackPhase(string gameId, string playerId)
```

#### Fase de Fortificación

```csharp
/// <summary>
/// El jugador mueve ejércitos entre dos territorios propios conectados.
/// Solo un movimiento por turno.
/// </summary>
public async Task Fortify(string gameId, string playerId, 
    string fromTerritoryId, string toTerritoryId, int armyCount)

/// <summary>
/// El jugador decide no fortificar y termina su turno.
/// </summary>
public async Task SkipFortification(string gameId, string playerId)
```

#### Cartas

```csharp
/// <summary>
/// El jugador canjea un conjunto de 3 cartas por ejércitos adicionales.
/// </summary>
public async Task TradeCards(string gameId, string playerId, 
    string[] cardIds)
```

#### Chat

```csharp
/// <summary>
/// El jugador envía un mensaje de chat a todos los jugadores de la partida.
/// </summary>
public async Task SendChatMessage(string gameId, string playerId, string message)
```

### 3.3 Métodos del Cliente (Server → Client)

Estos son los métodos que el servidor invoca en los clientes. Los componentes Blazor registran handlers para estos eventos:

#### Actualización de Estado

```csharp
/// <summary>
/// Envía el estado completo actualizado de la partida a todos los jugadores.
/// Se envía tras cualquier acción que modifique el estado del juego.
/// </summary>
"GameStateUpdated" → GameStateDto gameState

/// <summary>
/// Notifica que el turno ha cambiado a otro jugador.
/// </summary>
"TurnChanged" → TurnChangedDto { PlayerId, PlayerName, Phase }

/// <summary>
/// Notifica que la fase del turno actual ha cambiado.
/// </summary>
"PhaseChanged" → PhaseChangedDto { Phase, PlayerId }
```

#### Combate

```csharp
/// <summary>
/// Envía el resultado de una tirada de dados (ataque).
/// Incluye los dados del atacante, defensor y las pérdidas.
/// </summary>
"DiceRolled" → DiceResultDto
{
    AttackerDice: int[],          // Ej: [6, 3, 2]
    DefenderDice: int[],          // Ej: [5, 4]
    AttackerLosses: int,          // Ej: 1
    DefenderLosses: int,          // Ej: 1
    FromTerritoryId: string,
    ToTerritoryId: string,
    AttackerName: string,
    DefenderName: string
}

/// <summary>
/// Notifica que un territorio ha sido conquistado.
/// </summary>
"TerritoryConquered" → TerritoryConqueredDto
{
    TerritoryId: string,
    PreviousOwnerId: string,
    NewOwnerId: string,
    NewOwnerName: string,
    ArmiesMoved: int
}
```

#### Jugadores

```csharp
/// <summary>
/// Un jugador ha sido eliminado de la partida.
/// </summary>
"PlayerEliminated" → PlayerEliminatedDto
{
    EliminatedPlayerId: string,
    EliminatedPlayerName: string,
    EliminatedByPlayerId: string,
    EliminatedByPlayerName: string,
    CardsTransferred: int
}

/// <summary>
/// Un nuevo jugador se ha unido a la partida (en el lobby pre-partida).
/// </summary>
"PlayerJoined" → PlayerJoinedDto { PlayerId, PlayerName, Color }

/// <summary>
/// Un jugador ha abandonado la partida.
/// </summary>
"PlayerLeft" → PlayerLeftDto { PlayerId, PlayerName }

/// <summary>
/// Un jugador se ha reconectado a la partida.
/// </summary>
"PlayerReconnected" → PlayerReconnectedDto { PlayerId, PlayerName }

/// <summary>
/// Un jugador se ha desconectado (perdió la conexión).
/// </summary>
"PlayerDisconnected" → PlayerDisconnectedDto { PlayerId, PlayerName }
```

#### Cartas

```csharp
/// <summary>
/// Notifica a un jugador específico sobre sus cartas actualizadas.
/// Solo se envía al jugador propietario de las cartas (no al grupo).
/// </summary>
"CardsUpdated" → CardsUpdatedDto { Cards: CardDto[] }

/// <summary>
/// Notifica al grupo que un jugador ha canjeado cartas.
/// No revela qué cartas, solo el resultado.
/// </summary>
"CardsTraded" → CardsTradedDto
{
    PlayerId: string,
    PlayerName: string,
    ArmiesReceived: int,
    TradeNumber: int            // Número global de canje
}
```

#### Fin de Partida

```csharp
/// <summary>
/// La partida ha terminado. Un jugador ha ganado.
/// </summary>
"GameOver" → GameOverDto
{
    WinnerId: string,
    WinnerName: string,
    TotalTurns: int,
    Duration: TimeSpan
}
```

#### Chat

```csharp
/// <summary>
/// Un mensaje de chat ha sido enviado por un jugador.
/// </summary>
"ChatMessageReceived" → ChatMessageDto
{
    PlayerId: string,
    PlayerName: string,
    PlayerColor: PlayerColor,
    Message: string,
    Timestamp: DateTime
}
```

#### Sistema

```csharp
/// <summary>
/// Mensaje del sistema (no de un jugador) para notificaciones generales.
/// </summary>
"SystemMessage" → SystemMessageDto
{
    Message: string,
    Type: SystemMessageType,    // Info, Warning, Error
    Timestamp: DateTime
}

/// <summary>
/// Notifica un error al jugador que intentó una acción inválida.
/// Solo se envía al jugador que cometió el error (no al grupo).
/// </summary>
"ActionError" → ActionErrorDto
{
    Message: string,            // "No puedes atacar, no es tu turno"
    ActionAttempted: string     // "Attack"
}
```

---

## 4. Grupos de SignalR

### 4.1 Estrategia de Agrupación

Cada partida se mapea a un **grupo de SignalR** con el nombre `game-{gameId}`:

```
                      GameHub
                         │
        ┌────────────────┼────────────────┐
        │                │                │
   Grupo:           Grupo:           Grupo:
   "game-abc"       "game-def"       "game-ghi"
   ┌─────────┐      ┌─────────┐      ┌─────────┐
   │Carlos   │      │Pedro    │      │Eva      │
   │Ana      │      │Marta    │      │Miguel   │
   │Luis     │      │Raúl     │      │Laura    │
   │         │      │Sofía    │      │         │
   └─────────┘      └─────────┘      └─────────┘
```

### 4.2 Ciclo de Vida de un Grupo

```
  Creación de partida                     Partida finalizada
  (por un jugador)                        (victoria o abandono total)
         │                                        │
         ▼                                        ▼
  ┌──────────────┐   Jugadores   ┌──────────────┐
  │ Grupo vacío  │──se unen───▶│ Grupo activo  │───────────▶ Grupo eliminado
  │ "game-abc"   │              │ "game-abc"    │              (sin conexiones)
  │              │              │               │
  │ (el grupo se │              │ Carlos ✓      │
  │  crea al     │              │ Ana ✓         │
  │  unirse el   │              │ Luis ✓        │
  │  primer      │              │               │
  │  jugador)    │              └──────┬────────┘
  └──────────────┘                     │
                                       │ Luis se desconecta
                                       ▼
                                ┌──────────────┐
                                │ Grupo parcial│
                                │ "game-abc"   │
                                │              │
                                │ Carlos ✓     │
                                │ Ana ✓        │
                                │ Luis ✗       │──── Luis reconecta ──▶ Grupo completo
                                │ (desconectado│                        de nuevo
                                │  del grupo)  │
                                └──────────────┘
```

### 4.3 Operaciones de Grupo

```csharp
// ═══════════════════════════════════════
// UNIRSE A UNA PARTIDA
// ═══════════════════════════════════════
public async Task JoinGame(string gameId, string playerId, string playerName)
{
    // 1. Validar que la partida existe y acepta jugadores
    var game = _gameManager.GetGame(gameId);
    if (game == null)
    {
        await Clients.Caller.SendAsync("ActionError", new ActionErrorDto
        {
            Message = "La partida no existe",
            ActionAttempted = "JoinGame"
        });
        return;
    }

    // 2. Añadir al jugador a la partida (lógica de negocio)
    var result = _gameManager.AddPlayer(gameId, playerId, playerName, Context.ConnectionId);
    if (!result.Success)
    {
        await Clients.Caller.SendAsync("ActionError", new ActionErrorDto
        {
            Message = result.ErrorMessage,
            ActionAttempted = "JoinGame"
        });
        return;
    }

    // 3. Añadir la conexión al grupo de SignalR
    await Groups.AddToGroupAsync(Context.ConnectionId, $"game-{gameId}");

    // 4. Notificar a todos los jugadores de la partida
    await Clients.Group($"game-{gameId}").SendAsync("PlayerJoined", new PlayerJoinedDto
    {
        PlayerId = playerId,
        PlayerName = playerName,
        Color = result.AssignedColor
    });

    // 5. Enviar el estado actual de la partida al jugador que se unió
    var gameState = _gameManager.GetGameState(gameId);
    await Clients.Caller.SendAsync("GameStateUpdated", gameState);
}

// ═══════════════════════════════════════
// ABANDONAR UNA PARTIDA
// ═══════════════════════════════════════
public async Task LeaveGame(string gameId, string playerId)
{
    // 1. Eliminar al jugador de la partida (lógica de negocio)
    _gameManager.RemovePlayer(gameId, playerId);

    // 2. Eliminar la conexión del grupo de SignalR
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game-{gameId}");

    // 3. Notificar al grupo
    var playerName = _gameManager.GetPlayerName(playerId);
    await Clients.Group($"game-{gameId}").SendAsync("PlayerLeft", new PlayerLeftDto
    {
        PlayerId = playerId,
        PlayerName = playerName
    });

    // 4. Enviar estado actualizado
    var gameState = _gameManager.GetGameState(gameId);
    await Clients.Group($"game-{gameId}").SendAsync("GameStateUpdated", gameState);
}

// ═══════════════════════════════════════
// RECONECTARSE A UNA PARTIDA
// ═══════════════════════════════════════
public async Task RejoinGame(string gameId, string playerId)
{
    // 1. Actualizar el ConnectionId del jugador en el GameManager
    _gameManager.UpdatePlayerConnection(gameId, playerId, Context.ConnectionId);

    // 2. Añadir la nueva conexión al grupo
    await Groups.AddToGroupAsync(Context.ConnectionId, $"game-{gameId}");

    // 3. Notificar reconexión al grupo
    var playerName = _gameManager.GetPlayerName(playerId);
    await Clients.Group($"game-{gameId}").SendAsync("PlayerReconnected", new PlayerReconnectedDto
    {
        PlayerId = playerId,
        PlayerName = playerName
    });

    // 4. Enviar estado completo al jugador reconectado
    var gameState = _gameManager.GetGameState(gameId);
    await Clients.Caller.SendAsync("GameStateUpdated", gameState);
}
```

### 4.4 Tipos de Destino en los Envíos

| Destino | Método SignalR | Cuándo se usa |
|---------|---------------|---------------|
| **Todos en la partida** | `Clients.Group($"game-{gameId}")` | Estado actualizado, dados, conquistas, chat, turnos |
| **Solo el jugador que actúa** | `Clients.Caller` | Errores de validación, estado inicial al unirse |
| **Un jugador específico** | `Clients.Client(connectionId)` | Cartas privadas del jugador |
| **Todos excepto el que actúa** | `Clients.GroupExcept($"game-{gameId}", Context.ConnectionId)` | Notificaciones que el autor ya conoce |

---

## 5. DTOs (Data Transfer Objects) de Mensajes

### 5.1 Catálogo Completo de DTOs

Los DTOs son las clases C# que se serializan a JSON y viajan por SignalR. Se definen en el directorio `Models/Dtos/`.

#### Estado del Juego

```csharp
/// <summary>
/// Estado completo de la partida. Se envía tras cada acción.
/// Es el DTO más grande pero garantiza consistencia.
/// </summary>
public class GameStateDto
{
    public string GameId { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public GameStatus Status { get; set; }          // Waiting, Playing, Finished
    public GamePhase Phase { get; set; }             // Setup, Reinforcement, Attack, Fortification
    public string CurrentPlayerId { get; set; } = string.Empty;
    public string CurrentPlayerName { get; set; } = string.Empty;
    public int TurnNumber { get; set; }
    public int TradeCount { get; set; }              // Nº global de canjes realizados
    public int RemainingReinforcements { get; set; } // Ejércitos por colocar en esta fase
    public List<PlayerDto> Players { get; set; } = [];
    public List<TerritoryDto> Territories { get; set; } = [];
    public List<GameEventDto> RecentEvents { get; set; } = [];
    public DateTime StartedAt { get; set; }
}

/// <summary>
/// Información pública de un jugador (visible para todos).
/// </summary>
public class PlayerDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public PlayerColor Color { get; set; }
    public int TerritoryCount { get; set; }
    public int TotalArmies { get; set; }
    public int CardCount { get; set; }               // Solo cantidad, no detalle (info privada)
    public bool IsConnected { get; set; }
    public bool IsEliminated { get; set; }
}

/// <summary>
/// Estado de un territorio tal como lo ven todos los jugadores.
/// </summary>
public class TerritoryDto
{
    public string TerritoryId { get; set; } = string.Empty;
    public string TerritoryName { get; set; } = string.Empty;
    public string ContinentId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public PlayerColor OwnerColor { get; set; }
    public int Armies { get; set; }
}
```

#### Combate

```csharp
public class DiceResultDto
{
    public int[] AttackerDice { get; set; } = [];
    public int[] DefenderDice { get; set; } = [];
    public int AttackerLosses { get; set; }
    public int DefenderLosses { get; set; }
    public string FromTerritoryId { get; set; } = string.Empty;
    public string ToTerritoryId { get; set; } = string.Empty;
    public string AttackerId { get; set; } = string.Empty;
    public string AttackerName { get; set; } = string.Empty;
    public string DefenderId { get; set; } = string.Empty;
    public string DefenderName { get; set; } = string.Empty;
    public bool TerritoryConquered { get; set; }
}

public class TerritoryConqueredDto
{
    public string TerritoryId { get; set; } = string.Empty;
    public string TerritoryName { get; set; } = string.Empty;
    public string PreviousOwnerId { get; set; } = string.Empty;
    public string PreviousOwnerName { get; set; } = string.Empty;
    public string NewOwnerId { get; set; } = string.Empty;
    public string NewOwnerName { get; set; } = string.Empty;
    public int ArmiesMoved { get; set; }
}
```

#### Jugadores

```csharp
public class PlayerJoinedDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public PlayerColor Color { get; set; }
}

public class PlayerLeftDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
}

public class PlayerReconnectedDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
}

public class PlayerDisconnectedDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
}

public class PlayerEliminatedDto
{
    public string EliminatedPlayerId { get; set; } = string.Empty;
    public string EliminatedPlayerName { get; set; } = string.Empty;
    public string EliminatedByPlayerId { get; set; } = string.Empty;
    public string EliminatedByPlayerName { get; set; } = string.Empty;
    public int CardsTransferred { get; set; }
}
```

#### Turnos y Fases

```csharp
public class TurnChangedDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public PlayerColor PlayerColor { get; set; }
    public GamePhase Phase { get; set; }
    public int TurnNumber { get; set; }
    public int Reinforcements { get; set; }     // Ejércitos a colocar en fase de refuerzo
}

public class PhaseChangedDto
{
    public GamePhase NewPhase { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
}
```

#### Cartas

```csharp
/// <summary>
/// Cartas del jugador (solo para el propietario, no se envía al grupo).
/// </summary>
public class CardsUpdatedDto
{
    public List<CardDto> Cards { get; set; } = [];
}

public class CardDto
{
    public string CardId { get; set; } = string.Empty;
    public CardType Type { get; set; }              // Infantry, Cavalry, Artillery, Wildcard
    public string? TerritoryId { get; set; }        // null para comodines
    public string? TerritoryName { get; set; }
}

/// <summary>
/// Notificación pública de que alguien canjeó cartas.
/// </summary>
public class CardsTradedDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public int ArmiesReceived { get; set; }
    public int TradeNumber { get; set; }
}
```

#### Chat y Sistema

```csharp
public class ChatMessageDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public PlayerColor PlayerColor { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class SystemMessageDto
{
    public string Message { get; set; } = string.Empty;
    public SystemMessageType Type { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum SystemMessageType
{
    Info,
    Warning,
    Error
}

public class ActionErrorDto
{
    public string Message { get; set; } = string.Empty;
    public string ActionAttempted { get; set; } = string.Empty;
}
```

#### Fin de Partida

```csharp
public class GameOverDto
{
    public string WinnerId { get; set; } = string.Empty;
    public string WinnerName { get; set; } = string.Empty;
    public PlayerColor WinnerColor { get; set; }
    public int TotalTurns { get; set; }
    public TimeSpan Duration { get; set; }
    public List<PlayerFinalStatsDto> PlayerStats { get; set; } = [];
}

public class PlayerFinalStatsDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public PlayerColor Color { get; set; }
    public int TerritoriesConquered { get; set; }
    public int TerritoriesLost { get; set; }
    public int ArmiesDestroyed { get; set; }
    public int ArmiesLost { get; set; }
    public int CardsTraded { get; set; }
    public bool IsWinner { get; set; }
    public int? EliminatedOnTurn { get; set; }
}
```

### 5.2 Estructura de Carpetas para DTOs

```
Models/
├── Dtos/
│   ├── GameStateDto.cs
│   ├── PlayerDto.cs
│   ├── TerritoryDto.cs
│   ├── DiceResultDto.cs
│   ├── TerritoryConqueredDto.cs
│   ├── PlayerJoinedDto.cs
│   ├── PlayerLeftDto.cs
│   ├── PlayerReconnectedDto.cs
│   ├── PlayerDisconnectedDto.cs
│   ├── PlayerEliminatedDto.cs
│   ├── TurnChangedDto.cs
│   ├── PhaseChangedDto.cs
│   ├── CardsUpdatedDto.cs
│   ├── CardDto.cs
│   ├── CardsTradedDto.cs
│   ├── ChatMessageDto.cs
│   ├── SystemMessageDto.cs
│   ├── ActionErrorDto.cs
│   ├── GameOverDto.cs
│   └── PlayerFinalStatsDto.cs
└── ...
```

---

## 6. Información Pública vs Privada

Un aspecto importante del diseño de SignalR es controlar **qué información se envía a quién**:

### 6.1 Tabla de Visibilidad

| Información | Destinatario | Motivo |
|-------------|:------------:|--------|
| Estado del mapa (territorios, ejércitos, propietarios) | 👥 Todos | Información visible para todos en el tablero |
| Jugador en turno y fase actual | 👥 Todos | Todos necesitan saber quién juega |
| Resultado de dados | 👥 Todos | Todos ven los dados en el tablero |
| Número de cartas de un jugador | 👥 Todos | Se muestra como un número en el panel del jugador |
| **Cartas específicas de un jugador** | 👤 **Solo el propietario** | Las cartas son secretas; los otros no deben saber qué cartas tienes |
| Chat | 👥 Todos | El chat es público para toda la partida |
| Errores de validación | 👤 Solo el que falló | No es relevante para los demás |
| Canje de cartas (resultado) | 👥 Todos | Los ejércitos recibidos afectan al juego visible |

### 6.2 Implementación de Mensajes Privados

```csharp
// EN EL GAMEHUB:

// Enviar cartas solo al propietario (privado)
var ownerConnectionId = _gameManager.GetPlayerConnectionId(playerId);
if (ownerConnectionId != null)
{
    await Clients.Client(ownerConnectionId).SendAsync("CardsUpdated", 
        new CardsUpdatedDto { Cards = playerCards });
}

// Enviar error solo al que falló (privado)
await Clients.Caller.SendAsync("ActionError", 
    new ActionErrorDto { Message = "No es tu turno" });

// Enviar resultado de canje a todos (público)
await Clients.Group($"game-{gameId}").SendAsync("CardsTraded",
    new CardsTradedDto { PlayerName = "Carlos", ArmiesReceived = 8 });
```

---

## 7. Flujo de un Ataque Completo (Diagrama de Secuencia Detallado)

```
  Atacante (Carlos)          GameHub              GameEngine         Defensor (Ana)       Espectador (Luis)
  ─────────────────          ───────              ──────────         ──────────────       ─────────────────
         │                      │                      │                   │                      │
  1. Click territorio           │                      │                   │                      │
     propio (Brasil)            │                      │                   │                      │
         │                      │                      │                   │                      │
  2. Click territorio           │                      │                   │                      │
     enemigo (N. África)        │                      │                   │                      │
         │                      │                      │                   │                      │
  3. Elige 3 dados              │                      │                   │                      │
     pulsa "Atacar"             │                      │                   │                      │
         │                      │                      │                   │                      │
  4. hub.SendAsync   ─────────▶│                      │                   │                      │
     ("Attack",                 │                      │                   │                      │
      gameId,                   │  5. Validar:         │                   │                      │
      playerId,                 │     ¿Es su turno?    │                   │                      │
      "brazil",                 │     ¿Fase de ataque? │                   │                      │
      "north-africa",           │     ¿Territorio suyo?│                   │                      │
      3)                        │     ¿Adyacentes?     │                   │                      │
         │                      │     ¿Sufic. ejérc.?  │                   │                      │
         │                      │          │           │                   │                      │
         │                      │  6.      │           │                   │                      │
         │                      │──────────┼──────────▶│                   │                      │
         │                      │          │  ResolveAttack()              │                      │
         │                      │          │  - Roll attacker: [6,3,2]    │                      │
         │                      │          │  - Roll defender: [5,4]      │                      │
         │                      │          │  - Compare: 6>5, 3<4         │                      │
         │                      │          │  - Result: 1 loss each       │                      │
         │                      │◀─────────┼──────────│                   │                      │
         │                      │          │           │                   │                      │
         │                      │  7. Actualizar estado de la partida     │                      │
         │                      │     Brazil: 7→6 ejércitos               │                      │
         │                      │     N.África: 3→2 ejércitos             │                      │
         │                      │          │           │                   │                      │
         │                      │  8. Enviar a GRUPO:  │                   │                      │
         │                      │     "DiceRolled"     │                   │                      │
         │◀─── DiceRolled ─────│──────────┼───────────┼── DiceRolled ───▶│◀── DiceRolled ──────│
         │     [6,3,2]          │          │           │   [6,3,2]        │    [6,3,2]           │
         │     vs [5,4]         │          │           │   vs [5,4]       │    vs [5,4]          │
         │                      │          │           │                   │                      │
         │                      │  9. Enviar a GRUPO:  │                   │                      │
         │                      │     "GameStateUpdated"│                  │                      │
         │◀─── StateUpdated ───│──────────┼───────────┼── StateUpdated ─▶│◀── StateUpdated ────│
         │                      │          │           │                   │                      │
  10. Muestra animación         │          │           │   10. Muestra     │   10. Muestra        │
      de dados                  │          │           │       animación   │       animación      │
  11. Actualiza mapa            │          │           │   11. Actualiza   │   11. Actualiza      │
         │                      │          │           │       mapa        │       mapa           │
```

---

## 8. Gestión de Conexión/Desconexión en el Hub

### 8.1 OnConnectedAsync / OnDisconnectedAsync

```csharp
public override async Task OnConnectedAsync()
{
    // El ConnectionId está disponible en Context.ConnectionId.
    // En este punto, el jugador aún no se ha identificado.
    // Solo registramos la conexión.
    _logger.LogInformation("Nueva conexión al GameHub: {ConnectionId}", Context.ConnectionId);
    await base.OnConnectedAsync();
}

public override async Task OnDisconnectedAsync(Exception? exception)
{
    var connectionId = Context.ConnectionId;
    _logger.LogInformation("Desconexión del GameHub: {ConnectionId}", connectionId);

    // Buscar si este ConnectionId pertenece a algún jugador en partida
    var playerInfo = _gameManager.FindPlayerByConnectionId(connectionId);

    if (playerInfo != null)
    {
        // Marcar al jugador como desconectado
        _gameManager.SetPlayerDisconnected(playerInfo.PlayerId);

        // Notificar al grupo
        if (playerInfo.GameId != null)
        {
            await Clients.Group($"game-{playerInfo.GameId}").SendAsync(
                "PlayerDisconnected",
                new PlayerDisconnectedDto
                {
                    PlayerId = playerInfo.PlayerId,
                    PlayerName = playerInfo.PlayerName
                }
            );

            // Si era el turno del jugador desconectado, 
            // iniciar temporizador de 60s para saltar turno
            var game = _gameManager.GetGame(playerInfo.GameId);
            if (game != null && game.CurrentPlayerId == playerInfo.PlayerId)
            {
                _gameManager.StartDisconnectionTimer(playerInfo.GameId, playerInfo.PlayerId);
            }
        }
    }

    await base.OnDisconnectedAsync(exception);
}
```

### 8.2 Diagrama de Desconexión y Reconexión

```
  Jugador "Carlos" pierde WiFi
         │
         ▼
  ┌──────────────────────────┐
  │ OnDisconnectedAsync()    │
  │ - ConnectionId = "abc"   │
  │ - Buscar jugador por     │
  │   ConnectionId           │
  │ - Marcar desconectado    │
  └──────────┬───────────────┘
             │
             ▼
  ┌──────────────────────────┐
  │ Notificar al grupo:      │     "PlayerDisconnected"
  │ "Carlos se desconectó"   │     → Ana, Luis ven indicador ⚠️
  └──────────┬───────────────┘
             │
             │ ¿Es el turno de Carlos?
             │
      ┌──────┴──────────┐
      │ SÍ              │ NO
      ▼                 ▼
  Iniciar timer     (esperar sin
  de 60 segundos    hacer nada)
      │
      │ ¿Carlos reconecta?
      │
  ┌───┴───────────────┐
  │ SÍ (< 60s)        │ NO (≥ 60s)
  ▼                    ▼
  ┌──────────────┐  ┌──────────────────┐
  │ RejoinGame() │  │ Saltar turno     │
  │ - Nuevo      │  │ de Carlos        │
  │   ConnectionId│  │ - Pasar al       │
  │ - Añadir al  │  │   siguiente      │
  │   grupo      │  │   jugador        │
  │ - Notificar  │  │                  │
  │   grupo      │  │ Si Carlos no     │
  │              │  │ vuelve en 5 min: │
  │              │  │ territorios      │
  │              │  │ pasan a neutral  │
  └──────────────┘  └──────────────────┘
```

---

## 9. Conexión desde el Cliente (Componente Blazor)

### 9.1 Configuración de HubConnection en Game.razor

```csharp
@implements IAsyncDisposable
@inject NavigationManager Navigation
@inject IPlayerSessionService PlayerSession

@code {
    private HubConnection? _hubConnection;
    private bool _isReconnecting;
    private bool _isDisconnected;

    protected override async Task OnInitializedAsync()
    {
        // ═══════════════════════════════════════
        // 1. CREAR LA CONEXIÓN AL HUB
        // ═══════════════════════════════════════
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/gamehub"))
            .WithAutomaticReconnect(new[] 
            {
                TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(60)
            })
            .Build();

        // ═══════════════════════════════════════
        // 2. REGISTRAR HANDLERS DE EVENTOS
        // ═══════════════════════════════════════

        // Estado del juego actualizado
        _hubConnection.On<GameStateDto>("GameStateUpdated", async (state) =>
        {
            _gameState = state;
            await InvokeAsync(StateHasChanged);
        });

        // Resultado de dados
        _hubConnection.On<DiceResultDto>("DiceRolled", async (result) =>
        {
            _lastDiceResult = result;
            _showDiceAnimation = true;
            await InvokeAsync(StateHasChanged);
        });

        // Cambio de turno
        _hubConnection.On<TurnChangedDto>("TurnChanged", async (turn) =>
        {
            _currentTurn = turn;
            await InvokeAsync(StateHasChanged);
        });

        // Territorio conquistado
        _hubConnection.On<TerritoryConqueredDto>("TerritoryConquered", async (conquered) =>
        {
            // Animación de conquista
            _conqueredTerritory = conquered;
            await InvokeAsync(StateHasChanged);
        });

        // Jugador eliminado
        _hubConnection.On<PlayerEliminatedDto>("PlayerEliminated", async (eliminated) =>
        {
            _eliminationEvent = eliminated;
            await InvokeAsync(StateHasChanged);
        });

        // Chat
        _hubConnection.On<ChatMessageDto>("ChatMessageReceived", async (msg) =>
        {
            _chatMessages.Add(msg);
            await InvokeAsync(StateHasChanged);
        });

        // Cartas propias actualizadas
        _hubConnection.On<CardsUpdatedDto>("CardsUpdated", async (cards) =>
        {
            _myCards = cards.Cards;
            await InvokeAsync(StateHasChanged);
        });

        // Error de acción
        _hubConnection.On<ActionErrorDto>("ActionError", async (error) =>
        {
            _errorMessage = error.Message;
            await InvokeAsync(StateHasChanged);
        });

        // Fin de partida
        _hubConnection.On<GameOverDto>("GameOver", async (gameOver) =>
        {
            _gameOverResult = gameOver;
            _showVictoryScreen = true;
            await InvokeAsync(StateHasChanged);
        });

        // Conexión/desconexión de jugadores
        _hubConnection.On<PlayerDisconnectedDto>("PlayerDisconnected", async (p) =>
        {
            _systemMessages.Add($"⚠️ {p.PlayerName} se ha desconectado");
            await InvokeAsync(StateHasChanged);
        });

        _hubConnection.On<PlayerReconnectedDto>("PlayerReconnected", async (p) =>
        {
            _systemMessages.Add($"✅ {p.PlayerName} se ha reconectado");
            await InvokeAsync(StateHasChanged);
        });

        // ═══════════════════════════════════════
        // 3. EVENTOS DE ESTADO DE LA CONEXIÓN
        // ═══════════════════════════════════════
        _hubConnection.Reconnecting += (error) =>
        {
            _isReconnecting = true;
            InvokeAsync(StateHasChanged);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += async (connectionId) =>
        {
            _isReconnecting = false;
            await _hubConnection.SendAsync("RejoinGame", GameId, PlayerSession.PlayerId);
            await InvokeAsync(StateHasChanged);
        };

        _hubConnection.Closed += (error) =>
        {
            _isDisconnected = true;
            InvokeAsync(StateHasChanged);
            return Task.CompletedTask;
        };

        // ═══════════════════════════════════════
        // 4. INICIAR LA CONEXIÓN
        // ═══════════════════════════════════════
        await _hubConnection.StartAsync();

        // ═══════════════════════════════════════
        // 5. UNIRSE A LA PARTIDA
        // ═══════════════════════════════════════
        await _hubConnection.SendAsync("JoinGame", GameId, 
            PlayerSession.PlayerId, PlayerSession.PlayerName);
    }

    // ═══════════════════════════════════════
    // MÉTODOS DE ACCIÓN (invocan al hub)
    // ═══════════════════════════════════════

    private async Task HandleAttack(string from, string to, int diceCount)
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.SendAsync("Attack", GameId, 
                PlayerSession.PlayerId, from, to, diceCount);
        }
    }

    private async Task HandleFortify(string from, string to, int count)
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.SendAsync("Fortify", GameId, 
                PlayerSession.PlayerId, from, to, count);
        }
    }

    private async Task HandleSendChat(string message)
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.SendAsync("SendChatMessage", GameId, 
                PlayerSession.PlayerId, message);
        }
    }

    // ═══════════════════════════════════════
    // LIMPIEZA AL SALIR
    // ═══════════════════════════════════════

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.SendAsync("LeaveGame", GameId, PlayerSession.PlayerId);
            await _hubConnection.DisposeAsync();
        }
    }
}
```

---

## 10. Serialización

### 10.1 Formato: JSON

SignalR usa **JSON** como formato de serialización por defecto con `System.Text.Json`. Es adecuado para MiniRisk:

| Aspecto | Valor |
|---------|-------|
| **Formato** | JSON (por defecto en SignalR) |
| **Serializador** | `System.Text.Json` |
| **Tamaño típico de GameStateDto** | ~2-5 KB (42 territorios + 6 jugadores + eventos recientes) |
| **Latencia de serialización** | Despreciable en LAN (~1ms) |

### 10.2 Configuración del Serializador

```csharp
// En Program.cs
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = 
            JsonNamingPolicy.CamelCase;
        options.PayloadSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });
```

### 10.3 Alternativa Descartada: MessagePack

MessagePack es un protocolo binario más eficiente que JSON. Se descarta porque:

- La diferencia de rendimiento es irrelevante para 2-6 jugadores en LAN.
- JSON es más fácil de depurar (se puede leer en las Developer Tools del navegador).
- No requiere NuGet adicional (`Microsoft.AspNetCore.SignalR.Protocols.MessagePack`).

---

## 11. Mapeo de Acciones del Juego a Métodos del Hub

### 11.1 Tabla Completa de Mapeo

| Acción del jugador en la UI | Método del Hub invocado | Eventos del Hub enviados | Destino |
|-----------------------------|------------------------|------------------------|---------|
| Crear partida (lobby) | — (via GameManager directo) | — | — |
| Unirse a partida | `JoinGame` | `PlayerJoined`, `GameStateUpdated` | Grupo + Caller |
| Abandonar partida | `LeaveGame` | `PlayerLeft`, `GameStateUpdated` | Grupo |
| Indicar "Listo" (lobby) | `PlayerReady` | `GameStateUpdated` | Grupo |
| Iniciar partida | `StartGame` | `GameStateUpdated`, `TurnChanged` | Grupo |
| Colocar ejércitos iniciales | `PlaceInitialArmies` | `GameStateUpdated` | Grupo |
| Colocar refuerzos | `PlaceReinforcements` | `GameStateUpdated` | Grupo |
| Confirmar refuerzos | `ConfirmReinforcements` | `PhaseChanged`, `GameStateUpdated` | Grupo |
| Atacar | `Attack` | `DiceRolled`, `GameStateUpdated`, ¿`TerritoryConquered`? | Grupo |
| Mover tras conquista | `MoveArmiesAfterConquest` | `GameStateUpdated` | Grupo |
| Terminar fase de ataque | `EndAttackPhase` | `PhaseChanged`, `GameStateUpdated` | Grupo |
| Fortificar | `Fortify` | `GameStateUpdated`, `TurnChanged` | Grupo |
| No fortificar | `SkipFortification` | `TurnChanged`, `GameStateUpdated` | Grupo |
| Canjear cartas | `TradeCards` | `CardsTraded`, `CardsUpdated`, `GameStateUpdated` | Grupo + Caller |
| Enviar chat | `SendChatMessage` | `ChatMessageReceived` | Grupo |
| — (jugador conquistado) | — (automático) | `PlayerEliminated`, `GameStateUpdated` | Grupo |
| — (jugador desconectado) | `OnDisconnectedAsync` | `PlayerDisconnected` | Grupo |
| — (jugador reconectado) | `RejoinGame` | `PlayerReconnected`, `GameStateUpdated` | Grupo + Caller |
| — (alguien gana) | — (automático) | `GameOver` | Grupo |

---

## 12. Manejo de Errores y Validación en el Hub

### 12.1 Estrategia de Validación

Cada método del hub **valida la acción antes de ejecutarla**. Si la validación falla, se envía un `ActionError` solo al jugador que intentó la acción:

```csharp
public async Task Attack(string gameId, string playerId, 
    string fromTerritoryId, string toTerritoryId, int diceCount)
{
    // ═══ VALIDACIÓN ═══
    var game = _gameManager.GetGame(gameId);
    
    if (game == null)
        return await SendError("La partida no existe", "Attack");

    if (game.CurrentPlayerId != playerId)
        return await SendError("No es tu turno", "Attack");

    if (game.Phase != GamePhase.Attack)
        return await SendError("No estás en la fase de ataque", "Attack");

    var fromTerritory = game.GetTerritory(fromTerritoryId);
    if (fromTerritory?.OwnerId != playerId)
        return await SendError("No controlas ese territorio", "Attack");

    if (fromTerritory.Armies < 2)
        return await SendError("Necesitas al menos 2 ejércitos para atacar", "Attack");

    var toTerritory = game.GetTerritory(toTerritoryId);
    if (toTerritory?.OwnerId == playerId)
        return await SendError("No puedes atacar tus propios territorios", "Attack");

    if (!_gameEngine.AreAdjacent(fromTerritoryId, toTerritoryId))
        return await SendError("Los territorios no son adyacentes", "Attack");

    if (diceCount < 1 || diceCount > Math.Min(3, fromTerritory.Armies - 1))
        return await SendError("Número de dados inválido", "Attack");

    // ═══ EJECUCIÓN ═══
    var result = _gameEngine.ResolveAttack(game, fromTerritoryId, toTerritoryId, diceCount);
    _gameManager.ApplyAttackResult(gameId, result);

    // ═══ NOTIFICACIÓN ═══
    await Clients.Group($"game-{gameId}").SendAsync("DiceRolled", result.ToDiceResultDto());
    
    if (result.TerritoryConquered)
    {
        await Clients.Group($"game-{gameId}").SendAsync("TerritoryConquered", 
            result.ToConqueredDto());
    }

    await Clients.Group($"game-{gameId}").SendAsync("GameStateUpdated", 
        _gameManager.GetGameState(gameId));
}

// Helper para enviar errores al caller
private async Task SendError(string message, string action)
{
    await Clients.Caller.SendAsync("ActionError", new ActionErrorDto
    {
        Message = message,
        ActionAttempted = action
    });
}
```

### 12.2 Categorías de Error

| Categoría | Ejemplo | Acción |
|-----------|---------|--------|
| **Partida no encontrada** | "La partida no existe" | Redirigir al lobby |
| **Turno incorrecto** | "No es tu turno" | Mostrar toast de aviso |
| **Fase incorrecta** | "No estás en la fase de ataque" | Mostrar toast de aviso |
| **Acción inválida** | "Los territorios no son adyacentes" | Resaltar error en la UI |
| **Parámetros incorrectos** | "Número de dados inválido" | Mostrar mensaje en el diálogo |
| **Partida llena** | "La partida ya tiene 6 jugadores" | Mostrar error en lobby |

---

## 13. Configuración de SignalR en Program.cs

```csharp
// ═══════════════════════════════════════════════════════════
// Program.cs — Configuración de SignalR
// ═══════════════════════════════════════════════════════════

// 1. Registrar los servicios de SignalR
builder.Services.AddSignalR(options =>
{
    // Tiempo máximo sin actividad antes de cerrar la conexión
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    
    // Tiempo que el servidor espera una respuesta del cliente
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    
    // Tamaño máximo de un mensaje (suficiente para GameStateDto)
    options.MaximumReceiveMessageSize = 64 * 1024; // 64 KB
    
    // Habilitar logs detallados en desarrollo
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
})
.AddJsonProtocol(options =>
{
    // Usar camelCase en las propiedades JSON
    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    
    // Serializar enums como strings legibles
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// ...

// 2. Mapear el endpoint del hub
app.MapHub<GameHub>("/gamehub");
```

---

## 14. Resumen de Decisiones

| Decisión | Elección | Alternativa descartada |
|----------|---------|----------------------|
| **Protocolo de comunicación** | SignalR | WebSocket directo (más bajo nivel) |
| **Formato de serialización** | JSON (`System.Text.Json`) | MessagePack (innecesario para LAN) |
| **Agrupación** | Un grupo por partida (`game-{id}`) | Sin grupos (broadcast global) |
| **Hub compartido vs múltiples hubs** | Un solo `GameHub` | Hubs separados para chat, juego, etc. (sobreingeniería) |
| **Info privada (cartas)** | `Clients.Client(connectionId)` | Enviar todo al grupo y filtrar en cliente (inseguro) |
| **Reconexión** | `WithAutomaticReconnect` con backoff | Manual con polling |
| **Validación** | Servidor valida todo | Validación solo en cliente (manipulable) |
| **Estado completo vs delta** | Se envía `GameStateDto` completo tras cada acción | Solo deltas (más complejo, propenso a desincronización) |

---

> **Siguiente documento:** [05 — Modelo de Dominio](./05_Modelo_Dominio.md)
