# 06 — Motor del Juego (Game Engine)

> **Documento:** 06 de 14  
> **Versión:** 1.0  
> **Última actualización:** 22 de febrero de 2026  
> **Volver al índice:** [00_Indice.md](./00_Indice.md)  
> **Documento anterior:** [05 — Modelo de Dominio](./05_Modelo_Dominio.md)

---

## 1. Visión General del Motor del Juego

El **Game Engine** (`IGameEngine` / `GameEngine`) es el servicio que implementa **toda la lógica de reglas** del Risk. Es el corazón del sistema: recibe el estado actual de la partida, valida las acciones de los jugadores y produce el nuevo estado resultante.

### 1.1 Responsabilidades

| Responsabilidad | Descripción |
|----------------|-------------|
| **Validar acciones** | Comprobar que una acción es legal según las reglas y el estado actual |
| **Resolver combates** | Tirar dados, comparar resultados, aplicar pérdidas |
| **Calcular refuerzos** | Determinar ejércitos por territorios, continentes y canjes |
| **Gestionar cartas** | Validar canjes, calcular ejércitos por canje |
| **Gestionar turnos** | Avanzar fases, cambiar de jugador, detectar victoria |
| **Verificar caminos** | Determinar si dos territorios están conectados para fortificación |
| **Aplicar conquista** | Transferir territorio, mover ejércitos, detectar eliminación |

### 1.2 Principios de Diseño

| Principio | Aplicación |
|-----------|-----------|
| **Sin estado propio** | El motor no almacena estado. Recibe `Game` como parámetro y lo modifica. Es **Transient** en DI. |
| **Puro en lógica** | No accede a SignalR, no envía notificaciones. Solo aplica reglas. La orquestación (notificaciones, serialización) la hacen `GameHub` y `GameManager`. |
| **Validación exhaustiva** | Cada método público valida precondiciones antes de actuar. Retorna un `Result` para indicar éxito o error. |
| **Testeable** | Al no depender de infraestructura (solo de `IDiceService` y `IMapService`), es fácil de testear con mocks. |

### 1.3 Dependencias

```
GameEngine
├── IDiceService     → Generación de tiradas de dados
├── ICardService     → Validación de canjes y cálculo de ejércitos por canje
└── IMapService      → Datos del mapa (adyacencias, continentes, territorios)
```

```csharp
public class GameEngine : IGameEngine
{
    private readonly IDiceService _diceService;
    private readonly ICardService _cardService;
    private readonly IMapService _mapService;

    public GameEngine(
        IDiceService diceService,
        ICardService cardService,
        IMapService mapService)
    {
        _diceService = diceService;
        _cardService = cardService;
        _mapService = mapService;
    }
}
```

---

## 2. Interfaz del Motor

```csharp
public interface IGameEngine
{
    // ── Inicialización ──
    GameResult InitializeGame(Game game);
    GameResult DistributeTerritoriesRandomly(Game game);
    GameResult PlaceInitialArmies(Game game, string playerId, TerritoryName territory, int count);
    GameResult StartPlaying(Game game);

    // ── Refuerzo ──
    int CalculateReinforcements(Game game, Player player);
    GameResult PlaceReinforcements(Game game, string playerId, TerritoryName territory, int count);
    GameResult ConfirmReinforcements(Game game, string playerId);

    // ── Ataque ──
    AttackGameResult Attack(Game game, string playerId,
        TerritoryName from, TerritoryName to, int attackDiceCount);
    GameResult MoveArmiesAfterConquest(Game game, string playerId,
        TerritoryName from, TerritoryName to, int armyCount);
    GameResult EndAttackPhase(Game game, string playerId);

    // ── Fortificación ──
    GameResult Fortify(Game game, string playerId,
        TerritoryName from, TerritoryName to, int armyCount);
    GameResult SkipFortification(Game game, string playerId);
    bool AreConnected(Game game, string playerId, TerritoryName from, TerritoryName to);

    // ── Cartas ──
    GameResult TradeCards(Game game, string playerId, string[] cardIds);

    // ── Turno ──
    GameResult EndTurn(Game game, string playerId);

    // ── Consultas ──
    bool IsGameOver(Game game);
    Player? GetWinner(Game game);
}
```

### 2.1 Tipos de Resultado

```csharp
/// <summary>
/// Resultado genérico de una acción del motor del juego.
/// </summary>
public class GameResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public static GameResult Ok() => new() { Success = true };
    public static GameResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}

/// <summary>
/// Resultado de un ataque, incluye el resultado de los dados.
/// </summary>
public class AttackGameResult : GameResult
{
    public AttackResult? AttackResult { get; set; }
    public bool PlayerEliminated { get; set; }
    public string? EliminatedPlayerId { get; set; }
    public bool GameOver { get; set; }

    public static AttackGameResult OkAttack(AttackResult result) => new()
    {
        Success = true,
        AttackResult = result
    };
}
```

---

## 3. Máquina de Estados de la Partida

### 3.1 Diagrama de Estados Completo

