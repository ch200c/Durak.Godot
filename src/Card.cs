using Durak.Gameplay;
using Godot;

namespace Durak.Godot;

public partial class Card : Node
{
	public override void _Ready()
	{
		var t = nameof(FrenchSuited36CardProvider);
		base._Ready();
	}
}
