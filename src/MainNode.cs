using Durak.Gameplay;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Durak.Godot;

public static class Constants
{
	public static readonly string Player1Id = "P1";
	public static readonly string CardGroup = "cards";
	public static readonly string TrumpCardGroup = "trumpCard";
	public static readonly string TalonGroup = "talon";
	public static readonly string CardSpriteFront = "Front";
	public static readonly PackedScene CardScene = GD.Load<PackedScene>("res://scenes/card.tscn");
}

public partial class MainNode : Node3D
{
	private const string _playersGroup = "players";
	private static readonly PackedScene _playerScene = GD.Load<PackedScene>("res://scenes/player.tscn");

	private int _cardPhysicsCooldownIteration = 0;

	private TurnLogic? _turnLogic;
	private IAttack? _currentAttack;
	private Dealer? _dealer;
	private Deck? _deck;
	private static string? _endScreenMessage;
	
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

	[Export]
	private int _nonMainPlayerDelayMs = 750;

	private IAttack Attack { get => _currentAttack ?? throw new GameException("Current attack not initialized"); }

	private IEnumerable<PlayerNode> PlayerNodes => GetChildren().Where(c => c.IsInGroup(_playersGroup)).Cast<PlayerNode>();

	private IEnumerable<PlayerNode> OrderedPlayerNodes => PlayerNodes.OrderBy(p => p.Player.Id);

	private PlayerNode MainPlayerNode => GetPlayerNode(Constants.Player1Id);

	private PlayerNode GetPlayerNode(string id) => PlayerNodes.Single(p => p.Player.Id == id);

	private Table Table => GetNode<Table>("/root/Main/Table");

	public override void _EnterTree()
	{
		GetNode<VBoxContainer>("%HUD").Hide();

		if (_endScreenMessage == null)
		{
			GetNode<CenterContainer>("EndScreen").Hide();
		}
		else
		{
			GetNode<CenterContainer>("EndScreen").Show();
		}

		var endScreenLabel = GetNode<Label>("EndScreen/Label");
		endScreenLabel.Text = _endScreenMessage;
	}

	private void Camera_Moved()
	{
		UpdateMainPlayerCardsPositionAndRotation();
		RepositionPlayerCards(Constants.Player1Id);
	}

	private void UpdateMainPlayerCardsPositionAndRotation()
	{
		var camera = GetNode<Camera>("%Camera");

		MainPlayerNode.CardsPosition = GetMainPlayerPosition(camera);
		MainPlayerNode.CardsRotationDegrees = new Vector3(camera.RotationDegrees.X, camera.RotationDegrees.Y, camera.RotationDegrees.Z);
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
		var childCount = GetChildCount();
		var totalChildCount = GetTree().GetNodeCount();

		GD.Print($"Main node child count: {childCount}, total: {totalChildCount}");

		GetNode<CenterContainer>("EndScreen").Hide();
		GetNode<MarginContainer>("%Menu").Hide();
		GetNode<VBoxContainer>("%HUD").Show();
		GetNode<Button>("%BackToMenuButton").Show();

		var opponentCount = GetNode<SpinBox>("%OpponentsSpinBox");
		CreatePlayers((int)opponentCount.Value + 1);

		var camera = GetNode<Camera>("%Camera");
		camera.Moved -= Camera_Moved;
		camera.Moved += Camera_Moved;

		var players = OrderedPlayerNodes.Select(p => p.Player).ToList();
		Table.Initialize(players.Select(p => p.Id).ToList());
		
		SetMainPlayerCardsPositionAndRotation(camera);
		AddOpponentPlayerData();

		_deck = new Deck(new FrenchSuited36CardProvider(), new DefaultCardShuffler());
		_deck.CardRemoved += _deck_CardRemoved;
		_turnLogic = new TurnLogic(players, _deck.TrumpSuit);

		_dealer = new Dealer(6, players, _deck);

		GetNode<Label>("%TrumpSuit").Text = $" Trump suit: {GetUserFriendlyTrumpSuitName(_deck.TrumpSuit)}";
		CreateTrumpCard(_deck.TrumpCard);
		CreateTalon();

		_dealer.Deal(null);

		StartAttack();
	}

	private static string GetUserFriendlyTrumpSuitName(char trumpSuit)
	{
		if (trumpSuit == Suit.Clubs)
		{
			return "Clubs";
		}
		else if (trumpSuit == Suit.Diamonds)
		{
			return "Diamonds";
		}
		else if (trumpSuit == Suit.Hearts)
		{
			return "Hearts";
		}
		else if (trumpSuit == Suit.Spades)
		{
			return "Spades";
		}
		else
		{
			throw new GameException($"Unknown card suit {trumpSuit}");
		}
	}

