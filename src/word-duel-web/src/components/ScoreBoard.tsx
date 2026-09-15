import type { MatchSnapshot } from '../api/types'

interface ScoreBoardProps {
  match: MatchSnapshot
}

export function ScoreBoard({ match }: ScoreBoardProps) {
  return (
    <div className="score-board">
      {match.players.map((player) => {
        const isTurn = match.status === 'InProgress' && match.currentTurnSeat === player.seat
        const isYou = match.yourSeat === player.seat
        return (
          <div key={player.playerId} className={`score-card${isTurn ? ' current-turn' : ''}`}>
            <div className="score-card-name">
              {player.displayName}
              {isYou ? ' (you)' : ''}
              {player.hasResigned ? ' — resigned' : ''}
            </div>
            <div className="score-card-score">{player.score}</div>
            <div className="score-card-meta">
              {player.rackTileCount} tiles{isTurn ? ' • their turn' : ''}
            </div>
          </div>
        )
      })}
      {match.players.length < 2 && <div className="score-card waiting">Waiting for opponent…</div>}
    </div>
  )
}
