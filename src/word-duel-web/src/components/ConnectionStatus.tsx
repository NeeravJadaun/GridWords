import type { ConnectionState } from '../api/signalrClient'

const LABELS: Record<ConnectionState, string> = {
  connecting: 'Connecting…',
  connected: 'Live',
  reconnecting: 'Reconnecting…',
  disconnected: 'Disconnected',
}

export function ConnectionStatus({ state }: { state: ConnectionState }) {
  return (
    <span className={`connection-status ${state}`} title="Real-time connection status">
      <span className="dot" />
      {LABELS[state]}
    </span>
  )
}
