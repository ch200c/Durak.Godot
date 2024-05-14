using Durak.Gameplay;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Durak.Godot;

public partial class MainScene : Node3D
{
	private int _cardPhysicsCooldownIteration;
	private readonly PackedScene _cardScene;
	private readonly Dictionary<string, PlayerData> _playerData;
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

	public MainScene()
	{
		_cardScene = GD.Load<PackedScene>("res://scenes/card.tscn");
		_playerData = [];
		_cardPhysicsCooldownIteration = 0;
	}

	public override void _EnterTree()
	{
		GetNode<MarginContainer>("%HUD").Hide();
	}

	private void Camera_Moved(object? sender, EventArgs e)
	{
		RearrangeMainPlayerCards();
	}

	private void RearrangeMainPlayerCards()
	{
		var camera = GetNode<Camera>("%Camera");
		var mainPlayerGlobalPosition = GetMainPlayerGlobalPosition(camera);

		_playerData["P1"].GlobalPosition = mainPlayerGlobalPosition;
		_playerData["P1"].RotationDegrees = new Vector3(camera.RotationDegrees.X, camera.RotationDegrees.Y, camera.RotationDegrees.Z);

		RearrangePlayerCards("P1");
	}

	private void RearrangePlayerCards(string id)
	{
		var inHandCards = _playerData[id].CardScenes.Where(c => c.CardState == CardState.InHand).ToList();
		GD.Print($"Rearranging {id} cards: {string.Join(',', inHandCards.Select(c => c.Card))}");

		var cardOffsets = GetCardOffsets(inHandCards.Count);

		foreach (var (existingCardScene, offset) in inHandCards.Zip(cardOffsets))
		{
			var targetPosition = _playerData[id].GlobalPosition + offset;

			existingCardScene.TargetPosition = targetPosition;
			existingCardScene.GlobalPosition = targetPosition;
			existingCardScene.TargetRotationDegrees = _playerData[id].RotationDegrees;
			existingCardScene.RotationDegrees = _playerData[id].RotationDegrees;
		}
	}

	private void _on_end_attack_button_pressed()
	{
		if (_currentAttack!.NextToPlay().Id != "P1")
		{
			GD.Print("Ignoring end attack");
			return;
		}

		GD.Print("Ending attack by P1");
		_currentAttack!.End();
	}

	private void _on_play_button_pressed()
	{
		var mainSceneChildCount = GetChildCount();
		var totalChildCount = GetTree().GetNodeCount();

		GD.Print($"Main scene child count: {mainSceneChildCount}, total: {totalChildCount}");

		GetNode<MarginContainer>("%Menu").Hide();
		GetNode<MarginContainer>("%HUD").Show();

		var opponentCount = GetNode<SpinBox>("%OpponentsSpinBox");
		var players = CreatePlayers((int)opponentCount.Value + 1);

		var camera = GetNode<Camera>("%Camera");
		camera.Moved -= Camera_Moved;
		camera.Moved += Camera_Moved;

		AddMainPlayerData(players[0], camera);
		AddOpponentPlayerData(players);

		_deck = new Deck(new FrenchSuited36CardProvider(), new DefaultCardShuffler());
		_turnLogic = new TurnLogic(players, _deck.TrumpSuit);

		_dealer = new Dealer(6, players, _deck);
		_dealer.Deal(_currentAttack);

		CreateTrumpCard(_deck.TrumpCard);
		CreateTalon();

		StartAttack();
	}

	private void CurrentAttack_AttackEnded(object? sender, EventArgs e)
	{
		_currentAttack!.AttackCardAdded -= CurrentAttack_AttackCardAdded;
		_currentAttack.AttackEnded -= CurrentAttack_AttackEnded;

		var attackerIds = _currentAttack!.Attackers.Select(a => a.Id);
		var attackCards = _currentAttack!.Cards.Select(c => c.Card);
		GD.Print($"Attack state: {_currentAttack.State} | {string.Join(',', attackerIds)} vs {_currentAttack.Defender.Id} | {string.Join(',', attackCards)}");

		switch (_currentAttack.State)
		{
			case AttackState.BeatenOff:
				{
					DiscardTableCards();
					break;
				}
			case AttackState.Successful:
				{
					RemoveCardScenes();
					break;
				}
			default:
				{
					throw new GameException($"Invalid current attack state {_currentAttack!.State}");
				}
		}

		_dealer!.Deal(_currentAttack);

		StartAttack();
	}

