using MiniRisk.Models;
using MiniRisk.Models.Enums;
using MiniRisk.Services.Interfaces;

namespace MiniRisk.Services;

public class GameEngine : IGameEngine
{
    private readonly IDiceService _diceService;
    private readonly ICardService _cardService;
    private readonly IMapService _mapService;

    public GameEngine(
        IDiceService diceService,
        ICardService cardService,
        IMapService mapService)
    {
        _diceService = diceService;
        _cardService = cardService;
        _mapService = mapService;
    }

    // ═══════════════════════════════════════
    // INICIALIZACIÓN
    // ═══════════════════════════════════════

    public GameResult InitializeGame(Game game)
    {
        game.Territories = _mapService.CreateTerritories();
        game.Continents = _mapService.CreateContinents();
        
        var fullDeck = _mapService.CreateCardDeck();
        game.CardDeck = new Queue<Card>(fullDeck.OrderBy(_ => Random.Shared.Next()));

        int initialArmies = game.Players.Count switch
        {
            2 => 40,
            3 => 35,
            4 => 30,
            5 => 25,
            6 => 20,
            _ => throw new InvalidOperationException($"Invalid player count: {game.Players.Count}")
        };

        foreach (var player in game.Players)
        {
            player.InitialArmiesRemaining = initialArmies;
        }

        // Aleatorizar el orden de turno
        game.Players = game.Players.OrderBy(_ => Random.Shared.Next()).ToList();

        game.Status = GameStatus.Playing;
        game.Phase = GamePhase.Setup;
        game.CurrentPlayerIndex = 0;
        game.TurnNumber = 0;
        game.StartedAt = DateTime.UtcNow;

        game.AddEvent(new GameEvent
        {
            Type = GameEventType.GameStarted,
            Message = $"La partida \"{game.Name}\" ha comenzado con {game.Players.Count} jugadores."
        });

        // En partidas de 2 jugadores, se añade el jugador neutral
        if (game.Players.Count == 2)
        {
            var neutralPlayer = new Player
            {
                Id = "neutral",
                Name = "Neutral",
                Color = PlayerColor.Neutral,
                IsConnected = false
            };
            // Note: En Risk original de 2, el neutral recibe 40 ejércitos también? 
            // El doc dice repartir 1/3 (14) territorios.
            game.Players.Add(neutralPlayer);
            // Re-shuffle order or just put at end? Doc says "salta su turno automáticamente".
        }

        return GameResult.Ok();
    }

    public GameResult DistributeTerritoriesRandomly(Game game)
    {
        var territories = game.Territories.Keys.ToList();
        var shuffled = territories.OrderBy(_ => Random.Shared.Next()).ToList();
        var players = game.Players.ToList();

        for (int i = 0; i < shuffled.Count; i++)
        {
            var player = players[i % players.Count];
            var territory = game.Territories[shuffled[i]];

            territory.OwnerId = player.Id;
            territory.Armies = 1;
            player.InitialArmiesRemaining--;
        }

        return GameResult.Ok();
    }

    public GameResult PlaceInitialArmies(Game game, string playerId, TerritoryName territory, int count)
    {
        if (game.Phase != GamePhase.Setup)
            return GameResult.Fail("La partida no está en fase de configuración.");

        var player = game.GetPlayerById(playerId);
        if (player == null)
            return GameResult.Fail("Jugador no encontrado.");

        if (game.GetCurrentPlayer().Id != playerId)
            return GameResult.Fail("No es tu turno para colocar ejércitos.");

        var terr = game.Territories[territory];
        if (terr.OwnerId != playerId)
            return GameResult.Fail("Solo puedes colocar ejércitos en tus propios territorios.");

        if (count < 1 || count > player.InitialArmiesRemaining)
            return GameResult.Fail($"Cantidad inválida. Tienes {player.InitialArmiesRemaining} restantes.");

        terr.AddArmies(count);
        player.InitialArmiesRemaining -= count;

        if (player.InitialArmiesRemaining == 0)
        {
            AdvanceToNextSetupPlayer(game);
        }

        return GameResult.Ok();
    }

