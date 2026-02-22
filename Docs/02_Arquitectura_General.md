# 02 — Arquitectura General

> **Documento:** 02 de 14  
> **Versión:** 1.0  
> **Última actualización:** 22 de febrero de 2026  
> **Volver al índice:** [00_Indice.md](./00_Indice.md)  
> **Documento anterior:** [01 — Visión General](./01_Vision_General.md)

---

## 1. Diagrama de Arquitectura de Alto Nivel

```
┌──────────────────────────────────────────────────────────────────────────┐
│                           RED LOCAL (LAN)                               │
│                                                                         │
│  ┌─────────────┐  ┌─────────────┐       ┌─────────────┐                │
│  │  Navegador   │  │  Navegador   │  ...  │  Navegador   │               │
│  │  Jugador 1   │  │  Jugador 2   │       │  Jugador N   │               │
│  │             │  │             │       │             │               │
│  │ ┌─────────┐ │  │ ┌─────────┐ │       │ ┌─────────┐ │               │
│  │ │ DOM/HTML │ │  │ │ DOM/HTML │ │       │ │ DOM/HTML │ │               │
│  │ │ (render) │ │  │ │ (render) │ │       │ │ (render) │ │               │
│  │ └────┬────┘ │  │ └────┬────┘ │       │ └────┬────┘ │               │
│  │      │      │  │      │      │       │      │      │               │
│  │ blazor.web.js│  │ blazor.web.js│       │ blazor.web.js│               │
│  └──────┬──────┘  └──────┬──────┘       └──────┬──────┘               │
│         │ WebSocket      │ WebSocket           │ WebSocket             │
│         │ (Circuito)     │ (Circuito)          │ (Circuito)            │
│         └────────────────┼─────────────────────┘                       │
│                          │                                              │
│                          ▼                                              │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                    SERVIDOR BLAZOR SERVER                        │   │
│  │                     (Kestrel / .NET 10)                          │   │
│  │                                                                  │   │
│  │  ┌──────────────────────────────────────────────────────────┐    │   │
│  │  │                   ASP.NET Core Pipeline                   │    │   │
│  │  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌─────────┐  │    │   │
│  │  │  │ Static   │  │Antiforgery│  │  Razor   │  │ SignalR │  │    │   │
│  │  │  │ Assets   │  │          │  │Components│  │  Hubs   │  │    │   │
│  │  │  └──────────┘  └──────────┘  └────┬─────┘  └────┬────┘  │    │   │
│  │  └───────────────────────────────────┼──────────────┼───────┘    │   │
│  │                                      │              │            │   │
│  │  ┌───────────────────────────────────┼──────────────┼───────┐    │   │
│  │  │              Capa de Servicios    │              │        │    │   │
│  │  │  ┌─────────────┐  ┌──────────────┴──┐  ┌───────┴─────┐  │    │   │
│  │  │  │PlayerSession│  │  GameManager    │  │  GameHub    │  │    │   │
│  │  │  │  Service    │  │  (Singleton)    │  │ (SignalR)   │  │    │   │
│  │  │  │  (Scoped)   │  │                 │  │             │  │    │   │
│  │  │  └─────────────┘  └───────┬─────────┘  └─────────────┘  │    │   │
│  │  │                           │                              │    │   │
│  │  │  ┌─────────────┐  ┌──────┴──────┐  ┌───────────────┐    │    │   │
│  │  │  │ MapService  │  │ GameEngine  │  │  DiceService  │    │    │   │
│  │  │  │(Singleton)  │  │(Transient)  │  │ (Transient)   │    │    │   │
│  │  │  └─────────────┘  └─────────────┘  └───────────────┘    │    │   │
│  │  │                                     ┌───────────────┐    │    │   │
│  │  │                                     │ CardService   │    │    │   │
│  │  │                                     │ (Transient)   │    │    │   │
│  │  │                                     └───────────────┘    │    │   │
│  │  └──────────────────────────────────────────────────────────┘    │   │
│  │                                                                  │   │
│  │  ┌──────────────────────────────────────────────────────────┐    │   │
│  │  │                  Estado en Memoria (RAM)                  │    │   │
│  │  │                                                          │    │   │
│  │  │  ┌──────────┐  ┌──────────┐  ┌──────────┐               │    │   │
│  │  │  │ Partida 1│  │ Partida 2│  │ Partida 3│   ...         │    │   │
│  │  │  │ (Game)   │  │ (Game)   │  │ (Game)   │               │    │   │
│  │  │  └──────────┘  └──────────┘  └──────────┘               │    │   │
│  │  └──────────────────────────────────────────────────────────┘    │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│                     ⚠️ SIN BASE DE DATOS                                │
│                     Todo el estado vive en memoria                       │
└──────────────────────────────────────────────────────────────────────────┘
```

### 1.1 Principios Arquitectónicos

| Principio | Aplicación en MiniRisk |
|-----------|----------------------|
| **Simplicidad** | Sin base de datos, sin autenticación, sin capas innecesarias |
| **Estado centralizado** | Todo el estado de las partidas vive en un `GameManager` singleton en el servidor |
| **Tiempo real** | SignalR para comunicación bidireccional instantánea |
| **Separación de responsabilidades** | Motor del juego (lógica) separado de los componentes (UI) y del hub (comunicación) |
| **Inyección de dependencias** | Todos los servicios se registran en el contenedor de DI de ASP.NET Core |
| **Diseño para la red local** | Optimizado para latencia mínima, sin preocupación por escalabilidad masiva |

---

## 2. Modelo Blazor Server

### 2.1 ¿Qué es Blazor Server?

Blazor Server es un modelo de hosting donde:

