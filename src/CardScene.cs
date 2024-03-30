using Durak.Gameplay;
using Godot;
using System.Text;

namespace Durak.Godot;

public partial class CardScene : AnimatableBody3D
{
	public Card? Card { get; private set; }

	public void Initialize(Card card)
	{
		Card = card;

		var texture = GetTexture(Card);
		GetNode<Sprite3D>("Front").Texture = texture;
	}

	private Texture2D GetTexture(Card card)
	{
		var fileName = new StringBuilder("res://art/cards/fronts/");

		if (card.Suit == Suit.Clubs)
		{
			fileName.Append('c');
		}
		else if (card.Suit == Suit.Diamonds)
		{
			fileName.Append('d');
		}
		else if (card.Suit == Suit.Hearts)
		{
			fileName.Append('h');
		}
		else if (card.Suit == Suit.Spades)
		{
			fileName.Append('d');
		}
		else
		{
			throw new GameException("TODO");
		}

		var normalizedRank = card.Rank == 14 ? 1 : card.Rank;
		var serializedRank = $"{normalizedRank:00}";
		fileName.Append($"{serializedRank}.png");

		return GD.Load<Texture2D>(fileName.ToString());
	}
}
