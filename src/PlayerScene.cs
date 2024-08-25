using Durak.Gameplay;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Durak.Godot;

public partial class PlayerScene : Node3D
{
	[Signal]
	public delegate void CardSceneClickedEventHandler(CardScene cardScene);

	[Signal]
	public delegate void CardAddedEventHandler(CardScene cardScene);

	public Player Player => _player ?? throw new GameException("Player not initialized");

	public IEnumerable<CardScene> CardScenes => GetChildren().Where(c => c.IsInGroup(Constants.CardGroup)).Cast<CardScene>();

	public Vector3 CardPosition { get; set; }

	public Vector3 CardRotation { get; set; }

	private bool _isAnimationEnabled;
	private Player? _player;

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

			var (cardScene, isNewCard, isPlayerCard) = GetAddedCardData(card);

			if (isNewCard)
			{
				cardScene = InstantiateAndInitializeCardScene(card);
			}
			else if (!isPlayerCard)
			{
				cardScene!.Reparent(this);
			}

			GD.Print($"isNew: {isNewCard}, isPlayerCard: {isPlayerCard}");

			if (Player.Id == Constants.Player1Id)
			{
				cardScene!.Clicked -= CardScene_Clicked;
				cardScene.Clicked += CardScene_Clicked;
				cardScene.GetNode<MeshInstance3D>("MeshInstance3D").Hide();
			}

			cardScene!.CardState = CardState.InHand;

			if (_isAnimationEnabled)
			{
				cardScene.TargetRotationDegrees = CardRotation;

				if (isNewCard)
				{
					cardScene.RotationDegrees = talon.RotationDegrees;
					cardScene.GlobalPosition = talon.GlobalPosition;
				}
			}
			else
			{
				cardScene.RotationDegrees = CardRotation;
			}

			var inHandCards = CardScenes.Where(c => c.CardState == CardState.InHand).ToList();
			var offsets = GetParent<MainScene>().GetCardOffsets(inHandCards.Count);

			foreach (var (existingCardScene, offset) in inHandCards.Zip(offsets))
			{
				var targetPosition = CardPosition + offset;
				existingCardScene.MoveGlobally(targetPosition);
			}

			EmitSignal(SignalName.CardAdded, cardScene);
		}
	}

	private void CardScene_Clicked(CardScene cardScene)
	{
		EmitSignal(SignalName.CardSceneClicked, cardScene);
	}

	private sealed record AddedCardData(CardScene? CardScene, bool IsNewCard, bool IsPlayerCard);

	private AddedCardData GetAddedCardData(Card card)
	{
		var cardScene = GetTree()
			.GetNodesInGroup(Constants.CardGroup)
			.Where(n => !n.IsInGroup(Constants.TalonGroup) && !n.IsInGroup(Constants.TrumpCardGroup))
			.Cast<CardScene>()
			.SingleOrDefault(c => c.Card == card);

		string? previousPlayerId = null;

		if (cardScene != null && cardScene.GetParent() is PlayerScene playerScene)
		{
			previousPlayerId = playerScene.Player.Id;
		}

		var isPlayerCard = previousPlayerId == Player.Id;
		var isNewCard = cardScene == null;

		return new AddedCardData(cardScene, isNewCard, isPlayerCard);
	}

	private CardScene InstantiateAndInitializeCardScene(Card card)
	{
		var cardScene = Constants.CardScene.Instantiate<CardScene>();
		cardScene.Initialize(card, CardState.InHand);
		cardScene.IsAnimationEnabled = _isAnimationEnabled;

		AddChild(cardScene);
		cardScene.AddToGroup(Constants.CardGroup);
		cardScene.SetPhysicsProcess(_isAnimationEnabled);
		return cardScene;
	}
}
