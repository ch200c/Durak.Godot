﻿using System.Diagnostics.CodeAnalysis;

namespace Durak.Gameplay;

public class Deck : IDeck
{
    private readonly Queue<Card> _cards;

    public char TrumpSuit { get; private set; }

    public Card TrumpCard { get; private set; }

    public int Count => _cards.Count;

    public Deck(ICardProvider cardProvider, ICardShuffler cardShuffler)
    {
        var cards = cardProvider.GetCards();
        cards = cardShuffler.Shuffle(cards).ToList();

        TrumpCard = cards.Last();
        TrumpSuit = TrumpCard.Suit;

        _cards = new Queue<Card>(cards);
    }

    public bool TryDequeue([MaybeNullWhen(false)] out Card card)
    {
        return _cards.TryDequeue(out card);
    }
}
