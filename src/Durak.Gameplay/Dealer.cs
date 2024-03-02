namespace Durak.Gameplay;

public class Dealer : IDealer
{
    private readonly int _requiredPlayerCardCount;
    private readonly IDeck _deck;
    private readonly IEnumerable<Player> _players;

    public Dealer(int requiredPlayerCardCount, IEnumerable<Player> players, IDeck deck)
    {
        _requiredPlayerCardCount = requiredPlayerCardCount;
        _players = players;
        _deck = deck;
    }

    public bool Deal()
    {
        foreach (var player in _players)
        {
            if (!Replenish(player))
            {
                return false;
            }
        }

        return true;
    }

    private bool Replenish(Player player)
    {
        var isReplenished = true;
       
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

        return isReplenished;
    }
}
