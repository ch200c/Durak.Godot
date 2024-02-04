namespace Durak.Gameplay.UnitTests;

public class DealerTests
{
    [Fact]
    public void Deal_PlayersShouldHaveRequiredPlayerCardCount()
    {
        // Arrange
        var requiredPlayerCardCount = 6;
        var players = new List<Player>() { new() };

        var deck = Substitute.For<IDeck>();

        deck
            .TryDequeue(out Arg.Any<Card?>())
            .Returns(x =>
            {
                x[0] = new Card(1, ' ');
                return true;
            });

        var sut = new Dealer(requiredPlayerCardCount, players, deck);

        // Act
        sut.Deal();

        // Assert
        sut.PlayerCards[0].Cards.Should().HaveCount(requiredPlayerCardCount);
    }
}