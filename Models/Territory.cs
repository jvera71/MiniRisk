using MiniRisk.Models.Enums;

namespace MiniRisk.Models;

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
