# 05 — Modelo de Dominio

> **Documento:** 05 de 14  
> **Versión:** 1.0  
> **Última actualización:** 22 de febrero de 2026  
> **Volver al índice:** [00_Indice.md](./00_Indice.md)  
> **Documento anterior:** [04 — Comunicación en Tiempo Real — SignalR](./04_SignalR.md)

---

## 1. Visión General del Modelo de Dominio

El modelo de dominio de MiniRisk representa todas las entidades, relaciones y reglas de negocio necesarias para simular una partida de Risk clásico. Al no existir base de datos, todas las entidades viven **exclusivamente en memoria** dentro del servidor.

### 1.1 Principios de Diseño

| Principio | Aplicación |
|-----------|-----------|
| **Rich Domain Model** | Las entidades contienen lógica de negocio relevante, no son simples DTOs |
| **Inmutabilidad donde sea posible** | Las enumeraciones y datos estáticos (mapa, adyacencias) son inmutables |
| **Encapsulación** | El estado interno de las entidades se modifica a través de métodos con validación |
| **Separación de responsabilidades** | Las entidades gestionan su propio estado; la orquestación la realiza `GameEngine` |
| **Código en inglés** | Nombres de clases, propiedades y métodos en inglés; documentación en español |

### 1.2 Ubicación en el Proyecto

```
MiniRisk/
├── Models/
│   ├── Game.cs
│   ├── Player.cs
│   ├── Territory.cs
│   ├── Continent.cs
│   ├── Card.cs
│   ├── AttackResult.cs
│   ├── GameEvent.cs
│   ├── PlayerSession.cs
│   ├── GameSettings.cs
│   ├── GameSummary.cs
│   └── Enums/
│       ├── GamePhase.cs
│       ├── GameStatus.cs
│       ├── CardType.cs
│       ├── TerritoryName.cs
│       ├── ContinentName.cs
│       ├── PlayerColor.cs
│       └── GameEventType.cs
```

---

## 2. Diagrama de Clases

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        MODELO DE DOMINIO                                │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────┐           │
│  │                         Game                             │           │
│  │──────────────────────────────────────────────────────────│           │
│  │ + Id: string                                             │           │
│  │ + Name: string                                           │           │
│  │ + Status: GameStatus                                     │           │
│  │ + Phase: GamePhase                                       │           │
│  │ + CurrentPlayerIndex: int                                │           │
│  │ + TurnNumber: int                                        │           │
│  │ + TradeCount: int                                        │           │
│  │ + RemainingReinforcements: int                           │           │
│  │ + ConqueredThisTurn: bool                                │           │
│  │ + Settings: GameSettings                                 │           │
│  │ + CreatedAt: DateTime                                    │           │
│  │ + StartedAt: DateTime?                                   │           │
│  │ + FinishedAt: DateTime?                                  │           │
│  │ + CreatorPlayerId: string                                │           │
│  │──────────────────────────────────────────────────────────│           │
│  │ + Players: List<Player>                          1──*    │───┐       │
│  │ + Territories: Dictionary<TerritoryName, Territory> 1─42 │───┤       │
│  │ + Continents: Dictionary<ContinentName, Continent>  1─6  │───┤       │
│  │ + CardDeck: Queue<Card>                          1──*    │───┤       │
│  │ + DiscardPile: List<Card>                                │   │       │
│  │ + EventLog: List<GameEvent>                      1──*    │───┤       │
│  │──────────────────────────────────────────────────────────│   │       │
│  │ + GetCurrentPlayer(): Player                             │   │       │
│  │ + GetPlayerById(id): Player?                             │   │       │
│  │ + AddPlayer(player): void                                │   │       │
│  │ + RemovePlayer(playerId): void                           │   │       │
│  │ + AdvanceTurn(): void                                    │   │       │
│  │ + AdvancePhase(): void                                   │   │       │
│  │ + DrawCard(): Card?                                      │   │       │
│  │ + AddEvent(event): void                                  │   │       │
│  │ + IsFinished(): bool                                     │   │       │
│  └──────────────────────────────────────────────────────────┘   │       │
│       │          │              │              │                │       │
│       │          │              │              │                │       │
│       ▼          ▼              ▼              ▼                │       │
│  ┌──────────┐ ┌───────────┐ ┌──────────┐ ┌──────────┐         │       │
│  │  Player  │ │ Territory │ │Continent │ │   Card   │         │       │
│  │──────────│ │───────────│ │──────────│ │──────────│         │       │
│  │Id        │ │Name       │ │Name      │ │Id        │         │       │
│  │Name      │ │Continent  │ │Bonus     │ │Type      │         │       │
│  │Color     │ │OwnerId    │ │Territories│ │Territory │         │       │
│  │Cards     │ │Armies     │ │          │ │          │         │       │
│  │IsElimin. │ │Adjacencies│ │          │ │          │         │       │
│  │IsConnect.│ │           │ │          │ │          │         │       │
│  └──────────┘ └───────────┘ └──────────┘ └──────────┘         │       │
│                                                                │       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐     │       │
│  │ AttackResult │  │  GameEvent   │  │   GameSettings   │     │       │
│  │──────────────│  │──────────────│  │──────────────────│     │       │
│  │AttackerDice  │  │Type          │  │MaxPlayers        │     │       │
│  │DefenderDice  │  │Message       │  │DistributionMode  │     │       │
│  │AttackerLoss  │  │PlayerId      │  │                  │     │       │
│  │DefenderLoss  │  │Timestamp     │  │                  │     │       │
│  │IsConquest    │  │              │  │                  │     │       │
│  └──────────────┘  └──────────────┘  └──────────────────┘     │       │
│                                                                │       │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Entidades Principales