1. **Toda la lógica de la aplicación se ejecuta en el servidor**, dentro del proceso ASP.NET Core.
2. El navegador solo renderiza HTML/CSS y captura eventos del usuario.
3. La comunicación entre navegador y servidor se realiza a través de una **conexión WebSocket persistente** llamada **circuito**.
4. Cuando el usuario interactúa con la UI (click, input, etc.), el evento viaja al servidor, se procesa, y el servidor envía de vuelta las diferencias del DOM (diffs) para actualizar la UI.

### 2.2 ¿Por qué Blazor Server para MiniRisk?

| Ventaja | Relevancia para MiniRisk |
|---------|-------------------------|
| **Código C# en servidor** | Toda la lógica del juego en C#, sin duplicar en JavaScript |
| **SignalR integrado** | La conexión WebSocket ya existe por defecto; añadir un hub de juego es natural |
| **Sin API REST** | No necesitamos endpoints REST; la comunicación es directa servidor↔cliente |
| **Estado en servidor** | El estado del juego ya vive en el servidor; no hay que sincronizar con el cliente |
| **Latencia baja en LAN** | En red local, la latencia del circuito es despreciable (<5ms) |
| **Seguridad del estado** | Los jugadores no pueden manipular el estado del juego desde el navegador |
| **Inicio rápido** | Sin descarga de WebAssembly; la app carga inmediatamente |

| Limitación | Impacto en MiniRisk |
|------------|-------------------|
| **Dependencia del servidor** | Aceptable: el servidor es un PC en la red local |
| **Conexión persistente** | Aceptable: solo 2–6 jugadores simultaneos |
| **Escalabilidad limitada** | Irrelevante: máximo 3 partidas simultáneas |
| **Sin modo offline** | Irrelevante: el juego es multijugador por definición |

### 2.3 Ciclo de Vida de un Circuito Blazor Server

```
┌─────────┐                              ┌──────────┐
│ Navegador│                              │ Servidor │
└────┬────┘                              └────┬─────┘
     │                                        │
     │──── GET / (solicitud HTTP inicial) ────▶│
     │                                        │
     │◀─── HTML + blazor.web.js ──────────────│
     │                                        │
     │──── WebSocket (circuito abierto) ─────▶│
     │                                        │ Crea instancia del circuito
     │                                        │ Crea scope de DI (Scoped)
     │                                        │ Inicializa componentes
     │                                        │
     │◀─── Render inicial (DOM completo) ─────│
     │                                        │
     │                                        │
     │  ╔══════════════════════════════════╗   │
     │  ║   CICLO DE INTERACCIÓN           ║   │
     │  ║                                  ║   │
     │  ║  1. Usuario hace click/input     ║   │
     │  ║  2. Evento → servidor (WS)       ║   │
     │  ║  3. Servidor procesa evento      ║   │
     │  ║  4. Servidor recalcula render    ║   │
     │  ║  5. Diff del DOM → cliente (WS)  ║   │
     │  ║  6. blazor.web.js aplica diff    ║   │
     │  ║                                  ║   │
     │  ╚══════════════════════════════════╝   │
     │                                        │
     │──── Cierre (navegador cerrado) ───────▶│
     │                                        │ Destruye circuito
     │                                        │ Dispone scope de DI
     │                                        │
```

### 2.4 Circuito vs Hub: Dos Canales de Comunicación

En MiniRisk conviven **dos canales SignalR** con propósitos distintos:

| Canal | Propósito | Creado por | Ámbito |
|-------|----------|-----------|--------|
| **Circuito Blazor** | Renderizar la UI del jugador individual. Capturar eventos del DOM y enviar diffs de vuelta. | Blazor Server (automático) | Un circuito por navegador/pestaña |
| **GameHub** | Comunicar eventos del juego a todos los jugadores de una partida (estado, turnos, dados, chat). | Nosotros (explícito) | Un grupo de hub por partida |

```
                    Jugador 1                          Jugador 2
                 ┌────────────┐                     ┌────────────┐
                 │  Navegador  │                     │  Navegador  │
                 └──┬─────┬───┘                     └──┬─────┬───┘
                    │     │                             │     │
         Circuito   │     │  Hub                Hub    │     │  Circuito
         (render)   │     │  (juego)          (juego)  │     │  (render)
                    │     │                             │     │
                    ▼     ▼                             ▼     ▼
              ┌──────────────────────────────────────────────────┐
              │                    SERVIDOR                      │
              │                                                  │
              │  Circuito 1          GameHub           Circuito 2│
              │  (scope J1)    ┌──────────────┐       (scope J2) │
              │       │        │ Grupo:       │           │      │
              │       │        │ "partida-abc"│           │      │
              │       ▼        │              │           ▼      │
              │  Componentes   │ Jugador 1    │    Componentes   │
              │  Blazor J1     │ Jugador 2    │    Blazor J2     │
              │                └──────┬───────┘                  │
              │                       │                          │
              │                       ▼                          │
              │               ┌──────────────┐                   │
              │               │ GameManager  │                   │
              │               │  (Singleton) │                   │
              │               │              │                   │
              │               │ ┌──────────┐ │                   │
              │               │ │ Partida  │ │                   │
              │               │ │  "abc"   │ │                   │
              │               │ └──────────┘ │                   │
              │               └──────────────┘                   │
              └──────────────────────────────────────────────────┘
```

**¿Por qué dos canales?**

- El **circuito Blazor** es individual: gestiona la UI de **un** jugador. Cuando un jugador hace click en un territorio, solo su circuito lo procesa.
- El **GameHub** es colectivo: cuando se resuelve un ataque, **todos** los jugadores de la partida deben ver el resultado. El hub envía el evento al grupo.
- Los **componentes Blazor** escuchan los eventos del Hub y llaman a `StateHasChanged()` para re-renderizar la UI con el nuevo estado.

---

## 3. Comunicación en Tiempo Real con SignalR

