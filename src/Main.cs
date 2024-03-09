using Godot;

namespace Durak.Godot;

public partial class Main : Node3D
{
	private void _on_play_button_pressed()
	{
		var node = GetNode<MarginContainer>("/root/Main/Menu");
		node.Hide();
	}
}
