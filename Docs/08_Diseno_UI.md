# 08 — Diseño de la Interfaz de Usuario

> **Documento:** 08 de 14  
> **Versión:** 1.0  
> **Última actualización:** 22 de febrero de 2026  
> **Volver al índice:** [00_Indice.md](./00_Indice.md)  
> **Documento anterior:** [07 — Gestión de Estado](./07_Gestion_Estado.md)

---

## 1. Visión General del Diseño

MiniRisk adopta una estética **moderna y oscura** con acentos vibrantes. La interfaz está pensada para usarse en **pantallas de escritorio o portátil** (mínimo 1024×768), sin necesidad de soporte móvil (el juego se desarrolla en red local entre amigos).

### 1.1 Principios de Diseño

| Principio | Aplicación |
|-----------|-----------|
| **Claridad del estado** | El jugador siempre sabe de quién es el turno, en qué fase está, y qué puede hacer |
| **Feedback inmediato** | Cada acción produce una respuesta visual: animación de dados, parpadeo de territorio, notificación |
| **Jerarquía visual** | El mapa es el centro; los paneles secundarios son compactos y no compiten por atención |
| **Consistencia cromática** | Los colores de los jugadores son coherentes en mapa, paneles, dados, cartas y log |
| **Accesibilidad básica** | Contraste suficiente, textos legibles, indicadores no solo por color |

### 1.2 Tecnologías UI

| Tecnología | Uso |
|-----------|-----|
| **Blazor Server** | Renderizado de componentes en el servidor |
| **CSS puro** | Estilos personalizados, variables CSS, sin frameworks CSS |
| **SVG** | Mapa del mundo interactivo |
| **CSS Animations / Transitions** | Microinteracciones, dados, conquistas |
| **Google Fonts** | Tipografía (Inter para UI, Orbitron para títulos temáticos) |

---

## 2. Paleta de Colores

### 2.1 Base (Tema Oscuro)

```css
:root {
    /* ── Fondo ── */
    --bg-primary: #0f1117;        /* Fondo principal (casi negro azulado) */
    --bg-secondary: #1a1d27;      /* Paneles, tarjetas */
    --bg-tertiary: #242836;       /* Elementos elevados, hover */
    --bg-surface: #2d3248;        /* Inputs, áreas interactivas */

    /* ── Texto ── */
    --text-primary: #e8eaed;      /* Texto principal */
    --text-secondary: #9aa0b0;    /* Texto secundario */
    --text-muted: #5f6679;        /* Texto deshabilitado */

    /* ── Bordes ── */
    --border-subtle: #2d3248;
    --border-default: #3d4260;
    --border-strong: #5f6679;

    /* ── Acento ── */
    --accent-primary: #6c63ff;    /* Botones principales, enlaces */
    --accent-hover: #7f78ff;
    --accent-glow: rgba(108, 99, 255, 0.3);

    /* ── Semánticos ── */
    --color-success: #2dd4a8;
    --color-warning: #f5a623;
    --color-danger: #ef4444;
    --color-info: #38bdf8;
}
```

### 2.2 Colores de Jugadores

```css
:root {
    /* Colores saturados para territorios en el mapa */
    --player-red: #e63946;
    --player-blue: #457b9d;
    --player-green: #2a9d8f;
    --player-yellow: #e9c46a;
    --player-purple: #7b2d8e;
    --player-orange: #f4845f;
    --player-neutral: #6b7280;

    /* Versiones claras para texto sobre fondo oscuro */
    --player-red-light: #ff6b6b;
    --player-blue-light: #74b9ff;
    --player-green-light: #55efc4;
    --player-yellow-light: #ffeaa7;
    --player-purple-light: #a29bfe;
    --player-orange-light: #fab1a0;

    /* Versiones oscuras para fondos de paneles */
    --player-red-bg: rgba(230, 57, 70, 0.15);
    --player-blue-bg: rgba(69, 123, 157, 0.15);
    --player-green-bg: rgba(42, 157, 143, 0.15);
    --player-yellow-bg: rgba(233, 196, 106, 0.15);
    --player-purple-bg: rgba(123, 45, 142, 0.15);
    --player-orange-bg: rgba(244, 132, 95, 0.15);
}
```

### 2.3 Mapa de Colores por Continente (fondo SVG)

| Continente | Color de fondo | Borde |
|:----------:|:--------------:|:-----:|
| América del Norte | `#1b3a4b` | `#2d5f7a` |
| América del Sur | `#1b4332` | `#2d6b4d` |
| Europa | `#3b1f2b` | `#5c3049` |
| África | `#3d2c1e` | `#5c4330` |
| Asia | `#2b2440` | `#433764` |
| Oceanía | `#1a3342` | `#2b5068` |

