# 09 — Componentes Blazor (Detalle)

> **Documento:** 09 de 14  
> **Versión:** 1.0  
> **Última actualización:** 22 de febrero de 2026  
> **Volver al índice:** [00_Indice.md](./00_Indice.md)  
> **Documento anterior:** [08 — Diseño de la Interfaz de Usuario](./08_Diseno_UI.md)

---

## 1. Visión General

Este documento detalla la implementación de cada componente Blazor del juego: su markup, parámetros, ciclo de vida, lógica y comunicación. Se complementa con el [documento 08](./08_Diseno_UI.md) que cubre el diseño visual.

### 1.1 Convenciones

| Convención | Regla |
|-----------|-------|
| **Ubicación** | Páginas en `Components/Pages/`, compartidos en `Components/Shared/` |
| **Nomenclatura** | PascalCase, nombre descriptivo, sufijo implícito `.razor` |
| **Estilos** | CSS aislado por componente (`Component.razor.css`) |
| **Parámetros** | `[Parameter]` para datos padre→hijo, `[EventCallback]` para hijo→padre |
| **Inyección** | Solo en componentes de página. Hijos reciben datos por parámetros. |
| **Estado** | El estado del juego vive en `Game.razor`; los hijos son presentacionales. |

### 1.2 Estructura de Archivos

```
Components/
├── _Imports.razor
├── App.razor
├── Routes.razor
├── Layout/
│   └── MainLayout.razor
├── Pages/
│   ├── Home.razor
│   ├── Home.razor.css
│   ├── Lobby.razor
│   ├── Lobby.razor.css
│   ├── Game.razor
│   ├── Game.razor.css
│   └── Error.razor
└── Shared/
    ├── WorldMap.razor
    ├── WorldMap.razor.css
    ├── TerritoryPath.razor
    ├── GameHeader.razor
    ├── GameHeader.razor.css
    ├── PlayerSidebar.razor
    ├── PlayerSidebar.razor.css
    ├── PlayerCard.razor
    ├── CardHand.razor
    ├── CardHand.razor.css
    ├── TerritoryCard.razor
    ├── TurnControls.razor
    ├── TurnControls.razor.css
    ├── ReinforcementPanel.razor
    ├── AttackPanel.razor
    ├── AttackPanel.razor.css
    ├── DiceRoller.razor
    ├── DiceRoller.razor.css
    ├── FortifyPanel.razor
    ├── TradeCardsDialog.razor
    ├── TradeCardsDialog.razor.css
    ├── EventLog.razor
    ├── EventLog.razor.css
    ├── GameOverOverlay.razor
    ├── GameOverOverlay.razor.css
    ├── WaitingRoom.razor
    ├── WaitingRoom.razor.css
    ├── CreateGameDialog.razor
    ├── GameCard.razor
    ├── PlayerSlot.razor
    ├── LobbyChat.razor
    ├── ReconnectionBanner.razor
    └── ToastContainer.razor
```

---

## 2. Componentes de Página (Routable)

### 2.1 Home.razor — Pantalla de Bienvenida

**Ruta:** `/`

```razor
@page "/"
@inject IPlayerSessionService PlayerSession
@inject IGameManager GameManager
@inject NavigationManager Navigation

<PageTitle>MiniRisk — Ingresa tu nombre</PageTitle>

<div class="home-container">
    <div class="home-logo">
        <h1 class="logo-text">🎲 MiniRisk</h1>
        <p class="logo-subtitle">Conquista el mundo con tus amigos</p>
    </div>

    @if (_reconnectionInfo != null)
    {
        <ReconnectionBanner Info="_reconnectionInfo"
                            OnReconnect="HandleReconnect"
                            OnDecline="HandleDeclineReconnect" />
    }

    <div class="home-card">
        <label class="input-label" for="playerName">Tu nombre</label>
        <input id="playerName"
               class="input @(_errorMessage != null ? "input--error" : "")"
               type="text"
               maxlength="20"
               placeholder="Ej: Carlos"
               @bind="_playerName"
               @bind:event="oninput"
               @onkeydown="HandleKeyDown" />

        @if (_errorMessage != null)
        {
            <p class="input-error">@_errorMessage</p>
        }

        <button id="btnEnter"
                class="btn btn-primary btn-full"
                disabled="@(!CanEnter)"
                @onclick="HandleEnter">
            ENTRAR →
        </button>
    </div>

    <p class="home-footer">v1.0 • Red Local</p>
</div>

@code {
    private string _playerName = string.Empty;
    private string? _errorMessage;
    private ReconnectionInfo? _reconnectionInfo;

    private bool CanEnter => !string.IsNullOrWhiteSpace(_playerName)
                          && _playerName.Trim().Length >= 3
                          && _errorMessage == null;

    protected override void OnInitialized()
    {
        // Si ya se identificó, redirigir al lobby
        if (PlayerSession.IsIdentified)
        {
            Navigation.NavigateTo("/lobby");
        }
    }

    private void HandleEnter()
    {
        var name = _playerName.Trim();

        // Validaciones
        if (name.Length < 3 || name.Length > 20)
        {
            _errorMessage = "El nombre debe tener entre 3 y 20 caracteres.";
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z0-9áéíóúñÁÉÍÓÚÑ\s]+$"))
        {
            _errorMessage = "Solo se permiten letras, números y espacios.";
            return;
        }

        if (GameManager.IsNameTaken(name))
        {
            _errorMessage = "Ese nombre ya está en uso.";
            return;
        }

        // Comprobar reconexión
        _reconnectionInfo = GameManager.GetReconnectionInfo(name);
        if (_reconnectionInfo != null)
        {
            return; // Mostrar el banner de reconexión
        }

        // Identificar y navegar
        PlayerSession.SetPlayer(name);
        Navigation.NavigateTo("/lobby");
    }

    private void HandleReconnect()
    {
        PlayerSession.SetPlayer(_playerName.Trim());
        PlayerSession.JoinGame(_reconnectionInfo!.GameId);
        Navigation.NavigateTo($"/game/{_reconnectionInfo.GameId}");
    }

    private void HandleDeclineReconnect()
    {
        _reconnectionInfo = null;
        PlayerSession.SetPlayer(_playerName.Trim());
        Navigation.NavigateTo("/lobby");
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        _errorMessage = null; // Limpiar error al escribir
        if (e.Key == "Enter" && CanEnter)
        {
            HandleEnter();
        }
    }
}
```

