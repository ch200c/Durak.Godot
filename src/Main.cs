using Durak.Gameplay;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Durak.Godot;

public partial class Main : Node3D
{
	private const string _mainPlayerCardsGroup = "main_player_cards";
	private readonly PackedScene _cardScene;

	[Export]
	private float _mainPlayerCardDistanceMultiplier;

	[Export]
	private bool _isTopDownView;

	public Main()
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

		var awayFromCamera = -camera.GlobalTransform.Basis.Z;
		var adjustedAwayFromCamera = awayFromCamera * _mainPlayerCardDistanceMultiplier;
		var mainPlayerPosition = camera.GlobalPosition + adjustedAwayFromCamera + new Vector3(0,-0.1f,0);

		CreateMainPlayerHand(players, mainPlayerPosition, camera.RotationDegrees.X);
		CreateOpponentHands(players);
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
		var trumpCard = _cardScene.Instantiate<Card>();
		AddChild(trumpCard);

		var talon = GetNode<Node3D>("/root/Main/Table/GameSurface/Talon");

		trumpCard.SyncToPhysics = false;
		trumpCard.RotateX(Mathf.DegToRad(90));
		trumpCard.GlobalPosition = talon.GlobalPosition;
	}

	private void CreateMainPlayerHand(List<Player> players, Vector3 mainPlayerPosition, float cameraRotationX)
	{
		foreach (var card in players[0].Cards)
		{
			var cardSceneInstance = _cardScene.Instantiate<Card>();
			AddChild(cardSceneInstance);

			cardSceneInstance.SyncToPhysics = false;
			cardSceneInstance.RotationDegrees = new Vector3(cameraRotationX, -90, 0);
			cardSceneInstance.GlobalPosition = mainPlayerPosition;
			cardSceneInstance.GetNode<MeshInstance3D>("MeshInstance3D").Hide();

			cardSceneInstance.AddToGroup(_mainPlayerCardsGroup);
		}

		RearrangeCards(GetTree().GetNodesInGroup(_mainPlayerCardsGroup).Cast<Card>().ToList());
	}

	private void RearrangeCards(IList<Card> cards)
	{
		if (cards.Count == 0)
		{ 
			return; 
		}

		var cardWidth = 0.06f;
		var cardPaddingX = 0.005f;
		var minX = -0.3f;
		var maxX = 0.3f;

		var increment = cardWidth + cardPaddingX;
		var isEven = cards.Count % 2 == 0;
		var midX = (minX + maxX) / 2.0f;

		var positions = new List<float>();
		if (!isEven)
		{
			positions.Add(midX);
		}

		var positionLeft = isEven ? midX - increment / 2 : midX - increment;
		var positionRight = isEven ? midX + increment / 2 : midX + increment;


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
				var cardSceneInstance = _cardScene.Instantiate<Card>();
				AddChild(cardSceneInstance);

				cardSceneInstance.SyncToPhysics = false;
				cardSceneInstance.RotateX(Mathf.DegToRad(90));
				cardSceneInstance.GlobalPosition = opponentPosition.GlobalPosition;
			}
		}
	}
}

// ideally x -0.5  y 0.7
// rot        20     90
// diff 0.008  z

// /root/Main/Camera