> **Nota:** Este apartado es un resumen. El detalle completo del diseño de SignalR se documenta en [04 — SignalR](./04_SignalR.md).

### 3.1 GameHub: Visión General

El `GameHub` es la clase central de comunicación. Hereda de `Hub` y define:

- **Métodos del servidor** (invocados por los clientes): acciones como unirse a una partida, atacar, pasar de fase, enviar un mensaje de chat.
- **Métodos del cliente** (invocados por el servidor): notificaciones como actualizar el tablero, mostrar resultado de dados, nuevo mensaje de chat.

```
┌───────────────────────────────────────────────────────────┐
│                        GameHub                             │
│                                                           │
│  Métodos del Servidor (los clientes invocan):              │
│  ├── JoinGame(gameId, playerName)                          │
│  ├── LeaveGame(gameId)                                     │
│  ├── PlaceArmies(gameId, territoryId, count)               │
│  ├── Attack(gameId, fromTerritory, toTerritory, diceCount) │
│  ├── EndAttackPhase(gameId)                                │
│  ├── Fortify(gameId, fromTerritory, toTerritory, count)    │
│  ├── EndTurn(gameId)                                       │
│  ├── TradeCards(gameId, cardIds[])                          │
│  ├── SendChatMessage(gameId, message)                      │
│  └── ...                                                   │
│                                                           │
│  Métodos del Cliente (el servidor invoca):                  │
│  ├── GameStateUpdated(gameState)                           │
│  ├── TurnChanged(playerId)                                 │
│  ├── DiceRolled(attackResult)                              │
│  ├── TerritoryConquered(territoryId, newOwnerId)           │
│  ├── PlayerEliminated(playerId)                            │
│  ├── GameOver(winnerId)                                    │
│  ├── ChatMessageReceived(playerName, message)              │
│  ├── PlayerConnected(playerName)                           │
│  ├── PlayerDisconnected(playerName)                        │
│  └── ...                                                   │
└───────────────────────────────────────────────────────────┘
```

### 3.2 Grupos de SignalR

Cada partida se mapea a un **grupo de SignalR**. Cuando un jugador se une a una partida, su conexión se añade al grupo. Los mensajes del juego se envían al grupo completo:

```csharp
// Servidor: cuando un jugador se une
await Groups.AddToGroupAsync(Context.ConnectionId, $"game-{gameId}");

// Servidor: notificar a todos los jugadores de la partida
await Clients.Group($"game-{gameId}").SendAsync("GameStateUpdated", gameState);
```

### 3.3 Integración Hub ↔ Componentes Blazor

Los componentes Blazor se conectan al `GameHub` como clientes:

```
Componente Blazor (Game.razor)
        │
        ├── OnInitializedAsync()
        │       └── Crea HubConnection al GameHub
        │       └── Registra handlers para eventos del cliente
        │       └── Invoca JoinGame()
        │
        ├── Al recibir "GameStateUpdated":
        │       └── Actualiza estado local
        │       └── Llama InvokeAsync(StateHasChanged)
        │
        ├── Al hacer click en "Atacar":
        │       └── Invoca hub.SendAsync("Attack", ...)
        │
        └── OnDispose()
                └── Invoca LeaveGame()
                └── Cierra HubConnection
```

---

## 4. Gestión de Estado en Servidor

### 4.1 Sin Base de Datos

MiniRisk **no utiliza base de datos**. Todo el estado se mantiene en la memoria RAM del servidor:

| Aspecto | Decisión |
|---------|---------|
| **Almacenamiento** | `ConcurrentDictionary<string, Game>` dentro del `GameManager` (Singleton) |
| **Persistencia** | Ninguna. Si el servidor se reinicia, todas las partidas se pierden |
| **Recuperabilidad** | No aplica. Las partidas son efímeras por diseño |
| **Concurrencia** | Gestionada con `ConcurrentDictionary` y locks donde sea necesario |

### 4.2 Jerarquía del Estado

```
GameManager (Singleton)
│
├── Partidas activas: Dictionary<string, Game>
│   │
│   └── Game "abc-123"
│       ├── Id: "abc-123"
│       ├── Name: "Partida de los viernes"
│       ├── Phase: GamePhase.Attack
│       ├── CurrentPlayerIndex: 2
│       ├── TradeCount: 3
│       ├── Players: List<Player>
│       │   ├── Player { Name: "Carlos", Color: Red, ... }
│       │   ├── Player { Name: "Ana", Color: Blue, ... }
│       │   └── Player { Name: "Luis", Color: Green, ... }
│       ├── Territories: Dictionary<TerritoryName, Territory>
│       │   ├── Territory { Name: Alaska, Owner: "Carlos", Armies: 5 }
│       │   ├── Territory { Name: Kamchatka, Owner: "Ana", Armies: 3 }
│       │   └── ... (42 territorios)
│       ├── Cards: Queue<Card>  (mazo)
│       ├── DiscardPile: List<Card>
│       └── Log: List<GameEvent>
│
└── Jugadores conectados: Dictionary<string, PlayerSession>
    ├── ConnectionId → { PlayerName, GameId, ... }
    └── ...
```

### 4.3 Ciclos de Vida de los Servicios

| Servicio | Ciclo de vida | Justificación |
|----------|:------------:|---------------|
| `GameManager` | **Singleton** | Debe sobrevivir a todos los circuitos. Es el repositorio central de todas las partidas activas. Compartido por todos los jugadores. |
| `MapService` | **Singleton** | Los datos del mapa (territorios, adyacencias, continentes) son estáticos e inmutables. Se cargan una vez y se reutilizan. |
| `GameEngine` | **Transient** | Motor de reglas sin estado propio. Recibe el estado de la partida como parámetro y devuelve resultados. Puede ser transient sin problemas. |
| `DiceService` | **Transient** | Generación de tiradas de dados. Sin estado. Una nueva instancia por uso es óptima. |
| `CardService` | **Transient** | Lógica de validación de canjes y gestión de cartas. Sin estado propio. |
| `PlayerSessionService` | **Scoped** | Almacena el nombre y el estado de sesión del jugador actual. Vive durante el circuito Blazor de un jugador. Cada jugador tiene su propia instancia. |
| `GameHub` | **Transient** | Los hubs de SignalR son transient por diseño de ASP.NET Core. Se crea una instancia por invocación de método. |