### 3.1 Game (Partida)

La entidad raíz del modelo. Representa una partida completa de Risk con todo su estado.

```csharp
public class Game
{
    // ═══════════════════════════════════════
    // IDENTIFICACIÓN
    // ═══════════════════════════════════════
    
    /// <summary>
    /// Identificador único de la partida (GUID).
    /// Se genera al crear la partida.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Nombre descriptivo de la partida (ej: "Partida de los viernes").
    /// Lo elige el creador de la partida.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    // ═══════════════════════════════════════
    // ESTADO DE LA PARTIDA
    // ═══════════════════════════════════════
    
    /// <summary>
    /// Estado general de la partida: Waiting (en lobby), Playing, Finished.
    /// </summary>
    public GameStatus Status { get; set; } = GameStatus.WaitingForPlayers;
    
    /// <summary>
    /// Fase actual del turno: Setup, Reinforcement, Attack, Fortification.
    /// </summary>
    public GamePhase Phase { get; set; } = GamePhase.Setup;
    
    /// <summary>
    /// Índice del jugador actual en la lista Players.
    /// </summary>
    public int CurrentPlayerIndex { get; set; }
    
    /// <summary>
    /// Número del turno actual (comienza en 1).
    /// </summary>
    public int TurnNumber { get; set; }
    
    /// <summary>
    /// Número global de canjes de cartas realizados en la partida.
    /// Determina cuántos ejércitos otorga el próximo canje.
    /// </summary>
    public int TradeCount { get; set; }
    
    /// <summary>
    /// Ejércitos de refuerzo pendientes de colocar por el jugador actual.
    /// Se reduce conforme el jugador coloca ejércitos.
    /// </summary>
    public int RemainingReinforcements { get; set; }
    
    /// <summary>
    /// Indica si el jugador actual conquistó al menos un territorio en este turno.
    /// Determina si recibe una carta al final del turno.
    /// </summary>
    public bool ConqueredThisTurn { get; set; }
    
    // ═══════════════════════════════════════
    // CONFIGURACIÓN
    // ═══════════════════════════════════════
    
    /// <summary>
    /// Configuración de la partida (número máximo de jugadores, modo de distribución).
    /// </summary>
    public GameSettings Settings { get; set; } = new();
    
    /// <summary>
    /// ID del jugador que creó la partida. Solo él puede iniciarla.
    /// </summary>
    public string CreatorPlayerId { get; set; } = string.Empty;
    
    // ═══════════════════════════════════════
    // TIMESTAMPS
    // ═══════════════════════════════════════
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    
    // ═══════════════════════════════════════
    // COLECCIONES
    // ═══════════════════════════════════════
    
    /// <summary>
    /// Jugadores de la partida, ordenados por turno.
    /// </summary>
    public List<Player> Players { get; set; } = [];
    
    /// <summary>
    /// Los 42 territorios del mapa, indexados por nombre.
    /// </summary>
    public Dictionary<TerritoryName, Territory> Territories { get; set; } = new();
    
    /// <summary>
    /// Los 6 continentes con sus bonificaciones.
    /// </summary>
    public Dictionary<ContinentName, Continent> Continents { get; set; } = new();
    
    /// <summary>
    /// Mazo de cartas disponibles para robar.
    /// </summary>
    public Queue<Card> CardDeck { get; set; } = new();
    
    /// <summary>
    /// Cartas descartadas tras un canje.
    /// </summary>
    public List<Card> DiscardPile { get; set; } = [];
    
    /// <summary>
    /// Registro cronológico de todos los eventos de la partida.
    /// </summary>
    public List<GameEvent> EventLog { get; set; } = [];
    
    // ═══════════════════════════════════════
    // MÉTODOS
    // ═══════════════════════════════════════
    
    /// <summary>
    /// Obtiene el jugador cuyo turno es el actual.
    /// </summary>
    public Player GetCurrentPlayer() => Players[CurrentPlayerIndex];
    
    /// <summary>
    /// Busca un jugador por su ID. Retorna null si no existe.
    /// </summary>
    public Player? GetPlayerById(string playerId)
        => Players.FirstOrDefault(p => p.Id == playerId);
    
    /// <summary>
    /// Obtiene solo los jugadores activos (no eliminados).
    /// </summary>
    public IEnumerable<Player> GetActivePlayers()
        => Players.Where(p => !p.IsEliminated);
    
    /// <summary>
    /// Avanza al siguiente turno (siguiente jugador activo).
    /// Resetea las flags del turno.
    /// </summary>
    public void AdvanceTurn()
    {
        do
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
        }
        while (Players[CurrentPlayerIndex].IsEliminated);
        
        TurnNumber++;
        ConqueredThisTurn = false;
        Phase = GamePhase.Reinforcement;
    }
    
    /// <summary>
    /// Avanza a la siguiente fase del turno actual.
    /// Reinforcement → Attack → Fortification → (siguiente turno)
    /// </summary>
    public void AdvancePhase()
    {
        Phase = Phase switch
        {
            GamePhase.Reinforcement => GamePhase.Attack,
            GamePhase.Attack => GamePhase.Fortification,
            GamePhase.Fortification => GamePhase.Reinforcement, // AdvanceTurn lo gestiona
            _ => Phase
        };
    }
    
    /// <summary>
    /// Roba una carta del mazo. Si el mazo está vacío, baraja el descarte.
    /// </summary>
    public Card? DrawCard()
    {
        if (CardDeck.Count == 0 && DiscardPile.Count > 0)
        {
            ShuffleDiscardIntoDeck();
        }
        return CardDeck.Count > 0 ? CardDeck.Dequeue() : null;
    }
    
    /// <summary>
    /// Registra un evento en el log de la partida.
    /// </summary>
    public void AddEvent(GameEvent gameEvent)
    {
        EventLog.Add(gameEvent);
    }
    
    /// <summary>
    /// Verifica si la partida ha terminado (un solo jugador activo).
    /// </summary>
    public bool IsFinished()
    {
        return Status == GameStatus.Playing 
            && GetActivePlayers().Count() == 1;
    }
    
    private void ShuffleDiscardIntoDeck()
    {
        var random = new Random();
        var shuffled = DiscardPile.OrderBy(_ => random.Next()).ToList();
        DiscardPile.Clear();
        CardDeck = new Queue<Card>(shuffled);
    }
}
```

