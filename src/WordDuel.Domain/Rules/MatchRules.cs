namespace WordDuel.Domain.Rules;

/// <summary>Small, pure rules governing turn order and match completion.</summary>
public static class MatchRules
{
    public const int PassesToEndMatch = 3;

    public static int OtherSeat(int seat) => seat == 0 ? 1 : 0;

    public static bool ShouldEndDueToPasses(int consecutivePasses) => consecutivePasses >= PassesToEndMatch;

    /// <summary>Returns the winning seat (0/1), or null for a tie.</summary>
    public static int? DetermineWinnerSeat(int seat0Score, int seat1Score) =>
        seat0Score == seat1Score ? null : seat0Score > seat1Score ? 0 : 1;
}

/// <summary>Derives a deterministic tile-bag seed from a match's identity.</summary>
public static class BoardSeedFactory
{
    public static int FromMatchId(Guid matchId) => BitConverter.ToInt32(matchId.ToByteArray(), 0);
}
