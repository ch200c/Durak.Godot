namespace Durak.Gameplay;

public record Card(int Rank, char Suit);

public record AttackCard(Player Player, Card Card);

public record PlayerCards(Player Player, ICollection<Card> Cards);