---

## 5. Estructura de la Solución y Organización de Carpetas

### 5.1 Estructura Actual (Plantilla)

El proyecto ha sido creado con la plantilla de Blazor Server de .NET 10 y actualmente contiene la estructura por defecto:

```
MiniRisk/                          ← Raíz del proyecto
├── MiniRisk.slnx                  ← Solución
├── MiniRisk.csproj                ← Proyecto principal
├── Program.cs                     ← Punto de entrada
├── appsettings.json               ← Configuración base
├── appsettings.Development.json   ← Configuración de desarrollo
├── Properties/
│   └── launchSettings.json        ← Configuración de lanzamiento
├── Components/
│   ├── App.razor                  ← Componente raíz HTML
│   ├── Routes.razor               ← Configuración del router
│   ├── _Imports.razor             ← Usings globales de Razor
│   ├── Layout/
│   │   ├── MainLayout.razor       ← Layout principal (sidebar + contenido)
│   │   ├── MainLayout.razor.css
│   │   ├── NavMenu.razor          ← Menú de navegación lateral
│   │   ├── NavMenu.razor.css
│   │   ├── ReconnectModal.razor   ← Modal de reconexión (ya existente ✓)
│   │   ├── ReconnectModal.razor.css
│   │   └── ReconnectModal.razor.js
│   └── Pages/
│       ├── Home.razor             ← Página de inicio (por defecto)
│       ├── Counter.razor          ← Página de ejemplo (eliminar)
│       ├── Weather.razor          ← Página de ejemplo (eliminar)
│       ├── Error.razor            ← Página de error
│       └── NotFound.razor         ← Página 404
└── wwwroot/
    ├── app.css                    ← Estilos globales
    ├── favicon.png                ← Favicon
    └── lib/
        └── bootstrap/             ← Bootstrap (CSS framework)
```

### 5.2 Estructura Objetivo (MiniRisk)

```
MiniRisk/
├── MiniRisk.slnx
├── MiniRisk.csproj
├── Program.cs                         ← Configuración de DI y pipeline
├── appsettings.json
├── appsettings.Development.json
│
├── Components/
│   ├── App.razor                      ← Componente raíz (modificado)
│   ├── Routes.razor                   ← Router (sin cambios)
│   ├── _Imports.razor                 ← Usings globales (ampliado)
│   │
│   ├── Layout/
│   │   ├── GameLayout.razor           ← Layout específico para la partida (sin sidebar)
│   │   ├── GameLayout.razor.css
│   │   ├── MainLayout.razor           ← Layout para Home y Lobby (con sidebar)
│   │   ├── MainLayout.razor.css
│   │   ├── ReconnectModal.razor       ← Modal de reconexión (existente)
│   │   ├── ReconnectModal.razor.css
│   │   └── ReconnectModal.razor.js
│   │
│   ├── Pages/
│   │   ├── Home.razor                 ← Pantalla de bienvenida (nombre del jugador)
│   │   ├── Lobby.razor                ← Lista de partidas / crear / unirse
│   │   ├── Game.razor                 ← Tablero de juego principal
│   │   ├── Error.razor                ← Página de error
│   │   └── NotFound.razor             ← Página 404
│   │
│   └── Shared/                        ← Componentes reutilizables del juego
│       ├── WorldMap.razor             ← Mapa SVG interactivo
│       ├── WorldMap.razor.css
│       ├── TerritoryView.razor        ← Territorio individual en el mapa
│       ├── TerritoryView.razor.css
│       ├── DiceRoller.razor           ← Animación y resultado de dados
│       ├── DiceRoller.razor.css
│       ├── PlayerPanel.razor          ← Panel de info del jugador
│       ├── PlayerPanel.razor.css
│       ├── PlayersOverview.razor      ← Resumen de todos los jugadores
│       ├── PlayersOverview.razor.css
│       ├── CardHand.razor             ← Mano de cartas del jugador
│       ├── CardHand.razor.css
│       ├── CardTradeDialog.razor      ← Diálogo de canje de cartas
│       ├── CardTradeDialog.razor.css
│       ├── ChatBox.razor              ← Chat en tiempo real
│       ├── ChatBox.razor.css
│       ├── GameLog.razor              ← Historial de eventos
│       ├── GameLog.razor.css
│       ├── PhaseIndicator.razor       ← Indicador de fase del turno
│       ├── PhaseIndicator.razor.css
│       ├── AttackDialog.razor         ← Diálogo de selección de dados para ataque
│       ├── AttackDialog.razor.css
│       ├── FortifyDialog.razor        ← Diálogo de movimiento de tropas
│       ├── FortifyDialog.razor.css
│       ├── VictoryScreen.razor        ← Pantalla de victoria
│       └── VictoryScreen.razor.css
│
├── Hubs/
│   └── GameHub.cs                     ← Hub de SignalR para el juego
│
├── Models/
│   ├── Game.cs                        ← Entidad Partida
│   ├── Player.cs                      ← Entidad Jugador
│   ├── Territory.cs                   ← Entidad Territorio
│   ├── Continent.cs                   ← Entidad Continente
│   ├── Card.cs                        ← Entidad Carta de territorio
│   ├── AttackResult.cs                ← Resultado de un ataque (dados, pérdidas)
│   ├── GameEvent.cs                   ← Evento del log de la partida
│   ├── PlayerSession.cs               ← Sesión del jugador conectado
│   ├── GameSettings.cs                ← Configuración de una partida
│   ├── GameSummary.cs                 ← Resumen de partida (para el lobby)
│   └── Enums/
│       ├── GamePhase.cs               ← Fases del juego
│       ├── GameStatus.cs              ← Estado de la partida (Waiting, Playing, Finished)
│       ├── CardType.cs                ← Tipos de carta
│       ├── TerritoryName.cs           ← Nombres de los 42 territorios
│       ├── ContinentName.cs           ← Nombres de los 6 continentes
│       ├── PlayerColor.cs             ← Colores disponibles para jugadores
│       └── GameEventType.cs           ← Tipos de evento del log
│
├── Services/
│   ├── Interfaces/
│   │   ├── IGameManager.cs            ← Interfaz del gestor de partidas
│   │   ├── IGameEngine.cs             ← Interfaz del motor de reglas
│   │   ├── IDiceService.cs            ← Interfaz del servicio de dados
│   │   ├── ICardService.cs            ← Interfaz del servicio de cartas
│   │   ├── IMapService.cs             ← Interfaz del servicio de mapa
│   │   └── IPlayerSessionService.cs   ← Interfaz del servicio de sesión
│   │
│   ├── GameManager.cs                 ← Implementación del gestor de partidas
│   ├── GameEngine.cs                  ← Implementación del motor de reglas
│   ├── DiceService.cs                 ← Implementación del servicio de dados
│   ├── CardService.cs                 ← Implementación del servicio de cartas
│   ├── MapService.cs                  ← Implementación del servicio de mapa
│   └── PlayerSessionService.cs        ← Implementación del servicio de sesión
│
├── Docs/                              ← Documentación de diseño
│   ├── 00_Indice.md
│   ├── 01_Vision_General.md
│   ├── 02_Arquitectura_General.md     ← ← Este documento
│   └── ...
│
└── wwwroot/
    ├── css/
    │   ├── app.css                    ← Estilos globales del juego
    │   └── game.css                   ← Estilos específicos del tablero
    ├── images/
    │   ├── map/                       ← Recursos del mapa (si se necesitan)
    │   └── icons/                     ← Iconos del juego
    ├── favicon.png
    └── lib/
        └── bootstrap/                 ← Bootstrap (mantener para layout base)
```

