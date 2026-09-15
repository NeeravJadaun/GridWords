import { useState } from 'react'
import * as api from '../api/httpClient'
import { ApiError } from '../api/errors'
import type { PlayerSession } from '../state/session'

interface LobbyProps {
  onSessionStart: (session: PlayerSession) => void
}

export function Lobby({ onSessionStart }: LobbyProps) {
  const [mode, setMode] = useState<'create' | 'join'>('create')
  const [displayName, setDisplayName] = useState('')
  const [matchIdInput, setMatchIdInput] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleCreate = async () => {
    setBusy(true)
    setError(null)
    try {
      const res = await api.createMatch(displayName.trim())
      onSessionStart({
        matchId: res.matchId,
        playerId: res.playerId,
        token: res.playerToken,
        seat: res.seat,
        displayName: displayName.trim(),
      })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not create match.')
    } finally {
      setBusy(false)
    }
  }

  const handleJoin = async () => {
    setBusy(true)
    setError(null)
    try {
      const id = matchIdInput.trim()
      const res = await api.joinMatch(id, displayName.trim())
      onSessionStart({
        matchId: id,
        playerId: res.playerId,
        token: res.playerToken,
        seat: res.seat,
        displayName: displayName.trim(),
      })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not join match.')
    } finally {
      setBusy(false)
    }
  }

  const canSubmit = displayName.trim().length > 0 && (mode === 'create' || matchIdInput.trim().length > 0)

  return (
    <div className="lobby">
      <h1>Word Duel</h1>
      <p className="tagline">A small synthetic two-player word game — educational portfolio project.</p>

      <div className="lobby-tabs">
        <button type="button" className={mode === 'create' ? 'active' : ''} onClick={() => setMode('create')}>
          Create match
        </button>
        <button type="button" className={mode === 'join' ? 'active' : ''} onClick={() => setMode('join')}>
          Join match
        </button>
      </div>

      <div className="lobby-form">
        <label>
          Display name
          <input
            type="text"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            maxLength={40}
            placeholder="e.g. Ada"
          />
        </label>

        {mode === 'join' && (
          <label>
            Match ID
            <input
              type="text"
              value={matchIdInput}
              onChange={(e) => setMatchIdInput(e.target.value)}
              placeholder="Paste the match ID your opponent shared"
            />
          </label>
        )}

        {error && <p className="lobby-error">{error}</p>}

        <button
          type="button"
          disabled={!canSubmit || busy}
          onClick={mode === 'create' ? handleCreate : handleJoin}
        >
          {busy ? 'Please wait…' : mode === 'create' ? 'Create match' : 'Join match'}
        </button>
      </div>
    </div>
  )
}
