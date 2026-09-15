// Mirrors the JSON contracts in WordDuel.Api/Dtos/MatchDtos.cs.
// Keep this file in sync with the server DTOs by hand — there is no shared
// schema generation step in this project.

export type MatchStatus = 'WaitingForOpponent' | 'InProgress' | 'Completed'
export type MatchEndReason = 'None' | 'Resignation' | 'ThreeConsecutivePasses'
export type Direction = 'Across' | 'Down'
export type MoveType = 'Place' | 'Pass' | 'Resign'

export interface PlayerPublic {
  playerId: string
  seat: 0 | 1
  displayName: string
  score: number
  rackTileCount: number
  hasResigned: boolean
}

export interface MatchSnapshot {
  matchId: string
  status: MatchStatus
  version: number
  board: string // 49-char flat string, row-major, '.' = empty
  players: PlayerPublic[]
  currentTurnSeat: 0 | 1
  consecutivePasses: number
  endReason: MatchEndReason
  winnerSeat: 0 | 1 | null
  moveCount: number
  tilesRemainingInBag: number
  createdAt: string
  startedAt: string | null
  finishedAt: string | null
  yourRack?: string | null
  yourSeat?: 0 | 1 | null
  isYourTurn?: boolean | null
}

export interface CreateMatchResponse {
  matchId: string
  playerId: string
  seat: 0 | 1
  playerToken: string
  rack: string
  match: MatchSnapshot
}

export interface JoinMatchResponse {
  playerId: string
  seat: 0 | 1
  playerToken: string
  rack: string
  match: MatchSnapshot
}

export interface FormedWord {
  word: string
  points: number
}

export interface MoveResultResponse {
  match: MatchSnapshot
  wordsFormed: FormedWord[]
  pointsScored: number
  usedFullRackBonus: boolean
  yourRack: string
}

export interface MatchActionResponse {
  match: MatchSnapshot
}

export interface MoveHistoryItem {
  moveId: string
  sequenceNumber: number
  seat: 0 | 1
  playerDisplayName: string
  type: MoveType
  startRow: number | null
  startCol: number | null
  direction: Direction | null
  tilesSubmitted: string | null
  wordsFormed: FormedWord[]
  pointsScored: number
  createdAt: string
}

export interface MoveHistoryResponse {
  moves: MoveHistoryItem[]
}

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  type?: string
  instance?: string
  errorCode?: string
  correlationId?: string
  currentMatchVersion?: number
}