	private void _deck_CardRemoved(object? sender, CardRemovedEventArgs e)
	{
		if (_deck!.Count == 1)
		{
			GD.Print("Hiding talon");
			((CardNode)GetTree().GetFirstNodeInGroup(Constants.TalonGroup)).Hide();
		}
		else if (_deck!.Count == 0)
		{
			GD.Print("Hiding trump card");
			((CardNode)GetTree().GetFirstNodeInGroup(Constants.TrumpCardGroup)).Hide();
		}
	}

	private void ResetCurrentAttackLabel()
	{
		GetNode<Label>("%CurrentAttack").Text = "";
	}

	private string GetAttackTitle()
	{
		var attackerIds = Attack.Attackers.Select(a => a.Id);
		return $"{string.Join(',', attackerIds)} vs {Attack.Defender.Id}";
	}

	private async void CurrentAttack_AttackEnded(object? sender, EventArgs e)
	{
		ResetCurrentAttackLabel();

		Attack.AttackCardAdded -= CurrentAttack_AttackCardAdded;
		Attack.AttackEnded -= CurrentAttack_AttackEnded;

		var attackCards = Attack.Cards.Select(c => c.Card);
		GD.Print($"Attack state: {Attack.State} | {GetAttackTitle()} | {string.Join(',', attackCards)}");

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

		await Task.Delay(TimeSpan.FromMilliseconds(_nonMainPlayerDelayMs));
		_dealer!.Deal(Attack);
		
		StartAttack();
	}

	private void PrintCardNodes(string tag)
	{
		GD.Print(tag);

		foreach (var playerNode in OrderedPlayerNodes)
		{
			var nonDiscardedCards = playerNode.CardNodes.Where(c => c.CardState != CardState.Discarded).Select(c => $"{c.Card} {c.CardState}");
			var discardedCards = playerNode.CardNodes.Where(c => c.CardState == CardState.Discarded).Select(c => $"{c.Card}");
			GD.Print($"{playerNode.Player.Id} {string.Join(", ", nonDiscardedCards)}, Discarded: {string.Join(", ", discardedCards)}");
		}
	}

	private void StartAttack()
	{
		if (!_turnLogic!.TryGetNextAttack(out _currentAttack))
		{
			ResetAndUpdateEndScreenMessage();
			return;
		}

		GetNode<Label>("%CurrentAttack").Text = $" {GetAttackTitle()}";

		Attack.AttackCardAdded += CurrentAttack_AttackCardAdded;
		Attack.AttackEnded += CurrentAttack_AttackEnded;

		GD.Print($"Starting attack as {Attack.PrincipalAttacker.Id}");

		if (Attack.PrincipalAttacker.Id != Constants.Player1Id)
		{
			PlayAsNonMainPlayer();
		}
	}

	private void ResetAndUpdateEndScreenMessage()
	{
		var noCardsLeftPlayerData = PlayerNodes.Where(p => p.Player.Cards.Count == 0).ToList();
		if (noCardsLeftPlayerData.Count == PlayerNodes.Count())
		{
			_endScreenMessage = "Game ended in a draw!";
		}
		else
		{
			var loserPlayerData = PlayerNodes.Except(noCardsLeftPlayerData);
			_endScreenMessage = $"{string.Join(',', loserPlayerData.Select(p => p.Player.Id))} lost";
		}

		GD.Print(_endScreenMessage);
		Reset();
	}

	private void Reset()
	{
		var nodes = GetTree().GetNodesInGroup(Constants.CardGroup).Union(GetTree().GetNodesInGroup(_playersGroup));
		foreach (var node in nodes)
		{
			node.QueueFree();
		}

		GetTree().ReloadCurrentScene();
	}

	private async void CurrentAttack_AttackCardAdded(object? sender, AttackCardAddedEventArgs e)
	{
		RepositionPlayerCards(e.Card.Player.Id);

		if (Attack.NextToPlay().Id != Constants.Player1Id)
		{
			await Task.Delay(TimeSpan.FromMilliseconds(_nonMainPlayerDelayMs));
			PlayAsNonMainPlayer();
		}
	}