```
                         ┌───────────────────────┐
                         │   WaitingForPlayers    │
                         │   Phase: Setup         │
                         └───────────┬───────────┘
                                     │
                          Creador pulsa "Iniciar"
                          (min. 2 jugadores)
                                     │
                                     ▼
                         ┌───────────────────────┐
                         │   Playing / Setup      │
                         │                        │
                         │  ┌──────────────────┐  │
                         │  │  Distribución de │  │
                         │  │  territorios     │  │
                         │  │  (aleatoria o    │  │
                         │  │   por turnos)    │  │
                         │  └────────┬─────────┘  │
                         │           │             │
                         │           ▼             │
                         │  ┌──────────────────┐  │
                         │  │  Colocación      │  │
                         │  │  inicial de      │  │
                         │  │  ejércitos       │  │
                         │  │  (por turnos)    │  │
                         │  └────────┬─────────┘  │
                         └───────────┼───────────┘
                                     │
                       Todos los ejércitos colocados
                                     │
                                     ▼
  ┌═══════════════════════════════════════════════════════════════════════┐
  ║                        CICLO DE TURNOS                               ║
  ║                                                                       ║
  ║  ┌──────────────────┐    ┌──────────────────┐    ┌────────────────┐  ║
  ║  │  REINFORCEMENT   │───▶│     ATTACK       │───▶│ FORTIFICATION  │  ║
  ║  │                  │    │                  │    │                │  ║
  ║  │ 1. Calcular      │    │ 1. Seleccionar   │    │ 1. Seleccionar │  ║
  ║  │    refuerzos     │    │    atacante      │    │    origen      │  ║
  ║  │ 2. Canjear       │    │ 2. Seleccionar   │    │ 2. Seleccionar │  ║
  ║  │    cartas (opc.) │    │    defensor      │    │    destino     │  ║
  ║  │ 3. Colocar       │    │ 3. Elegir dados  │    │ 3. Elegir      │  ║
  ║  │    ejércitos     │    │ 4. Resolver      │    │    cantidad    │  ║
  ║  │                  │    │ 5. Repetir o     │    │ 4. Mover       │  ║
  ║  │                  │    │    pasar         │    │    (o pasar)   │  ║
  ║  └──────────────────┘    └──────────────────┘    └───────┬────────┘  ║
  ║         ▲                                                 │          ║
  ║         │                                                 │          ║
  ║         └─── Avanzar turno (siguiente jugador) ───────────┘          ║
  ║                                                                       ║
  ╚═══════════════════════════════════════════════════════════════════════╝
                                     │
                           ¿Un jugador controla
                            los 42 territorios?
                                     │
                                     ▼
                         ┌───────────────────────┐
                         │      Finished         │
                         │  WinnerId = playerId  │
                         └───────────────────────┘
```

### 3.2 Transiciones de Fase

| Desde | Hacia | Condición | Acción |
|-------|-------|-----------|--------|
| Setup | Reinforcement | Todos los ejércitos iniciales colocados | `StartPlaying()` → calcula refuerzos del primer jugador |
| Reinforcement | Attack | `RemainingReinforcements == 0` | `ConfirmReinforcements()` |
| Attack | Fortification | Jugador decide terminar ataques | `EndAttackPhase()` |
| Attack | Fortification | *(automático tras conquista total →)* GameOver | `IsGameOver()` |
| Fortification | Reinforcement | Turno finalizado → siguiente jugador | `EndTurn()` → `AdvanceTurn()` → calcula refuerzos |

---

## 4. Fase de Inicialización (Setup)

### 4.1 Inicializar la Partida

```csharp
public GameResult InitializeGame(Game game)
{
    // 1. Crear los 42 territorios con sus adyacencias
    game.Territories = _mapService.CreateTerritories();

    // 2. Crear los 6 continentes con sus bonificaciones
    game.Continents = _mapService.CreateContinents();

    // 3. Crear y barajar el mazo de 44 cartas
    game.CardDeck = new Queue<Card>(_mapService.CreateCardDeck().Shuffle());

    // 4. Determinar ejércitos iniciales por jugador
    int initialArmies = game.Players.Count switch
    {
        2 => 40,
        3 => 35,
        4 => 30,
        5 => 25,
        6 => 20,
        _ => throw new InvalidOperationException(
            $"Invalid player count: {game.Players.Count}")
    };

    foreach (var player in game.Players)
    {
        player.InitialArmiesRemaining = initialArmies;
    }

    // 5. Aleatorizar el orden de turno
    game.Players = game.Players.OrderBy(_ => Random.Shared.Next()).ToList();

    // 6. Establecer estado
    game.Status = GameStatus.Playing;
    game.Phase = GamePhase.Setup;
    game.CurrentPlayerIndex = 0;
    game.TurnNumber = 0;
    game.StartedAt = DateTime.UtcNow;

    game.AddEvent(new GameEvent
    {
        Type = GameEventType.GameStarted,
        Message = $"La partida \"{game.Name}\" ha comenzado con {game.Players.Count} jugadores."
    });

    return GameResult.Ok();
}
```

### 4.2 Distribución de Territorios

#### Modo Aleatorio

```csharp
public GameResult DistributeTerritoriesRandomly(Game game)
{
    var territories = game.Territories.Keys.ToList();
    var shuffled = territories.OrderBy(_ => Random.Shared.Next()).ToList();
    var activePlayers = game.GetActivePlayers().ToList();

    for (int i = 0; i < shuffled.Count; i++)
    {
        var player = activePlayers[i % activePlayers.Count];
        var territory = game.Territories[shuffled[i]];

        territory.OwnerId = player.Id;
        territory.Armies = 1;
        player.InitialArmiesRemaining--;  // 1 ejército ya colocado
    }

    return GameResult.Ok();
}
```

#### Tabla de ejércitos iniciales

| Jugadores | Ejércitos totales | – 1 por territorio asignado | Restantes por colocar |
|:---------:|:-----------------:|:---------------------------:|:---------------------:|
| 2 | 40 | 40 − 21 = 19 | 19 por jugador |
| 3 | 35 | 35 − 14 = 21 | 21 por jugador |
| 4 | 30 | 30 − 10 = 20 | 20 por jugador *(≈)* |
| 5 | 25 | 25 − 8 = 17 | 17 por jugador *(≈)* |
| 6 | 20 | 20 − 7 = 13 | 13 por jugador |

### 4.3 Colocación Inicial de Ejércitos

