namespace Durak.Gameplay;

public class FrenchSuited36CardProvider : ICardProvider
{
    public IEnumerable<Card> GetCards()
    {
        var ranks = Enumerable.Range(6, 8);
        var suits = new char[] { 'c', 'd', 'h', 's' };

        foreach (var rank in ranks)
        {
            foreach (var suit in suits)
            {
                yield return new(rank, suit);
            }
        }
    }
}
