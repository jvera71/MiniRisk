namespace MiniRisk.Models.Enums;

public enum GameStatus
{
    /// <summary>La partida está en el lobby esperando jugadores.</summary>
    WaitingForPlayers,
    
    /// <summary>La partida está en curso.</summary>
    Playing,
    
    /// <summary>La partida ha terminado.</summary>
    Finished
}
