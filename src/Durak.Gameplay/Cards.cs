namespace Durak.Gameplay;

public record Card(int Rank, char Suit)
{
    private int _rank = ValidateRank(Rank);
    public int Rank
    {
        get => _rank;
        init { _rank = ValidateRank(value); }
    }

    private char _suit = ValidateSuit(Suit);
    public char Suit
    {
        get => _suit;
        init { _suit = ValidateSuit(value); }
    }

    private static int ValidateRank(int rank)
    {
        if (rank < 2 || rank > 14)
        {
            throw new ArgumentOutOfRangeException(nameof(rank));
        }

        return rank;
    }

    private static char ValidateSuit(char suit)
    {
        if (!SuitValidator.Validate(suit))
        {
            throw new ArgumentOutOfRangeException(nameof(suit));
        }

        return suit;
    }

    public override string ToString()
    {
        return CardConverter.ToString(this);
    }
}

public record AttackCard(Player Player, Card Card);

public static class Suit
{
    public static readonly char Clubs = 'c';
    public static readonly char Diamonds = 'd';
    public static readonly char Hearts = 'h';
    public static readonly char Spades = 's';
}