#### Diagrama de estados de `Game`

```
┌──────────────────┐
│ WaitingForPlayers │──── Creador pulsa "Iniciar" ────┐
│ (en el lobby)    │     (min. 2 jugadores)           │
└──────────────────┘                                  │
                                                      ▼
                                              ┌──────────────┐
                                              │   Playing    │
                                              │ (en curso)   │
                                              └──────┬───────┘
                                                     │
                                    ┌────────────────┼────────────────┐
                                    │                │                │
                              Un jugador       Todos los         Todos los
                              controla los     jugadores         jugadores
                              42 territorios   abandonan         se desconectan
                                    │                │                │
                                    ▼                ▼                ▼
                              ┌──────────────────────────────────────────┐
                              │             Finished                     │
                              │  WinnerId set (o null si abandono total) │
                              └──────────────────────────────────────────┘
```

---

### 3.2 Player (Jugador)

Representa a un jugador humano dentro de una partida.

```csharp
public class Player
{
    /// <summary>
    /// Identificador único del jugador (GUID generado al identificarse).
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Nombre del jugador, introducido en la pantalla de bienvenida.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Color asignado al jugador al unirse a la partida.
    /// Determina el color de sus territorios en el mapa.
    /// </summary>
    public PlayerColor Color { get; set; }
    
    /// <summary>
    /// Cartas de territorio que el jugador tiene en la mano.
    /// Información privada: solo visible para el propio jugador.
    /// </summary>
    public List<Card> Cards { get; set; } = [];
    
    /// <summary>
    /// Indica si el jugador ha sido eliminado (perdió todos sus territorios).
    /// Un jugador eliminado no puede actuar pero sigue en la lista de Players.
    /// </summary>
    public bool IsEliminated { get; set; }
    
    /// <summary>
    /// Indica si el jugador está actualmente conectado vía SignalR.
    /// </summary>
    public bool IsConnected { get; set; } = true;
    
    /// <summary>
    /// ConnectionId de SignalR actual del jugador.
    /// Se actualiza en las reconexiones.
    /// </summary>
    public string? ConnectionId { get; set; }
    
    /// <summary>
    /// Momento en que el jugador se desconectó (para gestionar timeout).
    /// Null si está conectado.
    /// </summary>
    public DateTime? DisconnectedAt { get; set; }
    
    /// <summary>
    /// Ejércitos iniciales pendientes de colocar (solo durante fase Setup).
    /// </summary>
    public int InitialArmiesRemaining { get; set; }
    
    // ═══════════════════════════════════════
    // MÉTODOS
    // ═══════════════════════════════════════
    
    /// <summary>
    /// Obtiene los territorios que este jugador controla en la partida dada.
    /// </summary>
    public IEnumerable<Territory> GetOwnedTerritories(Game game)
        => game.Territories.Values.Where(t => t.OwnerId == Id);
    
    /// <summary>
    /// Cuenta el total de ejércitos del jugador en todos sus territorios.
    /// </summary>
    public int GetTotalArmies(Game game)
        => GetOwnedTerritories(game).Sum(t => t.Armies);
    
    /// <summary>
    /// Verifica si el jugador controla todos los territorios de un continente.
    /// </summary>
    public bool ControlsContinent(Game game, Continent continent)
        => continent.Territories.All(t => game.Territories[t].OwnerId == Id);
    
    /// <summary>
    /// Añade una carta a la mano del jugador.
    /// </summary>
    public void AddCard(Card card)
    {
        Cards.Add(card);
    }
    
    /// <summary>
    /// Elimina las cartas especificadas de la mano del jugador.
    /// </summary>
    public void RemoveCards(IEnumerable<Card> cards)
    {
        foreach (var card in cards)
        {
            Cards.Remove(card);
        }
    }
    
    /// <summary>
    /// Transfiere todas las cartas de este jugador a otro (al ser eliminado).
    /// </summary>
    public List<Card> SurrenderAllCards()
    {
        var surrendered = new List<Card>(Cards);
        Cards.Clear();
        return surrendered;
    }
}
```

