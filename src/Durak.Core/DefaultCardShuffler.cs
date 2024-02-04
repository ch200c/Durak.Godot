namespace Durak.Gameplay;

public class DefaultCardShuffler : ICardShuffler
{
    public IEnumerable<Card> Shuffle(IEnumerable<Card> cards)
    {
        return cards.OrderBy(_ => Random.Shared.Next());
    }
}