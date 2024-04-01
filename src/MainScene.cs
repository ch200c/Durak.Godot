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
	private float _mainPlayerCardDistanceMultiplier;

	[Export]
	private bool _isTopDownView;

	[Export]
	private float _cardPhysicsCooldownMs;

	[Export]
	private float _maxCardPhysicsCooldownMs;

	public MainScene()
	{
		_cardScene = GD.Load<PackedScene>("res://scenes/card.tscn");
		_mainPlayerCardDistanceMultiplier = 0.17f;
		_playerData = [];
		_cardPhysicsCooldownIteration = 0;
		_cardPhysicsCooldownMs = 50;
		_maxCardPhysicsCooldownMs = 1_250;
	}

	private void _on_play_button_pressed()
	{
		GetNode<MarginContainer>("%Menu").Hide();

		var opponentCount = GetNode<SpinBox>("%OpponentsSpinBox");
		var players = CreatePlayers((int)opponentCount.Value + 1);

		var camera = GetNode<Camera3D>("%Camera");

		if (_isTopDownView)
		{
			camera.GlobalPosition = new Vector3(0, 1.2f, 0);
			camera.RotationDegrees = new Vector3(-90, -90, 0);
		}

		var mainPlayerPosition = GetMainPlayerGlobalPosition(camera);
		var rotationX = GetNode<Camera3D>("%Camera").RotationDegrees.X;
		var mainPlayerData = new PlayerData(players[0], mainPlayerPosition, new Vector3(rotationX, -90, 0), []);
		_playerData.Add(mainPlayerData.Player, mainPlayerData);

		var twoPlayerGamePositions = GetNode<Node3D>("/root/Main/Table/GameSurface/TwoPlayerGamePositions")
			.GetChildren()
			.Cast<Node3D>()
			.Select(n => n.GlobalPosition);

		foreach (var (opponentGlobalPosition, opponent) in twoPlayerGamePositions.Skip(1).Zip(players.Skip(1)))
		{
			_playerData.Add(opponent, new PlayerData(opponent, opponentGlobalPosition, new Vector3(0, 90, 0), []));
		}

		GD.Print($"Player data count {_playerData.Count}");

		var deck = new Deck(new FrenchSuited36CardProvider(), new DefaultCardShuffler());
		var turnLogic = new TurnLogic(players, deck.TrumpSuit);

		var dealer = new Dealer(6, players, deck);
		dealer.Deal(null);

		CreateTrumpCard(deck.TrumpCard);
	}

	private Vector3 GetMainPlayerGlobalPosition(Camera3D camera)
	{
		var inFrontOfCamera = -camera.GlobalTransform.Basis.Z;
		var distancedInFrontOfCamera = inFrontOfCamera * _mainPlayerCardDistanceMultiplier;
		var lowered = new Vector3(0, -0.1f, 0);
		return camera.GlobalPosition + distancedInFrontOfCamera + lowered;
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

	private void Player_CardsAdded(object? sender, CardsAddedEventArgs e)
	{
		var talon = GetNode<Node3D>("/root/Main/Table/GameSurface/Talon");

		foreach (var card in e.Cards)
		{
			var playerData = _playerData[(Player)sender!];
			GD.Print($"{card} added for {playerData.Player.Id}");

			var cardScene = _cardScene.Instantiate<CardScene>();
			cardScene.Initialize(card);

			AddChild(cardScene);

			cardScene.RotationDegrees = CardScene.FaceDownDegrees;
			cardScene.GetNode<MeshInstance3D>("MeshInstance3D").Hide();
			cardScene.GlobalPosition = talon.GlobalPosition;
			cardScene.TargetRotationDegrees = playerData.RotationDegrees;
			playerData.CardScenes.Add(cardScene);

			var offsets = GetCardOffsets(playerData.CardScenes.Count);
			foreach (var (existingCardScene, offset) in playerData.CardScenes.Zip(offsets))
			{
				existingCardScene.TargetPosition = playerData.GlobalPosition + offset;
			}

			var cooldown = TimeSpan.FromMilliseconds(_cardPhysicsCooldownMs * _cardPhysicsCooldownIteration);
			cardScene.AddPhysicsCooldown(cooldown);

			_cardPhysicsCooldownIteration++;

			if (_cardPhysicsCooldownMs * _cardPhysicsCooldownIteration > _maxCardPhysicsCooldownMs)
			{
				_cardPhysicsCooldownIteration = 0;
			}
		}
	}

	private void CreateTrumpCard(Card card)
	{
		var cardScene = _cardScene.Instantiate<CardScene>();
		cardScene.Initialize(card);
		AddChild(cardScene);

		var talon = GetNode<Node3D>("/root/Main/Table/GameSurface/Talon");

		cardScene.RotationDegrees = CardScene.FaceDownDegrees;
		cardScene.TargetRotationDegrees = CardScene.FaceDownDegrees;
		cardScene.GlobalPosition = talon.GlobalPosition;
		cardScene.TargetPosition = talon.GlobalPosition;

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
}