using System.Diagnostics.CodeAnalysis;

namespace Durak.Gameplay;

public interface ITurnLogic
{
    IReadOnlyList<IAttack> Attacks { get; }

    [Obsolete($"Use {nameof(TryGetNextAttack)}")]
    IAttack? NextAttack();

    bool TryGetNextAttack([MaybeNullWhen(false)] out IAttack nextAttack);
}