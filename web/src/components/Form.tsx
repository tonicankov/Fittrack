import { useId, type ReactNode } from 'react'
import { ApiError } from '../api/client'
import type { Visibility } from '../api/types'

interface FieldProps {
  label: string
  error?: string
  hint?: string
  children: (props: { id: string; 'aria-invalid': boolean; 'aria-describedby'?: string }) => ReactNode
}

/** Label, input and its error message, wired together for screen readers. */
export function Field({ label, error, hint, children }: FieldProps) {
  const id = useId()
  const describedBy = error ? `${id}-error` : hint ? `${id}-hint` : undefined
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      {children({ id, 'aria-invalid': Boolean(error), 'aria-describedby': describedBy })}
      {hint && !error && (
        <span id={`${id}-hint`} className="field-hint">
          {hint}
        </span>
      )}
      {error && (
        <span id={`${id}-error`} className="field-error">
          {error}
        </span>
      )}
    </div>
  )
}

/** Shows the summary of an API error; field messages are shown next to the fields. */
export function ErrorAlert({ error }: { error: unknown }) {
  if (!error) return null
  const message =
    error instanceof ApiError
      ? Object.keys(error.fieldErrors).length > 0
        ? 'Some fields need a fix. See the messages below each field.'
        : error.message
      : 'Something went wrong. Try again.'
  return (
    <div className="alert" role="alert">
      {message}
    </div>
  )
}

export function fieldError(error: unknown, name: string): string | undefined {
  return error instanceof ApiError ? error.field(name) : undefined
}

export function VisibilityToggle({ value, onChange }: { value: Visibility; onChange: (value: Visibility) => void }) {
  return (
    <div className="field">
      <span className="field-label">Who can see this</span>
      <div className="segmented" role="group" aria-label="Who can see this">
        <button type="button" aria-pressed={value === 'Friends'} onClick={() => onChange('Friends')}>
          Friends
        </button>
        <button type="button" aria-pressed={value === 'Private'} onClick={() => onChange('Private')}>
          Only me
        </button>
      </div>
    </div>
  )
}

export function Empty({ children }: { children: ReactNode }) {
  return <div className="empty">{children}</div>
}
