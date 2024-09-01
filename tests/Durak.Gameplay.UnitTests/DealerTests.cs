namespace Durak.Gameplay.UnitTests;

public class DealerTests
{
    [Fact]
    public void Deal_PlayersShouldHaveRequiredPlayerCardCount()
    {
        // Arrange
        var requiredPlayerCardCount = 6;
        var players = new List<Player>() { new("P1") };

        var deck = new Deck(new FrenchSuited36CardProvider(), new DefaultCardShuffler());
        var sut = new Dealer(requiredPlayerCardCount, players, deck);

        // Act
        sut.Deal(null);

        // Assert
        players[0].Cards.Should().HaveCount(requiredPlayerCardCount);
    }
}
