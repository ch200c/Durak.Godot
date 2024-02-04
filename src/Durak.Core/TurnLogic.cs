namespace Durak.Gameplay;

public class TurnLogic
{
    private readonly IDealer _dealer;
    private readonly Stack<Bout> _bouts;

    public TurnLogic(IDealer dealer)
    {
        _dealer = dealer;
        _bouts = new Stack<Bout>();
    }

    public void AddBout(Bout bout)
    {
        _bouts.Push(bout);
    }

    public Player Next()
    {
        if (_bouts.Count == 0)
        {
            return FirstBout();
        }
        else
        {
            return ConsecutiveBout();
        }
    }

    private Player FirstBout()
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

    private Player ConsecutiveBout()
    {
        var latestBout = _bouts.Peek();

        for (var i = 0; i < _dealer.PlayerCards.Count; i++)
        {
            if (_dealer.PlayerCards[i].Player == latestBout.PrincipalAttacker)
            {
                var index = latestBout.AttackState switch
                {
                    AttackState.Successful => (i + 2) % _dealer.PlayerCards.Count,
                    AttackState.BeatenOff => (i + 1) % _dealer.PlayerCards.Count,
                    _ => throw new InvalidOperationException("TODO")
                };

                return _dealer.PlayerCards[index].Player;
            }
        }

        throw new NotImplementedException("TODO");
    }
}