Tras la distribución, los jugadores colocan sus ejércitos restantes **por turnos**, uno a uno:

```csharp
public GameResult PlaceInitialArmies(Game game, string playerId, 
    TerritoryName territory, int count)
{
    // Validaciones
    if (game.Phase != GamePhase.Setup)
        return GameResult.Fail("La partida no está en fase de configuración.");

    var player = game.GetPlayerById(playerId);
    if (player == null)
        return GameResult.Fail("Jugador no encontrado.");

    if (game.GetCurrentPlayer().Id != playerId)
        return GameResult.Fail("No es tu turno para colocar ejércitos.");

    var terr = game.Territories[territory];
    if (terr.OwnerId != playerId)
        return GameResult.Fail("Solo puedes colocar ejércitos en tus propios territorios.");

    if (count < 1 || count > player.InitialArmiesRemaining)
        return GameResult.Fail($"Cantidad inválida. Tienes {player.InitialArmiesRemaining} restantes.");

    // Aplicar
    terr.AddArmies(count);
    player.InitialArmiesRemaining -= count;

    // Si este jugador terminó, verificar si todos terminaron
    if (player.InitialArmiesRemaining == 0)
    {
        // Avanzar al siguiente jugador que aún tenga ejércitos por colocar
        AdvanceToNextSetupPlayer(game);
    }

    return GameResult.Ok();
}

private void AdvanceToNextSetupPlayer(Game game)
{
    int startIndex = game.CurrentPlayerIndex;
    do
    {
        game.CurrentPlayerIndex = (game.CurrentPlayerIndex + 1) % game.Players.Count;

        if (game.Players[game.CurrentPlayerIndex].InitialArmiesRemaining > 0)
            return; // Encontrado: este jugador aún debe colocar
    }
    while (game.CurrentPlayerIndex != startIndex);

    // Si volvemos al inicio, todos han terminado → iniciar juego
    StartPlaying(game);
}
```

### 4.4 Transición a Juego Activo

```csharp
public GameResult StartPlaying(Game game)
{
    game.Phase = GamePhase.Reinforcement;
    game.CurrentPlayerIndex = 0;
    game.TurnNumber = 1;

    // Calcular refuerzos del primer jugador
    var firstPlayer = game.GetCurrentPlayer();
    game.RemainingReinforcements = CalculateReinforcements(game, firstPlayer);

    game.AddEvent(new GameEvent
    {
        Type = GameEventType.TurnStarted,
        Message = $"Turno 1 — {firstPlayer.Name}. Recibe {game.RemainingReinforcements} ejércitos de refuerzo.",
        PlayerId = firstPlayer.Id,
        PlayerName = firstPlayer.Name
    });

    return GameResult.Ok();
}
```

---

## 5. Fase de Refuerzo (Reinforcement)

### 5.1 Cálculo de Ejércitos de Refuerzo

```
                    ┌─────────────────────────────┐
                    │  CÁLCULO DE REFUERZOS       │
                    │                              │
                    │  Fuente 1: Territorios       │
                    │  max(3, territorios / 3)     │──┐
                    │                              │  │
                    │  Fuente 2: Continentes       │  │
                    │  Σ bonus por cada continente │──┤── TOTAL
                    │  controlado completamente    │  │
                    │                              │  │
                    │  Fuente 3: Canje de cartas   │  │
                    │  (si el jugador decide       │──┘
                    │   canjear, o tiene 5+ cartas)│
                    └─────────────────────────────┘
```

```csharp
public int CalculateReinforcements(Game game, Player player)
{
    int total = 0;

    // ── Fuente 1: Por territorios controlados ──
    int territoryCount = player.GetOwnedTerritories(game).Count();
    int fromTerritories = Math.Max(3, territoryCount / 3);
    total += fromTerritories;

    // ── Fuente 2: Por continentes completos ──
    foreach (var continent in game.Continents.Values)
    {
        if (continent.IsControlledBy(player.Id, game.Territories))
        {
            total += continent.BonusArmies;
        }
    }

    return total;
}
```

**Ejemplo:**

```
Carlos controla:
  - 15 territorios          → max(3, 15/3) = 5 ejércitos
  - Toda América del Sur    → +2 ejércitos
  - Toda Oceanía            → +2 ejércitos
                             ─────────────
                  Total:       9 ejércitos de refuerzo
```

### 5.2 Colocación de Ejércitos de Refuerzo

```csharp
public GameResult PlaceReinforcements(Game game, string playerId,
    TerritoryName territory, int count)
{
    // ── Validaciones ──
    if (game.Phase != GamePhase.Reinforcement)
        return GameResult.Fail("No estás en la fase de refuerzo.");

    if (game.GetCurrentPlayer().Id != playerId)
        return GameResult.Fail("No es tu turno.");

    var terr = game.Territories[territory];
    if (terr.OwnerId != playerId)
        return GameResult.Fail("Solo puedes reforzar tus propios territorios.");

    if (count < 1 || count > game.RemainingReinforcements)
        return GameResult.Fail(
            $"Cantidad inválida. Te quedan {game.RemainingReinforcements} por colocar.");

    // ── Aplicar ──
    terr.AddArmies(count);
    game.RemainingReinforcements -= count;

    game.AddEvent(new GameEvent
    {
        Type = GameEventType.ReinforcementsPlaced,
        Message = $"{game.GetCurrentPlayer().Name} colocó {count} ejército(s) en {territory}.",
        PlayerId = playerId,
        PlayerName = game.GetCurrentPlayer().Name
    });

    return GameResult.Ok();
}
```

### 5.3 Confirmar Refuerzos y Avanzar a Ataque

