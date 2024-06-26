﻿using System.Diagnostics.CodeAnalysis;

namespace Durak.Gameplay;

public interface IDeck
{
    char TrumpSuit { get; }

    Card TrumpCard { get; }

    int Count { get; }

    bool TryDequeue([MaybeNullWhen(false)] out Card card);
}