import type { MatchSnapshot } from '../api/types'

export function MatchSummary({ match, onLeave }: { match: MatchSnapshot; onLeave: () => void }) {
  const winner = match.winnerSeat !== null ? match.players.find((p) => p.seat === match.winnerSeat) : null
  const reasonText =
    match.endReason === 'Resignation'
      ? 'a resignation'
      : match.endReason === 'ThreeConsecutivePasses'
        ? 'three consecutive passes'
        : 'the match ending'

  return (
    <div className="match-summary">
      <h2>Match complete</h2>
      <p>
        {winner ? (
          <>
            <strong>{winner.displayName}</strong> wins after {reasonText}.
          </>
        ) : (
          <>It's a tie after {reasonText}.</>
        )}
      </p>
      <div className="final-scores">
        {match.players.map((p) => (
          <div key={p.playerId} className="final-score-row">
            <span>{p.displayName}</span>
            <span>{p.score} pts</span>
          </div>
        ))}
      </div>
      <button type="button" onClick={onLeave}>
        Start a new match
      </button>
    </div>
  )
}
