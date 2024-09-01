namespace Durak.Gameplay.UnitTests;

public class DeckTests
{
    [Fact]
    public void TryDequeue_AllExceptLastCard_ShouldNotContainTrumpCard()
    {
        // Arrange
        var deck = new Deck(new FrenchSuited36CardProvider(), new DefaultCardShuffler());

        var cards = new List<Card>();

        // Act
        while (deck.TryDequeue(out var card))
        {
            cards.Add(card);
        }

        // Assert
        cards.SkipLast(1).Should().NotContain(deck.TrumpCard);
    }

    [Fact]
    public void TryDequeue_LastCard_ShouldBeTrumpCard()
    {
        // Arrange
        var deck = new Deck(new FrenchSuited36CardProvider(), new DefaultCardShuffler());

        Card? lastCard = null;

        // Act
        while (deck.TryDequeue(out var card))
        {
            lastCard = card;
        }

        // Assert
        lastCard.Should().Be(deck.TrumpCard);
    }
}
