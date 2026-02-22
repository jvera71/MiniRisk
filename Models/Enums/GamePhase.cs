namespace MiniRisk.Models.Enums;

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
