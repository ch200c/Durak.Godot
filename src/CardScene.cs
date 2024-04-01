using Durak.Gameplay;
using Godot;
using System;
using System.Text;

namespace Durak.Godot;

public partial class CardScene : StaticBody3D
{
	public static Vector3 FaceDownDegrees = new Vector3(90, 0, 0);
	//public static Vector3 FaceUpDegrees = new Vector3(90, -90, 0);

	public Card? Card { get; private set; }

	public Vector3 TargetPosition { get; set; }

	public Vector3 TargetRotationDegrees { get; set; }

	[Export]
	private float _positionLerpWeight;

	[Export]
	private float _rotationLerpWeight;

	[Export]
	private  long _cooldownMs = 100;

	private  DateTime _cooldownExpiration = DateTime.UtcNow;

	public CardScene()
	{
		_positionLerpWeight = 0.3f;
		_rotationLerpWeight = 0.3f;
		//_cooldownMs = 1_000;
	}

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

	public override void _PhysicsProcess(double delta)
	{
		if (DateTime.UtcNow >= _cooldownExpiration)
		{
			Position = Position.Lerp(TargetPosition, (float)delta * _positionLerpWeight);
			RotationDegrees = RotationDegrees.Lerp(TargetRotationDegrees, (float)delta * _rotationLerpWeight);
		}
	}

	public void AddToTargetPositionCooldown(int cooldownMs)
	{
		var cooldownStart = new DateTime(
			Math.Max(DateTime.UtcNow.Ticks, _cooldownExpiration.Ticks), DateTimeKind.Utc);

		_cooldownExpiration = cooldownStart + TimeSpan.FromMilliseconds(cooldownMs);

		GD.Print(Card, " ", _cooldownExpiration.ToString("HH:mm:ss:fff"));
	}
}
