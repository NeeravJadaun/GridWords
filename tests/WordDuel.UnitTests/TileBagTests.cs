using FluentAssertions;
using WordDuel.Domain.Tiles;
using Xunit;

namespace WordDuel.UnitTests;

public class TileBagTests
{
    [Fact]
    public void SameSeed_ProducesIdenticalDrawSequence()
    {
        var bagA = new TileBag(12345);
        var bagB = new TileBag(12345);

        var drawA = bagA.DrawUpTo(7);
        var drawB = bagB.DrawUpTo(7);

        drawA.Should().Equal(drawB);
    }

    [Fact]
    public void DifferentSeeds_TypicallyProduceDifferentDrawSequences()
    {
        var bagA = new TileBag(1);
        var bagB = new TileBag(2);

        var drawA = bagA.DrawUpTo(7);
        var drawB = bagB.DrawUpTo(7);

        drawA.Should().NotEqual(drawB);
    }

    [Fact]
    public void Reconstruct_MatchesLiveDrawState()
    {
        var live = new TileBag(999);
        live.DrawUpTo(7);
        live.DrawUpTo(7);
        var thirdDraw = live.DrawUpTo(5);

        var reconstructed = TileBag.Reconstruct(999, tilesAlreadyDrawn: 14);
        var reconstructedThirdDraw = reconstructed.DrawUpTo(5);

        reconstructedThirdDraw.Should().Equal(thirdDraw);
    }

    [Fact]
    public void DrawUpTo_NeverExceedsRemainingTiles()
    {
        var bag = new TileBag(42);
        var total = LetterValues.TotalTileCount;

        var draw = bag.DrawUpTo(total + 50);

        draw.Should().HaveCount(total);
        bag.RemainingCount.Should().Be(0);
    }
}