**Ciclo de vida:**

```
OnInitialized()
  │
  ├── ¿Ya identificado? → NavigateTo("/lobby")
  │
  └── No → Renderizar formulario
            │
            └── HandleEnter()
                ├── Validar nombre
                ├── ¿Nombre en uso? → Error
                ├── ¿Reconexión disponible? → Mostrar banner
                └── OK → SetPlayer() → NavigateTo("/lobby")
```

---

### 2.2 Lobby.razor — Gestión de Partidas

**Ruta:** `/lobby`

```razor
@page "/lobby"
@inject IPlayerSessionService PlayerSession
@inject IGameManager GameManager
@inject NavigationManager Navigation
@implements IAsyncDisposable

<PageTitle>MiniRisk — Lobby</PageTitle>

<div class="lobby-container">
    <header class="lobby-header">
        <h1 class="logo-text">🎲 MiniRisk</h1>
        <div class="player-badge">
            @PlayerSession.PlayerName
            <span class="connection-dot connected"></span>
        </div>
    </header>

    @if (_currentGameId != null)
    {
        <!-- Sala de espera -->
        <WaitingRoom GameId="@_currentGameId"
                     PlayerId="@PlayerSession.PlayerId"
                     IsCreator="@_isCreator"
                     OnLeave="HandleLeaveGame"
                     OnStart="HandleStartGame"
                     HubConnection="_hubConnection" />
    }
    else
    {
        <!-- Lista de partidas -->
        <div class="lobby-content">
            <div class="lobby-title-row">
                <h2>Partidas Disponibles</h2>
                <button class="btn btn-primary" @onclick="() => _showCreateDialog = true">
                    + Crear Partida
                </button>
            </div>

            @if (_games.Count == 0)
            {
                <div class="empty-state">
                    <p>No hay partidas disponibles.</p>
                    <p>¡Crea una nueva para empezar!</p>
                </div>
            }
            else
            {
                <div class="game-list">
                    @foreach (var game in _games)
                    {
                        <GameCard Game="@game" OnJoin="() => HandleJoinGame(game.Id)" />
                    }
                </div>
            }
        </div>
    }

    @if (_showCreateDialog)
    {
        <CreateGameDialog OnCreate="HandleCreateGame"
                          OnCancel="() => _showCreateDialog = false" />
    }
</div>

@code {
    private HubConnection? _hubConnection;
    private List<GameSummary> _games = [];
    private string? _currentGameId;
    private bool _isCreator;
    private bool _showCreateDialog;

    protected override async Task OnInitializedAsync()
    {
        if (!PlayerSession.IsIdentified)
        {
            Navigation.NavigateTo("/");
            return;
        }

        // Conectar al hub
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/gamehub"))
            .WithAutomaticReconnect()
            .Build();

        // Escuchar actualizaciones del lobby
        _hubConnection.On<List<GameSummary>>("LobbyUpdated", games =>
        {
            _games = games;
            InvokeAsync(StateHasChanged);
        });

        _hubConnection.On<string>("GameStarting", gameId =>
        {
            Navigation.NavigateTo($"/game/{gameId}");
        });

        await _hubConnection.StartAsync();

        // Registrar conexión y obtener partidas
        await _hubConnection.SendAsync("RegisterPlayer",
            PlayerSession.PlayerId, PlayerSession.PlayerName);

        _games = GameManager.GetAvailableGames();
    }

    private async Task HandleCreateGame(string name, GameSettings settings)
    {
        _showCreateDialog = false;

        var summary = GameManager.CreateGame(
            name, PlayerSession.PlayerId, PlayerSession.PlayerName, settings);

        await HandleJoinGame(summary.Id);
        _isCreator = true;
    }

    private async Task HandleJoinGame(string gameId)
    {
        if (_hubConnection == null) return;

        var result = GameManager.AddPlayer(
            gameId, PlayerSession.PlayerId,
            PlayerSession.PlayerName, _hubConnection.ConnectionId!);

        if (result.Success)
        {
            _currentGameId = gameId;
            PlayerSession.JoinGame(gameId);
            PlayerSession.SetColor(result.AssignedColor);

            await _hubConnection.SendAsync("JoinGameGroup", gameId);
        }
    }

    private async Task HandleLeaveGame()
    {
        if (_currentGameId != null && _hubConnection != null)
        {
            GameManager.RemovePlayer(_currentGameId, PlayerSession.PlayerId);
            await _hubConnection.SendAsync("LeaveGameGroup", _currentGameId);

            _currentGameId = null;
            _isCreator = false;
            PlayerSession.LeaveGame();
            _games = GameManager.GetAvailableGames();
        }
    }

    private async Task HandleStartGame()
    {
        if (_currentGameId != null && _hubConnection != null)
        {
            await _hubConnection.SendAsync("StartGame", _currentGameId, PlayerSession.PlayerId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
```

**Flujo:**

```
OnInitializedAsync()
  │
  ├── ¿No identificado? → NavigateTo("/")
  │
  └── Crear HubConnection → StartAsync()
      │
      ├── RegisterPlayer() → Servidor registra conexión
      │
      └── GetAvailableGames() → Renderizar lista
          │
          ├── [Crear Partida] → CreateGameDialog → CreateGame() → JoinGame()
          ├── [Unirse]        → AddPlayer() → JoinGameGroup() → WaitingRoom
          └── [Evento: GameStarting] → NavigateTo("/game/{id}")
```

---

### 2.3 Game.razor — Pantalla Principal del Juego

**Ruta:** `/game/{GameId}`

Este es el **componente orquestador principal**. Gestiona la conexión SignalR del juego, almacena todo el estado, y lo distribuye a los componentes hijos.

