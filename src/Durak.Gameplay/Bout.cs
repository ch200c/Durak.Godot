namespace Durak.Gameplay;

public enum AttackState
{
    InProgress,
    BeatenOff,
    Successful
}

public class Bout : IBout
{
    private readonly Player _defender;
    private readonly List<Player> _attackers;
    private AttackState _attackState;
    private readonly List<BoutCard> _cards;

    public Player PrincipalAttacker { get => _attackers[0]; }

    public Player Defender { get => _defender; }

    public IReadOnlyCollection<Player> Attackers { get => _attackers.AsReadOnly(); }

    public AttackState AttackState { get => _attackState; }

    public IReadOnlyCollection<BoutCard> Cards { get => _cards.AsReadOnly(); }

    public Bout(Player principalAttacker, Player defender)
    {
        _defender = defender;
        _attackers = [principalAttacker];
        _attackState = AttackState.InProgress;
        _cards = [];
    }

    public void AddAttacker(Player attacker)
    {
        _attackers.Add(attacker);
    }

    public void Attack(Player attacker, Card card)
    {
        _cards.Add(new BoutCard(attacker, card));
    }

    public void Defend(Card card)
    {
        _cards.Add(new BoutCard(_defender, card));
    }

    public void End()
    {
        if (_cards.Count % 2 == 0)
        {
            _attackState = AttackState.BeatenOff;
        }
        else
        {
            _attackState = AttackState.Successful;
        }
    }
}
