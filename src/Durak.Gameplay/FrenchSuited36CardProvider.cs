namespace Durak.Gameplay;

public class FrenchSuited36CardProvider : ICardProvider
{
    public IEnumerable<Card> GetCards()
    {
        var ranks = Enumerable.Range(6, 9);
        var suits = new char[] { Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades };

        foreach (var rank in ranks)
        {
            foreach (var suit in suits)
            {
                yield return new(rank, suit);
            }
        }
    }
}