```razor
@page "/game/{GameId}"
@inject IPlayerSessionService PlayerSession
@inject IGameManager GameManager
@inject NavigationManager Navigation
@implements IAsyncDisposable

<PageTitle>MiniRisk — @(_gameState?.GameName ?? "Cargando...")</PageTitle>

@if (_gameState == null)
{
    <div class="loading">Cargando partida...</div>
}
else if (_gameState.Status == GameStatus.Finished)
{
    <GameOverOverlay GameState="_gameState"
                     MyPlayerId="@PlayerSession.PlayerId"
                     OnBackToLobby="NavigateToLobby" />
}
else
{
    <div class="game-layout">
        <GameHeader GameState="_gameState"
                    MyPlayerId="@PlayerSession.PlayerId" />

        <WorldMap GameState="_gameState"
                  MyPlayerId="@PlayerSession.PlayerId"
                  CurrentPhase="_gameState.Phase"
                  SelectedAttacker="_selectedAttacker"
                  SelectedDefender="_selectedDefender"
                  SelectedFortifyFrom="_selectedFortifyFrom"
                  SelectedFortifyTo="_selectedFortifyTo"
                  OnTerritoryClicked="HandleTerritoryClicked" />

        <PlayerSidebar GameState="_gameState"
                       MyPlayerId="@PlayerSession.PlayerId"
                       OnTradeCards="HandleTradeCards" />

        <TurnControls GameState="_gameState"
                      MyPlayerId="@PlayerSession.PlayerId"
                      SelectedAttacker="_selectedAttacker"
                      SelectedDefender="_selectedDefender"
                      SelectedFortifyFrom="_selectedFortifyFrom"
                      SelectedFortifyTo="_selectedFortifyTo"
                      LastAttackResult="_lastAttackResult"
                      OnPlaceReinforcements="HandlePlaceReinforcements"
                      OnAttack="HandleAttack"
                      OnEndAttackPhase="HandleEndAttackPhase"
                      OnFortify="HandleFortify"
                      OnSkipFortification="HandleSkipFortification"
                      OnConfirmReinforcements="HandleConfirmReinforcements" />

        <EventLog Events="_gameState.RecentEvents" />
    </div>

    <ToastContainer Toasts="_toasts" />
}

@code {
    [Parameter] public string GameId { get; set; } = string.Empty;

    // ═══════════════════════════════════════
    // ESTADO
    // ═══════════════════════════════════════
    private HubConnection? _hubConnection;
    private GameStateDto? _gameState;

    // Selección de territorios
    private string? _selectedAttacker;
    private string? _selectedDefender;
    private string? _selectedFortifyFrom;
    private string? _selectedFortifyTo;

    // Resultado del último ataque
    private AttackResult? _lastAttackResult;

    // Notificaciones
    private List<ToastMessage> _toasts = [];

    // ═══════════════════════════════════════
    // PROPIEDADES COMPUTADAS
    // ═══════════════════════════════════════
    private bool IsMyTurn => _gameState?.CurrentPlayerId == PlayerSession.PlayerId;

    // ═══════════════════════════════════════
    // CICLO DE VIDA
    // ═══════════════════════════════════════

    protected override async Task OnInitializedAsync()
    {
        if (!PlayerSession.IsIdentified)
        {
            Navigation.NavigateTo("/");
            return;
        }

        // Crear conexión SignalR
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/gamehub"))
            .WithAutomaticReconnect()
            .Build();

        RegisterHubHandlers();

        await _hubConnection.StartAsync();

        // Unirse al grupo del juego
        await _hubConnection.SendAsync("JoinGameGroup", GameId);
        await _hubConnection.SendAsync("RegisterPlayer",
            PlayerSession.PlayerId, PlayerSession.PlayerName);

        // Obtener estado inicial
        _gameState = GameManager.GetGameState(GameId);
    }

    private void RegisterHubHandlers()
    {
        // ── Estado actualizado ──
        _hubConnection!.On<GameStateDto>("GameStateUpdated", state =>
        {
            _gameState = state;
            InvokeAsync(StateHasChanged);
        });

        // ── Resultado de dados ──
        _hubConnection.On<AttackResult>("DiceRolled", result =>
        {
            _lastAttackResult = result;
            InvokeAsync(StateHasChanged);
        });

        // ── Territorio conquistado ──
        _hubConnection.On<string, string>("TerritoryConquered",
            (playerName, territoryName) =>
        {
            AddToast($"🏴 {playerName} conquistó {territoryName}",
                ToastType.Conquest);
            // Limpiar selección
            _selectedAttacker = null;
            _selectedDefender = null;
            InvokeAsync(StateHasChanged);
        });

        // ── Jugador eliminado ──
        _hubConnection.On<string>("PlayerEliminated", playerName =>
        {
            AddToast($"💀 {playerName} ha sido eliminado", ToastType.Elimination);
            InvokeAsync(StateHasChanged);
        });

        // ── Tu turno ──
        _hubConnection.On("YourTurn", () =>
        {
            AddToast("¡Es tu turno! Fase de refuerzo.", ToastType.YourTurn);
            InvokeAsync(StateHasChanged);
        });

        // ── Fin de partida ──
        _hubConnection.On<string>("GameOver", winnerName =>
        {
            InvokeAsync(StateHasChanged);
        });

        // ── Error ──
        _hubConnection.On<ActionErrorDto>("ActionError", error =>
        {
            AddToast($"❌ {error.Message}", ToastType.Error);
            InvokeAsync(StateHasChanged);
        });

        // ── Desconexión de jugador ──
        _hubConnection.On<string>("PlayerDisconnected", playerName =>
        {
            AddToast($"⚠️ {playerName} se ha desconectado...",
                ToastType.Warning, persistent: true);
            InvokeAsync(StateHasChanged);
        });

        // ── Reconexión de jugador ──
        _hubConnection.On<string>("PlayerReconnected", playerName =>
        {
            RemoveToast($"⚠️ {playerName} se ha desconectado...");
            AddToast($"✅ {playerName} se ha reconectado", ToastType.Info);
            InvokeAsync(StateHasChanged);
        });
    }

    // ═══════════════════════════════════════
    // HANDLERS DE TERRITORIO (desde WorldMap)
    // ═══════════════════════════════════════

    private void HandleTerritoryClicked(string territoryId)
    {
        if (!IsMyTurn || _gameState == null) return;

        var territory = _gameState.Territories
            .FirstOrDefault(t => t.TerritoryId == territoryId);
        if (territory == null) return;

        switch (_gameState.Phase)
        {
            case GamePhase.Reinforcement:
                HandleReinforcementClick(territory);
                break;
            case GamePhase.Attack:
                HandleAttackClick(territory);
                break;
            case GamePhase.Fortification:
                HandleFortifyClick(territory);
                break;
        }
    }

    private void HandleReinforcementClick(TerritoryDto territory)
    {
        if (territory.OwnerId == PlayerSession.PlayerId)
        {
            _selectedAttacker = territory.TerritoryId;
        }
    }

    private void HandleAttackClick(TerritoryDto territory)
    {
        if (territory.OwnerId == PlayerSession.PlayerId && territory.Armies >= 2)
        {
            // Seleccionar como atacante
            _selectedAttacker = territory.TerritoryId;
            _selectedDefender = null;
            _lastAttackResult = null;
        }
        else if (territory.OwnerId != PlayerSession.PlayerId
                 && _selectedAttacker != null)
        {
            // Seleccionar como defensor
            _selectedDefender = territory.TerritoryId;
        }
    }

    private void HandleFortifyClick(TerritoryDto territory)
    {
        if (territory.OwnerId != PlayerSession.PlayerId) return;

        if (_selectedFortifyFrom == null)
        {
            _selectedFortifyFrom = territory.TerritoryId;
        }
        else if (_selectedFortifyTo == null)
        {
            _selectedFortifyTo = territory.TerritoryId;
        }
        else
        {
            // Reset y seleccionar nuevo origen
            _selectedFortifyFrom = territory.TerritoryId;
            _selectedFortifyTo = null;
        }
    }

    // ═══════════════════════════════════════
    // ACCIONES DEL JUEGO (envían al Hub)
    // ═══════════════════════════════════════

    private async Task HandlePlaceReinforcements(string territoryId, int count)
    {
        if (_hubConnection == null) return;
        await _hubConnection.SendAsync("PlaceReinforcements",
            GameId, PlayerSession.PlayerId, territoryId, count);
    }

    private async Task HandleConfirmReinforcements()
    {
        if (_hubConnection == null) return;
        await _hubConnection.SendAsync("ConfirmReinforcements",
            GameId, PlayerSession.PlayerId);
    }

    private async Task HandleAttack(int diceCount)
    {
        if (_hubConnection == null || _selectedAttacker == null
            || _selectedDefender == null) return;

        await _hubConnection.SendAsync("Attack",
            GameId, PlayerSession.PlayerId,
            _selectedAttacker, _selectedDefender, diceCount);
    }

    private async Task HandleEndAttackPhase()
    {
        if (_hubConnection == null) return;

        _selectedAttacker = null;
        _selectedDefender = null;
        _lastAttackResult = null;

        await _hubConnection.SendAsync("EndAttackPhase",
            GameId, PlayerSession.PlayerId);
    }

    private async Task HandleFortify(int armyCount)
    {
        if (_hubConnection == null || _selectedFortifyFrom == null
            || _selectedFortifyTo == null) return;

        await _hubConnection.SendAsync("Fortify",
            GameId, PlayerSession.PlayerId,
            _selectedFortifyFrom, _selectedFortifyTo, armyCount);

        _selectedFortifyFrom = null;
        _selectedFortifyTo = null;
    }

    private async Task HandleSkipFortification()
    {
        if (_hubConnection == null) return;

        _selectedFortifyFrom = null;
        _selectedFortifyTo = null;

        await _hubConnection.SendAsync("SkipFortification",
            GameId, PlayerSession.PlayerId);
    }

    private async Task HandleTradeCards(string[] cardIds)
    {
        if (_hubConnection == null) return;
        await _hubConnection.SendAsync("TradeCards",
            GameId, PlayerSession.PlayerId, cardIds);
    }

    // ═══════════════════════════════════════
    // UTILIDADES
    // ═══════════════════════════════════════

    private void NavigateToLobby()
    {
        PlayerSession.LeaveGame();
        Navigation.NavigateTo("/lobby");
    }

    private void AddToast(string message, ToastType type,
        bool persistent = false)
    {
        _toasts.Add(new ToastMessage
        {
            Message = message,
            Type = type,
            Persistent = persistent,
            CreatedAt = DateTime.UtcNow
        });
    }

    private void RemoveToast(string message)
    {
        _toasts.RemoveAll(t => t.Message == message);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.SendAsync("LeaveGameGroup", GameId);
            await _hubConnection.DisposeAsync();
        }
    }
}
```