	private void PrintCardScenes(string tag)
	{
		GD.Print(tag);

		foreach (var playerData in _playerData)
		{
			var nonDiscardedCards = playerData.Value.CardScenes.Where(c => c.CardState != CardState.Discarded).Select(c => $"{c.Card} {c.CardState}");
			var discardedCards = playerData.Value.CardScenes.Where(c => c.CardState == CardState.Discarded).Select(c => $"{c.Card}");
			GD.Print($"{playerData.Key} {string.Join(", ", nonDiscardedCards)}, Discarded: {string.Join(", ", discardedCards)}");
		}
	}

	private void DiscardTableCards()
	{
		var discardPile = GetNode<Node3D>("/root/Main/Table/GameSurface/DiscardPilePosition");

		var attackPlayerIds = _currentAttack!.Attackers.Select(a => a.Id).Union([_currentAttack.Defender.Id]);

		foreach (var id in attackPlayerIds)
		{
			var tableCards = _playerData[id!].CardScenes.Where(c => c.CardState == CardState.InAttack).ToList();

			foreach (var tableCard in tableCards)
			{
				GD.Print($"Discarding {tableCard.Card}");

				MoveCard(tableCard, discardPile.GlobalPosition);
				RotateCard(tableCard, CardScene.FaceDownDiscardedDegrees);
				tableCard.CardState = CardState.Discarded;
			}
		}
	}

	private void RemoveCardScenes()
	{
		var attackPlayerIds = _currentAttack!.Attackers.Select(a => a.Id).Union([_currentAttack.Defender.Id]);

		foreach (var id in attackPlayerIds)
		{
			var tableCards = _playerData[id!].CardScenes.Where(c => c.CardState == CardState.InAttack).ToList();

			foreach (var tableCard in tableCards)
			{
				GD.Print($"Discarding {tableCard.Card}");
				tableCard.CardState = CardState.Discarded;
			}
		}
	}

	private void StartAttack()
	{
		if (!_turnLogic!.TryGetNextAttack(out _currentAttack))
		{
			Reset();
			return;
		}

		_currentAttack.AttackCardAdded += CurrentAttack_AttackCardAdded;
		_currentAttack.AttackEnded += CurrentAttack_AttackEnded;

		GD.Print($"Starting attack as {_currentAttack.PrincipalAttacker.Id}");

		if (_currentAttack.PrincipalAttacker.Id != "P1")
		{
			PlayAsNonMainPlayer();
		}
	}

	private void Reset()
	{
		var noCardsLeftPlayerData = _playerData.Values.Where(p => p.Player.Cards.Count == 0).ToList();
		if (noCardsLeftPlayerData.Count == _playerData.Count)
		{
			GD.Print("Game ended in a draw!");
		}
		else
		{
			var loserPlayerData = _playerData.Values.Except(noCardsLeftPlayerData);
			GD.Print($"{string.Join(',', loserPlayerData.Select(p => p.Player.Id))} lost");
		}

		foreach (var (key, value) in _playerData)
		{
			value.Player.CardsAdded -= Player_CardsAdded;
		}

		_playerData.Clear();

		var cardScenes = GetTree().GetNodesInGroup("card");
		foreach (var cardScene in cardScenes)
		{
			cardScene.QueueFree();
		}

		GetTree().ReloadCurrentScene();
	}

	private void CurrentAttack_AttackCardAdded(object? sender, AttackCardAddedEventArgs e)
	{
		if (e.Card.Player.Id != "P1")
		{
			RearrangePlayerCards(e.Card.Player.Id!);
			GD.Print($"Ending call stack after {e.Card.Player.Id} card added");
			return;
		}

		RearrangeMainPlayerCards();
		PlayAsNonMainPlayer();
	}

	private bool PlayAsNonMainPlayer()
	{
		// todo click on mini map to make it full screen or keyboard shortcut

		var player = _currentAttack!.NextToPlay();
		var availableCardIndices = new Queue<int>(Enumerable.Range(0, player.Cards.Count));

		PrintCardScenes($"{player.Id} about to play");

		while (availableCardIndices.TryDequeue(out var cardIndex))
		{
			if (_currentAttack.CanPlay(player, player.Cards[cardIndex]))
			{
				GD.Print($"Playing {player.Cards[cardIndex]} as {player.Id}");
				var cardScene = _playerData[player.Id!].CardScenes.Single(c => c.Card == player.Cards[cardIndex]);

				PlaceCardOnTable(cardScene);

				_currentAttack.Play(player, player.Cards[cardIndex]);
				PrintCardScenes($"{player.Id} played");
				return true;
			}
		}

		PrintCardScenes($"{player.Id} played");

		GD.Print($"Ending attack as {player.Id}");

		_currentAttack.End();
		return false;
	}

	private List<Player> CreatePlayers(int count)
	{
		var players = new List<Player>() { };

		for (var i = 0; i < count; i++)
		{
			var player = new Player($"P{i + 1}");
			player.CardsAdded += Player_CardsAdded;
			players.Add(player);
		}

		return players;
	}

