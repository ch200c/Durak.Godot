namespace Durak.Gameplay;

public enum AttackState
{
    InProgress,
    BeatenOff,
    Successful
}

public class Bout
{
    private readonly Player _defender;
    private readonly List<Player> _attackers;
    private AttackState _attackState;

    public AttackState AttackState { get => _attackState; }
    public Player Defender { get => _defender; }
    public IReadOnlyCollection<Player> Attackers { get => _attackers.AsReadOnly(); }
    public Player PrincipalAttacker { get => _attackers[0]; }

    public Bout(Player principalAttacker, Player defender)
    {
        _defender = defender;
        _attackers = [principalAttacker];
        _attackState = AttackState.InProgress;
    }

    public void AddAttacker(Player attacker)
    {
        _attackers.Add(attacker);
    }

    public void SetAttackState(AttackState state)
    {
        _attackState = state;
    }
}
