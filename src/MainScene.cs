using Durak.Gameplay;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Durak.Godot;

public static class Constants
{
	public static readonly string Player1Id = "P1";
	public static readonly string CardGroup = "cards";
	public static readonly string TrumpCardGroup = "trumpCard";
	public static readonly string TalonGroup = "talon";
	public static readonly PackedScene CardScene = GD.Load<PackedScene>("res://scenes/card.tscn");
}

public partial class MainScene : Node3D
{
	private const string PlayersGroup = "players";
	private static readonly PackedScene _playerScene = GD.Load<PackedScene>("res://scenes/player.tscn");

	private int _cardPhysicsCooldownIteration = 0;

	private TurnLogic? _turnLogic;
	private IAttack? _currentAttack;
	private Dealer? _dealer;
	private Deck? _deck;
	
	[Export]
	private float _cardWidth = 0.06f;

	[Export]
	private float _cardPaddingX = 0.005f;

	[Export]
	private float _minMainPlayerHandX = -0.3f;

	[Export]
	private float _maxMainPlayerHandX = 0.3f;

	[Export]
	private float _mainPlayerCardDistanceMultiplier = 0.17f;

	[Export]
	private int _cardPhysicsCooldownMs = 50;

	[Export]
	private int _maxCardPhysicsCooldownMs = 1_250;

	[Export]
	private bool _isAnimationEnabled = false;

	private IAttack Attack { get => _currentAttack ?? throw new GameException("Current attack not initialized"); }

	private Node3D DiscardPile { get => GetNode<Node3D>("/root/Main/Table/GameSurface/DiscardPile"); }

	private IEnumerable<PlayerScene> PlayerScenes => GetChildren().Where(c => c.IsInGroup(PlayersGroup)).Cast<PlayerScene>();

	private IEnumerable<PlayerScene> OrderedPlayerScenes => PlayerScenes.OrderBy(p => p.Player.Id);

	private PlayerScene MainPlayerScene => GetPlayerScene(Constants.Player1Id);

	private PlayerScene GetPlayerScene(string id) => PlayerScenes.Single(p => p.Player.Id == id);

	public override void _EnterTree()
	{
		GetNode<MarginContainer>("%HUD").Hide();
	}

	private void Camera_Moved()
	{
		RearrangeMainPlayerCards();
	}

	private void RearrangeMainPlayerCards()
	{
		var camera = GetNode<Camera>("%Camera");
		var mainPlayerGlobalPosition = GetMainPlayerGlobalPosition(camera);

		MainPlayerScene.CardPosition = mainPlayerGlobalPosition;
		MainPlayerScene.CardRotation = new Vector3(camera.RotationDegrees.X, camera.RotationDegrees.Y, camera.RotationDegrees.Z);

		RearrangePlayerCards(Constants.Player1Id);
	}

	private void _on_end_attack_button_pressed()
	{
		if (Attack.NextToPlay().Id != Constants.Player1Id)
		{
			GD.Print("Ignoring end attack");
			return;
		}

		GD.Print("Ending attack by P1");
		Attack.End();
	}

	private void _on_play_button_pressed()
	{
		var mainSceneChildCount = GetChildCount();
		var totalChildCount = GetTree().GetNodeCount();

		GD.Print($"Main scene child count: {mainSceneChildCount}, total: {totalChildCount}");

		GetNode<MarginContainer>("%Menu").Hide();
		GetNode<MarginContainer>("%HUD").Show();

		var opponentCount = GetNode<SpinBox>("%OpponentsSpinBox");
		CreatePlayers((int)opponentCount.Value + 1);

		var camera = GetNode<Camera>("%Camera");
		camera.Moved -= Camera_Moved;
		camera.Moved += Camera_Moved;

		var players = OrderedPlayerScenes.Select(p => p.Player).ToList();
		
		AddMainPlayerData(camera);
		AddOpponentPlayerData();
		
		_deck = new Deck(new FrenchSuited36CardProvider(), new DefaultCardShuffler());
		_deck.CardRemoved += _deck_CardRemoved;
		_turnLogic = new TurnLogic(players, _deck.TrumpSuit);

		_dealer = new Dealer(6, players, _deck);
		_dealer.Deal(null);

		CreateTrumpCard(_deck.TrumpCard);
		CreateTalon();

		StartAttack();
	}

	private void _deck_CardRemoved(object? sender, CardRemovedEventArgs e)
	{
		if (_deck!.Count == 1)
		{
			GD.Print("Hiding talon");
			((CardScene)GetTree().GetFirstNodeInGroup(Constants.TalonGroup)).Hide();
		}
		else if (_deck!.Count == 0)
		{
			GD.Print("Hiding trump card");
			((CardScene)GetTree().GetFirstNodeInGroup(Constants.TrumpCardGroup)).Hide();
		}
	}

