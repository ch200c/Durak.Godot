namespace Durak.Gameplay;

public class TurnLogic
{
    private readonly IReadOnlyList<Player> _players;
    private readonly char _trumpSuit;
    private readonly Stack<IAttack> _attacks;

    public TurnLogic(IReadOnlyList<Player> players, char trumpSuit)
    {
        _players = players;
        _trumpSuit = trumpSuit;
        _attacks = new Stack<IAttack>();
    }

    public void AddAttack(IAttack attack)
    {
        if (_attacks.Count > 100)
        {
            throw new NotImplementedException("TODO1");
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
        var lowestTrumpCard = _players
            .SelectMany(p => p.Cards)
            .Where(c => c.Suit == _trumpSuit)
            .OrderBy(c => c.Rank)
            .FirstOrDefault();

        int attackerIndex;

        if (lowestTrumpCard == null)
        {
            attackerIndex = Random.Shared.Next(0, _players.Count);
        }
        else
        {
            attackerIndex = _players
                .Select((p, i) => new { Cards = p.Cards, Index = i })
                .Single(p => p.Cards.Contains(lowestTrumpCard))
                .Index;
        }

        return CreateAttack(attackerIndex);
    }

    private Attack? ConsecutiveAttack()
    {
        var isOneOrZeroPlayersLeft = _players.Count(p => p.Cards.Count > 0) <= 1;
        if (isOneOrZeroPlayersLeft)
        {
            return null;
        }

        var latestAttack = _attacks.Peek();

        for (var i = 0; i < _players.Count; i++)
        {
            if (_players[i] == latestAttack.PrincipalAttacker)
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
        index = (index + 1) % _players.Count;

        var originalIndex = index;

        while (_players[index].Cards.Count == 0)
        {
            index = (index + 1) % _players.Count;
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
        return new Attack(_players[attackerIndex], _players[defenderIndex], _trumpSuit);
    }
}