**Diagrama de estado interno de Game.razor:**

```
  Game.razor
  ┌────────────────────────────────┐
  │                                │
  │  _gameState: GameStateDto      │ ← Fuente de verdad para toda la UI
  │  _selectedAttacker: string?    │ ← Selección de territorios (local)
  │  _selectedDefender: string?    │ ← Selección de territorios (local)
  │  _selectedFortifyFrom: string? │ ← Selección de fortificación (local)
  │  _selectedFortifyTo: string?   │ ← Selección de fortificación (local)
  │  _lastAttackResult: Result?    │ ← Último resultado de dados (local)
  │  _toasts: List<ToastMessage>   │ ← Notificaciones (local)
  │                                │
  │  _hubConnection: HubConnection │ ← Canal bidireccional al servidor
  │                                │
  └────────────────────────────────┘
```

---

## 3. Componentes Compartidos

### 3.1 GameHeader.razor — Barra Superior

```razor
<div class="game-header">
    <span class="header-logo">🎲 MiniRisk</span>
    <span class="header-turn">Turno @GameState.TurnNumber</span>
    <span class="header-phase phase-@GameState.Phase.ToString().ToLower()">
        @GetPhaseLabel()
    </span>
    <span class="header-current-player"
          style="color: var(--player-@GetColorClass())">
        @(IsMyTurn ? "► TU TURNO" : $"Turno de: {GameState.CurrentPlayerName}")
    </span>
    <span class="header-actions">
        <button class="btn btn-secondary btn-small" title="Configuración">⚙</button>
    </span>
</div>

@code {
    [Parameter] public GameStateDto GameState { get; set; } = default!;
    [Parameter] public string MyPlayerId { get; set; } = string.Empty;

    private bool IsMyTurn => GameState.CurrentPlayerId == MyPlayerId;

    private string GetPhaseLabel() => GameState.Phase switch
    {
        GamePhase.Setup => "⚙ CONFIGURACIÓN",
        GamePhase.Reinforcement => "🛡️ REFUERZO",
        GamePhase.Attack => "⚔️ ATAQUE",
        GamePhase.Fortification => "🏰 FORTIFICACIÓN",
        _ => GameState.Phase.ToString()
    };

    private string GetColorClass()
    {
        var player = GameState.Players
            .FirstOrDefault(p => p.PlayerId == GameState.CurrentPlayerId);
        return player?.Color.ToString().ToLower() ?? "neutral";
    }
}
```

