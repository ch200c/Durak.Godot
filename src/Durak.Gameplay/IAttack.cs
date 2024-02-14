namespace Durak.Gameplay;

public interface IAttack
{
    Player PrincipalAttacker { get; }

    Player Defender { get; }

    IReadOnlyList<Player> Attackers { get; }

    IReadOnlyList<AttackCard> Cards { get; }

    AttackState State { get; }

    void AddAttacker(Player attacker);

    void Play(Player player, Card card);

    void End();
}