using Durak.Gameplay;
using Godot;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Durak.Godot;

public interface IDiscardPileProvider
{
	Node3D DiscardPile { get; }
}

public interface IAttackProvider
{
	IAttack Attack { get; }
}

public interface IPlayerDataProvider
{
	Dictionary<string, PlayerData> PlayerData { get; }
}

//public class Reset : INotification 
//{ 
//}

public partial class MainScene : Node3D, IDiscardPileProvider, IAttackProvider, IPlayerDataProvider
{
	private const string Player1Id = "P1";
	private int _cardPhysicsCooldownIteration;
	private readonly PackedScene _cardScene;
	private readonly Dictionary<string, PlayerData> _playerData;
	private TurnLogic? _turnLogic;
	private IAttack? _currentAttack;
	private Dealer? _dealer;
	private Deck? _deck;
	private ServiceProvider _serviceProvider;

	private IMediator Mediator => _serviceProvider.GetRequiredService<IMediator>();
	
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

	public IAttack Attack { get => _currentAttack ?? throw new GameException("Current attack not initialized"); }

	public Dictionary<string, PlayerData> PlayerData => _playerData;

	public Node3D DiscardPile { get => GetNode<Node3D>("/root/Main/Table/GameSurface/DiscardPile"); }
	
	public MainScene()
	{
		var services = new ServiceCollection();
		services.AddMediatR(c => c.RegisterServicesFromAssembly(typeof(MainScene).Assembly));
		services.AddSingleton<IPlayerDataProvider, MainScene>(_ => this);
		services.AddSingleton<IDiscardPileProvider, MainScene>(_ => this);
		services.AddSingleton<IAttackProvider, MainScene>(_ => this);
		services.AddTransient<IRequestHandler<BeatenOffAttackRequest>, BeatenOffAttackHandler>();
		//services.AddSingleton<INotificationHandler<Reset>, BeatenOffAttackHandler>();
		services.AddTransient<IRequestHandler<SuccessfulAttackRequest>, SuccessfulAttackHandler>();

		_serviceProvider = services.BuildServiceProvider();
		_cardScene = GD.Load<PackedScene>("res://scenes/card.tscn");
		_playerData = [];
		_cardPhysicsCooldownIteration = 0;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		_serviceProvider.Dispose();
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

		_playerData[Player1Id].GlobalPosition = mainPlayerGlobalPosition;
		_playerData[Player1Id].RotationDegrees = new Vector3(camera.RotationDegrees.X, camera.RotationDegrees.Y, camera.RotationDegrees.Z);

		RearrangePlayerCards(Player1Id);
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
		if (Attack.NextToPlay().Id != Player1Id)
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
		var players = CreatePlayers((int)opponentCount.Value + 1);

		var camera = GetNode<Camera>("%Camera");
		camera.Moved -= Camera_Moved;
		camera.Moved += Camera_Moved;

		AddMainPlayerData(players[0], camera);
		AddOpponentPlayerData(players);

		_deck = new Deck(new FrenchSuited36CardProvider(), new DefaultCardShuffler());
		_turnLogic = new TurnLogic(players, _deck.TrumpSuit);

		_dealer = new Dealer(6, players, _deck);
		_dealer.Deal(null);

		CreateTrumpCard(_deck.TrumpCard);
		CreateTalon();

		StartAttack();
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
					Mediator.Send(new BeatenOffAttackRequest()).GetAwaiter().GetResult();
					break;
				}
			case AttackState.Successful:
				{
					Mediator.Send(new SuccessfulAttackRequest()).GetAwaiter().GetResult();
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

		foreach (var playerData in _playerData)
		{
			var nonDiscardedCards = playerData.Value.CardScenes.Where(c => c.CardState != CardState.Discarded).Select(c => $"{c.Card} {c.CardState}");
			var discardedCards = playerData.Value.CardScenes.Where(c => c.CardState == CardState.Discarded).Select(c => $"{c.Card}");
			GD.Print($"{playerData.Key} {string.Join(", ", nonDiscardedCards)}, Discarded: {string.Join(", ", discardedCards)}");
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

		if (Attack.PrincipalAttacker.Id != Player1Id)
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

		//Mediator.Send(new Reset()).GetAwaiter().GetResult();

		GetTree().ReloadCurrentScene();
	}

	private void CurrentAttack_AttackCardAdded(object? sender, AttackCardAddedEventArgs e)
	{
		if (e.Card.Player.Id != Player1Id)
		{
			RearrangePlayerCards(e.Card.Player.Id);
			//GD.Print($"Ending call stack after {e.Card.Player.Id} card added");

			if (Attack.NextToPlay().Id != Player1Id)
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
				var cardScene = _playerData[player.Id].CardScenes.Single(c => c.Card == player.Cards[cardIndex]);

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
		_playerData.Add(playerData.Player.Id, playerData);
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
			3 => "ThreePlayerGame",
			_ => throw new NotImplementedException()
		};
		// todo better skipping than index
		var positions = GetNode<Node3D>($"/root/Main/Table/GameSurface/{nodeName}")
			.GetChildren()
			.Cast<Node3D>()
			.Select(n => n.GlobalPosition);

		foreach (var (globalPosition, opponent) in positions.Skip(1).Zip(players.Skip(1)))
		{
			_playerData.Add(opponent.Id, new PlayerData(opponent, globalPosition, new Vector3(0, 90, 0), []));
		}
	}

	private void CreateTrumpCard(Card card)
	{
		GD.Print($"Trump: {card}");
		var cardScene = _cardScene.Instantiate<CardScene>();
		cardScene.Initialize(card, CardState.InDeck);

		AddChild(cardScene);

		var trumpCard = GetNode<Node3D>("/root/Main/Table/GameSurface/Deck/TrumpCard");

		cardScene.MoveGlobally(trumpCard.GlobalPosition);
		cardScene.RotateDegrees(trumpCard.RotationDegrees);

		cardScene.IsAnimationEnabled = _isAnimationEnabled;
		cardScene.SetPhysicsProcess(false);

		cardScene.AddToGroup("trumpCard");
		cardScene.AddToGroup("card");
	}

	private void CreateTalon()
	{
		var cardScene = _cardScene.Instantiate<CardScene>();

		AddChild(cardScene);

		var talon = GetNode<Node3D>("/root/Main/Table/GameSurface/Deck/Talon");

		cardScene.MoveGlobally(talon.GlobalPosition);
		cardScene.RotateDegrees(talon.RotationDegrees);

		cardScene.IsAnimationEnabled = _isAnimationEnabled;
		cardScene.SetPhysicsProcess(false);

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

		var talon = GetNode<Node3D>("/root/Main/Table/GameSurface/Deck/Talon");
		var playerData = _playerData[((Player)sender!).Id];

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

			if (playerData.Player.Id == Player1Id)
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
					cardScene.RotationDegrees = talon.RotationDegrees;
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
				existingCardScene.MoveGlobally(targetPosition);
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
		cardScene.Initialize(card, CardState.InHand);
		cardScene.IsAnimationEnabled = _isAnimationEnabled;

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

	private void CardScene_Clicked(object? sender, EventArgs e)
	{
		var cardScene = (CardScene)sender!;
		GD.Print($"Received click {cardScene.Card}");

		if (Attack.NextToPlay() != _playerData[Player1Id].Player)
		{
			GD.Print("Ignoring P1 as it is other player's turn");
			return;
		}

		if (!_playerData[Player1Id].CardScenes.Contains(cardScene))
		{
			GD.Print("Ignoring P1 as it is not their card");
			return;
		}

		if (cardScene.CardState != CardState.InHand)
		{
			GD.Print("Ignoring P1 as card is not in their hand");
			return;
		}

		var canPlayResult = Attack.CanPlay(_playerData[Player1Id].Player, cardScene.Card!);

		if (!canPlayResult)
		{
			GD.Print($"Ignoring P1 as: {canPlayResult.Error}");
			return;
		}


		PrintCardScenes("P1 about to play");

		PlaceCardOnTable(cardScene);

		GD.Print($"Playing {cardScene.Card!} as P1");

		Attack.Play(_playerData[Player1Id].Player, cardScene.Card!);

		PrintCardScenes("P1 played");
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