---

### 3.2 WorldMap.razor — Mapa SVG Interactivo

```razor
<div class="world-map-container">
    <svg viewBox="0 0 1200 700"
         class="world-map"
         xmlns="http://www.w3.org/2000/svg">

        <!-- Fondo de continentes -->
        @foreach (var continent in ContinentBackgrounds)
        {
            <path d="@continent.PathData"
                  class="continent-bg continent-@continent.Name.ToLower()"
                  fill="@continent.FillColor"
                  stroke="@continent.StrokeColor"
                  opacity="0.3" />
        }

        <!-- Territorios -->
        @foreach (var territory in GameState.Territories)
        {
            <TerritoryPath Territory="territory"
                           IsSelectable="@IsTerritorySelectable(territory)"
                           IsSelected="@IsTerritorySelected(territory)"
                           IsTarget="@IsTerritoryTarget(territory)"
                           SelectionType="@GetSelectionType(territory)"
                           OnClicked="@(() => OnTerritoryClicked.InvokeAsync(territory.TerritoryId))" />
        }

        <!-- Líneas de conexión (adyacencias intercontinentales) -->
        <g class="adjacency-lines" opacity="0.2">
            <line x1="60" y1="180" x2="1140" y2="200"
                  stroke="white" stroke-dasharray="4,4" />
            <!-- Alaska — Kamchatka (a través del Pacífico) -->
        </g>
    </svg>
</div>

@code {
    [Parameter] public GameStateDto GameState { get; set; } = default!;
    [Parameter] public string MyPlayerId { get; set; } = string.Empty;
    [Parameter] public GamePhase CurrentPhase { get; set; }
    [Parameter] public string? SelectedAttacker { get; set; }
    [Parameter] public string? SelectedDefender { get; set; }
    [Parameter] public string? SelectedFortifyFrom { get; set; }
    [Parameter] public string? SelectedFortifyTo { get; set; }
    [Parameter] public EventCallback<string> OnTerritoryClicked { get; set; }

    private bool IsTerritorySelectable(TerritoryDto territory)
    {
        bool isMyTurn = GameState.CurrentPlayerId == MyPlayerId;
        if (!isMyTurn) return false;

        return CurrentPhase switch
        {
            GamePhase.Reinforcement => territory.OwnerId == MyPlayerId,
            GamePhase.Attack when SelectedAttacker == null
                => territory.OwnerId == MyPlayerId && territory.Armies >= 2,
            GamePhase.Attack when SelectedAttacker != null
                => territory.OwnerId != MyPlayerId, // Selección defensor
            GamePhase.Fortification => territory.OwnerId == MyPlayerId,
            _ => false
        };
    }

    private bool IsTerritorySelected(TerritoryDto territory)
    {
        return territory.TerritoryId == SelectedAttacker
            || territory.TerritoryId == SelectedFortifyFrom;
    }

    private bool IsTerritoryTarget(TerritoryDto territory)
    {
        return territory.TerritoryId == SelectedDefender
            || territory.TerritoryId == SelectedFortifyTo;
    }

    private string GetSelectionType(TerritoryDto territory)
    {
        if (territory.TerritoryId == SelectedAttacker) return "attacker";
        if (territory.TerritoryId == SelectedDefender) return "defender";
        if (territory.TerritoryId == SelectedFortifyFrom) return "fortify-from";
        if (territory.TerritoryId == SelectedFortifyTo) return "fortify-to";
        return "none";
    }
}
```

---

### 3.3 TerritoryPath.razor — Territorio Individual

```razor
<g class="territory
          @(IsSelectable ? "territory--selectable" : "territory--disabled")
          @(IsSelected ? "territory--selected" : "")
          @(IsTarget ? "territory--target" : "")
          territory--@SelectionType"
   @onclick="HandleClick">

    <!-- Forma del territorio -->
    <path d="@GetTerritoryPath()"
          fill="@GetFillColor()"
          stroke="@GetStrokeColor()"
          stroke-width="@GetStrokeWidth()"
          class="territory-shape" />

    <!-- Badge de ejércitos -->
    <circle cx="@GetCenterX()" cy="@GetCenterY()"
            r="12"
            fill="@GetFillColor()"
            stroke="rgba(0,0,0,0.5)"
            stroke-width="1.5"
            class="territory-army-circle" />
    <text x="@GetCenterX()" y="@(GetCenterY() + 4)"
          class="territory-army-badge">
        @Territory.Armies
    </text>
</g>

@code {
    [Parameter] public TerritoryDto Territory { get; set; } = default!;
    [Parameter] public bool IsSelectable { get; set; }
    [Parameter] public bool IsSelected { get; set; }
    [Parameter] public bool IsTarget { get; set; }
    [Parameter] public string SelectionType { get; set; } = "none";
    [Parameter] public EventCallback OnClicked { get; set; }

    private async Task HandleClick()
    {
        if (IsSelectable || IsSelected || IsTarget)
        {
            await OnClicked.InvokeAsync();
        }
    }

    private string GetFillColor()
    {
        return Territory.OwnerColor switch
        {
            PlayerColor.Red => "var(--player-red)",
            PlayerColor.Blue => "var(--player-blue)",
            PlayerColor.Green => "var(--player-green)",
            PlayerColor.Yellow => "var(--player-yellow)",
            PlayerColor.Purple => "var(--player-purple)",
            PlayerColor.Orange => "var(--player-orange)",
            _ => "var(--player-neutral)"
        };
    }

    private string GetStrokeColor() => SelectionType switch
    {
        "attacker" => "white",
        "defender" => "var(--color-danger)",
        "fortify-from" => "var(--color-info)",
        "fortify-to" => "var(--color-success)",
        _ => IsSelected ? "white" : "rgba(255,255,255,0.2)"
    };

    private string GetStrokeWidth() => (IsSelected || IsTarget) ? "3" : "1";

    // Coordenadas del territorio en el SVG
    // (obtenidas de MapService o hardcoded)
    private string GetTerritoryPath() => TerritoryPaths.Get(Territory.TerritoryId);
    private float GetCenterX() => TerritoryPaths.GetCenterX(Territory.TerritoryId);
    private float GetCenterY() => TerritoryPaths.GetCenterY(Territory.TerritoryId);
}
```

