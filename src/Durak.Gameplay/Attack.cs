namespace Durak.Gameplay;

public enum AttackState
{
    InProgress,
    BeatenOff,
    Successful
}

public class Attack(Player principalAttacker, Player defender, char trumpSuit) : IAttack
{
    private readonly List<Player> _attackers = [principalAttacker];
    private readonly List<AttackCard> _cards = [];
    private AttackState _state = AttackState.InProgress;

    public Player PrincipalAttacker => _attackers[0];

    public Player Defender => defender;

    public IReadOnlyList<Player> Attackers => _attackers.AsReadOnly();

    public IReadOnlyList<AttackCard> Cards => _cards.AsReadOnly();

    public AttackState State => _state;

    public void AddAttacker(Player attacker)
    {
        if (_state != AttackState.InProgress)
        {
            throw new GameplayException("Cannot add attacker when the attack is not in progress");
        }

        _attackers.Add(attacker);
    }

    public void Play(Player player, Card card)
    {
        if (_state != AttackState.InProgress)
        {
            throw new GameplayException("Cannot play when the attack is not in progress");
        }

        if (!player.Cards.Contains(card))
        {
            throw new GameplayException("Cannot play non-player's card");
        }

        var isAttacking = _cards.Count % 2 == 0;

        if (isAttacking && _cards.Count > 0 && _cards.TrueForAll(c => c.Card.Rank != card.Rank))
        {
            throw new GameplayException("Cannot play a card that does not match any other card's rank");
        }

        if (isAttacking && _cards.Count >= Math.Min(6, defender.Cards.Count))
        {
            throw new GameplayException("Cannot have more attacking cards in this attack");
        }

        if (!isAttacking && _cards.Count > 0 && card.Rank < _cards[^1].Card.Rank && card.Suit == _cards[^1].Card.Suit)
        {
            throw new GameplayException("Cannot defend with a lower ranked card");
        }

        if (!isAttacking && _cards.Count > 0 && card.Suit != _cards[^1].Card.Suit && card.Suit != trumpSuit)
        {
            throw new GameplayException("Cannot defend with a different suited card that is not in trump suit");
        }

        _cards.Add(new AttackCard(player, card));
        player.Shed(card);
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
            defender.PickUp(_cards.Select(c => c.Card));
            _state = AttackState.Successful;
        }
    }
}