    private void AdvanceToNextSetupPlayer(Game game)
    {
        int startIndex = game.CurrentPlayerIndex;
        do
        {
            game.CurrentPlayerIndex = (game.CurrentPlayerIndex + 1) % game.Players.Count;

            // Saltar jugador neutral si existe
            if (game.Players[game.CurrentPlayerIndex].Id == "neutral")
                continue;

            if (game.Players[game.CurrentPlayerIndex].InitialArmiesRemaining > 0)
                return;
        }
        while (game.CurrentPlayerIndex != startIndex);

        StartPlaying(game);
    }

    public GameResult StartPlaying(Game game)
    {
        game.Phase = GamePhase.Reinforcement;
        game.CurrentPlayerIndex = 0;
        // Encontrar primer jugador no neutral
        while (game.Players[game.CurrentPlayerIndex].Id == "neutral")
        {
            game.CurrentPlayerIndex = (game.CurrentPlayerIndex + 1) % game.Players.Count;
        }

        game.TurnNumber = 1;

        var firstPlayer = game.GetCurrentPlayer();
        game.RemainingReinforcements = CalculateReinforcements(game, firstPlayer);

        game.AddEvent(new GameEvent
        {
            Type = GameEventType.TurnStarted,
            Message = $"Turno 1 — {firstPlayer.Name}. Recibe {game.RemainingReinforcements} ejércitos de refuerzo.",
            PlayerId = firstPlayer.Id,
            PlayerName = firstPlayer.Name
        });

        return GameResult.Ok();
    }

    // ═══════════════════════════════════════
    // REFUERZO
    // ═══════════════════════════════════════

    public int CalculateReinforcements(Game game, Player player)
    {
        int territoryCount = player.GetOwnedTerritories(game).Count();
        int fromTerritories = Math.Max(3, territoryCount / 3);
        
        int fromContinents = 0;
        foreach (var continent in game.Continents.Values)
        {
            if (continent.IsControlledBy(player.Id, game.Territories))
            {
                fromContinents += continent.BonusArmies;
            }
        }

        return fromTerritories + fromContinents;
    }

    public GameResult PlaceReinforcements(Game game, string playerId, TerritoryName territory, int count)
    {
        if (game.Phase != GamePhase.Reinforcement)
            return GameResult.Fail("No estás en la fase de refuerzo.");

        if (game.GetCurrentPlayer().Id != playerId)
            return GameResult.Fail("No es tu turno.");

        var terr = game.Territories[territory];
        if (terr.OwnerId != playerId)
            return GameResult.Fail("Solo puedes reforzar tus propios territorios.");

        if (count < 1 || count > game.RemainingReinforcements)
            return GameResult.Fail($"Cantidad inválida. Te quedan {game.RemainingReinforcements} por colocar.");

        terr.AddArmies(count);
        game.RemainingReinforcements -= count;

        game.AddEvent(new GameEvent
        {
            Type = GameEventType.ReinforcementsPlaced,
            Message = $"{game.GetCurrentPlayer().Name} colocó {count} ejército(s) en {territory}.",
            PlayerId = playerId,
            PlayerName = game.GetCurrentPlayer().Name
        });

        return GameResult.Ok();
    }

    public GameResult ConfirmReinforcements(Game game, string playerId)
    {
        if (game.Phase != GamePhase.Reinforcement)
            return GameResult.Fail("No estás en la fase de refuerzo.");

        if (game.GetCurrentPlayer().Id != playerId)
            return GameResult.Fail("No es tu turno.");

        if (game.RemainingReinforcements > 0)
            return GameResult.Fail($"Aún tienes {game.RemainingReinforcements} ejércitos por colocar.");

        game.Phase = GamePhase.Attack;
        return GameResult.Ok();
    }

    // ═══════════════════════════════════════
    // ATAQUE
    // ═══════════════════════════════════════

