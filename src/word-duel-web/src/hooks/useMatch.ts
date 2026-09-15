import { useCallback, useEffect, useRef, useState } from 'react'
import * as api from '../api/httpClient'
import { ApiError } from '../api/errors'
import { connectToMatchHub, type ConnectionState, type MatchConnection } from '../api/signalrClient'
import type { Direction, MatchSnapshot } from '../api/types'
import type { PlayerSession } from '../state/session'

export interface PlacementArgs {
  startRow: number
  startCol: number
  direction: Direction
  tiles: string
}

export interface UseMatchResult {
  snapshot: MatchSnapshot | null
  connectionState: ConnectionState
  loading: boolean
  error: ApiError | null
  lastMoveSummary: { words: { word: string; points: number }[]; points: number; bonus: boolean } | null
  refresh: () => Promise<void>
  submitPlacement: (args: PlacementArgs) => Promise<boolean>
  pass: () => Promise<boolean>
  resign: () => Promise<boolean>
  clearError: () => void
}

export function useMatch(session: PlayerSession): UseMatchResult {
  const [snapshot, setSnapshot] = useState<MatchSnapshot | null>(null)
  const [connectionState, setConnectionState] = useState<ConnectionState>('connecting')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<ApiError | null>(null)
  const [lastMoveSummary, setLastMoveSummary] = useState<UseMatchResult['lastMoveSummary']>(null)
  const connectionRef = useRef<MatchConnection | null>(null)

  const mergePublicUpdate = useCallback(
    (incoming: MatchSnapshot) => {
      setSnapshot((prev) => ({
        ...incoming,
        yourRack: incoming.yourRack ?? prev?.yourRack ?? null,
        yourSeat: session.seat,
        isYourTurn: incoming.status === 'InProgress' && incoming.currentTurnSeat === session.seat,
      }))
    },
    [session.seat],
  )

  const refresh = useCallback(async () => {
    const fresh = await api.getMatch(session.matchId, session.token)
    setSnapshot(fresh)
  }, [session.matchId, session.token])

  useEffect(() => {
    let cancelled = false

    setLoading(true)
    refresh()
      .catch((err) => !cancelled && setError(err instanceof ApiError ? err : null))
      .finally(() => !cancelled && setLoading(false))

    const connection = connectToMatchHub(session.token)
    connectionRef.current = connection

    const unsubscribeState = connection.onStateChanged((state) => {
      setConnectionState(state)
      // Whenever we (re)connect, resync to the authoritative current snapshot.
      if (state === 'connected') {
        refresh().catch(() => undefined)
      }
    })
    const unsubscribeMatch = connection.onMatchUpdated(mergePublicUpdate)

    return () => {
      cancelled = true
      unsubscribeState()
      unsubscribeMatch()
      connection.stop().catch(() => undefined)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [session.matchId, session.token])

  const runAction = useCallback(
    async <T,>(action: () => Promise<T>, onSuccess: (result: T) => void): Promise<boolean> => {
      setError(null)
      try {
        const result = await action()
        onSuccess(result)
        return true
      } catch (err) {
        if (err instanceof ApiError) {
          setError(err)
          if (err.isStaleVersion) {
            // Our local version is behind — resync so the next attempt uses fresh state.
            refresh().catch(() => undefined)
          }
        }
        return false
      }
    },
    [refresh],
  )

  const submitPlacement = useCallback(
    (args: PlacementArgs) =>
      runAction(
        () =>
          api.submitMove({
            matchId: session.matchId,
            token: session.token,
            expectedMatchVersion: snapshot?.version ?? 0,
            startRow: args.startRow,
            startCol: args.startCol,
            direction: args.direction,
            tiles: args.tiles,
            idempotencyKey: crypto.randomUUID(),
          }),
        (result) => {
          setSnapshot(result.match)
          setLastMoveSummary({ words: result.wordsFormed, points: result.pointsScored, bonus: result.usedFullRackBonus })
        },
      ),
    [runAction, session.matchId, session.token, snapshot?.version],
  )

  const pass = useCallback(
    () =>
      runAction(
        () => api.passTurn(session.matchId, session.token, snapshot?.version ?? 0, crypto.randomUUID()),
        (result) => setSnapshot(result.match),
      ),
    [runAction, session.matchId, session.token, snapshot?.version],
  )

  const resign = useCallback(
    () =>
      runAction(
        () => api.resignMatch(session.matchId, session.token, snapshot?.version ?? 0, crypto.randomUUID()),
        (result) => setSnapshot(result.match),
      ),
    [runAction, session.matchId, session.token, snapshot?.version],
  )

  return {
    snapshot,
    connectionState,
    loading,
    error,
    lastMoveSummary,
    refresh,
    submitPlacement,
    pass,
    resign,
    clearError: () => setError(null),
  }
}