```csharp
public GameResult ConfirmReinforcements(Game game, string playerId)
{
    if (game.Phase != GamePhase.Reinforcement)
        return GameResult.Fail("No estás en la fase de refuerzo.");

    if (game.GetCurrentPlayer().Id != playerId)
        return GameResult.Fail("No es tu turno.");

    if (game.RemainingReinforcements > 0)
        return GameResult.Fail(
            $"Aún tienes {game.RemainingReinforcements} ejércitos por colocar.");

    // Avanzar a la fase de ataque
    game.Phase = GamePhase.Attack;

    return GameResult.Ok();
}
```

---

## 6. Fase de Ataque (Attack)

### 6.1 Flujo de un Ataque

```
  Jugador selecciona territorio atacante y defensor
         │
         ▼
  ┌──────────────────────────────┐
  │      VALIDACIONES            │
  │                              │
  │ ¿Es tu turno?               │── NO ──▶ Error
  │ ¿Estás en fase de ataque?   │── NO ──▶ Error
  │ ¿El atacante es tuyo?       │── NO ──▶ Error
  │ ¿Tiene ≥2 ejércitos?        │── NO ──▶ Error
  │ ¿El defensor es enemigo?    │── NO ──▶ Error
  │ ¿Son adyacentes?            │── NO ──▶ Error
  │ ¿Nº dados válido?           │── NO ──▶ Error
  └──────────┬───────────────────┘
             │ TODO OK
             ▼
  ┌──────────────────────────────┐
  │      TIRADA DE DADOS         │
  │                              │
  │ Atacante: N dados (1-3)      │
  │ Defensor: M dados (1-2)      │
  │ (auto: min(2, ejércitos))    │
  └──────────┬───────────────────┘
             │
             ▼
  ┌──────────────────────────────┐
  │   RESOLUCIÓN DEL COMBATE    │
  │                              │
  │ Ordenar dados desc           │
  │ Comparar por parejas         │
  │ Atacante > Defensor → def -1 │
  │ Atacante ≤ Defensor → atk -1 │
  └──────────┬───────────────────┘
             │
             ▼
  ┌──────────────────────────────┐
  │   APLICAR PÉRDIDAS           │
  │                              │
  │ Restar ejércitos de cada     │
  │ territorio según pérdidas    │
  └──────────┬───────────────────┘
             │
      ┌──────┴───────┐
      │              │
  Defensor        Defensor
  tiene           tiene
  ejércitos > 0   ejércitos = 0
      │              │
      ▼              ▼
  Continúa        ┌─────────────────┐
  (atacante       │   CONQUISTA     │
   puede          │                 │
   atacar         │ Territorio      │
   de nuevo)      │ cambia de dueño │
                  │                 │
                  │ ¿Último territr │
                  │  del defensor?  │
                  └──┬──────┬───────┘
                     │      │
                   NO      SÍ
                     │      │
                     ▼      ▼
                  Normal  ┌──────────────┐
                          │ ELIMINACIÓN  │
                          │ Cartas →     │
                          │ conquistador │
                          │              │
                          │ ¿42 territ.? │
                          └──┬─────┬─────┘
                             │     │
                           NO    SÍ
                             │     │
                             ▼     ▼
                          Normal  GAME OVER
```

### 6.2 Implementación del Ataque