    public AttackGameResult Attack(Game game, string playerId, TerritoryName from, TerritoryName to, int attackDiceCount)
    {
        if (game.Phase != GamePhase.Attack)
            return AttackGameResult.Fail("No estás en fase de ataque.");

        var currentPlayer = game.GetCurrentPlayer();
        if (currentPlayer.Id != playerId)
            return AttackGameResult.Fail("No es tu turno.");

        var attacker = game.Territories[from];
        var defender = game.Territories[to];

        if (attacker.OwnerId != playerId)
            return AttackGameResult.Fail("El territorio atacante no es tuyo.");

        if (defender.OwnerId == playerId)
            return AttackGameResult.Fail("No puedes atacar tus propios territorios.");

        if (!attacker.IsAdjacentTo(to))
            return AttackGameResult.Fail($"{from} no es adyacente a {to}.");

        if (!attacker.CanAttackFrom())
            return AttackGameResult.Fail($"{from} necesita al menos 2 ejércitos para atacar.");

        int maxAttackDice = Math.Min(3, attacker.Armies - 1);
        if (attackDiceCount < 1 || attackDiceCount > maxAttackDice)
            return AttackGameResult.Fail($"Puedes usar entre 1 y {maxAttackDice} dados.");

        int defenderDiceCount = Math.Min(2, defender.Armies);
        int[] attackerDice = _diceService.Roll(attackDiceCount);
        int[] defenderDice = _diceService.Roll(defenderDiceCount);

        var result = ResolveCombat(attackerDice, defenderDice, from, to);

        attacker.Armies -= result.AttackerLosses;
        defender.Armies -= result.DefenderLosses;

        game.AddEvent(new GameEvent
        {
            Type = GameEventType.DiceRolled,
            Message = $"{currentPlayer.Name} atacó {to} desde {from}: [{string.Join(",", result.AttackerDice)}] vs [{string.Join(",", result.DefenderDice)}] → Atk -{result.AttackerLosses}, Def -{result.DefenderLosses}",
            PlayerId = playerId,
            PlayerName = currentPlayer.Name
        });

        var attackGameResult = AttackGameResult.OkAttack(result);

        if (defender.Armies <= 0)
        {
            result.TerritoryConquered = true;
            game.ConqueredThisTurn = true;

            string previousOwnerId = defender.OwnerId;
            defender.OwnerId = playerId;
            defender.Armies = 0; 

            game.AddEvent(new GameEvent
            {
                Type = GameEventType.TerritoryConquered,
                Message = $"{currentPlayer.Name} conquistó {to}.",
                PlayerId = playerId,
                PlayerName = currentPlayer.Name
            });

            var eliminatedPlayer = game.GetPlayerById(previousOwnerId);
            if (eliminatedPlayer != null && eliminatedPlayer.Id != "neutral")
            {
                bool hasTerritoriesLeft = game.Territories.Values.Any(t => t.OwnerId == previousOwnerId);
                if (!hasTerritoriesLeft)
                {
                    eliminatedPlayer.IsEliminated = true;
                    attackGameResult.PlayerEliminated = true;
                    attackGameResult.EliminatedPlayerId = previousOwnerId;

                    var surrenderedCards = eliminatedPlayer.SurrenderAllCards();
                    foreach (var card in surrenderedCards)
                    {
                        currentPlayer.AddCard(card);
                    }

                    game.AddEvent(new GameEvent
                    {
                        Type = GameEventType.PlayerEliminated,
                        Message = $"{eliminatedPlayer.Name} ha sido eliminado por {currentPlayer.Name}." + (surrenderedCards.Count > 0 ? $" {currentPlayer.Name} recibe {surrenderedCards.Count} carta(s)." : ""),
                        PlayerId = previousOwnerId,
                        PlayerName = eliminatedPlayer.Name
                    });

                    if (IsGameOver(game))
                    {
                        attackGameResult.GameOver = true;
                        game.Status = GameStatus.Finished;
                        game.FinishedAt = DateTime.UtcNow;

                        game.AddEvent(new GameEvent
                        {
                            Type = GameEventType.GameOver,
                            Message = $"¡{currentPlayer.Name} ha ganado la partida!",
                            PlayerId = playerId,
                            PlayerName = currentPlayer.Name
                        });
                    }
                }
            }
        }

        return attackGameResult;
    }

