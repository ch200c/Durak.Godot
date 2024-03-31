using Durak.Gameplay;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Durak.Godot;

public partial class MainScene : Node3D
{
	private const string _mainPlayerCardsGroup = "main_player_cards";
	
	private readonly PackedScene _cardScene;
	private Dictionary<Player, PlayerData> _playerData;

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

	public MainScene()
	{
		_cardScene = GD.Load<PackedScene>("res://scenes/card.tscn");
		_mainPlayerCardDistanceMultiplier = 0.17f;
		_playerData = [];
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
		var mainPlayerData = new PlayerData(players[0], mainPlayerPosition, []);
		_playerData.Add(mainPlayerData.Player, mainPlayerData);

		var twoPlayerGamePositions = GetNode<Node3D>("/root/Main/Table/GameSurface/TwoPlayerGamePositions")
			.GetChildren()
			.Cast<Node3D>()
			.Select(n => n.GlobalPosition);

		foreach (var (opponentGlobalPosition, opponent) in twoPlayerGamePositions.Skip(1).Zip(players.Skip(1)))
		{
			_playerData.Add(opponent, new PlayerData(opponent, opponentGlobalPosition, []));
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


	private int _cooldownIteration = 0;

	private void Player_CardsAdded(object? sender, CardsAddedEventArgs e)
	{
		var rotationX = GetNode<Camera3D>("%Camera").RotationDegrees.X;
		var talon = GetNode<Node3D>("/root/Main/Table/GameSurface/Talon");

		foreach (var card in e.Cards)
		{
			var playerData = _playerData[(Player)sender!];
			GD.Print($"{card} added for {playerData.Player.Id}");
			

			var cardScene = _cardScene.Instantiate<CardScene>();
			cardScene.Initialize(card);
			
			AddChild(cardScene);
			
			cardScene.SyncToPhysics = false;
			cardScene.RotationDegrees = new Vector3(rotationX, -90, 0);
			cardScene.GetNode<MeshInstance3D>("MeshInstance3D").Hide();
			cardScene.GlobalPosition = talon.GlobalPosition;
	 

			cardScene.SyncToPhysics = true;

			var offsets = GetCardOffsets(playerData.CardScenes.Count);

			cardScene.TargetPosition = playerData.GlobalPosition;
			playerData.CardScenes.Add(cardScene);

			if (_cooldownIteration != 0) 
			{
				cardScene.AddToTargetPositionCooldown(50 * _cooldownIteration); 
			}
				
			_cooldownIteration++;
			_cooldownIteration %= 10;
		}
	}

	private void CreateTrumpCard(Card card)
	{
		var cardScene = _cardScene.Instantiate<CardScene>();
		cardScene.Initialize(card);
		AddChild(cardScene);

		var talon = GetNode<Node3D>("/root/Main/Table/GameSurface/Talon");

		cardScene.SyncToPhysics = false;
		cardScene.RotateX(Mathf.DegToRad(-90));
		cardScene.GlobalPosition = talon.GlobalPosition;
	}

	//private void CreateMainPlayerHand(PlayerData playerData, float cameraRotationX)
	//{
	//	foreach (var card in playerData.Player.Cards)
	//	{
	//		var cardScene = _cardScene.Instantiate<CardScene>();
	//		cardScene.Initialize(card);
	//		AddChild(cardScene);

	//		cardScene.SyncToPhysics = false;
	//		cardScene.RotationDegrees = new Vector3(cameraRotationX, -90, 0);
	//		cardScene.GlobalPosition = playerData.GlobalPosition;
	//		cardScene.GetNode<MeshInstance3D>("MeshInstance3D").Hide();

	//		cardScene.AddToGroup(_mainPlayerCardsGroup);
	//	}

	//	RearrangeCards(GetTree().GetNodesInGroup(_mainPlayerCardsGroup).Cast<CardScene>().ToList());
	//}

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

		var positionLeft = isEven ? middleX - increment / 2 : middleX - increment;
		var positionRight = isEven ? middleX + increment / 2 : middleX + increment;

		for (var i = 0; i < count / 2; i++, positionLeft -= increment, positionRight += increment)
		{
			positions.Add(positionLeft);
			positions.Add(positionRight);
		}


		return positions.Select(p => new Vector3(0, 0, p));
	}

	private void CreateOpponentHands(List<Player> players)
	{
		var twoPlayerGamePositions = GetNode<Node3D>("/root/Main/Table/GameSurface/TwoPlayerGamePositions")
			.GetChildren()
			.Cast<Node3D>();

		foreach (var (opponentPosition, opponent) in twoPlayerGamePositions.Skip(1).Zip(players.Skip(1)))
		{
			foreach (var card in opponent.Cards)
			{
				var cardScene = _cardScene.Instantiate<CardScene>();
				cardScene.Initialize(card);
				AddChild(cardScene);

				cardScene.SyncToPhysics = false;
				cardScene.RotateX(Mathf.DegToRad(90));
				cardScene.GlobalPosition = opponentPosition.GlobalPosition;
			}
		}
	}
}
