using Durak.Gameplay;
using Godot;
using System;
using System.Text;

namespace Durak.Godot;

public partial class CardScene : StaticBody3D
{
	public static readonly Vector3 FaceDownDegrees = new(90, -45, 0);
	public static readonly Vector3 TrumpCardFaceUpDegrees = new(-90, 45, 0);
	public static readonly Vector3 FaceUpDegrees = new(-90, -90, 0);

	public Card? Card { get; private set; }

	public Vector3 TargetPosition { get; set; }

	public Vector3 TargetRotationDegrees { get; set; }

	public event EventHandler? Clicked;

	[Export]
	private float _positionLerpWeight = 0.3f;

	[Export]
	private float _rotationLerpWeight = 0.3f;

	private DateTime _physicsCooldownExpiration = DateTime.UtcNow;

	public void Initialize(Card card)
	{
		Card = card;

		var texture = GetTexture(Card);
		GetNode<Sprite3D>("Front").Texture = texture;
	}

	private static Texture2D GetTexture(Card card)
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
			fileName.Append('s');
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
		if (DateTime.UtcNow >= _physicsCooldownExpiration)
		{
			Position = Position.Lerp(TargetPosition, (float)delta * _positionLerpWeight);
			RotationDegrees = RotationDegrees.Lerp(TargetRotationDegrees, (float)delta * _rotationLerpWeight);
		}
	}

	public override void _InputEvent(Camera3D camera, InputEvent @event, Vector3 position, Vector3 normal, int shapeIdx)
	{
		if (@event.IsActionPressed("left_mouse_button"))
		{
			GD.Print(Card);
			Clicked?.Invoke(this, EventArgs.Empty);
		}
	}

	public void AddPhysicsCooldown(TimeSpan cooldown)
	{
		var cooldownStart = new DateTime(
			Math.Max(DateTime.UtcNow.Ticks, _physicsCooldownExpiration.Ticks), DateTimeKind.Utc);

		_physicsCooldownExpiration = cooldownStart + cooldown;
	}
}