```csharp
public AttackGameResult Attack(Game game, string playerId,
    TerritoryName from, TerritoryName to, int attackDiceCount)
{
    // ═══════════════════════════════════════
    // VALIDACIONES
    // ═══════════════════════════════════════
    if (game.Phase != GamePhase.Attack)
        return new AttackGameResult { Success = false, ErrorMessage = "No estás en fase de ataque." };

    var currentPlayer = game.GetCurrentPlayer();
    if (currentPlayer.Id != playerId)
        return new AttackGameResult { Success = false, ErrorMessage = "No es tu turno." };

    var attacker = game.Territories[from];
    var defender = game.Territories[to];

    if (attacker.OwnerId != playerId)
        return new AttackGameResult { Success = false, 
            ErrorMessage = "El territorio atacante no es tuyo." };

    if (defender.OwnerId == playerId)
        return new AttackGameResult { Success = false, 
            ErrorMessage = "No puedes atacar tus propios territorios." };

    if (!attacker.IsAdjacentTo(to))
        return new AttackGameResult { Success = false, 
            ErrorMessage = $"{from} no es adyacente a {to}." };

    if (!attacker.CanAttackFrom())
        return new AttackGameResult { Success = false, 
            ErrorMessage = $"{from} necesita al menos 2 ejércitos para atacar." };

    int maxAttackDice = Math.Min(3, attacker.Armies - 1);
    if (attackDiceCount < 1 || attackDiceCount > maxAttackDice)
        return new AttackGameResult { Success = false, 
            ErrorMessage = $"Puedes usar entre 1 y {maxAttackDice} dados." };

    // ═══════════════════════════════════════
    // TIRADA DE DADOS
    // ═══════════════════════════════════════
    int defenderDiceCount = Math.Min(2, defender.Armies);

    int[] attackerDice = _diceService.Roll(attackDiceCount);
    int[] defenderDice = _diceService.Roll(defenderDiceCount);

    // ═══════════════════════════════════════
    // RESOLUCIÓN
    // ═══════════════════════════════════════
    var result = ResolveCombat(attackerDice, defenderDice, from, to);

    // ═══════════════════════════════════════
    // APLICAR PÉRDIDAS
    // ═══════════════════════════════════════
    attacker.Armies -= result.AttackerLosses;
    defender.Armies -= result.DefenderLosses;

    game.AddEvent(new GameEvent
    {
        Type = GameEventType.DiceRolled,
        Message = $"{currentPlayer.Name} atacó {to} desde {from}: " +
                  $"[{string.Join(",", result.AttackerDice)}] vs " +
                  $"[{string.Join(",", result.DefenderDice)}] → " +
                  $"Atk -{result.AttackerLosses}, Def -{result.DefenderLosses}",
        PlayerId = playerId,
        PlayerName = currentPlayer.Name
    });

    // ═══════════════════════════════════════
    // ¿CONQUISTA?
    // ═══════════════════════════════════════
    var attackGameResult = AttackGameResult.OkAttack(result);

    if (defender.Armies <= 0)
    {
        result.TerritoryConquered = true;
        game.ConqueredThisTurn = true;

        // El territorio pasa al atacante temporalmente con 0 ejércitos
        // El jugador deberá mover ejércitos con MoveArmiesAfterConquest
        string previousOwnerId = defender.OwnerId;
        defender.OwnerId = playerId;
        defender.Armies = 0; // Se rellenará en MoveArmiesAfterConquest

        game.AddEvent(new GameEvent
        {
            Type = GameEventType.TerritoryConquered,
            Message = $"{currentPlayer.Name} conquistó {to}.",
            PlayerId = playerId,
            PlayerName = currentPlayer.Name
        });

        // ¿El defensor ha perdido su último territorio?
        var eliminatedPlayer = game.GetPlayerById(previousOwnerId);
        if (eliminatedPlayer != null)
        {
            bool hasTerritoriesLeft = game.Territories.Values
                .Any(t => t.OwnerId == previousOwnerId);

            if (!hasTerritoriesLeft)
            {
                eliminatedPlayer.IsEliminated = true;
                attackGameResult.PlayerEliminated = true;
                attackGameResult.EliminatedPlayerId = previousOwnerId;

                // Transferir cartas al conquistador
                var surrenderedCards = eliminatedPlayer.SurrenderAllCards();
                foreach (var card in surrenderedCards)
                {
                    currentPlayer.AddCard(card);
                }

                game.AddEvent(new GameEvent
                {
                    Type = GameEventType.PlayerEliminated,
                    Message = $"{eliminatedPlayer.Name} ha sido eliminado por {currentPlayer.Name}." +
                              (surrenderedCards.Count > 0
                                  ? $" {currentPlayer.Name} recibe {surrenderedCards.Count} carta(s)."
                                  : ""),
                    PlayerId = previousOwnerId,
                    PlayerName = eliminatedPlayer.Name
                });

                // ¿Victoria?
                if (IsGameOver(game))
                {
                    attackGameResult.GameOver = true;
                    game.Status = GameStatus.Finished;
                    game.FinishedAt = DateTime.UtcNow;

                    game.AddEvent(new GameEvent
                    {
                        Type = GameEventType.GameOver,
                        Message = $"¡{currentPlayer.Name} ha ganado la partida!",
                        PlayerId = playerId,
                        PlayerName = currentPlayer.Name
                    });
                }
            }
        }
    }

    return attackGameResult;
}
```

### 6.3 Resolución del Combate (Dados)

```csharp
private AttackResult ResolveCombat(int[] attackerDice, int[] defenderDice,
    TerritoryName from, TerritoryName to)
{
    // Ordenar de mayor a menor
    var sortedAttacker = attackerDice.OrderByDescending(d => d).ToArray();
    var sortedDefender = defenderDice.OrderByDescending(d => d).ToArray();

    int attackerLosses = 0;
    int defenderLosses = 0;

    // Comparar por parejas
    int pairs = Math.Min(sortedAttacker.Length, sortedDefender.Length);
    for (int i = 0; i < pairs; i++)
    {
        if (sortedAttacker[i] > sortedDefender[i])
        {
            defenderLosses++;   // Atacante gana (estrictamente mayor)
        }
        else
        {
            attackerLosses++;   // Defensor gana (mayor o igual)
        }
    }

    return new AttackResult
    {
        AttackerDice = sortedAttacker,
        DefenderDice = sortedDefender,
        AttackerLosses = attackerLosses,
        DefenderLosses = defenderLosses,
        FromTerritory = from,
        ToTerritory = to
    };
}
```

**Tabla de probabilidades (referencia):**

| Atacante | Defensor | P(Atk gana) | P(Def gana) |
|:--------:|:--------:|:-----------:|:-----------:|
| 3 dados | 2 dados | ~37% ambos pierde def, ~29% ambos pierde atk, ~34% 1-1 | — |
| 3 dados | 1 dado | ~66% def pierde | ~34% atk pierde |
| 2 dados | 2 dados | ~23% ambos def, ~45% 1-1, ~32% ambos atk | — |
| 1 dado | 1 dado | ~42% def pierde | ~58% atk pierde |

### 6.4 Movimiento de Ejércitos tras Conquista

```csharp
public GameResult MoveArmiesAfterConquest(Game game, string playerId,
    TerritoryName from, TerritoryName to, int armyCount)
{
    var attacker = game.Territories[from];
    var conquered = game.Territories[to];

    if (attacker.OwnerId != playerId || conquered.OwnerId != playerId)
        return GameResult.Fail("Ambos territorios deben ser tuyos.");

    if (conquered.Armies > 0)
        return GameResult.Fail("Ya se movieron ejércitos a este territorio.");

    // Mínimo: tantos ejércitos como dados usó en el último ataque (simplificado a 1)
    if (armyCount < 1)
        return GameResult.Fail("Debes mover al menos 1 ejército.");

    // No puede dejar el territorio de origen con 0
    if (attacker.Armies - armyCount < 1)
        return GameResult.Fail(
            $"Debes dejar al menos 1 ejército en {from}. " +
            $"Máximo que puedes mover: {attacker.Armies - 1}.");

    attacker.Armies -= armyCount;
    conquered.Armies = armyCount;

    return GameResult.Ok();
}
```

### 6.5 Terminar Fase de Ataque