	private void CurrentAttack_AttackEnded(object? sender, EventArgs e)
	{
		Attack.AttackCardAdded -= CurrentAttack_AttackCardAdded;
		Attack.AttackEnded -= CurrentAttack_AttackEnded;

		var attackerIds = Attack.Attackers.Select(a => a.Id);
		var attackCards = Attack.Cards.Select(c => c.Card);
		GD.Print($"Attack state: {Attack.State} | {string.Join(',', attackerIds)} vs {Attack.Defender.Id} | {string.Join(',', attackCards)}");

		switch (Attack.State)
		{
			case AttackState.BeatenOff:
				{
					HandleBeatenOffAttack();
					break;
				}
			case AttackState.Successful:
				{
					break;
				}
			default:
				{
					throw new GameException($"Invalid current attack state {Attack.State}");
				}
		}

		_dealer!.Deal(Attack);

		StartAttack();
	}

	private void PrintCardScenes(string tag)
	{
		GD.Print(tag);

		foreach (var playerScene in OrderedPlayerScenes)
		{
			var nonDiscardedCards = playerScene.CardScenes.Where(c => c.CardState != CardState.Discarded).Select(c => $"{c.Card} {c.CardState}");
			var discardedCards = playerScene.CardScenes.Where(c => c.CardState == CardState.Discarded).Select(c => $"{c.Card}");
			GD.Print($"{playerScene.Player.Id} {string.Join(", ", nonDiscardedCards)}, Discarded: {string.Join(", ", discardedCards)}");
		}
	}

	private void StartAttack()
	{
		if (!_turnLogic!.TryGetNextAttack(out _currentAttack))
		{
			Reset();
			return;
		}

		Attack.AttackCardAdded += CurrentAttack_AttackCardAdded;
		Attack.AttackEnded += CurrentAttack_AttackEnded;

		GD.Print($"Starting attack as {Attack.PrincipalAttacker.Id}");

		if (Attack.PrincipalAttacker.Id != Constants.Player1Id)
		{
			PlayAsNonMainPlayer();
		}
	}

	private void Reset()
	{
		var noCardsLeftPlayerData = PlayerScenes.Where(p => p.Player.Cards.Count == 0).ToList();
		if (noCardsLeftPlayerData.Count == PlayerScenes.Count())
		{
			GD.Print("Game ended in a draw!");
		}
		else
		{
			var loserPlayerData = PlayerScenes.Except(noCardsLeftPlayerData);
			GD.Print($"{string.Join(',', loserPlayerData.Select(p => p.Player.Id))} lost");
		}

		var scenes = GetTree().GetNodesInGroup(Constants.CardGroup).Union(GetTree().GetNodesInGroup(PlayersGroup));
		foreach (var cardScene in scenes)
		{
			cardScene.QueueFree();
		}

		GetTree().ReloadCurrentScene();
	}

	private void CurrentAttack_AttackCardAdded(object? sender, AttackCardAddedEventArgs e)
	{
		if (e.Card.Player.Id != Constants.Player1Id)
		{
			RearrangePlayerCards(e.Card.Player.Id);
			//GD.Print($"Ending call stack after {e.Card.Player.Id} card added");

			if (Attack.NextToPlay().Id != Constants.Player1Id)
			{
				PlayAsNonMainPlayer();
			}

			return;
		}

		RearrangeMainPlayerCards();
		PlayAsNonMainPlayer();
	}

	private bool PlayAsNonMainPlayer()
	{
		// todo click on mini map to make it full screen or keyboard shortcut

		var player = Attack.NextToPlay();
		var availableCardIndices = new Queue<int>(Enumerable.Range(0, player.Cards.Count));

		PrintCardScenes($"{player.Id} about to play");

		while (availableCardIndices.TryDequeue(out var cardIndex))
		{
			if (Attack.CanPlay(player, player.Cards[cardIndex]))
			{
				GD.Print($"Playing {player.Cards[cardIndex]} as {player.Id}");
				var cardScene = GetPlayerScene(player.Id).CardScenes.Single(c => c.Card == player.Cards[cardIndex]);

				PlaceCardOnTable(cardScene);

				Attack.Play(player, player.Cards[cardIndex]);
				PrintCardScenes($"{player.Id} played");
				return true;
			}
		}

		PrintCardScenes($"{player.Id} played");

		GD.Print($"Ending attack as {player.Id}");

		Attack.End();
		return false;
	}