---

### 3.3 Territory (Territorio)

Representa una de las 42 regiones del mapa del mundo.

```csharp
public class Territory
{
    /// <summary>
    /// Nombre del territorio (enum TerritoryName).
    /// Actúa como identificador único e inmutable.
    /// </summary>
    public TerritoryName Name { get; set; }
    
    /// <summary>
    /// Continente al que pertenece este territorio.
    /// </summary>
    public ContinentName Continent { get; set; }
    
    /// <summary>
    /// ID del jugador que controla este territorio.
    /// Nunca es null durante una partida en curso (siempre hay un propietario).
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;
    
    /// <summary>
    /// Número de ejércitos desplegados en este territorio.
    /// Invariante: siempre >= 1 durante la partida.
    /// </summary>
    public int Armies { get; set; } = 1;
    
    /// <summary>
    /// Lista de territorios adyacentes (conexiones).
    /// Se inicializa una sola vez por el MapService y no cambia.
    /// </summary>
    public List<TerritoryName> AdjacentTerritories { get; set; } = [];
    
    // ═══════════════════════════════════════
    // MÉTODOS
    // ═══════════════════════════════════════
    
    /// <summary>
    /// Verifica si este territorio es adyacente a otro.
    /// </summary>
    public bool IsAdjacentTo(TerritoryName other)
        => AdjacentTerritories.Contains(other);
    
    /// <summary>
    /// Verifica si este territorio puede atacar (tiene al menos 2 ejércitos).
    /// </summary>
    public bool CanAttackFrom() => Armies >= 2;
    
    /// <summary>
    /// Añade ejércitos al territorio.
    /// </summary>
    public void AddArmies(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be positive.", nameof(count));
        Armies += count;
    }
    
    /// <summary>
    /// Remueve ejércitos del territorio.
    /// Lanza excepción si quedaría por debajo de 1.
    /// </summary>
    public void RemoveArmies(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be positive.", nameof(count));
        if (Armies - count < 1)
            throw new InvalidOperationException(
                $"Cannot remove {count} armies from {Name}. Would leave {Armies - count} (min 1).");
        Armies -= count;
    }
    
    /// <summary>
    /// Elimina TODOS los ejércitos (usado cuando un territorio es conquistado).
    /// </summary>
    public void RemoveAllArmies()
    {
        Armies = 0;
    }
    
    /// <summary>
    /// Transfiere la propiedad del territorio a otro jugador.
    /// </summary>
    public void SetOwner(string newOwnerId, int armies)
    {
        OwnerId = newOwnerId;
        Armies = armies;
    }
}
```