```csharp
public GameResult EndAttackPhase(Game game, string playerId)
{
    if (game.Phase != GamePhase.Attack)
        return GameResult.Fail("No estás en fase de ataque.");

    if (game.GetCurrentPlayer().Id != playerId)
        return GameResult.Fail("No es tu turno.");

    game.Phase = GamePhase.Fortification;
    return GameResult.Ok();
}
```

---

## 7. Fase de Fortificación (Fortification)

### 7.1 Reglas de Fortificación

```
  ┌─────────────────────────────────────────────────────────────┐
  │                   REGLAS DE FORTIFICACIÓN                    │
  │                                                             │
  │  ✅ Un único movimiento por turno                           │
  │  ✅ Ambos territorios deben ser propios                     │
  │  ✅ Debe existir un camino conectado de territorios propios │
  │  ✅ Dejar al menos 1 ejército en el territorio de origen    │
  │  ✅ Es opcional (el jugador puede pasar)                    │
  │                                                             │
  │  ❌ NO basta con que sean adyacentes si hay enemigos        │
  │     en medio del camino                                     │
  └─────────────────────────────────────────────────────────────┘
```

### 7.2 Implementación

```csharp
public GameResult Fortify(Game game, string playerId,
    TerritoryName from, TerritoryName to, int armyCount)
{
    if (game.Phase != GamePhase.Fortification)
        return GameResult.Fail("No estás en fase de fortificación.");

    if (game.GetCurrentPlayer().Id != playerId)
        return GameResult.Fail("No es tu turno.");

    var source = game.Territories[from];
    var target = game.Territories[to];

    if (source.OwnerId != playerId)
        return GameResult.Fail($"{from} no es tu territorio.");

    if (target.OwnerId != playerId)
        return GameResult.Fail($"{to} no es tu territorio.");

    if (from == to)
        return GameResult.Fail("Origen y destino no pueden ser el mismo territorio.");

    if (armyCount < 1 || armyCount >= source.Armies)
        return GameResult.Fail(
            $"Puedes mover entre 1 y {source.Armies - 1} ejércitos desde {from}.");

    // Verificar camino conectado
    if (!AreConnected(game, playerId, from, to))
        return GameResult.Fail(
            $"No existe un camino de territorios propios entre {from} y {to}.");

    // Aplicar movimiento
    source.Armies -= armyCount;
    target.AddArmies(armyCount);

    game.AddEvent(new GameEvent
    {
        Type = GameEventType.Fortified,
        Message = $"{game.GetCurrentPlayer().Name} movió {armyCount} ejército(s) de {from} a {to}.",
        PlayerId = playerId,
        PlayerName = game.GetCurrentPlayer().Name
    });

    // Terminar turno automáticamente tras fortificar
    return EndTurn(game, playerId);
}
```

### 7.3 Algoritmo de Camino Conectado (BFS)

Determina si dos territorios propios están conectados a través de otros territorios propios. Se usa **BFS** (búsqueda en anchura):

```csharp
public bool AreConnected(Game game, string playerId,
    TerritoryName from, TerritoryName to)
{
    if (from == to) return true;

    // BFS desde 'from', solo por territorios del mismo jugador
    var visited = new HashSet<TerritoryName> { from };
    var queue = new Queue<TerritoryName>();
    queue.Enqueue(from);

    while (queue.Count > 0)
    {
        var current = queue.Dequeue();
        var territory = game.Territories[current];

        foreach (var neighbor in territory.AdjacentTerritories)
        {
            if (visited.Contains(neighbor))
                continue;

            // Solo podemos transitar por territorios propios
            if (game.Territories[neighbor].OwnerId != playerId)
                continue;

            if (neighbor == to)
                return true;  // ¡Encontrado!

            visited.Add(neighbor);
            queue.Enqueue(neighbor);
        }
    }

    return false;  // No hay camino
}
```

**Ejemplo visual:**

```
  Alaska(tuyo,5) ─── NWT(tuyo,3) ─── Ontario(tuyo,2) ─── Quebec(enemigo,4)
       │                                    │
       │                              E.U.Occ(tuyo,7)
       │                                    │
  Kamchatka(enemigo,4)                E.U.Or(tuyo,3)

  ¿Conectados Alaska → E.U.Oriental?
  Camino: Alaska → NWT → Ontario → E.U.Occidental → E.U.Oriental  ✅ SÍ

  ¿Conectados Alaska → Quebec?
  Quebec es enemigo → ❌ NO (no importa si son adyacentes vía Ontario)

  ¿Conectados Alaska → Kamchatka?
  Kamchatka es enemigo → ❌ NO
```

### 7.4 Saltar Fortificación

```csharp
public GameResult SkipFortification(Game game, string playerId)
{
    if (game.Phase != GamePhase.Fortification)
        return GameResult.Fail("No estás en fase de fortificación.");

    if (game.GetCurrentPlayer().Id != playerId)
        return GameResult.Fail("No es tu turno.");

    return EndTurn(game, playerId);
}
```

---

## 8. Sistema de Cartas de Territorio

### 8.1 Obtención de Cartas

Una carta se otorga al **final del turno** si el jugador conquistó al menos un territorio:

```csharp
// Dentro de EndTurn():
if (game.ConqueredThisTurn)
{
    var card = game.DrawCard();
    if (card != null)
    {
        currentPlayer.AddCard(card);
    }
}
```

### 8.2 Canje de Cartas

