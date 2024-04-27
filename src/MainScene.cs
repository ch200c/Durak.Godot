using Durak.Gameplay;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Durak.Godot;

public partial class MainScene : Node3D
{
	private const string _mainPlayerCardsGroup = "main_player_cards";
	private int _cardPhysicsCooldownIteration;
	private readonly PackedScene _cardScene;
	private readonly Dictionary<Player, PlayerData> _playerData;

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

    private void _on_play_button_pressed()
	{
		GetNode<MarginContainer>("%Menu").Hide();

		var opponentCount = GetNode<SpinBox>("%OpponentsSpinBox");
		var players = CreatePlayers((int)opponentCount.Value + 1);

		var camera = GetNode<Camera3D>("%Camera");

		AddMainPlayerData(players[0], camera);
		AddOpponentPlayerData(players);

		var deck = new Deck(new FrenchSuited36CardProvider(), new DefaultCardShuffler());
		var turnLogic = new TurnLogic(players, deck.TrumpSuit);

		var dealer = new Dealer(6, players, deck);
		dealer.Deal(null);

		CreateTrumpCard(deck.TrumpCard);
		CreateTalon();

		var nextAttack = turnLogic.NextAttack();
		// todo in a loop, check if it is attacking or defending, all by last card
		// if no last card => attacker[0] turn
		// if last card by any attacker => def turn
		// if last card by defender => second to last card attacker turn

		// after all that determine if main player to wait, otherwise play first possible card,etc
		if (nextAttack.PrincipalAttacker == players[0])
		{
			// wait
		}
		else
		{
			PlayOrPassNonMainPlayer(nextAttack);
		}
	}

	private static bool PlayOrPassNonMainPlayer(IAttack attack)
	{
		var attackerIndex = 0;

		// TODO fix below, wrong
		// todo click on minimap to make it full screen or keyboard shortcut


		if (attack.Cards.Count > 0)
		{
			if (attack.Cards[^1].Player == attack.Defender)
			{
				attackerIndex = attack.Attackers.Select((a, i) => (a, i)).First(p => p.a == attack.Cards[^2].Player).i;
			}
			else
			{
				attackerIndex = attack.Attackers.Select((a, i) => (a, i)).First(p => p.a == attack.Cards[^1].Player).i;
			}
		}

		var attacker = attack.Attackers[attackerIndex];

		var availableCardIndices = new Queue<int>(Enumerable.Range(0, attacker.Cards.Count));
		while (availableCardIndices.TryDequeue(out var cardIndex))
		{
			if (attack.CanPlay(attacker, attacker.Cards[cardIndex]).CanPlay)
			{
				attack.Play(attacker, attacker.Cards[cardIndex]);
				return true;
			}
		}

		attack.End();
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
		var rotationX = GetNode<Camera3D>("%Camera").RotationDegrees.X;
		var playerData = new PlayerData(player, globalPosition, new Vector3(rotationX, -90, 0), []);
		_playerData.Add(playerData.Player, playerData);
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
			2 => "TwoPlayerGamePositions",
			_ => throw new NotImplementedException()
		};

		var positions = GetNode<Node3D>($"/root/Main/Table/GameSurface/{nodeName}")
			.GetChildren()
			.Cast<Node3D>()
			.Select(n => n.GlobalPosition);

