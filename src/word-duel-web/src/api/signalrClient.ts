import * as signalR from '@microsoft/signalr'
import type { MatchSnapshot } from './types'

export type ConnectionState = 'connecting' | 'connected' | 'reconnecting' | 'disconnected'

export interface MatchConnection {
  state(): ConnectionState
  onMatchUpdated(handler: (snapshot: MatchSnapshot) => void): () => void
  onStateChanged(handler: (state: ConnectionState) => void): () => void
  stop(): Promise<void>
}

/** Opens an authenticated SignalR connection to the match hub for the given player token. */
export function connectToMatchHub(token: string): MatchConnection {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/match', { accessTokenFactory: () => token })
    .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
    .configureLogging(signalR.LogLevel.Warning)
    .build()

  let currentState: ConnectionState = 'connecting'
  const stateHandlers = new Set<(state: ConnectionState) => void>()
  const matchHandlers = new Set<(snapshot: MatchSnapshot) => void>()

  const setState = (state: ConnectionState) => {
    currentState = state
    stateHandlers.forEach((h) => h(state))
  }

  connection.onreconnecting(() => setState('reconnecting'))
  connection.onreconnected(() => setState('connected'))
  connection.onclose(() => setState('disconnected'))

  connection.on('MatchUpdated', (snapshot: MatchSnapshot) => {
    matchHandlers.forEach((h) => h(snapshot))
  })

  connection
    .start()
    .then(() => setState('connected'))
    .catch(() => setState('disconnected'))

  return {
    state: () => currentState,
    onMatchUpdated(handler) {
      matchHandlers.add(handler)
      return () => matchHandlers.delete(handler)
    },
    onStateChanged(handler) {
      stateHandlers.add(handler)
      return () => stateHandlers.delete(handler)
    },
    stop: () => connection.stop(),
  }
}