---

### 3.4 Continent (Continente)

Agrupación de territorios con bonificación de ejércitos.

```csharp
public class Continent
{
    /// <summary>
    /// Nombre del continente (enum ContinentName).
    /// </summary>
    public ContinentName Name { get; set; }
    
    /// <summary>
    /// Ejércitos de bonificación por controlar todos los territorios del continente.
    /// </summary>
    public int BonusArmies { get; set; }
    
    /// <summary>
    /// Lista de territorios que componen este continente.
    /// Inmutable tras la inicialización.
    /// </summary>
    public List<TerritoryName> Territories { get; set; } = [];
    
    /// <summary>
    /// Verifica si un jugador controla todos los territorios del continente.
    /// </summary>
    public bool IsControlledBy(string playerId, Dictionary<TerritoryName, Territory> allTerritories)
        => Territories.All(t => allTerritories[t].OwnerId == playerId);
}
```

**Datos de los continentes:**

| Continente | `ContinentName` | Territorios | Bonificación |
|:----------:|:---------------:|:-----------:|:------------:|
| América del Norte | `NorthAmerica` | 9 | +5 |
| América del Sur | `SouthAmerica` | 4 | +2 |
| Europa | `Europe` | 7 | +5 |
| África | `Africa` | 6 | +3 |
| Asia | `Asia` | 12 | +7 |
| Oceanía | `Oceania` | 4 | +2 |
| **Total** | — | **42** | — |

---

### 3.5 Card (Carta de Territorio)

```csharp
public class Card
{
    /// <summary>
    /// Identificador único de la carta.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Tipo de la carta: Infantry, Cavalry, Artillery o Wildcard.
    /// </summary>
    public CardType Type { get; set; }
    
    /// <summary>
    /// Territorio asociado a la carta. Null para comodines.
    /// </summary>
    public TerritoryName? Territory { get; set; }
}
```

**Composición del mazo (44 cartas):**

| Tipo | Cantidad | ¿Tiene territorio? |
|------|:--------:|:------------------:|
| Infantry (🚶) | 14 | Sí |
| Cavalry (🐴) | 14 | Sí |
| Artillery (💣) | 14 | Sí |
| Wildcard (⭐) | 2 | No |

---

### 3.6 AttackResult (Resultado de Ataque)

Objeto de valor que encapsula el resultado de una tirada de dados.

```csharp
public class AttackResult
{
    /// <summary>Dados del atacante, ordenados de mayor a menor.</summary>
    public int[] AttackerDice { get; set; } = [];
    
    /// <summary>Dados del defensor, ordenados de mayor a menor.</summary>
    public int[] DefenderDice { get; set; } = [];
    
    /// <summary>Ejércitos perdidos por el atacante.</summary>
    public int AttackerLosses { get; set; }
    
    /// <summary>Ejércitos perdidos por el defensor.</summary>
    public int DefenderLosses { get; set; }
    
    /// <summary>Territorio desde el que se atacó.</summary>
    public TerritoryName FromTerritory { get; set; }
    
    /// <summary>Territorio atacado.</summary>
    public TerritoryName ToTerritory { get; set; }
    
    /// <summary>True si el defensor perdió todos sus ejércitos → conquista.</summary>
    public bool TerritoryConquered { get; set; }
}
```

---

### 3.7 GameEvent (Evento del Log)

Registro de una acción o suceso relevante en la partida.

```csharp
public class GameEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public GameEventType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PlayerId { get; set; }
    public string? PlayerName { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

### 3.8 GameSettings (Configuración de Partida)

```csharp
public class GameSettings
{
    /// <summary>Número máximo de jugadores (2–6).</summary>
    public int MaxPlayers { get; set; } = 6;
    
