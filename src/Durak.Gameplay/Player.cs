namespace Durak.Gameplay;

public class Player
{
    private readonly List<Card> _cards;

    public string? Id { get; }

    public IReadOnlyList<Card> Cards => _cards.AsReadOnly();

    public Player() : this(null)
    {
    }

    public Player(string? id)
    {
        Id = id;
        _cards = [];
    }

    public void PickUp(IEnumerable<Card> cards)
    {
        _cards.AddRange(cards);
    }

    public void Shed(Card card)
    {
        _cards.Remove(card);
    }
}
