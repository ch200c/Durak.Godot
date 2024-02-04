namespace Durak.Gameplay;

public interface IBout
{
    Player PrincipalAttacker { get; }

    Player Defender { get; }

    IReadOnlyCollection<Player> Attackers { get; }

    AttackState AttackState { get; }

    IReadOnlyCollection<BoutCard> Cards { get; }

    void AddAttacker(Player attacker);

    void Attack(Player attacker, Card card);

    void Defend(Card card);

    void End();
}