namespace Durak.Gameplay;

public record Card(int Rank, char Suit)
{
    public override string ToString()
    {
        var serializedRank = Rank switch
        {
            var r when r is >= 2 and <= 10 => r.ToString(),
            11 => "J",
            12 => "Q",
            13 => "K",
            14 => "A",
            _ => throw new ArgumentOutOfRangeException(nameof(Rank))
        };

        return $"{serializedRank}{Suit}";
    }
}

public record AttackCard(Player Player, Card Card);