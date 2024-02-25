namespace Durak.Gameplay;

public record Card(int Rank, char Suit);

public record AttackCard(Player Player, Card Card);