---

## 3. Tipografía

```css
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&family=Orbitron:wght@500;700&family=JetBrains+Mono:wght@400;500&display=swap');

:root {
    --font-body: 'Inter', system-ui, sans-serif;
    --font-display: 'Orbitron', sans-serif;
    --font-mono: 'JetBrains Mono', monospace;

    --text-xs: 0.75rem;     /* 12px — captions, badges */
    --text-sm: 0.875rem;    /* 14px — texto secundario */
    --text-base: 1rem;      /* 16px — texto principal */
    --text-lg: 1.125rem;    /* 18px — subtítulos */
    --text-xl: 1.25rem;     /* 20px — títulos de sección */
    --text-2xl: 1.5rem;     /* 24px — títulos de pantalla */
    --text-3xl: 2rem;       /* 32px — título principal */
}
```

| Elemento | Fuente | Peso | Tamaño |
|---------|--------|:----:|:------:|
| Título del juego ("MiniRisk") | Orbitron | 700 | 2rem |
| Títulos de pantalla | Orbitron | 500 | 1.5rem |
| Subtítulos / secciones | Inter | 600 | 1.125rem |
| Texto de UI (botones, labels) | Inter | 500 | 1rem |
| Texto de cuerpo | Inter | 400 | 1rem |
| Texto secundario | Inter | 400 | 0.875rem |
| Badges, contadores | Inter | 600 | 0.75rem |
| Log de eventos | JetBrains Mono | 400 | 0.8rem |
| Números (dados, ejércitos) | JetBrains Mono | 500 | varies |

---

## 4. Pantallas y Wireframes

### 4.1 Pantalla de Bienvenida (`Home.razor`)

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│                                                              │
│                                                              │
│                    ╔══════════════════╗                       │
│                    ║    🎲 MiniRisk    ║                      │
│                    ╚══════════════════╝                       │
│                                                              │
│                   Conquista el mundo con                      │
│                     tus amigos                               │
│                                                              │
│              ┌──────────────────────────┐                    │
│              │  Tu nombre              │                    │
│              │  ┌──────────────────────┐│                    │
│              │  │ Carlos_____________  ││                    │
│              │  └──────────────────────┘│                    │
│              │                          │                    │
│              │  ┌──────────────────────┐│                    │
│              │  │     ENTRAR  →        ││                    │
│              │  └──────────────────────┘│                    │
│              │                          │                    │
│              │  3 jugadores conectados  │                    │
│              └──────────────────────────┘                    │
│                                                              │
│                     v1.0 • Red Local                         │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

**Elementos clave:**
- Logo animado con efecto de brillo sutil
- Input de nombre con validación en tiempo real (3-20 caracteres, alfanumérico)
- Mensaje de error inline bajo el input
- Indicador de jugadores activos en el sistema
- Fondo con gradiente oscuro y partículas sutiles (CSS animation)
- Si hay reconexión disponible, se muestra un banner: _"Estabas en la partida 'Los viernes'. ¿Reconectar?"_

---

### 4.2 Lobby (`Lobby.razor`)

