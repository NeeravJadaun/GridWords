// Mirrors WordDuel.Domain/Board/BoardLayout.cs — purely for rendering bonus
// squares client-side; the server is the sole source of truth for scoring.

export const BOARD_SIZE = 7
export const CENTER = 3

const PATTERN = ['T..d..T', '.D...D.', '..t.t..', 'd..S..d', '..t.t..', '.D...D.', 'T..d..T']

export type BonusType = 'none' | 'doubleLetter' | 'tripleLetter' | 'doubleWord' | 'tripleWord' | 'start'

export function bonusAt(row: number, col: number): BonusType {
  switch (PATTERN[row][col]) {
    case 'T':
      return 'tripleWord'
    case 'D':
      return 'doubleWord'
    case 't':
      return 'tripleLetter'
    case 'd':
      return 'doubleLetter'
    case 'S':
      return 'start'
    default:
      return 'none'
  }
}

export function bonusLabel(bonus: BonusType): string {
  switch (bonus) {
    case 'tripleWord':
      return 'TW'
    case 'doubleWord':
      return 'DW'
    case 'tripleLetter':
      return 'TL'
    case 'doubleLetter':
      return 'DL'
    case 'start':
      return '★'
    default:
      return ''
  }
}

export function letterAt(board: string, row: number, col: number): string | null {
  const ch = board[row * BOARD_SIZE + col]
  return ch === '.' ? null : ch
}
