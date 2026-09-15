using FluentAssertions;
using WordDuel.Domain.Board;
using WordDuel.Domain.Model;
using WordDuel.Domain.Rules;
using WordDuel.Domain.Tiles;
using WordDuel.Domain.WordList;
using Xunit;

namespace WordDuel.UnitTests;

public class MoveProcessorTests
{
    private static readonly WordListProvider WordList = new();

    [Fact]
    public void FirstMove_CoveringCenter_ScoresWithStartBonus()
    {
        var board = new GameBoard();
        var rack = new Rack(new[] { 'C', 'A', 'T', 'X', 'X', 'X', 'X' });
        var request = new PlacementRequest(3, 1, Direction.Across, new List<char> { 'C', 'A', 'T' });

        var result = MoveProcessor.ProcessPlacement(board, rack, request, isFirstMoveOfMatch: true, WordList);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WordsFormed.Should().ContainSingle(w => w.Word == "CAT");
        // C(4)+A(1)+T(1) = 6 letter sum, x2 for the Start (double word) square.
        result.Value.TotalScore.Should().Be(12);
        result.Value.UsedFullRackBonus.Should().BeFalse();
    }

    [Fact]
    public void FirstMove_NotCoveringCenter_IsRejected()
    {
        var board = new GameBoard();
        var rack = new Rack(new[] { 'C', 'A', 'T', 'X', 'X', 'X', 'X' });
        var request = new PlacementRequest(0, 0, Direction.Across, new List<char> { 'C', 'A', 'T' });

        var result = MoveProcessor.ProcessPlacement(board, rack, request, isFirstMoveOfMatch: true, WordList);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(GameErrorCode.FirstMoveMustCoverStart);
    }

    [Fact]
    public void Placement_OutOfBounds_IsRejected()
    {
        var board = new GameBoard();
        var rack = new Rack(new[] { 'C', 'A', 'T', 'S', 'X', 'X', 'X' });
        // Starting at col 5 across with 3 letters would end at col 7 (out of a 0..6 board).
        var request = new PlacementRequest(3, 5, Direction.Across, new List<char> { 'C', 'A', 'T' });

        var result = MoveProcessor.ProcessPlacement(board, rack, request, isFirstMoveOfMatch: true, WordList);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(GameErrorCode.OutOfBounds);
    }

    [Fact]
    public void Placement_WordNotInDictionary_IsRejected()
    {
        var board = new GameBoard();
        var rack = new Rack(new[] { 'Z', 'Z', 'Q', 'X', 'X', 'X', 'X' });
        var request = new PlacementRequest(3, 2, Direction.Across, new List<char> { 'Z', 'Z', 'Q' });

        var result = MoveProcessor.ProcessPlacement(board, rack, request, isFirstMoveOfMatch: true, WordList);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(GameErrorCode.WordNotInDictionary);
    }

    [Fact]
    public void Placement_TilesNotInRack_IsRejected()
    {
        var board = new GameBoard();
        var rack = new Rack(new[] { 'C', 'A', 'X', 'X', 'X', 'X', 'X' }); // no 'T'
        var request = new PlacementRequest(3, 1, Direction.Across, new List<char> { 'C', 'A', 'T' });

        var result = MoveProcessor.ProcessPlacement(board, rack, request, isFirstMoveOfMatch: true, WordList);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(GameErrorCode.TilesNotInRack);
    }

    [Fact]
    public void SecondMove_NotConnectedToBoard_IsRejected()
    {
        var board = new GameBoard();
        board.SetLetter(3, 1, 'C');
        board.SetLetter(3, 2, 'A');
        board.SetLetter(3, 3, 'T');

        var rack = new Rack(new[] { 'D', 'O', 'G', 'X', 'X', 'X', 'X' });
        // Placed far away from the existing CAT run — should be rejected.
        var request = new PlacementRequest(0, 0, Direction.Across, new List<char> { 'D', 'O', 'G' });

        var result = MoveProcessor.ProcessPlacement(board, rack, request, isFirstMoveOfMatch: false, WordList);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(GameErrorCode.NotConnected);
    }

    [Fact]
    public void SecondMove_CrossingExistingWord_FormsBothWords()
    {
        var board = new GameBoard();
        board.SetLetter(3, 1, 'C');
        board.SetLetter(3, 2, 'A');
        board.SetLetter(3, 3, 'T'); // "CAT" across, T sits on the Start square.

        // Place "TIN" going down through the existing 'T' at (3,3).
        var rack = new Rack(new[] { 'T', 'I', 'N', 'X', 'X', 'X', 'X' });
        var request = new PlacementRequest(3, 3, Direction.Down, new List<char> { 'T', 'I', 'N' });

        var result = MoveProcessor.ProcessPlacement(board, rack, request, isFirstMoveOfMatch: false, WordList);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WordsFormed.Select(w => w.Word).Should().Contain("TIN");
        result.Value.NewlyPlacedTiles.Should().HaveCount(2); // I and N are new; T already existed.
    }

