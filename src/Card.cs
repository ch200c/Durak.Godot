using Durak.Gameplay;
using Godot;

namespace Durak.Godot;

public partial class Card : AnimatableBody3D
{
	public override void _Ready()
	{
		var t = nameof(FrenchSuited36CardProvider);
		GD.Print(t);
		base._Ready();
	}
}