```csharp
public GameResult TradeCards(Game game, string playerId, string[] cardIds)
{
    if (game.Phase != GamePhase.Reinforcement)
        return GameResult.Fail("Solo puedes canjear cartas durante la fase de refuerzo.");

    if (game.GetCurrentPlayer().Id != playerId)
        return GameResult.Fail("No es tu turno.");

    var player = game.GetPlayerById(playerId)!;

    if (cardIds.Length != 3)
        return GameResult.Fail("Debes seleccionar exactamente 3 cartas.");

    // Buscar las cartas en la mano del jugador
    var selectedCards = new List<Card>();
    foreach (var cardId in cardIds)
    {
        var card = player.Cards.FirstOrDefault(c => c.Id == cardId);
        if (card == null)
            return GameResult.Fail($"No tienes la carta {cardId}.");
        selectedCards.Add(card);
    }

    // Validar combinación
    if (!_cardService.IsValidTrade(selectedCards))
        return GameResult.Fail("Combinación de cartas no válida.");

    // Calcular ejércitos por canje
    game.TradeCount++;
    int armies = _cardService.GetArmiesForTrade(game.TradeCount);

    // Retirar cartas del jugador y añadir al descarte
    player.RemoveCards(selectedCards);
    game.DiscardPile.AddRange(selectedCards);

    // Añadir refuerzos
    game.RemainingReinforcements += armies;

    // Bonificación por territorio: +2 si el jugador controla el territorio de la carta
    foreach (var card in selectedCards)
    {
        if (card.Territory.HasValue &&
            game.Territories[card.Territory.Value].OwnerId == playerId)
        {
            game.Territories[card.Territory.Value].AddArmies(2);

            game.AddEvent(new GameEvent
            {
                Type = GameEventType.CardsTraded,
                Message = $"Bonificación: +2 ejércitos en {card.Territory.Value} " +
                          "(carta de territorio propio).",
                PlayerId = playerId,
                PlayerName = player.Name
            });
        }
    }

    game.AddEvent(new GameEvent
    {
        Type = GameEventType.CardsTraded,
        Message = $"{player.Name} canjeó cartas (canje #{game.TradeCount}) y recibió {armies} ejércitos.",
        PlayerId = playerId,
        PlayerName = player.Name
    });

    return GameResult.Ok();
}
```

### 8.3 CardService: Validación y Cálculo

```csharp
public interface ICardService
{
    bool IsValidTrade(List<Card> cards);
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

        // 1 de cada tipo (Infantry, Cavalry, Artillery)
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
            _ => 15 + (tradeNumber - 6) * 5   // 7→20, 8→25, 9→30...
        };
    }
}
```

**Tabla de ejércitos por canje:**

| Nº canje (global) | Ejércitos |
|:------------------:|:---------:|
| 1º | 4 |
| 2º | 6 |
| 3º | 8 |
| 4º | 10 |
| 5º | 12 |
| 6º | 15 |
| 7º | 20 |
| 8º | 25 |
| N (≥7) | 15 + (N−6) × 5 |

---

## 9. Gestión de Turnos

### 9.1 Fin de Turno

```csharp
public GameResult EndTurn(Game game, string playerId)
{
    var currentPlayer = game.GetCurrentPlayer();

    if (currentPlayer.Id != playerId)
        return GameResult.Fail("No es tu turno.");

    // ── Otorgar carta si conquistó este turno ──
    if (game.ConqueredThisTurn)
    {
        var card = game.DrawCard();
        if (card != null)
        {
            currentPlayer.AddCard(card);
        }
    }

    game.AddEvent(new GameEvent
    {
        Type = GameEventType.TurnEnded,
        Message = $"{currentPlayer.Name} terminó su turno.",
        PlayerId = playerId,
        PlayerName = currentPlayer.Name
    });

    // ── Avanzar al siguiente jugador ──
    game.AdvanceTurn();

    // ── Calcular refuerzos del nuevo jugador ──
    var nextPlayer = game.GetCurrentPlayer();
    game.RemainingReinforcements = CalculateReinforcements(game, nextPlayer);

    // ── Canje obligatorio si tiene 5+ cartas ──
    // (Se gestiona en la UI: el jugador no puede colocar refuerzos
    //  hasta que canjee si tiene 5+ cartas)

    game.AddEvent(new GameEvent
    {
        Type = GameEventType.TurnStarted,
        Message = $"Turno {game.TurnNumber} — {nextPlayer.Name}. " +
                  $"Recibe {game.RemainingReinforcements} ejércitos de refuerzo.",
        PlayerId = nextPlayer.Id,
        PlayerName = nextPlayer.Name
    });

    return GameResult.Ok();
}
```

### 9.2 Secuencia Completa de un Turno

```
  ┌────────────────────────────────────────────────────────────────┐
  │  TURNO DE "CARLOS" (Turno #7)                                  │
  │                                                                │
  │  1. INICIO DEL TURNO                                           │
  │     → CalculateReinforcements(game, Carlos) = 9                │
  │     → RemainingReinforcements = 9                              │
  │     → Phase = Reinforcement                                    │
  │                                                                │
  │  2. ¿Tiene 5+ cartas? → SÍ → TradeCards() obligatorio         │
  │     → TradeCount++ → RemainingReinforcements += 8              │
  │     → Total por colocar: 17                                    │
  │                                                                │
  │  3. PlaceReinforcements(Alaska, 5)       → Remaining: 12      │
  │     PlaceReinforcements(NWT, 3)          → Remaining: 9       │
  │     PlaceReinforcements(Ontario, 9)      → Remaining: 0       │
  │                                                                │
  │  4. ConfirmReinforcements()                                    │
  │     → Phase = Attack                                           │
  │                                                                │
  │  5. Attack(Alaska → Kamchatka, 3 dados)  → Resultado: ...     │
  │     Attack(Alaska → Kamchatka, 3 dados)  → ¡Conquista!        │
  │     → MoveArmiesAfterConquest(Alaska, Kamchatka, 4)            │
  │     → ConqueredThisTurn = true                                 │
  │                                                                │
  │  6. EndAttackPhase()                                           │
  │     → Phase = Fortification                                    │
  │                                                                │
  │  7. Fortify(Kamchatka → Mongolia, 2)                           │
  │     → Phase = Reinforcement (siguiente jugador)                │
  │     → Carta otorgada a Carlos (conquistó este turno)           │
  │                                                                │
  │  FIN DEL TURNO → Turno de "Ana"                                │
  └────────────────────────────────────────────────────────────────┘
```

