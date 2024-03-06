namespace Durak.Gameplay;

public interface IDealer
{
    bool Deal(IAttack? previousAttack);
}