---

### 3.4 PlayerSidebar.razor — Panel Lateral

```razor
<div class="sidebar">
    <div class="panel">
        <div class="panel-header">JUGADORES</div>

        @foreach (var player in GameState.Players.Where(p => p.Color != PlayerColor.Neutral))
        {
            <PlayerCard Player="@player"
                        IsCurrentTurn="@(player.PlayerId == GameState.CurrentPlayerId)"
                        IsMe="@(player.PlayerId == MyPlayerId)" />
        }
    </div>

    @if (GetMyCards().Any())
    {
        <div class="panel">
            <div class="panel-header">MIS CARTAS</div>
            <CardHand Cards="@GetMyCards()"
                      CanTrade="@CanTradeCards()"
                      MustTrade="@MustTradeCards()"
                      OnTradeCards="OnTradeCards" />
        </div>
    }
</div>

@code {
    [Parameter] public GameStateDto GameState { get; set; } = default!;
    [Parameter] public string MyPlayerId { get; set; } = string.Empty;
    [Parameter] public EventCallback<string[]> OnTradeCards { get; set; }

    private List<CardDto> GetMyCards()
    {
        // Las cartas se obtienen de una propiedad privada del GameState
        // que solo contiene las cartas del jugador actual
        return GameState.MyCards ?? [];
    }

    private bool CanTradeCards() =>
        GetMyCards().Count >= 3
        && GameState.CurrentPlayerId == MyPlayerId
        && GameState.Phase == GamePhase.Reinforcement;

    private bool MustTradeCards() =>
        GetMyCards().Count >= 5
        && GameState.CurrentPlayerId == MyPlayerId;
}
```

---

### 3.5 PlayerCard.razor — Tarjeta de Jugador

```razor
<div class="player-card @(IsCurrentTurn ? "player-card--active" : "")
                        @(Player.IsEliminated ? "player-card--eliminated" : "")
                        @(IsMe ? "player-card--me" : "")"
     style="border-left: 3px solid var(--player-@Player.Color.ToString().ToLower())">

    <div class="player-card__header">
        @if (IsCurrentTurn)
        {
            <span class="turn-indicator">▶</span>
        }
        <span class="player-card__name">
            @Player.PlayerName
            @if (IsMe) { <span class="badge badge-me">TÚ</span> }
        </span>
        <span class="player-card__color"
              style="background: var(--player-@Player.Color.ToString().ToLower())">
        </span>
    </div>

    <div class="player-card__stats">
        <span title="Territorios">🗺️ @Player.TerritoryCount</span>
        <span title="Ejércitos">⚔️ @Player.TotalArmies</span>
        <span title="Cartas">🃏 @Player.CardCount</span>
    </div>

    @if (!Player.IsConnected && !Player.IsEliminated)
    {
        <div class="player-card__status disconnected">⚠️ Desconectado</div>
    }
    @if (Player.IsEliminated)
    {
        <div class="player-card__status eliminated">✕ Eliminado</div>
    }
</div>

@code {
    [Parameter] public PlayerDto Player { get; set; } = default!;
    [Parameter] public bool IsCurrentTurn { get; set; }
    [Parameter] public bool IsMe { get; set; }
}
```

---

### 3.6 DiceRoller.razor — Animación de Dados

```razor
<div class="dice-roller @(_isRolling ? "dice-roller--rolling" : "")">
    @if (AttackResult != null || _isRolling)
    {
        <div class="dice-row dice-row--attacker">
            <span class="dice-label">Ataque:</span>
            @foreach (var (die, index) in GetAttackerDice().Select((d, i) => (d, i)))
            {
                <div class="dice @(_isRolling ? "dice-rolling" : "")
                            @(GetDieResult(index, true))">
                    @(die > 0 ? die.ToString() : "?")
                </div>
            }
        </div>

        <div class="dice-vs">VS</div>

        <div class="dice-row dice-row--defender">
            <span class="dice-label">Defensa:</span>
            @foreach (var (die, index) in GetDefenderDice().Select((d, i) => (d, i)))
            {
                <div class="dice @(_isRolling ? "dice-rolling" : "")
                            @(GetDieResult(index, false))">
                    @(die > 0 ? die.ToString() : "?")
                </div>
            }
        </div>

        @if (AttackResult != null && !_isRolling)
        {
            <div class="dice-summary">
                <span class="loss loss--attacker">Atk: -@AttackResult.AttackerLosses</span>
                <span class="loss loss--defender">Def: -@AttackResult.DefenderLosses</span>
            </div>
        }
    }
</div>

@code {
    [Parameter] public AttackResult? AttackResult { get; set; }

    private bool _isRolling;

    protected override async Task OnParametersSetAsync()
    {
        if (AttackResult != null)
        {
            // Animación de dados girando
            _isRolling = true;
            StateHasChanged();

            await Task.Delay(1200); // 3 giros × 0.4s

            _isRolling = false;
            StateHasChanged();
        }
    }

    private int[] GetAttackerDice() => AttackResult?.AttackerDice ?? [];
    private int[] GetDefenderDice() => AttackResult?.DefenderDice ?? [];

    private string GetDieResult(int index, bool isAttacker)
    {
        if (_isRolling || AttackResult == null) return "";

        var atkDice = AttackResult.AttackerDice;
        var defDice = AttackResult.DefenderDice;

        if (index >= Math.Min(atkDice.Length, defDice.Length))
            return "dice-result-neutral";

        if (isAttacker)
            return atkDice[index] > defDice[index]
                ? "dice-result-win" : "dice-result-lose";
        else
            return defDice[index] >= atkDice[index]
                ? "dice-result-win" : "dice-result-lose";
    }
}
```