    private AttackResult ResolveCombat(int[] attackerDice, int[] defenderDice, TerritoryName from, TerritoryName to)
    {
        var sortedAttacker = attackerDice.OrderByDescending(d => d).ToArray();
        var sortedDefender = defenderDice.OrderByDescending(d => d).ToArray();

        int attackerLosses = 0;
        int defenderLosses = 0;

        int pairs = Math.Min(sortedAttacker.Length, sortedDefender.Length);
        for (int i = 0; i < pairs; i++)
        {
            if (sortedAttacker[i] > sortedDefender[i]) defenderLosses++;
            else attackerLosses++;
        }

        return new AttackResult
        {
            AttackerDice = sortedAttacker,
            DefenderDice = sortedDefender,
            AttackerLosses = attackerLosses,
            DefenderLosses = defenderLosses,
            FromTerritory = from,
            ToTerritory = to
        };
    }

    public GameResult MoveArmiesAfterConquest(Game game, string playerId, TerritoryName from, TerritoryName to, int armyCount)
    {
        var attacker = game.Territories[from];
        var conquered = game.Territories[to];

        if (attacker.OwnerId != playerId || conquered.OwnerId != playerId)
            return GameResult.Fail("Ambos territorios deben ser tuyos.");

        if (conquered.Armies > 0)
            return GameResult.Fail("Ya se movieron ejércitos a este territorio.");

        if (armyCount < 1)
            return GameResult.Fail("Debes mover al menos 1 ejército.");

        if (attacker.Armies - armyCount < 1)
            return GameResult.Fail($"Debes dejar al menos 1 ejército en {from}. Máximo: {attacker.Armies - 1}.");

        attacker.Armies -= armyCount;
        conquered.Armies = armyCount;

        return GameResult.Ok();
    }

    public GameResult EndAttackPhase(Game game, string playerId)
    {
        if (game.Phase != GamePhase.Attack)
            return GameResult.Fail("No estás en fase de ataque.");

        if (game.GetCurrentPlayer().Id != playerId)
            return GameResult.Fail("No es tu turno.");

        game.Phase = GamePhase.Fortification;
        return GameResult.Ok();
    }

    // ═══════════════════════════════════════
    // FORTIFICACIÓN
    // ═══════════════════════════════════════

    public GameResult Fortify(Game game, string playerId, TerritoryName from, TerritoryName to, int armyCount)
    {
        if (game.Phase != GamePhase.Fortification)
            return GameResult.Fail("No estás en fase de fortificación.");

        if (game.GetCurrentPlayer().Id != playerId)
            return GameResult.Fail("No es tu turno.");

        var source = game.Territories[from];
        var target = game.Territories[to];

        if (source.OwnerId != playerId || target.OwnerId != playerId)
            return GameResult.Fail("Ambos territorios deben ser tuyos.");

        if (from == to)
            return GameResult.Fail("Origen y destino deben ser distintos.");

        if (armyCount < 1 || armyCount >= source.Armies)
            return GameResult.Fail($"Puedes mover entre 1 y {source.Armies - 1} ejércitos.");

        if (!AreConnected(game, playerId, from, to))
            return GameResult.Fail("No hay un camino conectado de territorios propios.");

        source.Armies -= armyCount;
        target.AddArmies(armyCount);

        game.AddEvent(new GameEvent
        {
            Type = GameEventType.Fortified,
            Message = $"{game.GetCurrentPlayer().Name} movió {armyCount} ejército(s) de {from} a {to}.",
            PlayerId = playerId,
            PlayerName = game.GetCurrentPlayer().Name
        });

        return EndTurn(game, playerId);
    }

    public GameResult SkipFortification(Game game, string playerId)
    {
        if (game.Phase != GamePhase.Fortification)
            return GameResult.Fail("No estás en fase de fortificación.");

        if (game.GetCurrentPlayer().Id != playerId)
            return GameResult.Fail("No es tu turno.");

        return EndTurn(game, playerId);
    }

