using Durak.Gameplay;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Durak.Godot;

public partial class MainScene : Node3D
{
	private const string _mainPlayerCardsGroup = "main_player_cards";
	
	private readonly PackedScene _cardScene;

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
	}

	private void _on_play_button_pressed()
	{
		GetNode<MarginContainer>("%Menu").Hide();

		var opponentCount = GetNode<SpinBox>("%OpponentsSpinBox");
		var players = CreatePlayers((int)opponentCount.Value + 1);

		var deck = new Deck(new FrenchSuited36CardProvider(), new DefaultCardShuffler());
		var turnLogic = new TurnLogic(players, deck.TrumpSuit);

		var dealer = new Dealer(6, players, deck);
		dealer.Deal(null);


		//var playerCards = new Dictionary<Player, List<Card>>();

		//foreach (var player in players)
		//{
		//	var cards = new List<Card>();
		//	foreach (var card in player.Cards)
		//	{
		//		cards.Add(cardScene.Instantiate() as Card);
		//	}
		//	playerCards.Add(player, cards);
		//}

		//var card1 = _cardScene.Instantiate() as Card;

		//AddChild(card1);

		var camera = GetNode<Camera3D>("%Camera");

		if (_isTopDownView)
		{
			camera.GlobalPosition = new Vector3(0, 1.2f, 0);
			camera.RotationDegrees = new Vector3(-90, -90, 0);
		}

		CreateTrumpCard();

		var mainPlayerPosition = GetMainPlayerPosition(camera);

		var mainPlayerData = new PlayerData(players[0], mainPlayerPosition);
		CreateMainPlayerHand(mainPlayerData, camera.RotationDegrees.X);
		CreateOpponentHands(players);
	}

	private Vector3 GetMainPlayerPosition(Camera3D camera)
	{
		var inFrontOfCamera = -camera.GlobalTransform.Basis.Z;
		var distancedInFrontOfCamera = inFrontOfCamera * _mainPlayerCardDistanceMultiplier;
		var lowered = new Vector3(0, -0.1f, 0);
		return camera.GlobalPosition + distancedInFrontOfCamera + lowered;
	}

	private static List<Player> CreatePlayers(int count)
	{
		var players = new List<Player>() { };

		for (var i = 0; i < count; i++)
		{
			players.Add(new Player($"P{i + 1}"));
		}

		return players;
	}

	private void CreateTrumpCard()
	{
		var trumpCard = _cardScene.Instantiate<CardScene>();
		AddChild(trumpCard);

		var talon = GetNode<Node3D>("/root/Main/Table/GameSurface/Talon");

		trumpCard.SyncToPhysics = false;
		trumpCard.RotateX(Mathf.DegToRad(90));
		trumpCard.GlobalPosition = talon.GlobalPosition;
	}

	private void CreateMainPlayerHand(PlayerData playerData, float cameraRotationX)
	{
		foreach (var card in playerData.Player.Cards)
		{
			var cardSceneInstance = _cardScene.Instantiate<CardScene>();
			cardSceneInstance.Initialize(card);
			AddChild(cardSceneInstance);

			cardSceneInstance.SyncToPhysics = false;
			cardSceneInstance.RotationDegrees = new Vector3(cameraRotationX, -90, 0);
			cardSceneInstance.GlobalPosition = playerData.Position;
			cardSceneInstance.GetNode<MeshInstance3D>("MeshInstance3D").Hide();

			cardSceneInstance.AddToGroup(_mainPlayerCardsGroup);
		}

		RearrangeCards(GetTree().GetNodesInGroup(_mainPlayerCardsGroup).Cast<CardScene>().ToList());
	}

	private void RearrangeCards(IList<CardScene> cards)
	{
		if (cards.Count == 0)
		{
			return;
		}

		var increment = _cardWidth + _cardPaddingX;
		var isEven = cards.Count % 2 == 0;
		var middleX = (_minMainPlayerHandX + _maxMainPlayerHandX) / 2.0f;

		var positions = new List<float>();
		if (!isEven)
		{
			positions.Add(middleX);
		}

		var positionLeft = isEven ? middleX - increment / 2 : middleX - increment;
		var positionRight = isEven ? middleX + increment / 2 : middleX + increment;

		for (var i = 0; i < cards.Count / 2; i++, positionLeft -= increment, positionRight += increment)
		{
			positions.Add(positionLeft);
			positions.Add(positionRight);
		}

		foreach (var (card, position) in cards.Zip(positions))
		{
			card.GlobalPosition = new Vector3(card.GlobalPosition.X, card.GlobalPosition.Y, card.GlobalPosition.Z + position);
		}
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
				var cardSceneInstance = _cardScene.Instantiate<CardScene>();
				AddChild(cardSceneInstance);

				cardSceneInstance.SyncToPhysics = false;
				cardSceneInstance.RotateX(Mathf.DegToRad(90));
				cardSceneInstance.GlobalPosition = opponentPosition.GlobalPosition;
			}
		}
	}
}