    /// <summary>Modo de distribución de territorios.</summary>
    public TerritoryDistributionMode DistributionMode { get; set; } 
        = TerritoryDistributionMode.Random;
}

public enum TerritoryDistributionMode
{
    /// <summary>El sistema reparte aleatoriamente los 42 territorios.</summary>
    Random,
    /// <summary>Los jugadores eligen territorios por turnos.</summary>
    Manual
}
```

---

### 3.9 GameSummary (Resumen para el Lobby)

Versión ligera de `Game` para mostrar en la lista del lobby sin exponer todo el estado.

```csharp
public class GameSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public GameStatus Status { get; set; }
    public int CurrentPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

---

## 4. Enumeraciones

### 4.1 GamePhase (Fases del Turno)

```csharp
public enum GamePhase
{
    /// <summary>Configuración inicial: distribución de territorios y colocación de ejércitos.</summary>
    Setup,
    
    /// <summary>El jugador recibe y coloca ejércitos de refuerzo.</summary>
    Reinforcement,
    
    /// <summary>El jugador puede atacar territorios enemigos adyacentes.</summary>
    Attack,
    
    /// <summary>El jugador puede mover ejércitos entre territorios propios conectados.</summary>
    Fortification
}
```

```
  Setup ──▶ Reinforcement ──▶ Attack ──▶ Fortification ──┐
                  ▲                                        │
                  └────────────────────────────────────────┘
                            (siguiente turno)
```

### 4.2 GameStatus (Estado de la Partida)

```csharp
public enum GameStatus
{
    /// <summary>La partida está en el lobby esperando jugadores.</summary>
    WaitingForPlayers,
    
    /// <summary>La partida está en curso.</summary>
    Playing,
    
    /// <summary>La partida ha terminado.</summary>
    Finished
}
```

### 4.3 CardType (Tipo de Carta)

```csharp
public enum CardType
{
    Infantry,     // 🚶 Infantería
    Cavalry,      // 🐴 Caballería
    Artillery,    // 💣 Artillería
    Wildcard      // ⭐ Comodín
}
```

### 4.4 TerritoryName (42 Territorios)

```csharp
public enum TerritoryName
{
    // América del Norte (9)
    Alaska, NorthwestTerritory, Greenland, Alberta,
    Ontario, Quebec, WesternUnitedStates, EasternUnitedStates,
    CentralAmerica,
    
    // América del Sur (4)
    Venezuela, Peru, Brazil, Argentina,
    
    // Europa (7)
    Iceland, GreatBritain, Scandinavia, WesternEurope,
    NorthernEurope, SouthernEurope, Ukraine,
    
    // África (6)
    NorthAfrica, Egypt, EastAfrica, Congo,
    SouthAfrica, Madagascar,
    
    // Asia (12)
    MiddleEast, Afghanistan, Ural, Siberia,
    Yakutsk, Irkutsk, Kamchatka, Mongolia,
    Japan, China, India, SoutheastAsia,
    
    // Oceanía (4)
    Indonesia, NewGuinea, WesternAustralia, EasternAustralia
}
```

### 4.5 ContinentName

```csharp
public enum ContinentName
{
    NorthAmerica,
    SouthAmerica,
    Europe,
    Africa,
    Asia,
    Oceania
}
```

### 4.6 PlayerColor

```csharp
public enum PlayerColor
{
    Red = 0,       // #E63946
    Blue = 1,      // #457B9D
    Green = 2,     // #2A9D8F
    Yellow = 3,    // #E9C46A
    Purple = 4,    // #7B2D8E
    Orange = 5,    // #F4845F
    Neutral = 99   // #ADB5BD — solo en partidas de 2 jugadores
}
```

### 4.7 GameEventType

```csharp
public enum GameEventType
{
    GameStarted,
    TurnStarted,
    ReinforcementsPlaced,
    AttackLaunched,
    DiceRolled,
    TerritoryConquered,
    PlayerEliminated,
    CardsTraded,
    Fortified,
    TurnEnded,
    PlayerConnected,
    PlayerDisconnected,
    PlayerReconnected,
    GameOver,
    ChatMessage
}
```

---

## 5. Relaciones entre Entidades

