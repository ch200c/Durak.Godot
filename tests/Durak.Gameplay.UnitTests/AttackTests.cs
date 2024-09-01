namespace Durak.Gameplay.UnitTests;

public class AttackTests
{
    [Fact]
    public void Construct_PrincipalAttacker_ShouldBeAttackerFromConstructor()
    {
        // Arrange
        var principalAttacker = new Player("P1");

        // Act
        var sut = new Attack(principalAttacker, new Player("P2"), Suit.Spades);

        // Assert
        sut.PrincipalAttacker.Should().Be(principalAttacker);
    }

    [Fact]
    public void Construct_Defender_ShouldBeDefenderFromConstructor()
    {
        // Arrange
        var defender = new Player("P2");

        // Act
        var sut = new Attack(new Player("P1"), defender, Suit.Spades);

        // Assert
        sut.Defender.Should().Be(defender);
    }

    [Fact]
    public void Construct_Attackers_ShouldContainSingleAttackerFromConstructor()
    {
        // Arrange
        var principalAttacker = new Player("P1");

        // Act
        var sut = new Attack(principalAttacker, new Player("P2"), Suit.Spades);

        // Assert
        sut.Attackers.Should().ContainSingle(a => a == principalAttacker);
    }

    [Fact]
    public void AddAttacker_Attackers_ShouldContainAddedAttacker()
    {
        // Arrange
        var sut = new Attack(new Player("P1"), new Player("P2"), Suit.Spades);
        var attacker = new Player("P3");

        // Act
        sut.AddAttacker(attacker);

        // Assert
        sut.Attackers.Should().Contain(attacker);
    }

    [Fact]
    public void Play_FirstCard_CardsShouldContainSinglePlayedCard()
    {
        // Arrange
        var attacker = new Player("P1");
        var firstCard = new Card(2, Suit.Diamonds);
        attacker.PickUp([firstCard]);

        var defender = new Player("P2");
        defender.PickUp([new Card(3, Suit.Diamonds)]);

        var sut = new Attack(attacker, defender, Suit.Diamonds);

        // Act
        sut.Play(attacker, attacker.Cards[0]);

        // Assert
        sut.Cards.Should().ContainSingle(c => c.Card == firstCard && c.Player == attacker);
    }

    [Fact]
    public void Play_DefenderWithNoCards_ShouldThrowGameplayException()
    {
        // Arrange
        var attacker = new Player("P1");
        attacker.PickUp([new Card(3, Suit.Clubs)]);

        var defender = new Player("P2");

        var sut = new Attack(attacker, defender, Suit.Clubs);

        // Act
        var act = () => sut.Play(attacker, attacker.Cards[0]);

        // Assert
        act.Should().Throw<GameplayException>();
    }

    public static IEnumerable<object[]> Data =>
        new List<object[]>
        {
            new object[] { new Card(7, Suit.Clubs), new Card(6, Suit.Clubs) },       // lower rank, same suit, trump
            new object[] { new Card(7, Suit.Clubs), new Card(6, Suit.Diamonds) },    // lower rank, different suit, non-trump
            new object[] { new Card(7, Suit.Diamonds), new Card(6, Suit.Diamonds) }, // lower rank, same suit, non-trump
            new object[] { new Card(7, Suit.Clubs), new Card(8, Suit.Diamonds) }     // higher rank, different suit, non-trump
        };

    [Theory]
    [MemberData(nameof(Data))]
    public void Play_DefendWithInvalidCard_ShouldThrowGameplayException(Card attackerCard, Card defenderCard)
    {
        // Arrange
        var attacker = new Player("P1");
        attacker.PickUp([attackerCard]);

        var defender = new Player("P2");
        defender.PickUp([defenderCard]);

        var sut = new Attack(attacker, defender, Suit.Clubs);
        sut.Play(attacker, attacker.Cards[0]);

        // Act
        var act = () => sut.Play(defender, defender.Cards[0]);

        // Assert
        act.Should().Throw<GameplayException>();
    }

    [Fact]
    public void Play_AttackWithInvalidRankCard_ShouldThrowGameplayException()
    {
        // Arrange
        var attacker = new Player("P1");
        attacker.PickUp([new Card(6, Suit.Clubs), new Card(7, Suit.Clubs)]);

        var defender = new Player("P2");
        defender.PickUp([new Card(10, Suit.Clubs), new Card(11, Suit.Clubs)]);

        var sut = new Attack(attacker, defender, Suit.Spades);

        sut.Play(attacker, attacker.Cards[0]);
        sut.Play(defender, defender.Cards[0]);

        // Act
        var act = () => sut.Play(attacker, attacker.Cards[0]);

        // Assert
        act.Should().Throw<GameplayException>();
    }
}
