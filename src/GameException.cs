using System;

namespace Durak.Godot;

public class GameException : Exception
{
    public GameException(string? message) : base(message)
    {
    }
}