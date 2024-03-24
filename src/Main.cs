using Durak.Gameplay;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Durak.Godot;

public partial class Main : Node3D
{
	private readonly PackedScene _cardScene;

	[Export]
	private float _mainPlayerCardDistanceMultiplier;

	[Export]
	private bool _isTopDownView;

	public Main()
	{
		_cardScene = GD.Load<PackedScene>("res://scenes/card.tscn");
		_mainPlayerCardDistanceMultiplier = 0.1f;
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


		var direction = -camera.GlobalTransform.Basis.Z;
		var forceDirection = direction * _mainPlayerCardDistanceMultiplier;
		var position = camera.GlobalPosition + forceDirection;
	

		//card1.GlobalPosition = position;
		//card2.GlobalPosition = position + new Vector3(0.05f,0.05f,0.05f);

		//var twoPlayerGamePositions = GetNode<Node3D>("/root/Main/Table/GameSurface/TwoPlayerGamePositions");
		//foreach ( var opponentPosition in twoPlayerGamePositions.GetChildren().Cast<Node3D>().Skip(1))
		//{
		//	//card2.GlobalPosition = opponentPosition.GlobalPosition;
		//}
		
	
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
}

// ideally x -0.5  y 0.7
// rot        20     90
// diff 0.008  z

// /root/Main/Camera
