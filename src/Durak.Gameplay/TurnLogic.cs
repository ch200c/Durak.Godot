namespace Durak.Gameplay;

public class TurnLogic
{
    private readonly IDealer _dealer;
    private readonly Stack<IAttack> _attacks;

    public TurnLogic(IDealer dealer)
    {
        _dealer = dealer;
        _attacks = new Stack<IAttack>();
    }

    public void AddAttack(IAttack attack)
    {
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
        var lowestTrumpCard = _dealer.Players
            .SelectMany(p => p.Cards)
            .Where(c => c.Suit == _dealer.TrumpSuit)
            .OrderBy(c => c.Rank)
            .FirstOrDefault();

        int attackerIndex;

        if (lowestTrumpCard == null)
        {
            attackerIndex = Random.Shared.Next(0, _dealer.Players.Count);
        }
        else
        {
            attackerIndex = _dealer.Players
                .Select((p, i) => new { Cards = p.Cards, Index = i })
                .Single(p => p.Cards.Contains(lowestTrumpCard))
                .Index;
        }

        return CreateAttack(attackerIndex);
    }

    private Attack? ConsecutiveAttack()
    {
        var latestAttack = _attacks.Peek();

        for (var i = 0; i < _dealer.Players.Count; i++)
        {
            if (_dealer.Players[i] == latestAttack.PrincipalAttacker)
            {
                var attackerIndex = latestAttack.State switch
                {
                    AttackState.Successful => NextIndex(i + 1),
                    AttackState.BeatenOff => NextIndex(i),
                    _ => throw new GameplayException("Invalid latest attack state")
                };

                while (_dealer.Players[attackerIndex].Cards.Count == 0)
                {
                    attackerIndex = NextIndex(attackerIndex);
                }

                return CreateAttack(attackerIndex);
            }
        }

        throw new GameplayException("Could not find latest attack's principal attacker");
    }

    private int NextIndex(int index)
    {
        return (index + 1) % _dealer.Players.Count;
    }

    private Attack? CreateAttack(int attackerIndex)
    {
        var defenderIndex = NextIndex(attackerIndex);
        
        if (_dealer.Players[defenderIndex].Cards.Count == 0)
        {
            return null;
        }

        return new Attack(_dealer.Players[attackerIndex], _dealer.Players[defenderIndex], _dealer.TrumpSuit);
    }
}
