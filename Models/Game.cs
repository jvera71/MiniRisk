using MiniRisk.Models.Enums;

namespace MiniRisk.Models;

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