```
Game ──────────┬──── 1:N ────── Player
               │                  │
               │                  ├── 1:N ── Card (mano del jugador)
               │                  │
               ├──── 1:42 ──── Territory
               │                  │
               │                  ├── N:1 ── ContinentName (pertenencia)
               │                  ├── N:1 ── Player (propietario, vía OwnerId)
               │                  └── N:N ── Territory (adyacencias)
               │
               ├──── 1:6 ───── Continent
               │                  └── 1:N ── TerritoryName (composición)
               │
               ├──── 1:N ───── Card (mazo + descarte)
               │
               ├──── 1:N ───── GameEvent (log)
               │
               └──── 1:1 ───── GameSettings (configuración)
```

### 5.1 Relaciones Clave

| Relación | Tipo | Navegación | Descripción |
|----------|:----:|:----------:|-------------|
| Game → Player | 1:N | `Game.Players` | Una partida tiene 2–6 jugadores |
| Game → Territory | 1:42 | `Game.Territories` | Una partida tiene exactamente 42 territorios |
| Game → Continent | 1:6 | `Game.Continents` | Una partida tiene 6 continentes |
| Game → Card | 1:N | `Game.CardDeck`, `Game.DiscardPile` | Mazo compartido de la partida |
| Player → Card | 1:N | `Player.Cards` | Cartas en la mano del jugador |
| Territory → Player | N:1 | `Territory.OwnerId` | Cada territorio tiene un propietario |
| Territory → Territory | N:N | `Territory.AdjacentTerritories` | Adyacencias bidireccionales |
| Territory → Continent | N:1 | `Territory.Continent` | Cada territorio pertenece a un continente |
| Continent → Territory | 1:N | `Continent.Territories` | Un continente agrupa varios territorios |

---

## 6. Reglas de Negocio del Dominio

### 6.1 Reglas de la Partida

| ID | Regla | Validación |
|----|-------|------------|
| **RN-01** | Una partida necesita entre 2 y 6 jugadores para iniciar | `Players.Count >= 2 && Players.Count <= Settings.MaxPlayers` |
| **RN-02** | Solo el creador puede iniciar la partida | `playerId == CreatorPlayerId` |
| **RN-03** | No se pueden unir jugadores a una partida ya iniciada | `Status == WaitingForPlayers` |
| **RN-04** | El orden de turno se determina aleatoriamente al inicio | Shuffle de `Players` en `StartGame()` |
| **RN-05** | Los canjes incrementan ejércitos globalmente: 4, 6, 8, 10, 12, 15, +5... | `TradeCount` determina la cantidad |

### 6.2 Reglas de Refuerzo

| ID | Regla | Fórmula |
|----|-------|---------|
| **RN-06** | Mínimo 3 ejércitos por turno | `max(3, territorios / 3)` |
| **RN-07** | Bonificación por continente completo | Sumar `Continent.BonusArmies` por cada continente controlado |
| **RN-08** | Canje obligatorio con 5+ cartas | `Player.Cards.Count >= 5` → forzar canje antes de continuar |
| **RN-09** | Todos los refuerzos deben colocarse antes de atacar | `RemainingReinforcements == 0` para avanzar |

### 6.3 Reglas de Ataque

| ID | Regla | Validación |
|----|-------|------------|
| **RN-10** | Solo puede atacar el jugador cuyo turno es | `playerId == GetCurrentPlayer().Id` |
| **RN-11** | Solo se puede atacar en fase de ataque | `Phase == GamePhase.Attack` |
| **RN-12** | Territorio atacante debe ser propio | `territory.OwnerId == playerId` |
| **RN-13** | Territorio atacante debe tener ≥2 ejércitos | `territory.Armies >= 2` |
| **RN-14** | Territorio defensor debe ser enemigo y adyacente | `defender.OwnerId != playerId && attacker.IsAdjacentTo(defender.Name)` |
| **RN-15** | Dados del atacante: 1–3, máximo `ejércitos - 1` | `diceCount >= 1 && diceCount <= min(3, armies - 1)` |
| **RN-16** | Dados del defensor: 1–2, máximo `ejércitos` | `diceCount >= 1 && diceCount <= min(2, armies)` |
| **RN-17** | Empate en dado favorece al defensor | `attackerDie > defenderDie` (estrictamente mayor) |

### 6.4 Reglas de Conquista y Eliminación

| ID | Regla |
|----|-------|
| **RN-18** | Al conquistar, mover al menos tantos ejércitos como dados usados (mín. 1) |
| **RN-19** | Al conquistar, dejar al menos 1 ejército en el territorio de origen |
| **RN-20** | Al eliminar un jugador, el conquistador recibe todas sus cartas |
| **RN-21** | Si al recibir cartas se acumulan ≥6, canjear inmediatamente |