### 5.3 Cambios Respecto a la Plantilla Original

| Cambio | Acción |
|--------|--------|
| `Counter.razor`, `Weather.razor` | **Eliminar** — Páginas de ejemplo innecesarias |
| `Home.razor` | **Reemplazar** — Convertir en pantalla de bienvenida con ingreso de nombre |
| `MainLayout.razor` | **Modificar** — Adaptar para el layout de Home y Lobby |
| `NavMenu.razor` | **Modificar** — Adaptar navegación al juego o eliminar |
| `_Imports.razor` | **Ampliar** — Añadir namespaces de Models, Services, Shared |
| `Program.cs` | **Ampliar** — Registrar servicios y mapear el GameHub |
| Carpetas `Hubs/`, `Models/`, `Services/` | **Crear** — Nuevas carpetas para la lógica del juego |
| Carpeta `Components/Shared/` | **Crear** — Componentes reutilizables del juego |
| `GameLayout.razor` | **Crear** — Layout específico para la página de juego |

---

## 6. Flujo de Datos

### 6.1 Flujo General: Acción del Jugador → Actualización Global

```
  Jugador 1 (navegador)              Servidor                    Jugador 2 (navegador)
  ─────────────────────              ────────                    ─────────────────────
         │                               │                               │
  1. Click en territorio                 │                               │
  2. Evento → Circuito Blazor ──────────▶│                               │
         │                    3. Componente procesa                       │
         │                       el click e invoca                        │
         │                       hub.SendAsync("Attack",...)              │
         │                               │                               │
         │                    4. GameHub recibe ──▶ GameEngine            │
         │                       la invocación       valida y             │
         │                                          resuelve             │
         │                               │                               │
         │                    5. GameManager actualiza                    │
         │                       el estado de la partida                  │
         │                               │                               │
         │                    6. Hub envía a grupo:                       │
         │                       "GameStateUpdated"                       │
         │                               │                               │
         │◀─────── 7. Handler del hub ───┤──── 7. Handler del hub ──────▶│
         │            recibe evento      │       recibe evento           │
         │                               │                               │
  8. StateHasChanged()                   │              8. StateHasChanged()
  9. UI se re-renderiza                  │              9. UI se re-renderiza
         │                               │                               │
```

### 6.2 Flujo Detallado: Un Ataque Completo

```
  Jugador Atacante                      Servidor                     Jugador Defensor
  ────────────────                      ────────                     ────────────────
         │                                  │                                │
  1. Selecciona territorio                  │                                │
     atacante (click)                       │                                │
         │                                  │                                │
  2. Selecciona territorio    ─────────────▶│                                │
     defensor (click)                       │                                │
         │                                  │                                │
  3. Elige nº de dados        ─────────────▶│                                │
     y confirma ataque                      │                                │
         │                          4. GameHub.Attack()                      │
         │                          5. GameEngine.ResolveAttack()            │
         │                             a. Valida: ¿tiene suficientes        │
         │                                ejércitos? ¿son adyacentes?       │
         │                             b. DiceService.Roll(atacante)         │
         │                             c. DiceService.Roll(defensor)         │
         │                             d. Compara dados                      │
         │                             e. Calcula pérdidas                   │
         │                             f. ¿Conquista? Mover ejércitos       │
         │                             g. ¿Eliminación? Transferir cartas   │
         │                          6. GameManager.UpdateState()             │
         │                          7. Hub → Grupo: DiceRolled(result)      │
         │                          8. Hub → Grupo: GameStateUpdated(state) │
         │                                  │                                │
         │◀──────── DiceRolled ─────────────┼─────── DiceRolled ───────────▶│
         │◀──────── GameStateUpdated ───────┼─────── GameStateUpdated ─────▶│
         │                                  │                                │
  9. Muestra animación                     │               9. Muestra animación
     de dados + resultado                   │                  de dados + resultado
  10. Actualiza mapa                        │               10. Actualiza mapa
         │                                  │                                │
```