---

## 10. Condiciones de Victoria y Eliminación

### 10.1 Victoria

```csharp
public bool IsGameOver(Game game)
{
    // Un solo jugador activo = victoria
    return game.GetActivePlayers().Count() == 1;
}

public Player? GetWinner(Game game)
{
    if (!IsGameOver(game)) return null;
    return game.GetActivePlayers().First();
}
```

**Condición:** Controlar los 42 territorios (equivale a ser el único jugador no eliminado).

### 10.2 Eliminación

Un jugador es eliminado cuando pierde su último territorio:

| Evento | Consecuencia |
|--------|-------------|
| Defensor pierde último territorio | `IsEliminated = true` |
| Cartas del eliminado | Se transfieren al conquistador |
| Si conquistador acumula ≥6 cartas | Debe canjear inmediatamente (en su próximo refuerzo) |
| Si queda 1 jugador activo | La partida termina |

### 10.3 Partida de 2 Jugadores (Jugador Neutral)

```csharp
// Al inicializar una partida de 2 jugadores:
if (game.Players.Count == 2)
{
    // Crear jugador neutral
    var neutralPlayer = new Player
    {
        Id = "neutral",
        Name = "Neutral",
        Color = PlayerColor.Neutral,
        IsConnected = false  // Nunca juega activamente
    };
    game.Players.Add(neutralPlayer);

    // En la distribución de territorios, el jugador neutral
    // recibe su parte (1/3 de los 42 territorios = 14)
}
```

El jugador neutral:
- **Nunca ataca** ni fortifica.
- **Se salta su turno** automáticamente.
- Cuando es atacado, el sistema tira los dados de defensa automáticamente.
- Sus territorios actúan como barreras estratégicas.

---

## 11. DiceService: Servicio de Dados

```csharp
public interface IDiceService
{
    /// <summary>
    /// Tira N dados de 6 caras. Retorna los resultados en orden descendente.
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
            .Select(_ => Random.Shared.Next(1, 7))  // 1-6
            .OrderByDescending(d => d)
            .ToArray();
    }
}
```

> **Nota:** Se usa `Random.Shared` (thread-safe en .NET 6+). No se necesita seed fijo excepto en tests, donde se inyecta un mock de `IDiceService`.

---

## 12. Integración: GameHub → GameEngine → GameManager

El flujo completo de una acción muestra cómo el motor se conecta con el resto del sistema:

```
  GameHub (SignalR)                    GameEngine                      GameManager
  ─────────────────                    ──────────                      ───────────
       │                                   │                               │
  1. Recibe invocación                     │                               │
     Attack(gameId, playerId,              │                               │
            from, to, dice)                │                               │
       │                                   │                               │
  2. ─── GetGame(gameId) ─────────────────────────────────────────────────▶│
       │◀── game ────────────────────────────────────────────────────────── │
       │                                   │                               │
  3. ─── Attack(game, playerId, ─────────▶│                               │
       │       from, to, dice)            │                               │
       │                                   │ Valida, tira dados,           │
       │                                   │ resuelve, aplica pérdidas     │
       │                                   │                               │
       │◀── AttackGameResult ─────────────│                               │
       │                                   │                               │
  4. ─── UpdateGame(game) ────────────────────────────────────────────────▶│
       │                                   │                               │
  5. Notifica al grupo SignalR:            │                               │
     → DiceRolled(result)                  │                               │
     → GameStateUpdated(state)             │                               │
     → TerritoryConquered (si aplica)      │                               │
     → PlayerEliminated (si aplica)        │                               │
     → GameOver (si aplica)                │                               │
       │                                   │                               │
```

---

## 13. Resumen de Métodos por Fase

| Fase | Métodos del GameEngine | Descripción |
|------|----------------------|-------------|
| **Setup** | `InitializeGame()` | Crea territorios, continentes, mazo, determina orden |
| | `DistributeTerritoriesRandomly()` | Reparte 42 territorios entre jugadores |
| | `PlaceInitialArmies()` | Coloca ejércitos restantes por turnos |
| | `StartPlaying()` | Transición a la fase de juego activo |
| **Reinforcement** | `CalculateReinforcements()` | Calcula ejércitos: territorios + continentes |
| | `TradeCards()` | Canjea cartas por ejércitos adicionales |
| | `PlaceReinforcements()` | Coloca ejércitos en territorios propios |
| | `ConfirmReinforcements()` | Valida que se colocaron todos → avanza a Attack |
| **Attack** | `Attack()` | Valida, tira dados, resuelve combate |
| | `MoveArmiesAfterConquest()` | Mueve ejércitos al territorio conquistado |
| | `EndAttackPhase()` | Pasa a la fase de fortificación |
| **Fortification** | `Fortify()` | Mueve ejércitos entre territorios propios conectados (BFS) |
| | `SkipFortification()` | Salta la fortificación, termina turno |
| | `AreConnected()` | Verifica camino conectado de territorios propios |
| **Turno** | `EndTurn()` | Otorga carta, avanza jugador, calcula refuerzos |
| **Victoria** | `IsGameOver()` | ¿Queda solo 1 jugador activo? |
| | `GetWinner()` | Retorna el jugador ganador |

---

> **Siguiente documento:** [07 — Gestión de Estado](./07_Gestion_Estado.md)
