using Godot;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Durak.Godot;

public record RearrangePlayerCardsRequest(string PlayerId) : IRequest;

public class RearrangePlayerCardsHandler : IRequestHandler<RearrangePlayerCardsRequest>
{
    private readonly IPlayerDataProvider _playerDataProvider;
    private readonly ICardOffsetsCalculator _cardOffsetsCalculator;

    public RearrangePlayerCardsHandler(IPlayerDataProvider playerDataProvider, ICardOffsetsCalculator cardOffsetsCalculator)
    {
        _playerDataProvider = playerDataProvider;
        _cardOffsetsCalculator = cardOffsetsCalculator;
    }

    public Task Handle(RearrangePlayerCardsRequest request, CancellationToken cancellationToken)
    {
        var inHandCards = _playerDataProvider.PlayerData[request.PlayerId].CardScenes.Where(c => c.CardState == CardState.InHand).ToList();
        GD.Print($"Rearranging {request.PlayerId} cards: {string.Join(',', inHandCards.Select(c => c.Card))}");

        var cardOffsets = _cardOffsetsCalculator.GetCardOffsets(inHandCards.Count);

        foreach (var (cardScene, offset) in inHandCards.Zip(cardOffsets))
        {
            var targetPosition = _playerDataProvider.PlayerData[request.PlayerId].GlobalPosition + offset;

            cardScene.TargetPosition = targetPosition;
            cardScene.GlobalPosition = targetPosition;

            cardScene.TargetRotationDegrees = _playerDataProvider.PlayerData[request.PlayerId].RotationDegrees;
            cardScene.RotationDegrees = _playerDataProvider.PlayerData[request.PlayerId].RotationDegrees;
        }

        return Task.CompletedTask;
    }
}