	private void AddMainPlayerData(Player player, Camera3D camera)
	{
		var globalPosition = GetMainPlayerGlobalPosition(camera);
		var playerData = new PlayerData(player, globalPosition, new Vector3(camera.RotationDegrees.X, -90, 0), []);
		_playerData.Add(playerData.Player.Id!, playerData);
	}

	private Vector3 GetMainPlayerGlobalPosition(Camera3D camera)
	{
		var inFrontOfCamera = -camera.GlobalTransform.Basis.Z;
		var distancedInFrontOfCamera = inFrontOfCamera * _mainPlayerCardDistanceMultiplier;
		var lowered = new Vector3(0, -0.1f, 0);
		return camera.GlobalPosition + distancedInFrontOfCamera + lowered;
	}

	private void AddOpponentPlayerData(List<Player> players)
	{
		var nodeName = players.Count switch
		{
			2 => "TwoPlayerGame",
			_ => throw new NotImplementedException()
		};

		var positions = GetNode<Node3D>($"/root/Main/Table/GameSurface/{nodeName}")
			.GetChildren()
			.Cast<Node3D>()
			.Select(n => n.GlobalPosition);

		foreach (var (globalPosition, opponent) in positions.Skip(1).Zip(players.Skip(1)))
		{
			_playerData.Add(opponent.Id!, new PlayerData(opponent, globalPosition, new Vector3(0, 90, 0), []));
		}
	}

	private void CreateTrumpCard(Card card)
	{
		GD.Print($"Trump: {card}");
		var cardScene = _cardScene.Instantiate<CardScene>();
		cardScene.Initialize(card);

		AddChild(cardScene);


		var trumpCard = GetNode<Node3D>("/root/Main/Table/GameSurface/Talon/TrumpCardPosition");

		MoveCard(cardScene, trumpCard.GlobalPosition);
		RotateCard(cardScene, CardScene.TrumpCardFaceUpDegrees);

		cardScene.SetPhysicsProcess(_isAnimationEnabled);
		cardScene.AddToGroup("trumpCard");
		cardScene.AddToGroup("card");
	}

	private void CreateTalon()
	{
		var cardScene = _cardScene.Instantiate<CardScene>();

		AddChild(cardScene);

		var talon = GetNode<Node3D>("/root/Main/Table/GameSurface/Talon/TalonPosition");

		MoveCard(cardScene, talon.GlobalPosition);
		RotateCard(cardScene, CardScene.FaceDownDegrees);

		cardScene.SetPhysicsProcess(_isAnimationEnabled);
		cardScene.GetNode<Sprite3D>("Back").SortingOffset = 1;
		cardScene.AddToGroup("talon");
		cardScene.AddToGroup("card");
	}

	private sealed record AddedCardData(CardScene? CardScene, bool IsNewCard, bool IsPlayerCard);

	private void Player_CardsAdded(object? sender, CardsAddedEventArgs e)
	{
		if (_deck!.Count == 1)
		{
			GD.Print("Hiding talon");
			((CardScene)GetTree().GetFirstNodeInGroup("talon")).Hide();
		}
		else if (_deck!.Count == 0)
		{
			GD.Print("Hiding trump card");
			((CardScene)GetTree().GetFirstNodeInGroup("trumpCard")).Hide();
		}

		var talon = GetNode<Node3D>("/root/Main/Table/GameSurface/Talon/TalonPosition");
		var playerData = _playerData[((Player)sender!).Id!];

		foreach (var card in e.Cards)
		{
			GD.Print($"{card} added for {playerData.Player.Id}");

			var (cardScene, isNewCard, isPlayerCard) = GetAddedCardData(playerData, card);

			if (isNewCard)
			{
				cardScene = InstantiateAndInitializeCardScene(card);
			}

			if (!isPlayerCard)
			{
				playerData.CardScenes.Add(cardScene!);
			}

			if (playerData.Player.Id == "P1")
			{
				cardScene!.Clicked -= CardScene_Clicked;
				cardScene.Clicked += CardScene_Clicked;
				cardScene.GetNode<MeshInstance3D>("MeshInstance3D").Hide();
			}

			cardScene!.CardState = CardState.InHand;

			if (_isAnimationEnabled)
			{
				cardScene.TargetRotationDegrees = playerData.RotationDegrees;

				if (isNewCard)
				{
					cardScene.RotationDegrees = CardScene.FaceDownDegrees;
					cardScene.GlobalPosition = talon.GlobalPosition;
				}
			}
			else
			{
				cardScene.RotationDegrees = playerData.RotationDegrees;
			}

			var inHandCards = playerData.CardScenes.Where(c => c.CardState == CardState.InHand).ToList();
			var offsets = GetCardOffsets(inHandCards.Count);

			foreach (var (existingCardScene, offset) in inHandCards.Zip(offsets))
			{
				var targetPosition = playerData.GlobalPosition + offset;
				MoveCard(existingCardScene, targetPosition);
			}

			if (_isAnimationEnabled)
			{
				AddPhysicsCooldown(cardScene);
			}
		}
	}