```
┌──────────────────────────────────────────────────────────────────────────┐
│  🎲 MiniRisk                                    Carlos │ 🟢 Conectado  │
│─────────────────────────────────────────────────────────────────────────│
│                                                                          │
│  Partidas Disponibles                            [+ Crear Partida]      │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │  📋 LISTA DE PARTIDAS                                              │  │
│  │                                                                    │  │
│  │  ┌──────────────────────────────────────────────────────────────┐  │  │
│  │  │  🎮 Partida de los viernes          Creada por: Ana          │  │  │
│  │  │  👥 3/6 jugadores                   Hace 2 minutos          │  │  │
│  │  │                                              [Unirse →]     │  │  │
│  │  └──────────────────────────────────────────────────────────────┘  │  │
│  │                                                                    │  │
│  │  ┌──────────────────────────────────────────────────────────────┐  │  │
│  │  │  🎮 Risk nocturno                   Creada por: Pedro        │  │  │
│  │  │  👥 2/4 jugadores                   Hace 5 minutos          │  │  │
│  │  │                                              [Unirse →]     │  │  │
│  │  └──────────────────────────────────────────────────────────────┘  │  │
│  │                                                                    │  │
│  │          — No hay más partidas disponibles —                       │  │
│  │                                                                    │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

**Elementos clave:**
- Header con nombre del jugador y estado de conexión
- Lista de partidas en estado `WaitingForPlayers`, actualizada en tiempo real vía SignalR
- Cada tarjeta de partida muestra: nombre, creador, jugadores actuales/máximos, antigüedad
- Botón "Crear Partida" abre un diálogo modal:

```
┌────────────────────────────────────────┐
│  Crear Nueva Partida                   │
│                                        │
│  Nombre de la partida                  │
│  ┌──────────────────────────────────┐  │
│  │ Partida de los viernes__________ │  │
│  └──────────────────────────────────┘  │
│                                        │
│  Máximo de jugadores                   │
│  ○ 2  ○ 3  ○ 4  ○ 5  ● 6             │
│                                        │
│  Distribución de territorios           │
│  ● Aleatoria  ○ Manual                │
│                                        │
│  ┌────────────┐  ┌────────────────┐   │
│  │  Cancelar   │  │  Crear Partida │   │
│  └────────────┘  └────────────────┘   │
└────────────────────────────────────────┘
```

---

### 4.3 Sala de Espera (dentro de Lobby, tras unirse)

```
┌──────────────────────────────────────────────────────────────────────────┐
│  🎲 MiniRisk                                    Carlos │ 🟢 Conectado  │
│─────────────────────────────────────────────────────────────────────────│
│                                                                          │
│  🎮 Partida de los viernes                              [← Abandonar]  │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────┐       │
│  │                                                              │       │
│  │   ┌──────┐  ┌──────┐  ┌──────┐  ┌ ─ ─ ─┐  ┌ ─ ─ ─┐       │       │
│  │   │ 🔴   │  │ 🔵   │  │ 🟢   │  │      │  │      │       │       │
│  │   │Carlos│  │ Ana  │  │ Luis │  │ ???  │  │ ???  │       │       │
│  │   │👑     │  │      │  │      │  │      │  │      │       │       │
│  │   └──────┘  └──────┘  └──────┘  └ ─ ─ ─┘  └ ─ ─ ─┘       │       │
│  │                                                              │       │
│  │              Esperando jugadores... 3/6                      │       │
│  │                                                              │       │
│  │                   ┌────────────────────┐                     │       │
│  │                   │  ▶ INICIAR PARTIDA │   ← solo creador   │       │
│  │                   └────────────────────┘                     │       │
│  │                                                              │       │
│  └──────────────────────────────────────────────────────────────┘       │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────┐       │
│  │  💬 Chat de la sala                                          │       │
│  │  ─────────────────────────────                               │       │
│  │  Ana: ¡Hola! ¿Empezamos ya?                                 │       │
│  │  Carlos: Esperemos a Luis                                    │       │
│  │  Luis se ha unido a la partida                               │       │
│  │  ┌────────────────────────────────────┐  ┌────────┐         │       │
│  │  │ Escribe un mensaje..._____________ │  │ Enviar │         │       │
│  │  └────────────────────────────────────┘  └────────┘         │       │
│  └──────────────────────────────────────────────────────────────┘       │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

**Elementos clave:**
- Avatares de los jugadores con su color asignado y nombre
- Corona 👑 en el creador de la partida
- Slots vacíos con borde punteado
- Botón "Iniciar Partida" visible solo para el creador, habilitado con ≥2 jugadores
- Chat de sala para comunicación pre-partida
- Animación de entrada cuando un nuevo jugador se une

---

