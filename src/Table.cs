using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Durak.Godot;

public partial class Table : StaticBody3D
{
	private IList<string> _playerIds = [];

	public void Initialize(IList<string> playerIds)
	{
		_playerIds = playerIds;

		var games = GetNode("GameSurface").GetChildren().Cast<Node3D>();
		foreach ( var game in games )
		{
			game.Hide();
		}

		switch (_playerIds.Count)
		{
			case 2:
				{
					GetNode<Node3D>("GameSurface/TwoPlayerGame").Show();
					GetNode<Label3D>("GameSurface/TwoPlayerGame/Player2Label").Text = _playerIds[1];
					break; 
				}
			case 3:
				{
					GetNode<Node3D>("GameSurface/ThreePlayerGame").Show();
					GetNode<Label3D>("GameSurface/ThreePlayerGame/Player2Label").Text = _playerIds[1];
					GetNode<Label3D>("GameSurface/ThreePlayerGame/Player3Label").Text = _playerIds[2]; 
					break;
				}
			case 4:
				{
					GetNode<Node3D>("GameSurface/FourPlayerGame").Show();
					GetNode<Label3D>("GameSurface/FourPlayerGame/Player2Label").Text = _playerIds[1];
					GetNode<Label3D>("GameSurface/FourPlayerGame/Player3Label").Text = _playerIds[2];
					GetNode<Label3D>("GameSurface/FourPlayerGame/Player4Label").Text = _playerIds[3];
					break;
				}
			case 5:
				{
					GetNode<Node3D>("GameSurface/FivePlayerGame").Show();
					GetNode<Label3D>("GameSurface/FivePlayerGame/Player2Label").Text = _playerIds[1];
					GetNode<Label3D>("GameSurface/FivePlayerGame/Player3Label").Text = _playerIds[2];
					GetNode<Label3D>("GameSurface/FivePlayerGame/Player4Label").Text = _playerIds[3];
					GetNode<Label3D>("GameSurface/FivePlayerGame/Player5Label").Text = _playerIds[4];
					break;
				}
			case 6:
				{
					GetNode<Node3D>("GameSurface/SixPlayerGame").Show();
					GetNode<Label3D>("GameSurface/SixPlayerGame/Player2Label").Text = _playerIds[1];
					GetNode<Label3D>("GameSurface/SixPlayerGame/Player3Label").Text = _playerIds[2];
					GetNode<Label3D>("GameSurface/SixPlayerGame/Player4Label").Text = _playerIds[3];
					GetNode<Label3D>("GameSurface/SixPlayerGame/Player5Label").Text = _playerIds[4];
					GetNode<Label3D>("GameSurface/SixPlayerGame/Player6Label").Text = _playerIds[5];
					break;
				}
			default:
				{
					throw new GameException("Cannot have other than 2-6 player count");
				}
		}
	}

	public IEnumerable<Path3D> GetPaths()
	{
		var nodeName = _playerIds.Count switch
		{
			2 => "TwoPlayerGame",
			3 => "ThreePlayerGame",
			4 => "FourPlayerGame",
			5 => "FivePlayerGame",
			6 => "SixPlayerGame",
			_ => throw new GameException("Cannot have other than 2-6 count")
		};

		return GetNode($"GameSurface/{nodeName}")
			.GetChildren()
			.OfType<Path3D>();
	}

	public Node3D GetCardPlacementOnTable(int cardsInAttackCount)
	{
		var path = new StringBuilder("GameSurface/AttackingAndDefending/");
		var position = cardsInAttackCount switch
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
}
