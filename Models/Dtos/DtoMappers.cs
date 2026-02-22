using MiniRisk.Models;
using MiniRisk.Models.Dtos;
using MiniRisk.Models.Enums;
using MiniRisk.Services.Interfaces;

namespace MiniRisk.Models.Dtos;

public static class DtoMappers
{
    public static GameStateDto ToDto(this Game game, IMapService mapService)
    {
        return new GameStateDto
        {
            GameId = game.Id,
            GameName = game.Name,
            Status = game.Status,
            Phase = game.Phase,
            CurrentPlayerId = game.GetCurrentPlayer().Id,
            CurrentPlayerName = game.GetCurrentPlayer().Name,
            TurnNumber = game.TurnNumber,
            TradeCount = game.TradeCount,
            RemainingReinforcements = game.RemainingReinforcements,
            StartedAt = game.StartedAt ?? DateTime.MinValue,
            Players = game.Players.Select(p => p.ToDto(game)).ToList(),
            Territories = game.Territories.Values.Select(t => t.ToDto(game, mapService)).ToList(),
            RecentEvents = game.EventLog.TakeLast(10).Select(e => e.ToDto()).ToList()
        };
    }

    public static PlayerDto ToDto(this Player player, Game game)
    {
        return new PlayerDto
        {
            PlayerId = player.Id,
            PlayerName = player.Name,
            Color = player.Color,
            IsConnected = player.IsConnected,
            IsEliminated = player.IsEliminated,
            CardCount = player.Cards.Count,
            TerritoryCount = player.GetOwnedTerritories(game).Count(),
            TotalArmies = player.GetOwnedTerritories(game).Sum(t => t.Armies)
        };
    }

    public static TerritoryDto ToDto(this Territory territory, Game game, IMapService mapService)
    {
        var owner = game.GetPlayerById(territory.OwnerId);
        return new TerritoryDto
        {
            TerritoryId = territory.Name.ToString(),
            TerritoryName = mapService.GetTerritoryDisplayName(territory.Name),
            Continent = territory.Continent,
            OwnerId = territory.OwnerId,
            OwnerName = owner?.Name ?? "Neutral",
            OwnerColor = owner?.Color ?? PlayerColor.Neutral,
            Armies = territory.Armies
        };
    }

    public static GameEventDto ToDto(this GameEvent gameEvent)
    {
        return new GameEventDto
        {
            Message = gameEvent.Message,
            Type = gameEvent.Type,
            PlayerId = gameEvent.PlayerId,
            PlayerName = gameEvent.PlayerName,
            Timestamp = gameEvent.Timestamp
        };
    }

    public static CardDto ToDto(this Card card, IMapService mapService)
    {
        return new CardDto
        {
            CardId = card.Id,
            Type = card.Type,
            Territory = card.Territory,
            TerritoryDisplayName = card.Territory.HasValue ? mapService.GetTerritoryDisplayName(card.Territory.Value) : null
        };
    }
}
