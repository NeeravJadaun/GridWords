import { ApiError } from './errors'
import type {
  CreateMatchResponse,
  Direction,
  JoinMatchResponse,
  MatchActionResponse,
  MatchSnapshot,
  MoveHistoryResponse,
  MoveResultResponse,
  ProblemDetails,
} from './types'

const BASE_URL = '/api/matches'

async function request<T>(
  path: string,
  init: RequestInit & { token?: string; idempotencyKey?: string } = {},
): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Content-Type', 'application/json')
  if (init.token) {
    headers.set('Authorization', `Bearer ${init.token}`)
  }
  if (init.idempotencyKey) {
    headers.set('Idempotency-Key', init.idempotencyKey)
  }

  const response = await fetch(path, { ...init, headers })

  if (!response.ok) {
    let problem: ProblemDetails
    try {
      problem = (await response.json()) as ProblemDetails
    } catch {
      problem = { title: response.statusText, status: response.status }
    }
    throw new ApiError(response.status, problem)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export function createMatch(displayName: string): Promise<CreateMatchResponse> {
  return request<CreateMatchResponse>(BASE_URL, {
    method: 'POST',
    body: JSON.stringify({ displayName }),
  })
}

export function joinMatch(matchId: string, displayName: string): Promise<JoinMatchResponse> {
  return request<JoinMatchResponse>(`${BASE_URL}/${matchId}/join`, {
    method: 'POST',
    body: JSON.stringify({ displayName }),
  })
}

export function getMatch(matchId: string, token?: string): Promise<MatchSnapshot> {
  return request<MatchSnapshot>(`${BASE_URL}/${matchId}`, { token })
}

export function getMoveHistory(matchId: string): Promise<MoveHistoryResponse> {
  return request<MoveHistoryResponse>(`${BASE_URL}/${matchId}/moves`)
}

export interface SubmitMoveArgs {
  matchId: string
  token: string
  expectedMatchVersion: number
  startRow: number
  startCol: number
  direction: Direction
  tiles: string
  idempotencyKey: string
}

export function submitMove(args: SubmitMoveArgs): Promise<MoveResultResponse> {
  const { matchId, token, idempotencyKey, ...body } = args
  return request<MoveResultResponse>(`${BASE_URL}/${matchId}/moves`, {
    method: 'POST',
    token,
    idempotencyKey,
    body: JSON.stringify(body),
  })
}

export function passTurn(
  matchId: string,
  token: string,
  expectedMatchVersion: number,
  idempotencyKey: string,
): Promise<MatchActionResponse> {
  return request<MatchActionResponse>(`${BASE_URL}/${matchId}/pass`, {
    method: 'POST',
    token,
    idempotencyKey,
    body: JSON.stringify({ expectedMatchVersion }),
  })
}

export function resignMatch(
  matchId: string,
  token: string,
  expectedMatchVersion: number,
  idempotencyKey: string,
): Promise<MatchActionResponse> {
  return request<MatchActionResponse>(`${BASE_URL}/${matchId}/resign`, {
    method: 'POST',
    token,
    idempotencyKey,
    body: JSON.stringify({ expectedMatchVersion }),
  })
}