### 6.5 Reglas de Fortificación

| ID | Regla |
|----|-------|
| **RN-22** | Solo un movimiento de fortificación por turno |
| **RN-23** | Ambos territorios deben ser propios |
| **RN-24** | Debe existir un camino conectado de territorios propios entre origen y destino |
| **RN-25** | Dejar al menos 1 ejército en el territorio de origen |

### 6.6 Reglas de Cartas

| ID | Regla |
|----|-------|
| **RN-26** | Se recibe 1 carta por turno, solo si se conquistó al menos un territorio |
| **RN-27** | Canje válido: 3 iguales, 1 de cada tipo, o comodín + 2 cualesquiera |
| **RN-28** | Si una carta canjeada corresponde a un territorio propio, +2 ejércitos en ese territorio |

---

## 7. Invariantes del Sistema

Condiciones que **siempre deben ser verdaderas** durante la ejecución:

| ID | Invariante | Verificación |
|----|-----------|--------------|
| **INV-01** | Todo territorio tiene ≥1 ejército | `Territories.All(t => t.Armies >= 1)` |
| **INV-02** | Todo territorio tiene un propietario | `Territories.All(t => !string.IsNullOrEmpty(t.OwnerId))` |
| **INV-03** | Hay exactamente 42 territorios | `Territories.Count == 42` |
| **INV-04** | Hay exactamente 6 continentes | `Continents.Count == 6` |
| **INV-05** | El mazo total (deck + descarte + manos) = 44 cartas | `CardDeck.Count + DiscardPile.Count + Players.Sum(p => p.Cards.Count) == 44` |
| **INV-06** | El jugador actual no está eliminado | `!GetCurrentPlayer().IsEliminated` |
| **INV-07** | Cada jugador tiene un color único en la partida | `Players.Select(p => p.Color).Distinct().Count() == Players.Count` |
| **INV-08** | Las adyacencias son bidireccionales | Si A es adyacente a B, entonces B es adyacente a A |
| **INV-09** | `CurrentPlayerIndex` está en rango válido | `CurrentPlayerIndex >= 0 && CurrentPlayerIndex < Players.Count` |
| **INV-10** | Solo hay un ganador posible a la vez | `GetActivePlayers().Count() >= 1` |

---

## 8. Ejemplo de Estado en Memoria

```
GameManager (Singleton)
│
└── Game "abc-123"
    ├── Id: "abc-123"
    ├── Name: "Partida de los viernes"
    ├── Status: Playing
    ├── Phase: Attack
    ├── CurrentPlayerIndex: 0  → Carlos
    ├── TurnNumber: 7
    ├── TradeCount: 2
    ├── RemainingReinforcements: 0
    ├── ConqueredThisTurn: true
    │
    ├── Players:
    │   ├── [0] Player { Id:"p1", Name:"Carlos", Color:Red, Cards:[2], IsEliminated:false }
    │   ├── [1] Player { Id:"p2", Name:"Ana", Color:Blue, Cards:[1], IsEliminated:false }
    │   └── [2] Player { Id:"p3", Name:"Luis", Color:Green, Cards:[0], IsEliminated:false }
    │
    ├── Territories (42):
    │   ├── Alaska         → Owner:"p1"(Carlos), Armies:5
    │   ├── NorthwestTerr. → Owner:"p1"(Carlos), Armies:3
    │   ├── Kamchatka      → Owner:"p2"(Ana),    Armies:7
    │   ├── Brazil         → Owner:"p3"(Luis),   Armies:2
    │   └── ... (38 más)
    │
    ├── Continents (6):
    │   ├── NorthAmerica  → Bonus:5, Territories:[Alaska, NWT, Greenland, ...]
    │   ├── SouthAmerica  → Bonus:2, Territories:[Venezuela, Peru, Brazil, Argentina]
    │   └── ... (4 más)
    │
    ├── CardDeck: Queue<Card> (32 cartas restantes)
    ├── DiscardPile: [6 cartas descartadas]
    │
    └── EventLog:
        ├── [GameStarted]  "La partida ha comenzado"
        ├── [TurnStarted]  "Turno 7 — Carlos"
        ├── [DiceRolled]   "Carlos atacó Kamchatka desde Alaska: [6,4,2] vs [5,3]"
        └── ...
```

---

> **Siguiente documento:** [06 — Motor del Juego (Game Engine)](./06_Motor_Juego.md)