    [Fact]
    public void Placement_OverlapConflict_IsRejected()
    {
        var board = new GameBoard();
        board.SetLetter(3, 1, 'C');
        board.SetLetter(3, 2, 'A');
        board.SetLetter(3, 3, 'T');

        var rack = new Rack(new[] { 'C', 'O', 'T', 'X', 'X', 'X', 'X' });
        // Claims 'O' sits where the board actually has 'A' — overlap conflict.
        var request = new PlacementRequest(3, 1, Direction.Across, new List<char> { 'C', 'O', 'T' });

        var result = MoveProcessor.ProcessPlacement(board, rack, request, isFirstMoveOfMatch: false, WordList);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(GameErrorCode.OverlapConflict);
    }

    [Fact]
    public void Placement_NoNewTiles_IsRejected()
    {
        var board = new GameBoard();
        board.SetLetter(3, 1, 'C');
        board.SetLetter(3, 2, 'A');
        board.SetLetter(3, 3, 'T');

        var rack = new Rack(new[] { 'X', 'X', 'X', 'X', 'X', 'X', 'X' });
        var request = new PlacementRequest(3, 1, Direction.Across, new List<char> { 'C', 'A', 'T' });

        var result = MoveProcessor.ProcessPlacement(board, rack, request, isFirstMoveOfMatch: false, WordList);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(GameErrorCode.NoTilesPlaced);
    }

    [Fact]
    public void Placement_UsingFullRack_AwardsSweepBonus()
    {
        var board = new GameBoard();
        // 7-letter word must be a bundled dictionary word and fit the 7x7 board exactly.
        var rack = new Rack(new[] { 'T', 'R', 'I', 'V', 'I', 'A', 'X' });
        var request = new PlacementRequest(3, 0, Direction.Across, "TRIVIA".ToCharArray());

        var result = MoveProcessor.ProcessPlacement(board, rack, request, isFirstMoveOfMatch: true, WordList);

        // 6-letter word only uses 6 tiles, not the full 7-tile rack — no bonus expected.
        result.IsSuccess.Should().BeTrue();
        result.Value!.UsedFullRackBonus.Should().BeFalse();
    }

    [Fact]
    public void Placement_UsingAllSevenRackTiles_AwardsFullRackBonus()
    {
        var board = new GameBoard();
        // "PICTURE" is 7 letters, a bundled dictionary word, and fits the board exactly.
        var rack = new Rack("PICTURE".ToCharArray());
        var request = new PlacementRequest(3, 0, Direction.Across, "PICTURE".ToCharArray());

        var result = MoveProcessor.ProcessPlacement(board, rack, request, isFirstMoveOfMatch: true, WordList);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UsedFullRackBonus.Should().BeTrue();
        result.Value.TotalScore.Should().Be(result.Value.WordsFormed.Sum(w => w.Points) + MoveProcessor.FullRackBonusPoints);
    }

    [Fact]
    public void Placement_OnTripleLetterSquare_MultipliesOnlyThatLettersValue()
    {
        var board = new GameBoard();

        // First move: "CAT" across row 3, cols 1-3 (col3 = center/start square).
        var firstRack = new Rack(new[] { 'C', 'A', 'T', 'X', 'X', 'X', 'X' });
        var firstRequest = new PlacementRequest(3, 1, Direction.Across, new List<char> { 'C', 'A', 'T' });
        var firstResult = MoveProcessor.ProcessPlacement(board, firstRack, firstRequest, isFirstMoveOfMatch: true, WordList);
        firstResult.IsSuccess.Should().BeTrue();
        foreach (var tile in firstResult.Value!.NewlyPlacedTiles)
        {
            board.SetLetter(tile.Row, tile.Col, tile.Letter);
        }

        // Second move: "TEA" down through column 2, ending at the existing 'A' at (3,2).
        // (2,2) is a triple-letter square per BoardLayout's "..t.t.." row.
        var secondRack = new Rack(new[] { 'T', 'E', 'X', 'X', 'X', 'X', 'X' });
        var secondRequest = new PlacementRequest(1, 2, Direction.Down, new List<char> { 'T', 'E', 'A' });
        var secondResult = MoveProcessor.ProcessPlacement(board, secondRack, secondRequest, isFirstMoveOfMatch: false, WordList);

        secondResult.IsSuccess.Should().BeTrue();
        var teaWord = secondResult.Value!.WordsFormed.Should().ContainSingle(w => w.Word == "TEA").Subject;
        // T(1) + E(1)*3 [triple letter at (2,2)] + A(1, pre-existing, no bonus) = 5.
        teaWord.Points.Should().Be(5);
    }

    [Fact]
    public void FirstMove_SingleTileAtCenter_FormsNoWord()
    {
        var board = new GameBoard();
        var rack = new Rack(new[] { 'A', 'X', 'X', 'X', 'X', 'X', 'X' });
        var request = new PlacementRequest(3, 3, Direction.Across, new List<char> { 'A' });

        var result = MoveProcessor.ProcessPlacement(board, rack, request, isFirstMoveOfMatch: true, WordList);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(GameErrorCode.NoWordFormed);
    }
}