---

### 3.7 TurnControls.razor — Controles Según Fase

```razor
<div class="turn-controls panel">
    @switch (GameState.Phase)
    {
        case GamePhase.Reinforcement:
            <ReinforcementPanel GameState="@GameState"
                                MyPlayerId="@MyPlayerId"
                                SelectedTerritory="@SelectedAttacker"
                                OnPlace="OnPlaceReinforcements"
                                OnConfirm="OnConfirmReinforcements" />
            break;

        case GamePhase.Attack:
            <AttackPanel GameState="@GameState"
                         MyPlayerId="@MyPlayerId"
                         SelectedAttacker="@SelectedAttacker"
                         SelectedDefender="@SelectedDefender"
                         LastResult="@LastAttackResult"
                         OnAttack="OnAttack"
                         OnEndPhase="OnEndAttackPhase" />
            break;

        case GamePhase.Fortification:
            <FortifyPanel GameState="@GameState"
                          MyPlayerId="@MyPlayerId"
                          SelectedFrom="@SelectedFortifyFrom"
                          SelectedTo="@SelectedFortifyTo"
                          OnFortify="OnFortify"
                          OnSkip="OnSkipFortification" />
            break;
    }
</div>

@code {
    [Parameter] public GameStateDto GameState { get; set; } = default!;
    [Parameter] public string MyPlayerId { get; set; } = string.Empty;
    [Parameter] public string? SelectedAttacker { get; set; }
    [Parameter] public string? SelectedDefender { get; set; }
    [Parameter] public string? SelectedFortifyFrom { get; set; }
    [Parameter] public string? SelectedFortifyTo { get; set; }
    [Parameter] public AttackResult? LastAttackResult { get; set; }
    [Parameter] public EventCallback<(string, int)> OnPlaceReinforcements { get; set; }
    [Parameter] public EventCallback OnConfirmReinforcements { get; set; }
    [Parameter] public EventCallback<int> OnAttack { get; set; }
    [Parameter] public EventCallback OnEndAttackPhase { get; set; }
    [Parameter] public EventCallback<int> OnFortify { get; set; }
    [Parameter] public EventCallback OnSkipFortification { get; set; }
}
```

---

### 3.8 EventLog.razor — Log de Eventos

```razor
<div class="event-log panel" @ref="_logContainer">
    <div class="panel-header">LOG DE EVENTOS</div>

    <div class="event-list">
        @foreach (var evt in Events)
        {
            <div class="event-entry event-@evt.Type.ToLower()">
                <span class="event-time">@evt.Timestamp.ToString("HH:mm")</span>
                <span class="event-icon">@GetEventIcon(evt.Type)</span>
                <span class="event-message">@evt.Message</span>
            </div>
        }
    </div>
</div>

@code {
    [Parameter] public List<GameEventDto> Events { get; set; } = [];

    private ElementReference _logContainer;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Auto-scroll al final
        // (requiere JS interop mínimo)
    }

    private string GetEventIcon(string eventType) => eventType switch
    {
        "GameStarted" => "🎮",
        "TurnStarted" => "🔄",
        "ReinforcementsPlaced" => "🛡️",
        "AttackLaunched" => "⚔️",
        "DiceRolled" => "🎲",
        "TerritoryConquered" => "🏴",
        "PlayerEliminated" => "💀",
        "CardsTraded" => "🃏",
        "Fortified" => "🏰",
        "TurnEnded" => "⏭️",
        "PlayerDisconnected" => "⚠️",
        "PlayerReconnected" => "✅",
        "GameOver" => "🏆",
        "ChatMessage" => "💬",
        _ => "📌"
    };
}
```

---

### 3.9 GameOverOverlay.razor — Pantalla de Victoria

```razor
<div class="gameover-overlay">
    <div class="gameover-backdrop"></div>
    <div class="gameover-content">
        <h1 class="gameover-title">🏆 ¡VICTORIA! 🏆</h1>

        <p class="gameover-winner"
           style="color: var(--player-@WinnerColor.ToLower())">
            @WinnerName ha conquistado el mundo
        </p>

        <div class="gameover-stats">
            <div class="stat">
                <span class="stat-label">Turnos</span>
                <span class="stat-value">@GameState.TurnNumber</span>
            </div>
            <div class="stat">
                <span class="stat-label">Duración</span>
                <span class="stat-value">@GetDuration()</span>
            </div>
        </div>

        @if (IsWinner)
        {
            <p class="gameover-message">¡Felicidades, has conquistado el mundo! 🎉</p>
        }
        else
        {
            <p class="gameover-message">Mejor suerte la próxima vez...</p>
        }

        <button class="btn btn-primary" @onclick="OnBackToLobby">
            Volver al Lobby
        </button>
    </div>
</div>

@code {
    [Parameter] public GameStateDto GameState { get; set; } = default!;
    [Parameter] public string MyPlayerId { get; set; } = string.Empty;
    [Parameter] public EventCallback OnBackToLobby { get; set; }

    private string WinnerName => GameState.Players
        .FirstOrDefault(p => !p.IsEliminated)?.PlayerName ?? "?";
    private string WinnerColor => GameState.Players
        .FirstOrDefault(p => !p.IsEliminated)?.Color.ToString() ?? "Neutral";
    private bool IsWinner => GameState.Players
        .FirstOrDefault(p => !p.IsEliminated)?.PlayerId == MyPlayerId;

    private string GetDuration()
    {
        var elapsed = DateTime.UtcNow - GameState.StartedAt;
        return elapsed.TotalMinutes < 60
            ? $"{(int)elapsed.TotalMinutes} min"
            : $"{(int)elapsed.TotalHours}h {elapsed.Minutes:D2}min";
    }
}
```

---

## 4. Ciclo de Vida de los Componentes

### 4.1 Diagrama General

