namespace Durak.Gameplay;

public interface ICardShuffler
{
    IEnumerable<Card> Shuffle(IEnumerable<Card> cards);
}
