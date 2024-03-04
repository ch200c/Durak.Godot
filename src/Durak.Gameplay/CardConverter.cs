namespace Durak.Gameplay;

public static class CardConverter
{
    public static string ToString(Card card)
    {
        var serializedRank = card.Rank switch
        {
            var r when r is >= 2 and <= 10 => r.ToString(),
            11 => "J",
            12 => "Q",
            13 => "K",
            14 => "A",
            _ => string.Empty
        };

        return $"{serializedRank}{card.Suit}";
    }
}
