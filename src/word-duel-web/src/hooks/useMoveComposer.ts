import { useMemo, useState } from 'react'
import { BOARD_SIZE } from '../board/boardLayout'
import type { Direction } from '../api/types'
import type { PreviewTile } from '../components/Board'

export function useMoveComposer() {
  const [selectedCell, setSelectedCell] = useState<{ row: number; col: number } | null>(null)
  const [direction, setDirection] = useState<Direction>('Across')
  const [tiles, setTiles] = useState('')

  const selectCell = (row: number, col: number) => setSelectedCell({ row, col })
  const toggleDirection = () => setDirection((d) => (d === 'Across' ? 'Down' : 'Across'))
  const setTilesInput = (value: string) => setTiles(value.toUpperCase().replace(/[^A-Z]/g, '').slice(0, 7))
  const reset = () => {
    setSelectedCell(null)
    setTiles('')
  }

  const previewTiles: PreviewTile[] = useMemo(() => {
    if (!selectedCell || tiles.length === 0) return []
    const [rowStep, colStep] = direction === 'Across' ? [0, 1] : [1, 0]
    const result: PreviewTile[] = []
    for (let i = 0; i < tiles.length; i++) {
      const row = selectedCell.row + rowStep * i
      const col = selectedCell.col + colStep * i
      if (row < 0 || row >= BOARD_SIZE || col < 0 || col >= BOARD_SIZE) break
      result.push({ row, col, letter: tiles[i] })
    }
    return result
  }, [selectedCell, direction, tiles])

  const isValidShape = selectedCell !== null && tiles.length > 0

  return {
    selectedCell,
    direction,
    tiles,
    previewTiles,
    isValidShape,
    selectCell,
    toggleDirection,
    setTilesInput,
    reset,
  }
}
