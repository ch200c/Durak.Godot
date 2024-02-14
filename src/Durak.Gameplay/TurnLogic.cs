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

    public Player Next()
    {
        if (_attacks.Count == 0)
        {
            return FirstAttack();
        }
        else
        {
            return ConsecutiveAttack();
        }
    }

    private Player FirstAttack()
    {
        var lowestTrumpCard = _dealer.PlayerCards
            .SelectMany(p => p.Cards)
            .Where(c => c.Suit == _dealer.TrumpSuit)
            .OrderBy(c => c.Rank)
            .FirstOrDefault();

        if (lowestTrumpCard == null)
        {
            var players = _dealer.PlayerCards.Select(p => p.Player);
            var index = Random.Shared.Next(0, players.Count());
            return players.ElementAt(index);
        }

        return _dealer.PlayerCards.Single(p => p.Cards.Contains(lowestTrumpCard)).Player;
    }

    private Player ConsecutiveAttack()
    {
        var latestAttack = _attacks.Peek();

        for (var i = 0; i < _dealer.PlayerCards.Count; i++)
        {
            if (_dealer.PlayerCards[i].Player == latestAttack.PrincipalAttacker)
            {
                var index = latestAttack.State switch
                {
                    AttackState.Successful => (i + 2) % _dealer.PlayerCards.Count,
                    AttackState.BeatenOff => (i + 1) % _dealer.PlayerCards.Count,
                    _ => throw new GameplayException("Invalid latest attack state")
                };

                return _dealer.PlayerCards[index].Player;
            }
        }

        throw new GameplayException("Could not find latest attack's principal attacker");
    }
}
