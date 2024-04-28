namespace Durak.Gameplay;

public interface IAttack
{
    event EventHandler<AttackCardAddedEventArgs> AttackCardAdded;
    event EventHandler AttackEnded;

    Player PrincipalAttacker { get; }

    Player Defender { get; }

    IReadOnlyList<Player> Attackers { get; }

    IReadOnlyList<AttackCard> Cards { get; }

    AttackState State { get; }

    void AddAttacker(Player attacker);

    void Play(Player player, Card card);

    CanPlayResult CanPlay(Player player, Card card);

    Player NextToPlay();

    void End();
}

public record CanPlayResult(bool CanPlay, string? Error)
{
    public static implicit operator bool(CanPlayResult result) => result.CanPlay;
}