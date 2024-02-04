using System.Diagnostics.CodeAnalysis;

namespace Durak.Gameplay;

public class Deck : IDeck
{
    private readonly Queue<Card> _cards;

    public char TrumpSuit { get; private set; }

    public Deck(ICardProvider cardProvider, ICardShuffler cardShuffler)
    {
        var cards = cardProvider.GetCards();
        cards = cardShuffler.Shuffle(cards);

        TrumpSuit = cards.Last().Suit;

        _cards = new Queue<Card>(cards);
    }

    public bool TryDequeue([MaybeNullWhen(false)] out Card card)
    {
        return _cards.TryDequeue(out card);
    }
}
