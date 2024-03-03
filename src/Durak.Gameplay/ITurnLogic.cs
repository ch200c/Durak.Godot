namespace Durak.Gameplay;

public interface ITurnLogic
{
    IReadOnlyList<IAttack> Attacks { get; }
    IAttack? NextAttack();
}