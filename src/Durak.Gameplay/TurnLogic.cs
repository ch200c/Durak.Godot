﻿namespace Durak.Gameplay;

public class TurnLogic(IReadOnlyList<Player> players, char trumpSuit) : ITurnLogic
{
    private const int _maxAttackCount = 100;
    private readonly Stack<IAttack> _attacks = new();

    public IReadOnlyList<IAttack> Attacks => _attacks.ToList().AsReadOnly();

    public void AddAttack(IAttack attack)
    {
        if (_attacks.Count > _maxAttackCount)
        {
            throw new GameplayException($"Reached more than {_maxAttackCount} attacks in a game");
        }

        _attacks.Push(attack);
    }

    public IAttack? NextAttack()
    {
        var attack = _attacks.Count == 0
            ? FirstAttack()
            : ConsecutiveAttack();

        if (attack != null)
        {
            AddAttack(attack);
        }

        return attack;
    }

    private Attack? FirstAttack()
    {
        var lowestTrumpCard = players
            .SelectMany(p => p.Cards)
            .Where(c => c.Suit == trumpSuit)
            .OrderBy(c => c.Rank)
            .FirstOrDefault();

        int attackerIndex;

        if (lowestTrumpCard == null)
        {
            attackerIndex = Random.Shared.Next(0, players.Count);
        }
        else
        {
            attackerIndex = players
                .Select((p, i) => new { Cards = p.Cards, Index = i })
                .Single(p => p.Cards.Contains(lowestTrumpCard))
                .Index;
        }

        return CreateAttack(attackerIndex);
    }

    private Attack? ConsecutiveAttack()
    {
        var isOneOrZeroPlayersLeft = players.Count(p => p.Cards.Count > 0) <= 1;
        if (isOneOrZeroPlayersLeft)
        {
            return null;
        }

        var latestAttack = _attacks.Peek();

        for (var i = 0; i < players.Count; i++)
        {
            if (players[i] == latestAttack.PrincipalAttacker)
            {
                var attackerIndex = latestAttack.State switch
                {
                    AttackState.Successful => NextIndex(i + 1),
                    AttackState.BeatenOff => NextIndex(i),
                    _ => throw new GameplayException("Invalid latest attack state")
                };

                return CreateAttack(attackerIndex);
            }
        }

        throw new GameplayException("Could not find latest attack's principal attacker");
    }

    private int NextIndex(int index)
    {
        index = (index + 1) % players.Count;

        var originalIndex = index;

        while (players[index].Cards.Count == 0)
        {
            index = (index + 1) % players.Count;
            if (originalIndex == index)
            {
                throw new NotImplementedException("TODO2");
            }
        }

        return index;
    }

    private Attack CreateAttack(int attackerIndex)
    {
        var defenderIndex = NextIndex(attackerIndex);
        return new Attack(players[attackerIndex], players[defenderIndex], trumpSuit);
    }
}
