import { BOARD_SIZE, bonusAt, bonusLabel, letterAt } from '../board/boardLayout'
import { pointsFor } from '../board/letterValues'

export interface PreviewTile {
  row: number
  col: number
  letter: string
}

interface BoardProps {
  board: string
  previewTiles?: PreviewTile[]
  selectedCell?: { row: number; col: number } | null
  onCellClick?: (row: number, col: number) => void
}

export function Board({ board, previewTiles = [], selectedCell, onCellClick }: BoardProps) {
  const previewByCell = new Map(previewTiles.map((t) => [`${t.row},${t.col}`, t.letter]))

  const cells = []
  for (let row = 0; row < BOARD_SIZE; row++) {
    for (let col = 0; col < BOARD_SIZE; col++) {
      const letter = letterAt(board, row, col)
      const preview = previewByCell.get(`${row},${col}`)
      const bonus = bonusAt(row, col)
      const isSelected = selectedCell?.row === row && selectedCell?.col === col

      cells.push(
        <button
          type="button"
          key={`${row}-${col}`}
          className={[
            'board-cell',
            `bonus-${bonus}`,
            letter ? 'filled' : '',
            preview ? 'preview' : '',
            isSelected ? 'selected' : '',
          ]
            .filter(Boolean)
            .join(' ')}
          onClick={() => onCellClick?.(row, col)}
          aria-label={`Row ${row + 1}, column ${col + 1}${letter ? `, letter ${letter}` : ''}`}
        >
          {letter ? (
            <>
              <span className="letter">{letter}</span>
              <span className="points">{pointsFor(letter)}</span>
            </>
          ) : preview ? (
            <span className="letter ghost">{preview}</span>
          ) : (
            bonus !== 'none' && <span className="bonus-label">{bonusLabel(bonus)}</span>
          )}
        </button>,
      )
    }
  }

  return (
    <div className="board" role="grid" aria-label="Word Duel board">
      {cells}
    </div>
  )
}
