using Godot;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Durak.Godot;

public class BeatenOffAttackRequest : IRequest { };

public class BeatenOffAttackHandler : IRequestHandler<BeatenOffAttackRequest>
{
	private readonly IDiscardPileProvider _discardPileProvider;
	private readonly IAttackProvider _attackProvider;
	private readonly IPlayerDataProvider _playerDataProvider;
	
	public BeatenOffAttackHandler(
		IDiscardPileProvider discardPileProvider, IAttackProvider attackProvider, IPlayerDataProvider playerDataProvider)
	{
		_discardPileProvider = discardPileProvider;
		_attackProvider = attackProvider;
		_playerDataProvider = playerDataProvider;
	}

	public Task Handle(BeatenOffAttackRequest request, CancellationToken cancellationToken)
	{
		GD.Print(nameof(BeatenOffAttackHandler), "handling started");

		var attack = _attackProvider.GetAttack();
		var attackPlayerIds = attack.Attackers.Select(a => a.Id).Union([attack.Defender.Id]);
		var playerData = _playerDataProvider.GetPlayerData();
		var discardPile = _discardPileProvider.GetDiscardPile();

        foreach (var id in attackPlayerIds)
		{
			var tableCards = playerData[id].CardScenes.Where(c => c.CardState == CardState.InAttack).ToList();

			foreach (var tableCard in tableCards)
			{
				GD.Print($"Discarding {tableCard.Card}");

				tableCard.MoveGlobally(discardPile.GlobalPosition);
				tableCard.RotateDegrees(discardPile.RotationDegrees);
				tableCard.CardState = CardState.Discarded;
                playerData[id].CardScenes.Remove(tableCard);
            }
		}

		GD.Print(nameof(BeatenOffAttackHandler), "handling complete");
		return Task.CompletedTask;
	}
}
