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
		var attack = _attackProvider.GetAttack();
		var playerData = _playerDataProvider.GetPlayerData();
        var attackPlayerIds = attack.Attackers.Select(a => a.Id).Union([attack.Defender.Id]);

        foreach (var id in attackPlayerIds)
        {
            var tableCards = playerData[id].CardScenes.Where(c => c.CardState == CardState.InAttack).ToList();

            foreach (var tableCard in tableCards)
            {
                //GD.Print($"Discarding {tableCard.Card}");
                tableCard.CardState = CardState.Discarded;
            }
        }

        return Task.CompletedTask;
    }
}