	private void CreatePlayers(int count)
	{
		for (var i = 0; i < count; i++)
		{
			var playerScene = _playerScene.Instantiate<PlayerScene>();
			playerScene.AddToGroup(PlayersGroup);
			playerScene.Initialize($"P{i + 1}", _isAnimationEnabled);
			playerScene.CardSceneClicked += PlayerScene_CardSceneClicked;
			playerScene.CardAdded += Player_CardsAdded;
			AddChild(playerScene);
		}
	}

	private void Player_CardsAdded(CardScene cardScene)
	{
		if (_isAnimationEnabled)
		{
			AddPhysicsCooldown(cardScene);
		}
	}

	private void AddMainPlayerData(Camera3D camera)
	{
		var playerScene = MainPlayerScene;
		var globalPosition = GetMainPlayerGlobalPosition(camera);
		playerScene.CardPosition = globalPosition;
		playerScene.CardRotation = new Vector3(camera.RotationDegrees.X, -90, 0);
	}

	private Vector3 GetMainPlayerGlobalPosition(Camera3D camera)
	{
		var inFrontOfCamera = -camera.GlobalTransform.Basis.Z;
		var distancedInFrontOfCamera = inFrontOfCamera * _mainPlayerCardDistanceMultiplier;
		var lowered = new Vector3(0, -0.1f, 0);
		return camera.GlobalPosition + distancedInFrontOfCamera + lowered;
	}

	private void AddOpponentPlayerData()
	{
		var nodeName = PlayerScenes.Count() switch
		{
			2 => "TwoPlayerGame",
			3 => "ThreePlayerGame",
			_ => throw new NotImplementedException()
		};

		var positions = GetNode<Node3D>($"/root/Main/Table/GameSurface/{nodeName}")
			.GetChildren()
			.Where(n => n.Name != "Player1Position")
			.Cast<Node3D>()
			.Select(n => n.GlobalPosition);

		foreach (var (globalPosition, opponent) in positions.Zip(OrderedPlayerScenes.Except([MainPlayerScene])))
		{
			opponent.CardPosition = globalPosition;
			opponent.CardRotation = new Vector3(0, 90, 0);
		}
	}

	private void CreateTrumpCard(Card card)
	{
		GD.Print($"Trump: {card}");
		var cardScene = Constants.CardScene.Instantiate<CardScene>();
		cardScene.Initialize(card, CardState.InDeck);

		AddChild(cardScene);

		var trumpCard = GetNode<Node3D>("/root/Main/Table/GameSurface/Deck/TrumpCard");

		cardScene.MoveGlobally(trumpCard.GlobalPosition);
		cardScene.RotateDegrees(trumpCard.RotationDegrees);

		cardScene.IsAnimationEnabled = _isAnimationEnabled;
		cardScene.SetPhysicsProcess(false);

		cardScene.AddToGroup(Constants.TrumpCardGroup);
		cardScene.AddToGroup(Constants.CardGroup);
	}

	private void CreateTalon()
	{
		var cardScene = Constants.CardScene.Instantiate<CardScene>();

		AddChild(cardScene);

		var talon = GetNode<Node3D>("/root/Main/Table/GameSurface/Deck/Talon");

		cardScene.MoveGlobally(talon.GlobalPosition);
		cardScene.RotateDegrees(talon.RotationDegrees);

		cardScene.IsAnimationEnabled = _isAnimationEnabled;
		cardScene.SetPhysicsProcess(false);

		cardScene.GetNode<Sprite3D>("Back").SortingOffset = 1;
		cardScene.AddToGroup(Constants.TalonGroup);
		cardScene.AddToGroup(Constants.CardGroup);
	}

	private void PlayerScene_CardSceneClicked(CardScene cardScene)
	{
		GD.Print($"Received click {cardScene.Card}");

		if (Attack.NextToPlay() != MainPlayerScene.Player)
		{
			GD.Print("Ignoring P1 as it is other player's turn");
			return;
		}

		if (!MainPlayerScene.CardScenes.Contains(cardScene))
		{
			GD.Print("Ignoring P1 as it is not their card");
			return;
		}

		if (cardScene.CardState != CardState.InHand)
		{
			GD.Print("Ignoring P1 as card is not in their hand");
			return;
		}

		var canPlayResult = Attack.CanPlay(MainPlayerScene.Player, cardScene.Card!);

		if (!canPlayResult)
		{
			GD.Print($"Ignoring P1 as: {canPlayResult.Error}");
			return;
		}

		PrintCardScenes("P1 about to play");
		PlaceCardOnTable(cardScene);

		GD.Print($"Playing {cardScene.Card!} as P1");

		Attack.Play(MainPlayerScene.Player, cardScene.Card!);

		PrintCardScenes("P1 played");
	}

