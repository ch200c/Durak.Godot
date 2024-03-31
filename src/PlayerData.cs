using Durak.Gameplay;
using Godot;
using System.Collections.Generic;

namespace Durak.Godot;

public record PlayerData(Player Player, Vector3 GlobalPosition, List<CardScene> CardScenes);

//public record CardData(Card Card);