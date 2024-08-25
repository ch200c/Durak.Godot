using System.Diagnostics.CodeAnalysis;

namespace Durak.Gameplay;

public interface IDeck
{
    event EventHandler<CardRemovedEventArgs> CardRemoved;

    char TrumpSuit { get; }

    Card TrumpCard { get; }

    int Count { get; }

    bool TryDequeue([MaybeNullWhen(false)] out Card card);
}