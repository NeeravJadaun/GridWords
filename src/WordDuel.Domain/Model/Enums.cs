namespace WordDuel.Domain.Model;

public enum Direction
{
    Across = 0,
    Down = 1
}

public enum BonusType
{
    None = 0,
    DoubleLetter = 1,
    TripleLetter = 2,
    DoubleWord = 3,
    TripleWord = 4,
    Start = 5
}

public enum MatchStatus
{
    WaitingForOpponent = 0,
    InProgress = 1,
    Completed = 2
}

public enum MoveType
{
    Place = 0,
    Pass = 1,
    Resign = 2
}

public enum MatchEndReason
{
    None = 0,
    Resignation = 1,
    ThreeConsecutivePasses = 2
}

public enum GameErrorCode
{
    None = 0,
    MatchNotFound = 1,
    MatchFull = 2,
    MatchNotInProgress = 3,
    NotYourTurn = 4,
    OutOfBounds = 5,
    NoTilesPlaced = 6,
    OverlapConflict = 7,
    NotConnected = 8,
    FirstMoveMustCoverStart = 9,
    TilesNotInRack = 10,
    WordNotInDictionary = 11,
    InvalidDirection = 12,
    StaleMatchVersion = 13,
    PlayerNotInMatch = 14,
    NoWordFormed = 15
}
