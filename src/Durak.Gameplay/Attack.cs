namespace Durak.Gameplay;

public enum AttackState
{
    InProgress,
    BeatenOff,
    Successful
}

public class Attack : IAttack
{
    private readonly char _trumpSuit;
    private readonly List<Player> _attackers;
    private readonly List<AttackCard> _cards;
    private readonly Player _defender;
    private AttackState _state;

    public event  EventHandler<AttackCardAddedEventArgs>? AttackCardAdded;
    public event EventHandler? AttackEnded;

    public Player PrincipalAttacker => _attackers[0];

    public Player Defender => _defender;

    public IReadOnlyList<Player> Attackers => _attackers.AsReadOnly();

    public IReadOnlyList<AttackCard> Cards => _cards.AsReadOnly();

    public AttackState State => _state;

    public Attack(Player principalAttacker, Player defender, char trumpSuit)
    {
        _defender = defender;
        _trumpSuit = SuitValidator.Validate(trumpSuit)
            ? trumpSuit
            : throw new ArgumentOutOfRangeException(nameof(trumpSuit));
        _attackers = [principalAttacker];
        _cards = [];
        _state = AttackState.InProgress;
    }

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
        var result = CanPlay(player, card);
        
        if (!result)
        {
            throw new GameplayException(result.Error);
        }

        var attackCard = new AttackCard(player, card);
        _cards.Add(attackCard);
        AttackCardAdded?.Invoke(this, new AttackCardAddedEventArgs(attackCard));

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
            _defender.PickUp(_cards.Select(c => c.Card));
            _state = AttackState.Successful;
        }

        AttackEnded?.Invoke(this, new EventArgs());
    }

    public CanPlayResult CanPlay(Player player, Card card)
    {
        if (_state != AttackState.InProgress)
        {
            return new CanPlayResult(false, "Cannot play when the attack is not in progress");
        }

        if (!player.Cards.Contains(card))
        {
            return new CanPlayResult(false, "Cannot play non-player's card");
        }

        if (_cards.Select(c => c.Card).Contains(card))
        {
            return new CanPlayResult(false, "Cannot play already played card");
        }

        var isAttacking = _cards.Count % 2 == 0;

        if (isAttacking && _cards.Count > 0 && _cards.TrueForAll(c => c.Card.Rank != card.Rank))
        {
            return new CanPlayResult(false, "Cannot play a card that does not match any other card's rank");
        }

        if (isAttacking && _cards.Count >= Math.Min(12, _defender.Cards.Count * 2))
        {
            return new CanPlayResult(false, "Cannot have more attacking cards in this attack");
        }

        if (!isAttacking && _cards.Count > 0 && card.Rank < _cards[^1].Card.Rank && card.Suit == _cards[^1].Card.Suit)
        {
            return new CanPlayResult(false, "Cannot defend with a lower ranked card");
        }

        if (!isAttacking && _cards.Count > 0 && card.Suit != _cards[^1].Card.Suit && card.Suit != _trumpSuit)
        {
            return new CanPlayResult(false, "Cannot defend with a different suited card that is not in trump suit");
        }

        return new CanPlayResult(true, null);
    }

    public Player NextToPlay()
    {
        if (_cards.Count == 0)
        {
            return _attackers[0];
        }

        var isLastCardPlayedByAttacker = _attackers.Contains(_cards[^1].Player);
        if (isLastCardPlayedByAttacker)
        {
            return _defender;
        }

        return _cards[^2].Player;
    }
}
