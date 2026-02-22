using MiniRisk.Models.Enums;

namespace MiniRisk.Models.Dtos;

public class PlayerJoinedDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public PlayerColor Color { get; set; }
}

public class PlayerLeftDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
}

public class PlayerReconnectedDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
}

public class PlayerDisconnectedDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
}

public class PlayerEliminatedDto
{
    public string EliminatedPlayerId { get; set; } = string.Empty;
    public string EliminatedPlayerName { get; set; } = string.Empty;
    public string EliminatedByPlayerId { get; set; } = string.Empty;
    public string EliminatedByPlayerName { get; set; } = string.Empty;
    public int CardsTransferred { get; set; }
}