	public IEnumerable<Vector3> GetCardOffsets(int count)
	{
		if (count == 0)
		{
			return [];
		}

		var increment = _cardWidth + _cardPaddingX;
		var isEven = count % 2 == 0;
		var middleX = (_minMainPlayerHandX + _maxMainPlayerHandX) / 2.0f;

		var positions = new List<float>();
		if (!isEven)
		{
			positions.Add(middleX);
		}

		var positionLeft = isEven
			? middleX - increment / 2
			: middleX - increment;

		var positionRight = isEven
			? middleX + increment / 2
			: middleX + increment;

		for (var i = 0; i < count / 2; i++, positionLeft -= increment, positionRight += increment)
		{
			positions.Add(positionLeft);
			positions.Add(positionRight);
		}

		return positions.Select(p => new Vector3(0, 0, p));
	}

	private void AddPhysicsCooldown(CardScene cardScene)
	{
		var cooldown = TimeSpan.FromMilliseconds(_cardPhysicsCooldownMs * _cardPhysicsCooldownIteration);
		cardScene.AddPhysicsCooldown(cooldown);

		_cardPhysicsCooldownIteration++;

		if (_cardPhysicsCooldownMs * _cardPhysicsCooldownIteration > _maxCardPhysicsCooldownMs)
		{
			_cardPhysicsCooldownIteration = 0;
		}
	}

	private Node3D GetCardPlacementOnTable()
	{
		var path = new StringBuilder("/root/Main/Table/GameSurface/AttackingAndDefending/");
		var position = Attack.Cards.Count switch
		{
			0 => "AttackingCard1",
			1 => "DefendingCard1",
			2 => "AttackingCard2",
			3 => "DefendingCard2",
			4 => "AttackingCard3",
			5 => "DefendingCard3",
			6 => "AttackingCard4",
			7 => "DefendingCard4",
			8 => "AttackingCard5",
			9 => "DefendingCard5",
			10 => "AttackingCard6",
			11 => "DefendingCard6",
			_ => throw new GameException("Invalid amount of cards in current attack")
		};

		path.Append(position);
		return GetNode<Node3D>(path.ToString());
	}

	private void HandleBeatenOffAttack()
	{
		GD.Print("HandleBeatenOffAttack");

		var attackPlayerIds = Attack.Attackers.Select(a => a.Id).Union([Attack.Defender.Id]);

		foreach (var id in attackPlayerIds)
		{
			var tableCards = GetPlayerScene(id).CardScenes.Where(c => c.CardState == CardState.InAttack).ToList();

			foreach (var tableCard in tableCards)
			{
				GD.Print($"Discarding {tableCard.Card}");

				tableCard.MoveGlobally(DiscardPile.GlobalPosition);
				tableCard.RotateDegrees(DiscardPile.RotationDegrees);
				tableCard.CardState = CardState.Discarded;
			}
		}

		GD.Print("HandleBeatenOffAttack complete");
	}

	private void PlaceCardOnTable(CardScene cardScene)
	{
		var placement = GetCardPlacementOnTable();

		cardScene.MoveGlobally(placement.GlobalPosition);
		cardScene.RotateDegrees(placement.RotationDegrees);

		if (Attack.IsDefending)
		{
			cardScene.GetNode<Sprite3D>("Front").SortingOffset = 1;
		}

		cardScene.CardState = CardState.InAttack;
		cardScene.GetNode<MeshInstance3D>("MeshInstance3D").Show();
	}

	private void RearrangePlayerCards(string playerId)
	{
		var playerScene = GetPlayerScene(playerId);

		var inHandCards = playerScene.CardScenes.Where(c => c.CardState == CardState.InHand).ToList();
		GD.Print($"Rearranging {playerId} cards: {string.Join(',', inHandCards.Select(c => c.Card))}");

		var cardOffsets = GetCardOffsets(inHandCards.Count);

		foreach (var (cardScene, offset) in inHandCards.Zip(cardOffsets))
		{
			var targetPosition = playerScene.CardPosition + offset;

			cardScene.TargetPosition = targetPosition;
			cardScene.GlobalPosition = targetPosition;

			cardScene.TargetRotationDegrees = playerScene.CardRotation;
			cardScene.RotationDegrees = playerScene.CardRotation;
		}
	}
}
