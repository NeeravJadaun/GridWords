import type { Direction } from '../api/types'

interface MoveControlsProps {
  selectedCell: { row: number; col: number } | null
  direction: Direction
  tiles: string
  isYourTurn: boolean
  submitting: boolean
  isValidShape: boolean
  onToggleDirection: () => void
  onTilesChange: (value: string) => void
  onSubmit: () => void
  onClear: () => void
  onPass: () => void
  onResign: () => void
}

export function MoveControls({
  selectedCell,
  direction,
  tiles,
  isYourTurn,
  submitting,
  isValidShape,
  onToggleDirection,
  onTilesChange,
  onSubmit,
  onClear,
  onPass,
  onResign,
}: MoveControlsProps) {
  return (
    <div className="move-controls">
      <p className="move-hint">
        {selectedCell
          ? `Start: row ${selectedCell.row + 1}, col ${selectedCell.col + 1}`
          : 'Click a board cell to start placing a word.'}
      </p>

      <div className="move-controls-row">
        <button type="button" className="secondary" onClick={onToggleDirection} disabled={!isYourTurn}>
          Direction: {direction}
        </button>

        <input
          type="text"
          value={tiles}
          onChange={(e) => onTilesChange(e.target.value)}
          placeholder="Type the full word (e.g. CAT)"
          disabled={!isYourTurn || !selectedCell}
          maxLength={7}
          aria-label="Tiles to place"
        />
      </div>

      <div className="move-controls-row">
        <button type="button" onClick={onSubmit} disabled={!isYourTurn || !isValidShape || submitting}>
          {submitting ? 'Submitting…' : 'Submit move'}
        </button>
        <button type="button" className="secondary" onClick={onClear} disabled={!selectedCell && tiles === ''}>
          Clear
        </button>
        <button type="button" className="secondary" onClick={onPass} disabled={!isYourTurn || submitting}>
          Pass
        </button>
        <button type="button" className="danger" onClick={onResign} disabled={submitting}>
          Resign
        </button>
      </div>
    </div>
  )
}
