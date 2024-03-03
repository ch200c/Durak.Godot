namespace Durak.Gameplay;

public class Player(string? id)
{
    private readonly List<Card> _cards = [];

    public string? Id { get; } = id;

    public IReadOnlyList<Card> Cards => _cards.AsReadOnly();

    public Player() : this(null)
    {
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