    public bool AreConnected(Game game, string playerId, TerritoryName from, TerritoryName to)
    {
        if (from == to) return true;

        var visited = new HashSet<TerritoryName> { from };
        var queue = new Queue<TerritoryName>();
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var territory = game.Territories[current];

            foreach (var neighbor in territory.AdjacentTerritories)
            {
                if (visited.Contains(neighbor)) continue;
                if (game.Territories[neighbor].OwnerId != playerId) continue;

                if (neighbor == to) return true;

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        return false;
    }

    // ═══════════════════════════════════════
    // CARTAS
    // ═══════════════════════════════════════

    public GameResult TradeCards(Game game, string playerId, string[] cardIds)
    {
        if (game.Phase != GamePhase.Reinforcement)
            return GameResult.Fail("Solo puedes canjear cartas en fase de refuerzo.");

        if (game.GetCurrentPlayer().Id != playerId)
            return GameResult.Fail("No es tu turno.");

        var player = game.GetPlayerById(playerId)!;
        if (cardIds.Length != 3)
            return GameResult.Fail("Debes seleccionar 3 cartas.");

        var selectedCards = new List<Card>();
        foreach (var id in cardIds)
        {
            var card = player.Cards.FirstOrDefault(c => c.Id == id);
            if (card == null) return GameResult.Fail("No tienes una de las cartas seleccionadas.");
            selectedCards.Add(card);
        }

        if (!_cardService.IsValidTrade(selectedCards))
            return GameResult.Fail("Combinación de cartas no válida.");

        game.TradeCount++;
        int armies = _cardService.GetArmiesForTrade(game.TradeCount);

        player.RemoveCards(selectedCards);
        game.DiscardPile.AddRange(selectedCards);
        game.RemainingReinforcements += armies;

        foreach (var card in selectedCards)
        {
            if (card.Territory.HasValue && game.Territories[card.Territory.Value].OwnerId == playerId)
            {
                game.Territories[card.Territory.Value].AddArmies(2);
                game.AddEvent(new GameEvent
                {
                    Type = GameEventType.CardsTraded,
                    Message = $"Bonificación: +2 ejércitos en {card.Territory.Value} (carta de territorio propio).",
                    PlayerId = playerId,
                    PlayerName = player.Name
                });
            }
        }

        game.AddEvent(new GameEvent
        {
            Type = GameEventType.CardsTraded,
            Message = $"{player.Name} canjeó cartas (canje #{game.TradeCount}) y recibió {armies} ejércitos.",
            PlayerId = playerId,
            PlayerName = player.Name
        });

        return GameResult.Ok();
    }

    // ═══════════════════════════════════════
    // TURNO
    // ═══════════════════════════════════════

    public GameResult EndTurn(Game game, string playerId)
    {
        var currentPlayer = game.GetCurrentPlayer();
        if (currentPlayer.Id != playerId)
            return GameResult.Fail("No es tu turno.");

        if (game.ConqueredThisTurn)
        {
            var card = game.DrawCard();
            if (card != null) currentPlayer.AddCard(card);
        }

        game.AddEvent(new GameEvent
        {
            Type = GameEventType.TurnEnded,
            Message = $"{currentPlayer.Name} terminó su turno.",
            PlayerId = playerId,
            PlayerName = currentPlayer.Name
        });

        game.AdvanceTurn();

        var nextPlayer = game.GetCurrentPlayer();
        game.RemainingReinforcements = CalculateReinforcements(game, nextPlayer);

        game.AddEvent(new GameEvent
        {
            Type = GameEventType.TurnStarted,
            Message = $"Turno {game.TurnNumber} — {nextPlayer.Name}. Recibe {game.RemainingReinforcements} ejércitos de refuerzo.",
            PlayerId = nextPlayer.Id,
            PlayerName = nextPlayer.Name
        });

        return GameResult.Ok();
    }

    public bool IsGameOver(Game game)
    {
        return game.GetActivePlayers().Count() == 1;
    }

    public Player? GetWinner(Game game)
    {
        return IsGameOver(game) ? game.GetActivePlayers().FirstOrDefault() : null;
    }
}
