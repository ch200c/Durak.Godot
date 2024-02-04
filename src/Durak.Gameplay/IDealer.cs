namespace Durak.Gameplay;

public interface IDealer
{
    IReadOnlyList<PlayerCards> PlayerCards { get; }

    char TrumpSuit { get; }

    void Deal();
}