### 6.3 Flujo: Entrada del Jugador (Sin Autenticación)

```
  Nuevo Jugador                          Servidor
  ─────────────                          ────────
         │                                  │
  1. Abre http://servidor:5000              │
         │                                  │
         │◀─── Home.razor (pedir nombre) ───│
         │                                  │
  2. Escribe nombre: "Carlos"               │
  3. Click "Entrar"           ─────────────▶│
         │                          4. PlayerSessionService
         │                             almacena "Carlos" (Scoped)
         │                          5. Redirige a /lobby
         │                                  │
         │◀─── Lobby.razor ────────────────│
         │     (lista de partidas)          │
         │                                  │
  6. Click "Crear partida"    ─────────────▶│
         │                          7. GameManager.CreateGame()
         │                          8. Hub: JoinGame(gameId)
         │                                  │
         │◀─── Game.razor ─────────────────│
         │     (tablero de juego)           │
         │                                  │
```

---

## 7. Diagrama de Componentes Principales

### 7.1 Mapa de Componentes y sus Relaciones

```
App.razor
└── Routes.razor
    ├── [MainLayout]
    │   ├── Home.razor                  ← @page "/"
    │   │   └── (formulario de nombre)
    │   │
    │   └── Lobby.razor                 ← @page "/lobby"
    │       └── (lista de partidas, crear/unirse)
    │
    └── [GameLayout]
        └── Game.razor                  ← @page "/game/{GameId}"
            │
            ├── PhaseIndicator          ← Muestra fase actual y jugador en turno
            │
            ├── PlayersOverview         ← Resumen de todos los jugadores
            │   └── PlayerPanel ×N      ← Info de cada jugador (nombre, color, territorios, ejércitos)
            │
            ├── WorldMap                ← Mapa SVG interactivo
            │   └── TerritoryView ×42   ← Cada territorio del mapa
            │
            ├── CardHand                ← Cartas del jugador actual
            │   └── CardTradeDialog     ← Diálogo de canje (condicional)
            │
            ├── AttackDialog            ← Selección de dados para ataque (condicional)
            │   └── DiceRoller          ← Animación de dados
            │
            ├── FortifyDialog           ← Movimiento de tropas (condicional)
            │
            ├── GameLog                 ← Historial de acciones
            │
            ├── ChatBox                 ← Chat en tiempo real
            │
            └── VictoryScreen           ← Pantalla de fin de partida (condicional)
```

### 7.2 Flujo de Datos entre Componentes

```
┌─────────────────────────────────────────────────────────────────┐
│  Game.razor (componente padre - orquestador)                    │
│                                                                 │
│  Estado:                                                        │
│  ├── HubConnection (conexión al GameHub)                        │
│  ├── GameState (estado completo de la partida)                  │
│  ├── CurrentPlayerId (jugador actual del circuito)              │
│  └── SelectedTerritory (territorio seleccionado)                │
│                                                                 │
│  ┌──────────┐     ┌──────────────┐     ┌────────────┐          │
│  │PhaseIndic│     │PlayersOverview│     │  WorldMap  │          │
│  │ator     │     │              │     │            │          │
│  │          │     │ [Parameter]  │     │ [Parameter]│          │
│  │ Phase ◀──┤     │ Players ◀────┤     │ Territories│◀─┐      │
│  │ Current  │     │ CurrentId    │     │ SelectedId │  │      │
│  │ Player   │     │              │     │            │  │      │
│  └──────────┘     └──────────────┘     │[EventCallback]│      │
│                                        │OnTerritoryClick──▶───┤ │
│                                        └────────────┘      │ │
│                                                            │ │
│  Game.razor recibe el click, ──────────────────────────────┘ │
│  determina la acción según                                    │
│  la fase actual, e invoca                                     │
│  el método del hub apropiado.                                 │
│                                                               │
│  ┌──────────┐     ┌──────────┐     ┌────────────┐            │
│  │ CardHand │     │AttackDlg │     │ FortifyDlg │            │
│  │          │     │          │     │            │            │
│  │Cards ◀───┤     │From ◀────┤     │From ◀──────┤            │
│  │OnTrade ──┤▶    │To ◀──────┤     │To ◀────────┤            │
│  │          │     │OnAttack──┤▶    │OnFortify ──┤▶           │
│  └──────────┘     │          │     │            │            │
│                   │DiceRoller│     └────────────┘            │
│                   │Result ◀──┤                                │
│                   └──────────┘                                │
│                                                               │
│  ┌──────────┐     ┌──────────┐                                │
│  │ GameLog  │     │ ChatBox  │                                │
│  │          │     │          │                                │
│  │Events ◀──┤     │Messages◀─┤                                │
│  │          │     │OnSend ───┤▶                                │
│  └──────────┘     └──────────┘                                │
│                                                               │
│  Leyenda:                                                     │
│  ◀── [Parameter] = datos del padre al hijo                    │
│  ──▶ [EventCallback] = evento del hijo al padre               │
└─────────────────────────────────────────────────────────────────┘
```

### 7.3 Patrón de Comunicación: Hub → Componente

Cuando el `GameHub` envía un evento, el componente `Game.razor` (que tiene la `HubConnection`) lo recibe y redistribuye a sus hijos:

```
GameHub                                Game.razor              Componentes hijos
───────                                ──────────              ─────────────────
   │                                       │                          │
   │── SendAsync("GameStateUpdated") ─────▶│                          │
   │                                       │                          │
   │                         Handler actualiza                        │
   │                         this.GameState                           │
   │                                       │                          │
   │                         InvokeAsync(() =>                        │
   │                           StateHasChanged())                     │
   │                                       │                          │
   │                         Blazor re-renderiza ─────────────────────▶│
   │                         Game.razor y pasa                        │
   │                         los nuevos [Parameter]            Re-render con
   │                         a los componentes hijos           nuevos datos
   │                                       │                          │
```

---

## 8. Pipeline de ASP.NET Core

### 8.1 Configuración Actual de `Program.cs`

El `Program.cs` actual contiene la configuración por defecto de la plantilla Blazor Server:

```csharp
// Estado actual (plantilla)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
```

### 8.2 Configuración Objetivo de `Program.cs`

```csharp
// ═══════════════════════════════════════════════════════════
// 1. REGISTRO DE SERVICIOS
// ═══════════════════════════════════════════════════════════

// Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SignalR (para el GameHub)
builder.Services.AddSignalR();

// Servicios del juego — Singleton (estado compartido)
builder.Services.AddSingleton<IGameManager, GameManager>();
builder.Services.AddSingleton<IMapService, MapService>();

// Servicios del juego — Transient (sin estado)
builder.Services.AddTransient<IGameEngine, GameEngine>();
builder.Services.AddTransient<IDiceService, DiceService>();
builder.Services.AddTransient<ICardService, CardService>();

// Servicios del juego — Scoped (por circuito/jugador)
builder.Services.AddScoped<IPlayerSessionService, PlayerSessionService>();

// ═══════════════════════════════════════════════════════════
// 2. PIPELINE HTTP
// ═══════════════════════════════════════════════════════════

app.UseStaticFiles();
app.UseAntiforgery();

// Mapear assets estáticos
app.MapStaticAssets();

// Mapear componentes Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Mapear el hub de SignalR del juego
app.MapHub<GameHub>("/gamehub");

app.Run();
```

### 8.3 Diagrama del Pipeline HTTP

```
Solicitud HTTP entrante
        │
        ▼
┌──────────────────┐
│  UseStaticFiles  │──── ¿Es un archivo estático? ──── SÍ ──▶ Servir archivo
│ (MapStaticAssets)│                                           (CSS, JS, imágenes)
└────────┬─────────┘
         │ NO
         ▼
┌──────────────────┐
│  UseAntiforgery  │──── Validar token antifalsificación
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ MapRazorComponents│──── ¿Es una ruta de página? ──── SÍ ──▶ Renderizar componente
│   <App>          │      ("/", "/lobby", "/game/...")          Blazor Server
└────────┬─────────┘
         │ NO
         ▼
┌──────────────────┐
│   MapHub         │──── ¿Es /gamehub? ──── SÍ ──▶ SignalR Hub
│  <GameHub>       │      (WebSocket)               para comunicación del juego
│  ("/gamehub")    │
└────────┬─────────┘
         │ NO
         ▼
      404 Not Found
```

---

## 9. Diagrama de Dependencias entre Servicios

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│                    Componentes Blazor (UI)                       │
│          Home.razor    Lobby.razor    Game.razor                 │
│                                                                 │
│          Usa: IPlayerSessionService (scoped)                    │
│          Usa: HubConnection (al GameHub)                        │
│                                                                 │
└────────────────────────┬────────────────────────────────────────┘
                         │ invoca métodos vía SignalR
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│                        GameHub (SignalR)                         │
│                                                                 │
│          Inyecta: IGameManager                                  │
│          Inyecta: IGameEngine                                   │
│                                                                 │
└───────┬─────────────────┬───────────────────────────────────────┘
        │                 │
        ▼                 ▼
┌──────────────┐  ┌──────────────┐
│ IGameManager │  │ IGameEngine  │
│ (Singleton)  │  │ (Transient)  │
│              │  │              │
│ Estado de    │  │ Valida       │
│ partidas:    │  │ acciones,    │
│ crear, unir, │  │ resuelve     │
│ buscar,      │  │ combates,    │
│ eliminar     │  │ calcula      │
│              │  │ refuerzos    │
│ Inyecta:     │  │              │
│ IMapService  │  │ Inyecta:     │
│              │  │ IDiceService │
│              │  │ ICardService │
│              │  │ IMapService  │
└──────┬───────┘  └──┬─────┬──┬─┘
       │             │     │  │
       │             ▼     │  │
       │     ┌────────────┐│  │
       │     │IDiceService││  │
       │     │(Transient) ││  │
       │     │            ││  │
       │     │Roll(n)     ││  │
       │     │→ int[]     ││  │
       │     └────────────┘│  │
       │                   ▼  │
       │     ┌────────────┐   │
       │     │ICardService│   │
       │     │(Transient) │   │
       │     │            │   │
       │     │ValidateTrade│  │
       │     │DrawCard     │  │
       │     │Shuffle      │  │
       │     └────────────┘   │
       │                      │
       ▼                      ▼