	private CardScene InstantiateAndInitializeCardScene(Card card)
	{
		var cardScene = _cardScene.Instantiate<CardScene>();
		cardScene.Initialize(card);

		AddChild(cardScene);
		cardScene.AddToGroup("card");
		cardScene.SetPhysicsProcess(_isAnimationEnabled);
		return cardScene;
	}

	private AddedCardData GetAddedCardData(PlayerData playerData, Card card)
	{
		CardScene? cardScene = null;
		string? previousPlayerId = null;

		foreach (var kvp in _playerData)
		{
			cardScene = kvp.Value.CardScenes.Find(c => c.Card == card);
			if (cardScene != null)
			{
				previousPlayerId = kvp.Key;

				break;
			}
		}

		var isPlayerCard = previousPlayerId == playerData.Player.Id;
		var isNewCard = cardScene == null;

		if (!isPlayerCard && !isNewCard)
		{
			_playerData[previousPlayerId!].CardScenes.Remove(cardScene!);
		}

		return new AddedCardData(cardScene, isNewCard, isPlayerCard);
	}

	private Vector3 GetCardGlobalPositionOnTable()
	{
		var path = new StringBuilder("/root/Main/Table/GameSurface/AttackingAndDefending/");
		var position = _currentAttack!.Cards.Count switch
		{
			0 => "AttackingCard1Position",
			1 => "DefendingCard1Position",
			2 => "AttackingCard2Position",
			3 => "DefendingCard2Position",
			4 => "AttackingCard3Position",
			5 => "DefendingCard3Position",
			6 => "AttackingCard4Position",
			7 => "DefendingCard4Position",
			8 => "AttackingCard5Position",
			9 => "DefendingCard5Position",
			10 => "AttackingCard6Position",
			11 => "DefendingCard6Position",
			_ => throw new GameException("Invalid amount of cards in current attack")
		};

		path.Append(position);
		return GetNode<Node3D>(path.ToString()).GlobalPosition;
	}

	private bool IsDefending()
	{
		return _currentAttack!.Cards.Count % 2 != 0;
	}

	private void CardScene_Clicked(object? sender, EventArgs e)
	{
		var cardScene = (CardScene)sender!;
		GD.Print($"Received click {cardScene.Card}");

		if (_currentAttack == null || _currentAttack.NextToPlay() != _playerData["P1"].Player)
		{
			GD.Print("Ignoring P1 as it is other player's turn");
			return;
		}

		if (!_playerData["P1"].CardScenes.Contains(cardScene))
		{
			GD.Print("Ignoring P1 as it is not their card");
			return;
		}

		if (cardScene.CardState != CardState.InHand)
		{
			GD.Print("Ignoring P1 as card is not in their hand");
			return;
		}

		var canPlayResult = _currentAttack!.CanPlay(_playerData["P1"].Player, cardScene.Card!);

		if (!canPlayResult)
		{
			GD.Print($"Ignoring P1 as: {canPlayResult.Error}");
			return;
		}


		PrintCardScenes("P1 about to play");

		PlaceCardOnTable(cardScene);

		GD.Print($"Playing {cardScene.Card!} as P1");

		_currentAttack.Play(_playerData["P1"].Player, cardScene.Card!);

		PrintCardScenes("P1 played");
	}

	private void PlaceCardOnTable(CardScene cardScene)
	{
		var position = GetCardGlobalPositionOnTable();

		MoveCard(cardScene, position);
		RotateCard(cardScene, CardScene.FaceUpDegrees);

		if (IsDefending())
		{
			cardScene.GetNode<Sprite3D>("Front").SortingOffset = 1;
		}

		cardScene.CardState = CardState.InAttack;
		cardScene.GetNode<MeshInstance3D>("MeshInstance3D").Show();
	}

	private void RotateCard(CardScene cardScene, Vector3 rotationDegrees)
	{
		if (_isAnimationEnabled)
		{
			cardScene.TargetRotationDegrees = rotationDegrees;
		}
		else
		{
			cardScene.RotationDegrees = rotationDegrees;
		}
	}

	private void MoveCard(CardScene cardScene, Vector3 position)
	{
		if (_isAnimationEnabled)
		{
			cardScene.TargetPosition = position;
		}
		else
		{
			cardScene.GlobalPosition = position;
		}
	}

	private IEnumerable<Vector3> GetCardOffsets(int count)
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
}
