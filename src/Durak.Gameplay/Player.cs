namespace Durak.Gameplay;

public class Player
{
    private readonly List<Card> _cards;

    public event EventHandler<CardsAddedEventArgs>? CardsAdded;

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

        CardsAdded?.Invoke(this, new CardsAddedEventArgs(cards));
    }

    public void Shed(Card card)
    {
        _cards.Remove(card);
    }
}

public class CardsAddedEventArgs : EventArgs
{
    public IEnumerable<Card> Cards { get; }

    public CardsAddedEventArgs(IEnumerable<Card> cards)
    {
        Cards = cards;
    }
}