┌──────────────────────────────┐
│       IMapService            │
│       (Singleton)            │
│                              │
│ GetTerritories()             │
│ GetContinents()              │
│ GetAdjacencies()             │
│ AreAdjacent(t1, t2)          │
│ AreConnected(t1, t2, owner)  │
│ GetContinentBonus(continent) │
│                              │
│ Datos inmutables cargados    │
│ una sola vez al inicio       │
└──────────────────────────────┘
```

---

## 10. Decisiones Arquitectónicas Detalladas

### 10.1 ADR-01: Blazor Server sobre Blazor WebAssembly

**Contexto:** Se debe elegir el modelo de hosting de Blazor.

**Decisión:** Blazor Server.

**Justificación:**
- En WebAssembly, la lógica del juego se ejecutaría en el cliente, lo que requeriría una API REST o SignalR igualmente para sincronizar el estado entre jugadores. Solo añade complejidad.
- En Server, el estado del juego vive naturalmente en el servidor. No hay que sincronizar.
- La latencia en red local es despreciable (~1ms), eliminando la principal desventaja de Blazor Server.
- No necesitamos modo offline (el juego es multijugador obligatorio).
- Inicio más rápido: no hay descarga de runtime .NET al navegador.

### 10.2 ADR-02: Hub Dedicado vs Circuitos Blazor para Comunicación del Juego

**Contexto:** Blazor Server ya tiene conexiones WebSocket (circuitos). ¿Necesitamos un hub adicional?

**Decisión:** Sí, crear un `GameHub` dedicado además del circuito Blazor.

**Justificación:**
- Los circuitos Blazor son individuales (uno por navegador). No tienen concepto de "grupo" ni "broadcast".
- SignalR Hubs permiten **grupos**: enviar un mensaje a todos los jugadores de una partida con una sola llamada.
- Separación de responsabilidades: el circuito gestiona la UI individual; el hub gestiona la comunicación colectiva del juego.
- No es una conexión WebSocket adicional costosa en el contexto de 2–6 jugadores en LAN.

**Alternativa descartada:** Usar un servicio Singleton con eventos C# (`event`) para notificar a los componentes Blazor. Viable pero más complejo de gestionar correctamente con `InvokeAsync` y `StateHasChanged`, y no permite gestegar la reconexión del jugador de forma natural.

### 10.3 ADR-03: Sin Base de Datos

**Contexto:** ¿Se necesita persistencia para el estado de las partidas?

**Decisión:** No. Todo en memoria.

**Justificación:**
- Las partidas son efímeras: duran entre 1 y 3 horas y no se reanudan.
- La complejidad de serializar/deserializar el estado completo del juego a una BD no aporta valor.
- El servidor será un PC de un amigo; si se reinicia, simplemente se empieza una nueva partida.
- Para el caso de uso (amigos jugando en LAN), la pérdida de una partida por reinicio no es un problema.

### 10.4 ADR-04: GameEngine sin Estado (Stateless)

**Contexto:** ¿Dónde debe vivir la lógica de las reglas del juego?

**Decisión:** En un servicio `GameEngine` transient y sin estado propio. Recibe el objeto `Game` como parámetro y devuelve resultados.

**Justificación:**
- **Testability:** Un motor sin estado es trivial de testear unitariamente. Le pasas un estado conocido y verificas la salida.
- **Separación de responsabilidades:** El `GameManager` gestiona el ciclo de vida de las partidas; el `GameEngine` aplica las reglas.
- **Sin acoplamiento:** El motor no depende de SignalR, Blazor ni ninguna infraestructura. Solo conoce modelos del dominio.
- **Transient seguro:** Al no tener estado, crear una nueva instancia por uso no tiene coste significativo y evita problemas de concurrencia.

### 10.5 ADR-05: Patrón de Comunicación Componente → Hub → Componentes

**Contexto:** ¿Cómo fluyen las acciones del jugador al resto de jugadores?

**Decisión:** El componente Blazor invoca un método del Hub → el Hub procesa la acción → el Hub notifica al grupo → los componentes de todos los jugadores reaccionan.

**Justificación:**
- El Hub actúa como punto central de coordinación.
- Todas las acciones del juego pasan por el servidor, garantizando que el estado es autoritativo.
- Los clientes nunca modifican el estado directamente; solo envían intenciones.
- El servidor valida cada acción y solo propaga el resultado si es válida.

---

## 11. Consideraciones de Seguridad

Aunque la aplicación **no se publica en internet** y los usuarios son de confianza, se documentan las consideraciones básicas:

| Aspecto | Enfoque en MiniRisk |
|---------|-------------------|
| **Validación de acciones** | El servidor valida todas las acciones antes de ejecutarlas. Un jugador no puede atacar si no es su turno, mover ejércitos de territorios que no le pertenecen, etc. |
| **Estado autoritativo** | El servidor es la única fuente de verdad. El cliente no decide el resultado de los dados ni el estado del juego. |
| **Sin datos sensibles** | No hay contraseñas, datos personales ni información que proteger. Solo nombres de jugadores. |
| **HTTPS** | Deshabilitado por defecto para desarrollo local. Puede activarse opcionalmente. |
| **Antiforgery** | Incluido por defecto en la plantilla de Blazor Server. Se mantiene. |
| **CORS** | No aplica: no hay API REST consumida por otros orígenes. |

---

## 12. Consideraciones de Rendimiento

| Aspecto | Enfoque |
|---------|---------|
| **Renderizado Blazor** | Uso de `ShouldRender()` en componentes pesados (WorldMap) para evitar re-renders innecesarios |
| **Diff del DOM** | Blazor envía solo las diferencias; actualizar un territorio no re-renderiza todo el mapa |
| **SignalR** | Los mensajes del hub son ligeros (JSON). Solo se envían los datos necesarios, no el estado completo cuando no es necesario |
| **Memoria** | Cada partida en memoria ocupa ~50-100 KB (42 territorios, 6 jugadores, log de eventos). 3 partidas = ~300 KB |
| **Concurrencia** | `ConcurrentDictionary` para acceso thread-safe al `GameManager`. Locks puntuales para operaciones críticas en una partida |
| **SVG del mapa** | Se carga una vez y se reutiliza. Los 42 paths SVG se renderizan como componentes Blazor individuales para permitir actualizaciones granulares |

---

> **Siguiente documento:** [03 — Identificación de Jugadores](./03_Identificacion_Jugadores.md)
