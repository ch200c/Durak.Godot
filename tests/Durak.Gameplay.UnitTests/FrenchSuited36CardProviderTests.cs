namespace Durak.Gameplay.UnitTests;

public class FrenchSuited36CardProviderTests
{
    [Fact]
    public void GetCards_ShouldOnlyHaveUniqueCards()
    {
        // Arrange
        var sut = new FrenchSuited36CardProvider();

        // Act
        var cards = sut.GetCards();

        // Assert
        cards.Should().OnlyHaveUniqueItems();
    }
}