	private bool PlayAsNonMainPlayer()
	{
		var player = Attack.NextToPlay();
		var availableCardIndices = new Queue<int>(Enumerable.Range(0, player.Cards.Count));

		PrintCardNodes($"{player.Id} about to play");

		while (availableCardIndices.TryDequeue(out var cardIndex))
		{
			if (Attack.CanPlay(player, player.Cards[cardIndex]))
			{
				GD.Print($"Playing {player.Cards[cardIndex]} as {player.Id}");
				var cardNode = GetPlayerNode(player.Id).CardNodes.Single(c => c.Card == player.Cards[cardIndex]);

				PlaceCardOnTable(cardNode);

				Attack.Play(player, player.Cards[cardIndex]);
				PrintCardNodes($"{player.Id} played");
				return true;
			}
		}

		PrintCardNodes($"{player.Id} played");

		GD.Print($"Ending attack as {player.Id}");

		Attack.End();
		return false;
	}

	private void CreatePlayers(int count)
	{
		for (var i = 0; i < count; i++)
		{
			var playerNode = _playerScene.Instantiate<PlayerNode>();
			playerNode.AddToGroup(_playersGroup);
			playerNode.Initialize($"P{i + 1}", _isAnimationEnabled);
			playerNode.CardClicked += Player_CardClicked;
			playerNode.CardAdded += Player_CardAdded;
			playerNode.CardsAdded += PlayerNode_CardsAdded;
			AddChild(playerNode);
		}
	}

	private void PlayerNode_CardsAdded(string playerId)
	{
		RepositionPlayerCards(playerId);
	}

	private void Player_CardAdded(CardNode cardNode)
	{
		if (_isAnimationEnabled)
		{
			AddPhysicsCooldown(cardNode);
		}
	}

	private void SetMainPlayerCardsPositionAndRotation(Camera3D camera)
	{
		MainPlayerNode.CardsPosition = GetMainPlayerPosition(camera);
		MainPlayerNode.CardsRotationDegrees = new Vector3(camera.RotationDegrees.X, -90, 0);
	}

	private Vector3 GetMainPlayerPosition(Camera3D camera)
	{
		var inFrontOfCamera = -camera.Transform.Basis.Z;
		var distancedInFrontOfCamera = inFrontOfCamera * _mainPlayerCardDistanceMultiplier;
		var lowered = new Vector3(0, -0.1f, 0);
		return camera.Position + distancedInFrontOfCamera + lowered;
	}

	private void AddOpponentPlayerData()
	{
		var paths = Table.GetPaths();

		foreach (var (path, opponent) in paths.Zip(OrderedPlayerNodes.Except([MainPlayerNode])))
		{
			opponent.CardsPath = path;
			opponent.CardsPosition = path.GlobalPosition;
		
			var angle = Mathf.RadToDeg(MainPlayerNode.CardsPosition.SignedAngleTo(path.GlobalPosition, Vector3.Left));
			if (angle < 0) 
			{ 
				angle += 180.0f;
			}

			GD.Print("angle ", angle);
			opponent.CardsRotationDegrees = new Vector3(90, 0, angle);
		}
	}

	private void CreateTrumpCard(Card card)
	{
		GD.Print($"Trump: {card}");
		var cardNode = Constants.CardScene.Instantiate<CardNode>();
		cardNode.Initialize(card, CardState.InDeck);

		AddChild(cardNode);

		var trumpCard = GetNode<Node3D>("/root/Main/Table/GameSurface/Deck/TrumpCard");

		cardNode.MoveGlobally(trumpCard.GlobalPosition);
		cardNode.RotateDegrees(trumpCard.RotationDegrees);

		cardNode.IsAnimationEnabled = _isAnimationEnabled;
		cardNode.SetPhysicsProcess(false);

		cardNode.AddToGroup(Constants.TrumpCardGroup);
		cardNode.AddToGroup(Constants.CardGroup);
	}

	private void CreateTalon()
	{
		var cardNode = Constants.CardScene.Instantiate<CardNode>();

		AddChild(cardNode);

		var talon = GetNode<Node3D>("/root/Main/Table/GameSurface/Deck/Talon");

		cardNode.MoveGlobally(talon.GlobalPosition);
		cardNode.RotateDegrees(talon.RotationDegrees);

		cardNode.IsAnimationEnabled = _isAnimationEnabled;
		cardNode.SetPhysicsProcess(false);
		cardNode.UpdateSortingOffsets();
		
		cardNode.AddToGroup(Constants.TalonGroup);
		cardNode.AddToGroup(Constants.CardGroup);
	}

