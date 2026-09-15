import type { ApiError } from '../api/errors'

export function ErrorBanner({ error, onDismiss }: { error: ApiError; onDismiss: () => void }) {
  return (
    <div className="error-banner" role="alert">
      <div>
        <strong>{error.errorCode ?? 'Error'}:</strong> {error.message}
      </div>
      <button type="button" onClick={onDismiss} aria-label="Dismiss error">
        ×
      </button>
    </div>
  )
}
