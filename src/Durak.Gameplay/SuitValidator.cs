namespace Durak.Gameplay;

public static class SuitValidator
{
    public static bool Validate(char suit)
    {
        return suit == Gameplay.Suit.Clubs
            || suit == Gameplay.Suit.Diamonds
            || suit == Gameplay.Suit.Hearts
            || suit == Gameplay.Suit.Spades;
    }
}