### 4.4 Pantalla de Juego (`Game.razor`) — Layout Principal

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│ 🎲 MiniRisk │ Turno 7 │ ATAQUE │ Turno de: Carlos 🔴    │ ⏱ 02:30 │ ⚙ │ ✖  │
│─────────────────────────────────────────────────────────────────────────────────│
│                                                                                  │
│  ┌────────────────────────────────────────────────┐  ┌──────────────────────┐   │
│  │                                                │  │  JUGADORES           │   │
│  │                                                │  │                      │   │
│  │                                                │  │  ▶ Carlos 🔴         │   │
│  │                                                │  │    15 terr. │ 47 ej. │   │
│  │                                                │  │    🃏 2 cartas       │   │
│  │               MAPA DEL MUNDO                   │  │                      │   │
│  │                  (SVG)                          │  │    Ana 🔵            │   │
│  │                                                │  │    14 terr. │ 38 ej. │   │
│  │                                                │  │    🃏 1 carta        │   │
│  │          Territorios interactivos              │  │                      │   │
│  │          con colores de sus dueños             │  │    ░ Luis 🟢  ×      │   │
│  │                                                │  │    0 terr. │ 0 ej.   │   │
│  │                                                │  │    ELIMINADO         │   │
│  │                                                │  │                      │   │
│  │                                                │  ├──────────────────────┤   │
│  │                                                │  │  MIS CARTAS          │   │
│  │                                                │  │                      │   │
│  │                                                │  │  🚶 Alaska           │   │
│  │                                                │  │  🐴 Brazil           │   │
│  │                                                │  │                      │   │
│  │                                                │  │  [Canjear Cartas]    │   │
│  │                                                │  │  (necesitas 3)       │   │
│  └────────────────────────────────────────────────┘  └──────────────────────┘   │
│                                                                                  │
│  ┌──────────────────────────────────────────┐  ┌───────────────────────────┐    │
│  │  CONTROLES DE TURNO                      │  │  LOG DE EVENTOS           │    │
│  │                                          │  │                           │    │
│  │  Fase: ATAQUE                            │  │  22:05 Carlos colocó 5    │    │
│  │                                          │  │        ejércitos en Alaska │    │
│  │  Atacar desde: Alaska (5 ej.)            │  │  22:05 Carlos atacó       │    │
│  │  Atacar a: Kamchatka (3 ej.)             │  │        Kamchatka desde     │    │
│  │                                          │  │        Alaska: [6,4,2]    │    │
│  │  Dados: ○1  ●2  ○3                      │  │        vs [5,3] → Atk -0  │    │
│  │                                          │  │        Def -2             │    │
│  │  ┌──────────┐  ┌────────────────────┐   │  │  22:06 Carlos conquistó   │    │
│  │  │  ATACAR!  │  │  Terminar Ataques  │   │  │        Kamchatka          │    │
│  │  └──────────┘  └────────────────────┘   │  │                           │    │
│  └──────────────────────────────────────────┘  └───────────────────────────┘    │
│                                                                                  │
└──────────────────────────────────────────────────────────────────────────────────┘
```

### 4.5 Layout con CSS Grid

```css
.game-layout {
    display: grid;
    grid-template-columns: 1fr 280px;
    grid-template-rows: auto 1fr auto;
    grid-template-areas:
        "header     header"
        "map        sidebar"
        "controls   log";
    gap: 12px;
    height: 100vh;
    padding: 12px;
    background: var(--bg-primary);
}

.game-header     { grid-area: header; }
.game-map        { grid-area: map; }
.game-sidebar    { grid-area: sidebar; }
.game-controls   { grid-area: controls; }
.game-log        { grid-area: log; }
```

---

## 5. Jerarquía de Componentes Blazor

### 5.1 Árbol Completo

```
App.razor
└── Routes.razor
    ├── MainLayout.razor
    │   ├── Home.razor                        ← Pantalla de bienvenida
    │   │   ├── ReconnectionBanner.razor       ← Banner "¿Reconectar?"
    │   │   └── PlayerCount.razor              ← "N jugadores conectados"
    │   │
    │   ├── Lobby.razor                        ← Lobby
    │   │   ├── GameList.razor                 ← Lista de partidas
    │   │   │   └── GameCard.razor             ← Tarjeta de cada partida
    │   │   ├── CreateGameDialog.razor         ← Modal crear partida
    │   │   └── WaitingRoom.razor              ← Sala de espera pre-partida
    │   │       ├── PlayerSlot.razor            ← Slot de jugador (avatar+color)
    │   │       └── LobbyChat.razor             ← Chat de la sala
    │   │
    │   └── Game.razor                         ← Pantalla principal del juego
    │       ├── GameHeader.razor               ← Barra superior (turno, fase, timer)
    │       ├── WorldMap.razor                 ← Mapa SVG interactivo
    │       │   └── TerritoryPath.razor        ← Cada territorio individual
    │       ├── PlayerSidebar.razor            ← Panel lateral derecho
    │       │   ├── PlayerCard.razor           ← Info de cada jugador
    │       │   └── CardHand.razor             ← Cartas del jugador actual
    │       │       └── TerritoryCard.razor    ← Cada carta individual
    │       ├── TurnControls.razor             ← Panel de controles según fase
    │       │   ├── ReinforcementPanel.razor   ← Controles de fase de refuerzo
    │       │   ├── AttackPanel.razor          ← Controles de fase de ataque
    │       │   │   └── DiceRoller.razor       ← Animación de dados
    │       │   ├── FortifyPanel.razor         ← Controles de fortificación
    │       │   └── TradeCardsDialog.razor     ← Modal de canje de cartas
    │       ├── EventLog.razor                 ← Log de eventos
    │       │   └── EventEntry.razor           ← Cada evento individual
    │       └── GameOverOverlay.razor          ← Overlay de fin de partida
    │
    └── Error.razor                            ← Página de error
