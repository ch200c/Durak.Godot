using Godot;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Durak.Godot;

public record PlaceCardOnTableRequest(CardScene CardScene) : IRequest;

public class PlaceCardOnTableHandler : IRequestHandler<PlaceCardOnTableRequest>
{
    private readonly ICardPlacement _cardPlacement;
    private readonly IAttackProvider _attackProvider;

    public PlaceCardOnTableHandler(ICardPlacement cardPlacement, IAttackProvider attackProvider)
    {
        _cardPlacement = cardPlacement;
        _attackProvider = attackProvider;
    }

    public Task Handle(PlaceCardOnTableRequest request, CancellationToken cancellationToken)
    {
		var placement = _cardPlacement.GetCardPlacementOnTable();

        request.CardScene.MoveGlobally(placement.GlobalPosition);
        request.CardScene.RotateDegrees(placement.RotationDegrees);

        if (_attackProvider.Attack.IsDefending)
        {
            request.CardScene.GetNode<Sprite3D>("Front").SortingOffset = 1;
        }

        request.CardScene.CardState = CardState.InAttack;
        request.CardScene.GetNode<MeshInstance3D>("MeshInstance3D").Show();
		return Task.CompletedTask;
    }
}