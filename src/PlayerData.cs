using Durak.Gameplay;
using Godot;
using System.Collections.Generic;

namespace Durak.Godot;

public record PlayerData(Player Player, Vector3 GlobalPosition, Vector3 RotationDegrees, List<CardScene> CardScenes)
{
    public Vector3 GlobalPosition { get; set; } = GlobalPosition;
    public Vector3 RotationDegrees { get; set; } = RotationDegrees;
}