import type { ProblemDetails } from './types'

/** Thrown for any non-2xx API response; carries the parsed ProblemDetails body. */
export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }

  get errorCode(): string | undefined {
    return this.problem.errorCode
  }

  get isStaleVersion(): boolean {
    return this.errorCode === 'StaleMatchVersion'
  }
}