```
  ┌─────────────────────────────────────────────────────────┐
  │         CICLO DE VIDA DE UN COMPONENTE BLAZOR            │
  │                                                         │
  │  1. SetParametersAsync()    ← Parámetros del padre      │
  │     │                                                   │
  │  2. OnInitialized[Async]()  ← Solo la primera vez       │
  │     │                                                   │
  │  3. OnParametersSet[Async]()← Cada vez que cambian params│
  │     │                                                   │
  │  4. ShouldRender()          ← ¿Debe re-renderizar?      │
  │     │                                                   │
  │  5. BuildRenderTree()       ← Genera el DOM virtual      │
  │     │                                                   │
  │  6. OnAfterRender[Async]()  ← DOM actualizado           │
  │     │   (firstRender: bool)                             │
  │     │                                                   │
  │  ───── Eventos del usuario o SignalR ─────              │
  │     │                                                   │
  │  7. StateHasChanged()       ← Forzar re-render          │
  │     │                                                   │
  │  (vuelve a paso 4)                                      │
  │                                                         │
  │  8. Dispose[Async]()        ← Al destruir el componente │
  │                                                         │
  └─────────────────────────────────────────────────────────┘
```

### 4.2 Uso en MiniRisk

| Método | Dónde se usa | Ejemplo |
|--------|:------------|---------|
| `OnInitialized` | `Home.razor` | Verificar si ya está identificado |
| `OnInitializedAsync` | `Lobby.razor`, `Game.razor` | Crear `HubConnection`, conectar, obtener estado |
| `OnParametersSetAsync` | `DiceRoller.razor` | Detectar nuevo resultado → lanzar animación |
| `OnAfterRenderAsync` | `EventLog.razor` | Auto-scroll al final del log |
| `DisposeAsync` | `Game.razor`, `Lobby.razor` | Cerrar `HubConnection` |
| `StateHasChanged` | `Game.razor` (handlers) | Siempre vía `InvokeAsync(StateHasChanged)` dentro de handlers de SignalR |

### 4.3 InvokeAsync — Regla Clave

Los handlers de SignalR se ejecutan en un hilo del ThreadPool, **no en el hilo del circuito Blazor**. Por eso, siempre se debe usar `InvokeAsync(StateHasChanged)`:

```csharp
// ❌ INCORRECTO: StateHasChanged desde otro hilo
_hubConnection.On<GameStateDto>("GameStateUpdated", state =>
{
    _gameState = state;
    StateHasChanged(); // Excepción: wrong synchronization context
});

// ✅ CORRECTO: InvokeAsync vuelve al hilo del circuito
_hubConnection.On<GameStateDto>("GameStateUpdated", state =>
{
    _gameState = state;
    InvokeAsync(StateHasChanged);
});
```

---

## 5. Comunicación entre Componentes — Resumen

```
┌───────────────────────────────────────────────────────────────────┐
│  Game.razor (Orquestador)                                         │
│                                                                   │
│  Recibe de SignalR:           Envía a SignalR:                    │
│  ├── GameStateUpdated         ├── Attack(...)                     │
│  ├── DiceRolled               ├── PlaceReinforcements(...)        │
│  ├── TerritoryConquered       ├── Fortify(...)                    │
│  ├── PlayerEliminated         ├── TradeCards(...)                  │
│  ├── YourTurn                 ├── EndAttackPhase(...)              │
│  ├── GameOver                 ├── SkipFortification(...)           │
│  ├── ActionError              └── ConfirmReinforcements(...)       │
│  └── PlayerDisconnected                                           │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │ Distribuye datos a hijos vía [Parameter]:                   │  │
│  │                                                             │  │
│  │  GameHeader ← GameState, MyPlayerId                         │  │
│  │  WorldMap   ← GameState, Selections, OnTerritoryClicked     │  │
│  │  Sidebar    ← GameState, MyPlayerId, OnTradeCards           │  │
│  │  Controls   ← GameState, Selections, On*                   │  │
│  │  EventLog   ← Events                                       │  │
│  │  Toast      ← Toasts                                       │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │ Recibe acciones de hijos vía [EventCallback]:               │  │
│  │                                                             │  │
│  │  WorldMap     → OnTerritoryClicked(territoryId)             │  │
│  │  Controls     → OnAttack(diceCount)                         │  │
│  │               → OnPlaceReinforcements(territory, count)     │  │
│  │               → OnFortify(armyCount)                        │  │
│  │               → OnEndAttackPhase()                          │  │
│  │               → OnSkipFortification()                       │  │
│  │               → OnConfirmReinforcements()                   │  │
│  │  Sidebar      → OnTradeCards(cardIds)                       │  │
│  │  GameOver     → OnBackToLobby()                             │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                                                                   │
└───────────────────────────────────────────────────────────────────┘
```

---

## 6. DTOs para la Vista

Objetos transferidos del servidor a la UI:

```csharp
// Estado completo de la partida (para la vista)
public class GameStateDto
{
    public string GameId { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public GameStatus Status { get; set; }
    public GamePhase Phase { get; set; }
    public string CurrentPlayerId { get; set; } = string.Empty;
    public string CurrentPlayerName { get; set; } = string.Empty;
    public int TurnNumber { get; set; }
    public int TradeCount { get; set; }
    public int RemainingReinforcements { get; set; }
    public List<PlayerDto> Players { get; set; } = [];
    public List<TerritoryDto> Territories { get; set; } = [];
    public List<GameEventDto> RecentEvents { get; set; } = [];
    public List<CardDto>? MyCards { get; set; }  // Solo las cartas del destinatario
    public DateTime StartedAt { get; set; }
}

public class PlayerDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public PlayerColor Color { get; set; }
    public int TerritoryCount { get; set; }
    public int TotalArmies { get; set; }
    public int CardCount { get; set; }
    public bool IsConnected { get; set; }
    public bool IsEliminated { get; set; }
}

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

public class CardDto
{
    public string CardId { get; set; } = string.Empty;
    public CardType Type { get; set; }
    public string? TerritoryName { get; set; }
}

public class GameEventDto
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? PlayerName { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ActionErrorDto
{
    public string Message { get; set; } = string.Empty;
    public string ActionAttempted { get; set; } = string.Empty;
}

public class ToastMessage
{
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; }
    public bool Persistent { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum ToastType
{
    Info, Success, Warning, Error,
    Conquest, Elimination, YourTurn
}
```

---

> **Siguiente documento:** [10 — Mapa del Mundo](./10_Mapa_Mundo.md)
