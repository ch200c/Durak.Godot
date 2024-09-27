using Durak.Gameplay;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Durak.Godot;

public partial class PlayerNode : Node3D
{
	[Signal]
	public delegate void CardClickedEventHandler(CardNode cardNode);

	[Signal]
	public delegate void CardAddedEventHandler(CardNode cardNode);

    [Signal]
    public delegate void CardsAddedEventHandler(string playerId);

    public Player Player => _player ?? throw new GameException("Player not initialized");

	public IEnumerable<CardNode> CardNodes => GetChildren().Where(c => c.IsInGroup(Constants.CardGroup)).Cast<CardNode>();

	public Vector3 CardsPosition { get; set; }

	public Vector3 CardsRotationDegrees { get; set; }

	private bool _isAnimationEnabled;
	private Player? _player;
	private readonly SortedList<int, Card> _order = [];

	public void Initialize(string id, bool isAnimationEnabled)
	{
		_player = new Player(id);
		_player.CardsAdded += Player_CardsAdded;
		_isAnimationEnabled = isAnimationEnabled;
	}

	private void Player_CardsAdded(object? sender, CardsAddedEventArgs e)
	{
		var talon = GetNode<Node3D>("/root/Main/Table/GameSurface/Deck/Talon");

		foreach (var card in e.Cards)
		{
			GD.Print($"{card} added for {Player.Id}");

			var (cardNode, isNewCard, isPlayerCard) = GetAddedCardData(card);

			if (isNewCard)
			{
				cardNode = InstantiateAndInitializeCardScene(card);
			}
			else if (!isPlayerCard)
			{
				cardNode!.Reparent(this);
			}

			UpdateOrder(cardNode!);

			GD.Print($"isNew: {isNewCard}, isPlayerCard: {isPlayerCard}");
			cardNode!.CardState = CardState.InHand;

			if (Player.Id == Constants.Player1Id)
			{
				cardNode!.Clicked -= Card_Clicked;
				cardNode.Clicked += Card_Clicked;
				cardNode.GetNode<MeshInstance3D>("MeshInstance3D").Hide();
			}

			cardNode.UpdateSortingOffsets();

			if (_isAnimationEnabled)
			{
				cardNode.TargetRotationDegrees = CardsRotationDegrees;

				if (isNewCard)
				{
					cardNode.RotationDegrees = talon.RotationDegrees;
					cardNode.GlobalPosition = talon.GlobalPosition;
				}
			}
			else
			{
				cardNode.RotationDegrees = CardsRotationDegrees;
			}

			var inHandCards = CardNodes.Where(c => c.CardState == CardState.InHand).ToList();
			var offsets = GetParent<MainNode>().GetCardOffsets(inHandCards.Count);

			foreach (var (existingCardNode, offset) in inHandCards.Zip(offsets))
			{
				var targetPosition = CardsPosition + offset;
				existingCardNode.MoveGlobally(targetPosition);
			}

			EmitSignal(SignalName.CardAdded, cardNode);
		}

		EmitSignal(SignalName.CardsAdded, Player.Id);
	}

	private void Card_Clicked(CardNode cardNode)
	{
		EmitSignal(SignalName.CardClicked, cardNode);
	}

	private sealed record AddedCardData(CardNode? CardNode, bool IsNewCard, bool IsPlayerCard);

	private AddedCardData GetAddedCardData(Card card)
	{
		var cardNode = GetTree()
			.GetNodesInGroup(Constants.CardGroup)
			.Where(n => !n.IsInGroup(Constants.TalonGroup) && !n.IsInGroup(Constants.TrumpCardGroup))
			.Cast<CardNode>()
			.SingleOrDefault(c => c.Card == card);

		string? previousPlayerId = null;

		if (cardNode != null && cardNode.GetParent() is PlayerNode playerNode)
		{
			previousPlayerId = playerNode.Player.Id;
		}

		var isPlayerCard = previousPlayerId == Player.Id;
		var isNewCard = cardNode == null;

		return new AddedCardData(cardNode, isNewCard, isPlayerCard);
	}

	private CardNode InstantiateAndInitializeCardScene(Card card)
	{
		var cardNode = Constants.CardScene.Instantiate<CardNode>();
		cardNode.Initialize(card, CardState.InHand);
		cardNode.IsAnimationEnabled = _isAnimationEnabled;

		AddChild(cardNode);
		cardNode.AddToGroup(Constants.CardGroup);
		cardNode.SetPhysicsProcess(_isAnimationEnabled);

		return cardNode;
	}

	private void UpdateOrder(CardNode cardNode)
	{
        var orderKey = _order.Count == 0 ? 0 : _order.Last().Key + 1;
        _order.Add(orderKey, cardNode.Card);
        cardNode.OrderInHand = orderKey;
    }

    public void RemoveCardFromOrder(Card card)
    {
        _order.Remove(_order.IndexOfValue(card));
    }
}
