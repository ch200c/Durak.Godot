namespace Durak.Gameplay;

public enum AttackState
{
    InProgress,
    BeatenOff,
    Successful
}

public class Attack : IAttack
{
    private readonly Player _defender;
    private readonly List<Player> _attackers;
    private readonly List<AttackCard> _cards;
    private AttackState _state;

    public Player PrincipalAttacker { get => _attackers[0]; }

    public Player Defender { get => _defender; }

    public IReadOnlyList<Player> Attackers { get => _attackers.AsReadOnly(); }

    public IReadOnlyList<AttackCard> Cards { get => _cards.AsReadOnly(); }

    public AttackState State { get => _state; }

    public Attack(Player principalAttacker, Player defender)
    {
        _defender = defender;
        _attackers = [principalAttacker];
        _cards = [];
        _state = AttackState.InProgress;
    }

    public void AddAttacker(Player attacker)
    {
        if (_state != AttackState.InProgress)
        {
            throw new GameplayException("Cannot add attacker when attack is not in progress");
        }

        _attackers.Add(attacker);
    }

    public void Play(Player player, Card card)
    {
        if (_state != AttackState.InProgress)
        {
            throw new GameplayException("Cannot add card when attack is not in progress");
        }

        _cards.Add(new AttackCard(player, card));
    }

    public void End()
    {
        if (_state != AttackState.InProgress)
        {
            throw new GameplayException("Attack already ended");
        }

        if (_cards.Count % 2 == 0)
        {
            _state = AttackState.BeatenOff;
        }
        else
        {
            _state = AttackState.Successful;
        }
    }
}
