import { useState } from 'react'
import { useMatch } from '../hooks/useMatch'
import { useMoveComposer } from '../hooks/useMoveComposer'
import type { PlayerSession } from '../state/session'
import { Board } from './Board'
import { Rack } from './Rack'
import { MoveControls } from './MoveControls'
import { ScoreBoard } from './ScoreBoard'
import { ConnectionStatus } from './ConnectionStatus'
import { ErrorBanner } from './ErrorBanner'
import { MoveHistory } from './MoveHistory'
import { MatchSummary } from './MatchSummary'

interface MatchViewProps {
  session: PlayerSession
  onLeave: () => void
}

export function MatchView({ session, onLeave }: MatchViewProps) {
  const { snapshot, connectionState, loading, error, lastMoveSummary, submitPlacement, pass, resign, clearError } =
    useMatch(session)
  const composer = useMoveComposer()
  const [submitting, setSubmitting] = useState(false)

  if (loading && !snapshot) {
    return <p className="centered">Loading match…</p>
  }

  if (!snapshot) {
    return <p className="centered">Could not load this match.</p>
  }

  const isYourTurn = Boolean(snapshot.isYourTurn)

  const handleSubmit = async () => {
    if (!composer.selectedCell) return
    setSubmitting(true)
    const ok = await submitPlacement({
      startRow: composer.selectedCell.row,
      startCol: composer.selectedCell.col,
      direction: composer.direction,
      tiles: composer.tiles,
    })
    setSubmitting(false)
    if (ok) composer.reset()
  }

  const handlePass = async () => {
    setSubmitting(true)
    await pass()
    setSubmitting(false)
  }

  const handleResign = async () => {
    setSubmitting(true)
    await resign()
    setSubmitting(false)
  }

  return (
    <div className="match-view">
      <header className="match-header">
        <div>
          <h2>Word Duel</h2>
          <p className="match-id">
            Match ID: <code>{snapshot.matchId}</code>
          </p>
        </div>
        <div className="match-header-right">
          <ConnectionStatus state={connectionState} />
          <button type="button" className="secondary" onClick={onLeave}>
            Leave
          </button>
        </div>
      </header>

      {error && <ErrorBanner error={error} onDismiss={clearError} />}

      {snapshot.status === 'WaitingForOpponent' && (
        <div className="waiting-banner">
          Waiting for an opponent to join. Share this match ID: <code>{snapshot.matchId}</code>
        </div>
      )}

      {lastMoveSummary && lastMoveSummary.words.length > 0 && (
        <div className="move-summary-banner">
          Scored {lastMoveSummary.points} pts: {lastMoveSummary.words.map((w) => `${w.word} (+${w.points})`).join(', ')}
          {lastMoveSummary.bonus ? ' — full rack bonus!' : ''}
        </div>
      )}

      <ScoreBoard match={snapshot} />

      {snapshot.status === 'Completed' ? (
        <MatchSummary match={snapshot} onLeave={onLeave} />
      ) : (
        <>
          <Board
            board={snapshot.board}
            previewTiles={composer.previewTiles}
            selectedCell={composer.selectedCell}
            onCellClick={isYourTurn ? composer.selectCell : undefined}
          />

          {snapshot.yourRack && <Rack rack={snapshot.yourRack} usedLetters={composer.tiles} />}

          <MoveControls
            selectedCell={composer.selectedCell}
            direction={composer.direction}
            tiles={composer.tiles}
            isYourTurn={isYourTurn}
            submitting={submitting}
            isValidShape={composer.isValidShape}
            onToggleDirection={composer.toggleDirection}
            onTilesChange={composer.setTilesInput}
            onSubmit={handleSubmit}
            onClear={composer.reset}
            onPass={handlePass}
            onResign={handleResign}
          />
        </>
      )}

      <MoveHistory matchId={snapshot.matchId} refreshToken={snapshot.version} />
    </div>
  )
}