```

### 5.2 Responsabilidades por Componente

| Componente | Nivel | Responsabilidad |
|-----------|:-----:|----------------|
| `Home.razor` | Página | Identificación del jugador, validación de nombre, detección de reconexión |
| `Lobby.razor` | Página | Listar partidas, crear/unirse, gestionar sala de espera. Establece conexión SignalR para lobby. |
| `Game.razor` | Página | **Orquestador principal.** Gestiona la conexión SignalR del juego, obtiene el estado, y distribuye datos a los componentes hijos vía `[Parameter]`. |
| `WorldMap.razor` | Componente | Renderizar el SVG del mapa. Gestiona click en territorios. Emite `EventCallback` al padre. |
| `TurnControls.razor` | Componente | Muestra los controles relevantes según la fase actual. Delega al sub-panel correspondiente. |
| `DiceRoller.razor` | Componente | Animación de dados girando y resultado final. Puramente visual. |
| `PlayerSidebar.razor` | Componente | Muestra la lista de jugadores y las cartas del jugador actual. |
| `EventLog.razor` | Componente | Muestra los últimos 20 eventos con scroll automático. |

---

## 6. Flujo de Datos en la Pantalla de Juego

```
  Game.razor (Orquestador)
  ┌──────────────────────────────────────────────────────────────┐
  │                                                              │
  │  Estado:                                                     │
  │  - GameStateDto gameState                                    │
  │  - string? selectedAttacker                                  │
  │  - string? selectedDefender                                  │
  │  - int attackDice                                            │
  │  - AttackResult? lastAttackResult                            │
  │                                                              │
  │  HubConnection:                                              │
  │  - Recibe: GameStateUpdated, DiceRolled, PlayerDisconnected  │
  │  - Envía: Attack, PlaceReinforcements, Fortify, TradeCards   │
  │                                                              │
  └──────┬──────────┬────────────────┬──────────────┬────────────┘
         │          │                │              │
    [Parameter] [Parameter]     [Parameter]    [Parameter]
    gameState   gameState       gameState      gameState
    onClick     myCards         selectedTerr   events
         │          │                │              │
         ▼          ▼                ▼              ▼
  ┌──────────┐ ┌──────────┐  ┌──────────────┐ ┌──────────┐
  │WorldMap  │ │PlayerSide│  │TurnControls  │ │EventLog  │
  │          │ │bar       │  │              │ │          │
  │Emite:    │ │Emite:    │  │Emite:        │ │Solo      │
  │Territory │ │CardSelect│  │Attack()      │ │lectura   │
  │Clicked   │ │          │  │Fortify()     │ │          │
  │(callback)│ │(callback)│  │EndPhase()    │ │          │
  └──────────┘ └──────────┘  └──────────────┘ └──────────┘
         │          │                │
    [EventCallback] [EventCallback] [EventCallback]
         │          │                │
         ▼          ▼                ▼
  Game.razor recibe callbacks → invoca HubConnection.SendAsync()
```

### 6.1 Patrón: Estado abajo, eventos arriba

| Dirección | Mecanismo | Ejemplo |
|-----------|-----------|---------|
| **Padre → Hijo** | `[Parameter]` | `Game.razor` pasa `gameState` a `WorldMap.razor` |
| **Hijo → Padre** | `[EventCallback]` | `WorldMap.razor` emite `OnTerritoryClicked` a `Game.razor` |
| **servidor → cliente** | SignalR `hubConnection.On(...)` | Hub envía `GameStateUpdated`, `Game.razor` lo recibe |
| **Cliente → servidor** | SignalR `hubConnection.SendAsync(...)` | `Game.razor` invoca `Attack(...)` en el Hub |

---

## 7. Estados Visuales de los Territorios

### 7.1 Estilos SVG por Estado

| Estado | Visual | CSS |
|--------|--------|-----|
| **Normal** | Relleno con color del dueño | `fill: var(--player-color); opacity: 0.7` |
| **Hover** | Brillo + borde iluminado | `opacity: 1; stroke: white; stroke-width: 2; filter: brightness(1.2)` |
| **Seleccionado (atacante)** | Borde brillante pulsante | `stroke: white; stroke-width: 3; animation: pulse 1.5s infinite` |
| **Objetivo (defensor)** | Borde rojo pulsante | `stroke: var(--color-danger); stroke-width: 2; animation: pulse` |
| **Seleccionable (puede atacar)** | Brillo sutil | `cursor: pointer; filter: brightness(1.1)` |
| **No seleccionable** | Atenuado | `opacity: 0.4; cursor: not-allowed` |
| **Recién conquistado** | Flash de color + onda | `animation: conquest-flash 0.8s ease-out` |
| **Fortificable (conectado)** | Borde verde sutil | `stroke: var(--color-success); stroke-width: 1.5` |

### 7.2 Información en Territorio

Cada territorio muestra sobre el SVG:
- **Número de ejércitos** en un círculo con el color del dueño
- Al hacer hover: tooltip con nombre del territorio, dueño, ejércitos y continente

```css
.territory-army-badge {
    font-family: var(--font-mono);
    font-size: 12px;
    font-weight: 600;
    fill: white;
    text-anchor: middle;
    pointer-events: none;
}

