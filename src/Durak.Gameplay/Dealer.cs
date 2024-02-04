namespace Durak.Gameplay;

public class Dealer : IDealer
{
    private readonly int _requiredPlayerCardCount;
    private readonly IDeck _deck;
    private readonly List<PlayerCards> _playerCards;
    private bool _isDeckExhausted;

    public IReadOnlyList<PlayerCards> PlayerCards { get => _playerCards.AsReadOnly(); }

    public char TrumpSuit { get => _deck.TrumpSuit; }

    public Dealer(int requiredPlayerCardCount, IEnumerable<Player> players, IDeck deck)
    {
        _requiredPlayerCardCount = requiredPlayerCardCount;
        _deck = deck;

        _playerCards = [];

        foreach (var player in players)
        {
            var playerCards = new PlayerCards(player, new List<Card>());
            _playerCards.Add(playerCards);
        }
    }

    public void Deal()
    {
        foreach (var playerCards in _playerCards)
        {
            Replenish(playerCards);
        }
    }

    private void Replenish(PlayerCards playerCards)
    {
        while (playerCards.Cards.Count < _requiredPlayerCardCount && !_isDeckExhausted)
        {
            if (_deck.TryDequeue(out var card))
            {
                playerCards.Cards.Add(card);
            }
            else
            {
                _isDeckExhausted = true;
            }
        }
    }
}
