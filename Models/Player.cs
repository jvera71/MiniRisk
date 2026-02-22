using MiniRisk.Models.Enums;

namespace MiniRisk.Models;

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