.territory-army-circle {
    r: 12;
    stroke: rgba(0, 0, 0, 0.5);
    stroke-width: 1.5;
    /* fill dinámico según color del jugador */
}
```

---

## 8. Componentes de Feedback Visual

### 8.1 Animación de Dados (`DiceRoller.razor`)

```
  ┌─────────────────────────┐
  │                         │
  │   Antes del resultado:  │
  │                         │
  │   ┌───┐ ┌───┐ ┌───┐    │     Atacante (rojo)
  │   │ ? │ │ ? │ │ ? │    │     Dados girando
  │   └───┘ └───┘ └───┘    │     (animation: spin)
  │                         │
  │      VS                 │
  │                         │
  │   ┌───┐ ┌───┐          │     Defensor (azul)
  │   │ ? │ │ ? │          │     Dados girando
  │   └───┘ └───┘          │
  │                         │
  │─────────────────────────│
  │                         │
  │   Después (resultado):  │
  │                         │
  │   ┌───┐ ┌───┐ ┌───┐    │     Dados ordenados desc
  │   │ 6 │ │ 4 │ │ 2 │    │     Verde = ganó la comparación
  │   └───┘ └───┘ └───┘    │     Rojo = perdió
  │                         │
  │   ┌───┐ ┌───┐          │
  │   │ 5 │ │ 3 │          │
  │   └───┘ └───┘          │
  │                         │
  │   Atk: -0  Def: -2     │
  │                         │
  └─────────────────────────┘
```

```css
@keyframes dice-spin {
    0% { transform: rotateX(0deg) rotateY(0deg); }
    100% { transform: rotateX(360deg) rotateY(360deg); }
}

.dice-rolling {
    animation: dice-spin 0.4s ease-in-out 3;
}

.dice-result-win {
    background: var(--color-success);
    color: white;
    box-shadow: 0 0 12px var(--color-success);
}

