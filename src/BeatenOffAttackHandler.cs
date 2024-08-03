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

		var attackPlayerIds = _attackProvider.Attack.Attackers.Select(a => a.Id).Union([_attackProvider.Attack.Defender.Id]);

        foreach (var id in attackPlayerIds)
		{
			var tableCards = _playerDataProvider.PlayerData[id].CardScenes.Where(c => c.CardState == CardState.InAttack).ToList();

			foreach (var tableCard in tableCards)
			{
				GD.Print($"Discarding {tableCard.Card}");

				tableCard.MoveGlobally(_discardPileProvider.DiscardPile.GlobalPosition);
				tableCard.RotateDegrees(_discardPileProvider.DiscardPile.RotationDegrees);
				tableCard.CardState = CardState.Discarded;
                _playerDataProvider.PlayerData[id].CardScenes.Remove(tableCard);
            }
		}

		GD.Print(nameof(BeatenOffAttackHandler), "handling complete");
		return Task.CompletedTask;
	}
}