		foreach (var (globalPosition, opponent) in positions.Skip(1).Zip(players.Skip(1)))
		{
			_playerData.Add(opponent, new PlayerData(opponent, globalPosition, new Vector3(0, 90, 0), []));
		}
	}

	private void CreateTrumpCard(Card card)
	{
		var cardScene = _cardScene.Instantiate<CardScene>();
		cardScene.Initialize(card);
		cardScene.Clicked += CardScene_Clicked;
		AddChild(cardScene);

		var trumpCard = GetNode<Node3D>("/root/Main/Table/GameSurface/Talon/TrumpCardPosition");

		cardScene.RotationDegrees = CardScene.TrumpCardFaceUpDegrees;
		cardScene.GlobalPosition = trumpCard.GlobalPosition;

		if (_isAnimationEnabled)
		{
			cardScene.TargetRotationDegrees = CardScene.TrumpCardFaceUpDegrees;
			cardScene.TargetPosition = trumpCard.GlobalPosition;
		}

		cardScene.SetPhysicsProcess(_isAnimationEnabled);
	}

	private void CreateTalon()
	{
		var cardScene = _cardScene.Instantiate<CardScene>();

		cardScene.Clicked += CardScene_Clicked;
		AddChild(cardScene);

		var talon = GetNode<Node3D>("/root/Main/Table/GameSurface/Talon/TalonPosition");

		cardScene.RotationDegrees = CardScene.FaceDownDegrees;
		cardScene.GlobalPosition = talon.GlobalPosition;

		if (_isAnimationEnabled)
		{
			cardScene.TargetRotationDegrees = CardScene.FaceDownDegrees;
			cardScene.TargetPosition = talon.GlobalPosition;
		}

		cardScene.SetPhysicsProcess(_isAnimationEnabled);
		cardScene.GetNode<Sprite3D>("Back").SortingOffset = 1;
	}

	private void Player_CardsAdded(object? sender, CardsAddedEventArgs e)
	{
		var talon = GetNode<Node3D>("/root/Main/Table/GameSurface/Talon/TalonPosition");

		foreach (var card in e.Cards)
		{
			var playerData = _playerData[(Player)sender!];
			GD.Print($"{card} added for {playerData.Player.Id}");

			var cardScene = _cardScene.Instantiate<CardScene>();
			cardScene.Initialize(card);
			cardScene.Clicked += CardScene_Clicked;
			AddChild(cardScene);

			cardScene.GetNode<MeshInstance3D>("MeshInstance3D").Hide();

			cardScene.SetPhysicsProcess(_isAnimationEnabled);

			if (_isAnimationEnabled)
			{
				cardScene.RotationDegrees = CardScene.FaceDownDegrees;
				cardScene.TargetRotationDegrees = playerData.RotationDegrees;
				cardScene.GlobalPosition = talon.GlobalPosition;
			}
			else
			{
				cardScene.RotationDegrees = playerData.RotationDegrees;
			}

			playerData.CardScenes.Add(cardScene);
			var offsets = GetCardOffsets(playerData.CardScenes.Count);

			foreach (var (existingCardScene, offset) in playerData.CardScenes.Zip(offsets))
			{
				var targetPosition = playerData.GlobalPosition + offset;

				if (_isAnimationEnabled)
				{
					existingCardScene.TargetPosition = targetPosition;
				}
				else
				{
					existingCardScene.GlobalPosition = targetPosition;
				}
			}

			if (_isAnimationEnabled)
			{
				AddPhysicsCooldown(cardScene);
			}
		}
	}

	private int _flag;

	private void CardScene_Clicked(object? sender, EventArgs e)
	{
		var cardScene = (CardScene)sender!;
		GD.Print("Received ", cardScene.Card);

		if (_flag == 0)
		{
			cardScene.TargetPosition = GetNode<Node3D>("/root/Main/Table/GameSurface/AttackingCard1Position").GlobalPosition;
		}
		else if (_flag == 1)
		{
			cardScene.TargetPosition = GetNode<Node3D>("/root/Main/Table/GameSurface/DefendingCard1Position").GlobalPosition;
			cardScene.GetNode<Sprite3D>("Front").SortingOffset = 1;
		}
		else if (_flag == 2)
		{
			cardScene.TargetPosition = GetNode<Node3D>("/root/Main/Table/GameSurface/AttackingCard2Position").GlobalPosition;
		}
		else if (_flag == 3)
		{
			cardScene.TargetPosition = GetNode<Node3D>("/root/Main/Table/GameSurface/DefendingCard2Position").GlobalPosition;
			cardScene.GetNode<Sprite3D>("Front").SortingOffset = 1;
		}
		else if (_flag == 4)
		{
			cardScene.TargetPosition = GetNode<Node3D>("/root/Main/Table/GameSurface/AttackingCard3Position").GlobalPosition;
		}
		else if (_flag == 5)
		{
			cardScene.TargetPosition = GetNode<Node3D>("/root/Main/Table/GameSurface/DefendingCard3Position").GlobalPosition;
			cardScene.GetNode<Sprite3D>("Front").SortingOffset = 1;
		}
		_flag++;


		cardScene.TargetRotationDegrees = CardScene.FaceUpDegrees;
	}

	//		cardScene.AddToGroup(_mainPlayerCardsGroup);
	//	RearrangeCards(GetTree().GetNodesInGroup(_mainPlayerCardsGroup).Cast<CardScene>().ToList());

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
