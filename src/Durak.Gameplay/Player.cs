namespace Durak.Gameplay;

public class Player
{
    private readonly List<Card> _cards;

    public event EventHandler<CardsAddedEventArgs>? CardsAdded;
    public event EventHandler<CardRemovedEventArgs>? CardRemoved;

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

        CardRemoved?.Invoke(this, new CardRemovedEventArgs(card));
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

public class CardRemovedEventArgs : EventArgs
{
    public Card Card { get; }

    public CardRemovedEventArgs(Card card)
    {
        Card = card;
    }
}

public class AttackCardAddedEventArgs : EventArgs
{
    public AttackCard Card { get; }

    public AttackCardAddedEventArgs(AttackCard card)
    {
        Card = card;
    }
}