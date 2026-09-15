export interface PlayerSession {
  matchId: string
  playerId: string
  token: string
  seat: 0 | 1
  displayName: string
}

const STORAGE_KEY = 'worddue.session.v1'

// sessionStorage (not localStorage) is deliberate: it's scoped per browser
// tab rather than shared across every tab on the origin. Two players
// testing locally in two tabs of the same browser must not clobber each
// other's session, while a same-tab refresh (the actual reconnect case)
// still restores state from it.
export function loadSession(): PlayerSession | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as PlayerSession) : null
  } catch {
    return null
  }
}

export function saveSession(session: PlayerSession): void {
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session))
  } catch {
    // Best-effort only — private browsing / storage quota, etc.
  }
}

export function clearSession(): void {
  try {
    sessionStorage.removeItem(STORAGE_KEY)
  } catch {
    // ignore
  }
}
