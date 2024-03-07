namespace Durak.Gameplay;

public class Dealer : IDealer
{
    private readonly int _requiredPlayerCardCount;
    private readonly IEnumerable<Player> _players;
    private readonly IDeck _deck;

    public Dealer(int requiredPlayerCardCount, IEnumerable<Player> players, IDeck deck)
    {
        _requiredPlayerCardCount = requiredPlayerCardCount < 1
            ? throw new ArgumentOutOfRangeException(nameof(requiredPlayerCardCount))
            : requiredPlayerCardCount;

        _players = players;
        _deck = deck;
    }

    public bool Deal(IAttack? previousAttack)
    {
        return previousAttack == null
            ? FirstDeal()
            : ConsecutiveDeal(previousAttack);
    }

    private bool FirstDeal()
    {
        var isReplenished = true;

        for (var i = 0; i < _requiredPlayerCardCount; i++)
        {
            foreach (var player in _players)
            {
                if (_deck.TryDequeue(out var card))
                {
                    player.PickUp([card]);
                }
                else
                {
                    isReplenished = false;
                    break;
                }
            }
        }

        return isReplenished;
    }

    private bool ConsecutiveDeal(IAttack previousAttack)
    {
        var isReplenished = true;

        foreach (var player in previousAttack.Attackers.Union([previousAttack.Defender]))
        {
            while (player.Cards.Count < _requiredPlayerCardCount)
            {
                if (_deck.TryDequeue(out var card))
                {
                    player.PickUp([card]);
                }
                else
                {
                    isReplenished = false;
                    break;
                }
            }
        }

        return isReplenished;
    }
}
