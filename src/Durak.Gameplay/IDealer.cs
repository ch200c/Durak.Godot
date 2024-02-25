namespace Durak.Gameplay;

public interface IDealer
{
    IReadOnlyList<Player> Players { get; }

    char TrumpSuit { get; }

    bool Deal();
}