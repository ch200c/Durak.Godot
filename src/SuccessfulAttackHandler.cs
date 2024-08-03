using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Durak.Godot;

public class SuccessfulAttackRequest : IRequest { };

public class SuccessfulAttackHandler : IRequestHandler<SuccessfulAttackRequest>
{
    private readonly IAttackProvider _attackProvider;
    private readonly IPlayerDataProvider _playerDataProvider;

    public SuccessfulAttackHandler(IAttackProvider attackProvider, IPlayerDataProvider playerDataProvider)
    {
        _attackProvider = attackProvider;
        _playerDataProvider = playerDataProvider;
    }

    public Task Handle(SuccessfulAttackRequest request, CancellationToken cancellationToken)
    {
        var attackPlayerIds = _attackProvider.Attack.Attackers.Select(a => a.Id).Union([_attackProvider.Attack.Defender.Id]);

        foreach (var id in attackPlayerIds)
        {
            var tableCards = _playerDataProvider.PlayerData[id].CardScenes.Where(c => c.CardState == CardState.InAttack).ToList();

            foreach (var tableCard in tableCards)
            {
                tableCard.CardState = CardState.Discarded; // todo
            }
        }

        return Task.CompletedTask;
    }
}
