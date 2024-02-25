namespace Durak.Gameplay;

public class Dealer : IDealer
{
    private readonly int _requiredPlayerCardCount;
    private readonly IDeck _deck;
    private readonly List<Player> _players;

    public IReadOnlyList<Player> Players => _players.AsReadOnly();

    public char TrumpSuit => _deck.TrumpSuit;

    public Dealer(int requiredPlayerCardCount, IEnumerable<Player> players, IDeck deck)
    {
        _requiredPlayerCardCount = requiredPlayerCardCount;
        _deck = deck;
        _players = players.ToList();
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
        var isExhausted = true;
       
        while (player.Cards.Count < _requiredPlayerCardCount)
        {
            if (_deck.TryDequeue(out var card))
            {
                player.PickUp([card]);
            }
            else
            {
                isExhausted = false;
                break;
            }
        }

        return isExhausted;
    }
}