.dice-result-lose {
    background: var(--color-danger);
    color: white;
    opacity: 0.7;
}
```

### 8.2 Notificaciones Toast

Mensajes emergentes temporales para eventos importantes:

```css
.toast-container {
    position: fixed;
    top: 80px;
    right: 20px;
    z-index: 1000;
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.toast {
    padding: 12px 20px;
    border-radius: 8px;
    background: var(--bg-secondary);
    border-left: 4px solid var(--accent-primary);
    color: var(--text-primary);
    font-size: var(--text-sm);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
    animation: toast-slide-in 0.3s ease-out;
}

.toast--conquest {
    border-left-color: var(--color-success);
    background: linear-gradient(135deg, var(--bg-secondary), rgba(45, 212, 168, 0.1));
}

.toast--elimination {
    border-left-color: var(--color-danger);
    background: linear-gradient(135deg, var(--bg-secondary), rgba(239, 68, 68, 0.1));
}

.toast--your-turn {
    border-left-color: var(--color-warning);
    background: linear-gradient(135deg, var(--bg-secondary), rgba(245, 166, 35, 0.1));
}

@keyframes toast-slide-in {
    from { transform: translateX(100%); opacity: 0; }
    to { transform: translateX(0); opacity: 1; }
}
```

| Tipo | Mensaje | Duración |
|------|---------|:--------:|
| Tu turno | "¡Es tu turno! Fase de refuerzo." | 5s |
| Conquista propia | "¡Has conquistado Kamchatka!" | 4s |
| Conquista ajena | "Ana ha conquistado Brazil." | 3s |
| Eliminación | "¡Luis ha sido eliminado!" | 6s |
| Victoria | Overlay completo | Permanente |
| Desconexión | "Pedro se ha desconectado..." | Permanente hasta reconexión |
| Error | "Acción no válida: ..." | 4s |

### 8.3 Overlay de Victoria

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │
│  ░░░░░░░░░ (mapa difuminado de fondo) ░░░░░░░░░░░░░░░░░░░  │
│  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │
│  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │
│  ░░░  ┌──────────────────────────────────────────┐  ░░░░░  │
│  ░░░  │                                          │  ░░░░░  │
│  ░░░  │            🏆 ¡VICTORIA! 🏆              │  ░░░░░  │
│  ░░░  │                                          │  ░░░░░  │
│  ░░░  │           Carlos ha conquistado          │  ░░░░░  │
│  ░░░  │              el mundo                    │  ░░░░░  │
│  ░░░  │                                          │  ░░░░░  │
│  ░░░  │     ┌─────────────────────────────┐      │  ░░░░░  │
│  ░░░  │     │  Turnos: 23                 │      │  ░░░░░  │
│  ░░░  │     │  Duración: 45 min           │      │  ░░░░░  │
│  ░░░  │     │  Territorios conquistados:42│      │  ░░░░░  │
│  ░░░  │     │  Ejércitos desplegados: 127 │      │  ░░░░░  │
│  ░░░  │     └─────────────────────────────┘      │  ░░░░░  │
│  ░░░  │                                          │  ░░░░░  │
│  ░░░  │         ┌──────────────────┐             │  ░░░░░  │
│  ░░░  │         │  Volver al Lobby │             │  ░░░░░  │
│  ░░░  │         └──────────────────┘             │  ░░░░░  │
│  ░░░  │                                          │  ░░░░░  │
│  ░░░  └──────────────────────────────────────────┘  ░░░░░  │
│  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## 9. Controles por Fase de Turno

### 9.1 Fase de Refuerzo

```
┌──────────────────────────────────────────────┐
│  🛡️ FASE DE REFUERZO                         │
│                                              │
│  Ejércitos por colocar: 9                    │
│  ████████░░░░░░░░░░░░░░░  (barra progreso)  │
│                                              │
│  Haz click en un territorio tuyo             │
│  para colocar ejércitos.                     │
│                                              │
│  Cantidad:  [-] 3 [+]                        │
│                                              │
│  ┌─────────────────────────────────────────┐ │
│  │          Colocar en Alaska (5 ej.)       │ │
│  └─────────────────────────────────────────┘ │
│                                              │
│  ⚠️ Tienes 5 cartas. Debes canjear.         │
│  ┌─────────────────────────────────────────┐ │
│  │          Canjear Cartas                  │ │
│  └─────────────────────────────────────────┘ │
└──────────────────────────────────────────────┘
```

### 9.2 Fase de Ataque

```
┌──────────────────────────────────────────────┐
│  ⚔️ FASE DE ATAQUE                           │
│                                              │
│  1. Haz click en tu territorio (atacante)    │
│  2. Haz click en territorio enemigo          │
│     adyacente (defensor)                     │
│  3. Elige número de dados                    │
│                                              │
│  Desde: Alaska 🔴 (5 ejércitos)             │
│  Hacia: Kamchatka 🔵 (3 ejércitos)          │
│                                              │
│  Dados de ataque:                            │
│  ○ 1    ● 2    ○ 3                           │
│                                              │
│  ┌──────────┐  ┌────────────────────────┐   │
│  │ ⚔ ATACAR │  │  Terminar fase ataque   │   │
│  └──────────┘  └────────────────────────┘   │
│                                              │
│  ── Último resultado ──                      │
│  🔴 [6] [4] [2]  vs  🔵 [5] [3]            │
│  Resultado: Atacante -0, Defensor -2         │
└──────────────────────────────────────────────┘
```

### 9.3 Fase de Fortificación

```
┌──────────────────────────────────────────────┐
│  🏰 FASE DE FORTIFICACIÓN                    │
│                                              │
│  Mueve ejércitos entre territorios propios   │
│  conectados. (Opcional, 1 movimiento)        │
│                                              │
│  Desde: Alaska 🔴 (5 ejércitos)             │
│  Hacia: Kamchatka 🔴 (2 ejércitos)          │
│  ✅ Camino conectado                         │
│                                              │
│  Cantidad:  [-] 2 [+]  (máx: 4)             │
│                                              │
│  ┌──────────────────┐  ┌──────────────────┐ │
│  │   🏰 Fortificar   │  │  Pasar (no mover)│ │
│  └──────────────────┘  └──────────────────┘ │
└──────────────────────────────────────────────┘
```

---

## 10. Estilos de Componentes Comunes

### 10.1 Botones

```css
.btn {
    font-family: var(--font-body);
    font-weight: 500;
    font-size: var(--text-base);
    padding: 10px 24px;
    border: none;
    border-radius: 8px;
    cursor: pointer;
    transition: all 0.2s ease;
    display: inline-flex;
    align-items: center;
    gap: 8px;
}

.btn-primary {
    background: linear-gradient(135deg, var(--accent-primary), #5448d6);
    color: white;
    box-shadow: 0 2px 8px var(--accent-glow);
}
.btn-primary:hover {
    background: linear-gradient(135deg, var(--accent-hover), #6c5ce7);
    transform: translateY(-1px);
    box-shadow: 0 4px 16px var(--accent-glow);
}

.btn-danger {
    background: linear-gradient(135deg, #ef4444, #dc2626);
    color: white;
}

.btn-secondary {
    background: var(--bg-tertiary);
    color: var(--text-primary);
    border: 1px solid var(--border-default);
}

.btn:disabled {
    opacity: 0.4;
    cursor: not-allowed;
    transform: none;
    box-shadow: none;
}
```

### 10.2 Tarjetas / Paneles

```css
.panel {
    background: var(--bg-secondary);
    border: 1px solid var(--border-subtle);
    border-radius: 12px;
    padding: 16px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
}

.panel-header {
    font-family: var(--font-display);
    font-size: var(--text-lg);
    color: var(--text-primary);
    margin-bottom: 12px;
    padding-bottom: 8px;
    border-bottom: 1px solid var(--border-subtle);
}

.card {
    background: var(--bg-tertiary);
    border: 1px solid var(--border-default);
    border-radius: 8px;
    padding: 12px;
    transition: all 0.2s ease;
}

.card:hover {
    border-color: var(--accent-primary);
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
}
```

### 10.3 Inputs

```css
.input {
    width: 100%;
    padding: 12px 16px;
    background: var(--bg-surface);
    border: 1px solid var(--border-default);
    border-radius: 8px;
    color: var(--text-primary);
    font-family: var(--font-body);
    font-size: var(--text-base);
    transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.input:focus {
    outline: none;
    border-color: var(--accent-primary);
    box-shadow: 0 0 0 3px var(--accent-glow);
}

.input--error {
    border-color: var(--color-danger);
    box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.2);
}
```

---

## 11. Animaciones y Transiciones

### 11.1 Catálogo de Animaciones

| Animación | Trigger | Duración | Tipo |
|-----------|---------|:--------:|------|
| `pulse` | Territorio seleccionado | 1.5s loop | Borde pulsante |
| `conquest-flash` | Territorio conquistado | 0.8s once | Flash blanco + onda |
| `dice-spin` | Tirada de dados | 0.4s × 3 | Rotación 3D |
| `toast-slide-in` | Notificación aparece | 0.3s once | Deslizar desde derecha |
| `toast-slide-out` | Notificación desaparece | 0.3s once | Deslizar hacia derecha |
| `fade-in` | Elementos que aparecen | 0.3s once | Opacidad 0→1 |
| `shake` | Error de validación | 0.4s once | Sacudida horizontal |
| `glow-pulse` | Botón "Es tu turno" | 2s loop | Brillo pulsante |
| `card-flip` | Carta revelada | 0.5s once | Rotación Y |
| `counter-up` | Ejércitos incrementan | 0.3s once | Número sube suavemente |
| `player-join` | Jugador se une al lobby | 0.5s once | Escala 0→1 + rebote |
| `confetti` | Victoria | 3s once | Partículas cayendo |

### 11.2 Definiciones CSS

```css
@keyframes pulse {
    0%, 100% { opacity: 1; }
    50% { opacity: 0.5; }
}

@keyframes conquest-flash {
    0% { fill: white; filter: brightness(2); }
    100% { fill: var(--new-owner-color); filter: brightness(1); }
}

@keyframes shake {
    0%, 100% { transform: translateX(0); }
    25% { transform: translateX(-6px); }
    75% { transform: translateX(6px); }
}

@keyframes glow-pulse {
    0%, 100% { box-shadow: 0 0 4px var(--accent-glow); }
    50% { box-shadow: 0 0 20px var(--accent-glow); }
}

@keyframes card-flip {
    0% { transform: rotateY(180deg); }
    100% { transform: rotateY(0deg); }
}
```

---

## 12. Accesibilidad

| Aspecto | Implementación |
|---------|---------------|
| **Contraste** | Todos los textos cumplen WCAG AA (ratio ≥ 4.5:1 para texto normal) |
| **No solo color** | Los territorios muestran el número de ejércitos además del color. Los jugadores eliminados tienen icono ✕ además de atenuarse. |
| **Foco visible** | Los elementos interactivos tienen `:focus-visible` con borde de alto contraste |
| **Semántica** | `<button>` para acciones, `<nav>` para navegación, `<main>` para contenido principal |
| **Tooltips** | Los territorios muestran tooltip con nombre, dueño y ejércitos al hacer hover |
| **Aria-labels** | Botones con iconos tienen `aria-label` descriptivo |
| **Tamaño de click** | Áreas de click mínimo de 44×44 px en controles del juego |
| **Indicadores de turno** | Además del color, el jugador actual tiene un indicador ▶ y borde destacado |

```css
:focus-visible {
    outline: 2px solid var(--accent-primary);
    outline-offset: 2px;
}

/* Reducir animaciones si el usuario lo prefiere */
@media (prefers-reduced-motion: reduce) {
    *, *::before, *::after {
        animation-duration: 0.01ms !important;
        transition-duration: 0.01ms !important;
    }
}
```

---

> **Siguiente documento:** [09 — Componentes Blazor](./09_Componentes_Blazor.md)