	private void Player_CardClicked(CardNode cardNode)
	{
		GD.Print($"Received click {cardNode.Card}");

		if (Attack.NextToPlay() != MainPlayerNode.Player)
		{
			GD.Print("Ignoring P1 as it is other player's turn");
			return;
		}

		if (!MainPlayerNode.CardNodes.Contains(cardNode))
		{
			GD.Print("Ignoring P1 as it is not their card");
			return;
		}

		if (cardNode.CardState != CardState.InHand)
		{
			GD.Print("Ignoring P1 as card is not in their hand");
			return;
		}

		var canPlayResult = Attack.CanPlay(MainPlayerNode.Player, cardNode.Card!);

		if (!canPlayResult)
		{
			GD.Print($"Ignoring P1 as: {canPlayResult.Error}");
			return;
		}

		PrintCardNodes("P1 about to play");
		PlaceCardOnTable(cardNode);

		GD.Print($"Playing {cardNode.Card!} as P1");

		Attack.Play(MainPlayerNode.Player, cardNode.Card!);

		PrintCardNodes("P1 played");
	}

	public IEnumerable<Vector3> GetCardOffsets(int count)
	{
		if (count == 0)
		{
			return [];
		}

		var increment = (_cardWidth + _cardPaddingX) / 2;
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

		return positions.Order().Select((p, i) => new Vector3(0, 0 + (i / 10000f), p));
	}

	private void AddPhysicsCooldown(CardNode cardNode)
	{
		var cooldown = TimeSpan.FromMilliseconds(_cardPhysicsCooldownMs * _cardPhysicsCooldownIteration);
		cardNode.AddPhysicsCooldown(cooldown);

		_cardPhysicsCooldownIteration++;

		if (_cardPhysicsCooldownMs * _cardPhysicsCooldownIteration > _maxCardPhysicsCooldownMs)
		{
			_cardPhysicsCooldownIteration = 0;
		}
	}

	private void HandleBeatenOffAttack()
	{
		GD.Print("HandleBeatenOffAttack");

		var attackPlayerIds = Attack.Attackers.Select(a => a.Id).Union([Attack.Defender.Id]);

		foreach (var id in attackPlayerIds)
		{
			var tableCards = GetPlayerNode(id).CardNodes.Where(c => c.CardState == CardState.InAttack).ToList();

			foreach (var tableCard in tableCards)
			{
				tableCard.Discard();
			}
		}

		GD.Print("HandleBeatenOffAttack complete");
	}

	private void PlaceCardOnTable(CardNode cardNode)
	{
		var placement = Table.GetCardPlacementOnTable(Attack.Cards.Count);
		cardNode.PlaceCardOnTable(placement, Attack);
		cardNode.GetParent<PlayerNode>().RemoveCardFromOrder(cardNode.Card);
	}

	private void RepositionPlayerCards(string playerId)
	{
		var playerNode = GetPlayerNode(playerId);

		var inHandCards = playerNode.CardNodes.Where(c => c.CardState == CardState.InHand).OrderBy(c => c.OrderInHand).ToList();
		GD.Print($"Rearranging {playerId} cards: {string.Join(',', inHandCards.Select(c => $"{c.Card} {c.OrderInHand}"))}");

		var sortingOffset = 0;

		if (playerId != Constants.Player1Id)
		{
			var sampling = 0.0f;
			var sampleStep = playerNode.CardsPath!.Curve.GetBakedLength() / inHandCards.Count;
			foreach (var cardNode in inHandCards)
			{
				var targetPosition = playerNode.CardsPath!.Curve.SampleBaked(sampling);
				targetPosition = playerNode.CardsPath!.ToGlobal(targetPosition);
			
				MoveRotateAndSetSortingOffset(cardNode, targetPosition, playerNode, sortingOffset);

				sortingOffset++;
				sampling += sampleStep;
			}

			return;
		}

		var cardOffsets = GetCardOffsets(inHandCards.Count);
		foreach (var (cardNode, offset) in inHandCards.Zip(cardOffsets))
		{
			var targetPosition = playerNode.CardsPosition + offset;
			MoveRotateAndSetSortingOffset(cardNode, targetPosition, playerNode, sortingOffset);
			sortingOffset++;
		}
	}

	private static void MoveRotateAndSetSortingOffset(CardNode cardNode, Vector3 targetPosition, PlayerNode playerNode, int sortingOffset)
	{
		cardNode.MoveGlobally(targetPosition);
		cardNode.RotateDegrees(playerNode.CardsRotationDegrees);
		cardNode.GetNode<Sprite3D>(Constants.CardSpriteFront).SortingOffset = sortingOffset;
	}

	private void _on_back_to_menu_button_pressed()
	{
		Attack.End();
		Reset();
	}
}
