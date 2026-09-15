import { useEffect, useState } from 'react'
import * as api from '../api/httpClient'
import type { MoveHistoryItem } from '../api/types'

interface MoveHistoryProps {
  matchId: string
  /** Bump this (e.g. with match.version) to trigger a refetch after each state change. */
  refreshToken: number
}

export function MoveHistory({ matchId, refreshToken }: MoveHistoryProps) {
  const [moves, setMoves] = useState<MoveHistoryItem[]>([])

  useEffect(() => {
    let cancelled = false
    api
      .getMoveHistory(matchId)
      .then((res) => !cancelled && setMoves(res.moves))
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [matchId, refreshToken])

  return (
    <div className="move-history">
      <h3>Move history</h3>
      {moves.length === 0 && <p className="muted">No moves yet.</p>}
      <ol>
        {moves.map((m) => (
          <li key={m.moveId}>
            <span className="move-seat">P{m.seat + 1}</span>
            <span className="move-player">{m.playerDisplayName}</span>
            {m.type === 'Place' && (
              <span className="move-detail">
                played <strong>{m.tilesSubmitted}</strong>
                {m.wordsFormed.length > 0 && (
                  <> ({m.wordsFormed.map((w) => `${w.word} +${w.points}`).join(', ')})</>
                )}{' '}
                for {m.pointsScored} pts
              </span>
            )}
            {m.type === 'Pass' && <span className="move-detail">passed</span>}
            {m.type === 'Resign' && <span className="move-detail">resigned</span>}
          </li>
        ))}
      </ol>
    </div>
  )
}
