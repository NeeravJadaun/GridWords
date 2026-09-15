import { useState } from 'react'
import { Lobby } from './components/Lobby'
import { MatchView } from './components/MatchView'
import { clearSession, loadSession, saveSession, type PlayerSession } from './state/session'

export default function App() {
  const [session, setSession] = useState<PlayerSession | null>(() => loadSession())

  const handleSessionStart = (next: PlayerSession) => {
    saveSession(next)
    setSession(next)
  }

  const handleLeave = () => {
    clearSession()
    setSession(null)
  }

  return (
    <div className="app-shell">
      {session ? <MatchView session={session} onLeave={handleLeave} /> : <Lobby onSessionStart={handleSessionStart} />}
    </div>
  )
}
