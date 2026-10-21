import { useState, type FormEvent } from 'react'
import { ApiError, api, getAccessToken } from '../../api/client'
import type { Me, Visibility } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'
import { ErrorAlert, Field, fieldError, VisibilityToggle } from '../../components/Form'

// The browser's list of IANA zones, with the member's own zone first
function timeZones(current: string): string[] {
  const supported = (Intl as unknown as { supportedValuesOf?: (key: string) => string[] }).supportedValuesOf?.('timeZone') ?? []
  return Array.from(new Set([current, ...supported]))
}

async function downloadExport() {
  const response = await fetch('/api/me/export', { headers: { Authorization: `Bearer ${getAccessToken()}` } })
  if (!response.ok) throw new ApiError(response.status, 'The download did not work. Try again.')
  const blob = await response.blob()
  const name = /filename="?([^";]+)"?/.exec(response.headers.get('Content-Disposition') ?? '')?.[1] ?? 'fittrack-export.json'
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = name
  link.click()
  URL.revokeObjectURL(url)
}

export default function SettingsPage() {
  const { user, updateUser, logout } = useAuth()
  const [displayName, setDisplayName] = useState(user?.displayName ?? '')
  const [timeZone, setTimeZone] = useState(user?.timeZone ?? 'Europe/Amsterdam')
  const [visibility, setVisibility] = useState<Visibility>(user?.defaultVisibility ?? 'Friends')
  const [error, setError] = useState<unknown>(null)
  const [saved, setSaved] = useState(false)
  const [busy, setBusy] = useState(false)
  const [dataError, setDataError] = useState<unknown>(null)

  if (!user) return null

  async function save(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    setSaved(false)
    try {
      updateUser(await api.put<Me>('/api/me', { displayName, timeZone, defaultVisibility: visibility }))
      setSaved(true)
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  async function exportData() {
    setDataError(null)
    try {
      await downloadExport()
    } catch (err) {
      setDataError(err)
    }
  }

  async function deleteAccount() {
    const typed = prompt('This deletes your account and everything you logged. It cannot be undone. Type DELETE to confirm.')
    if (typed !== 'DELETE') return
    try {
      await api.delete('/api/me')
      logout()
    } catch (err) {
      setDataError(err)
    }
  }

  return (
    <main className="page">
      <div className="page-header">
        <h1>Me</h1>
        <button className="btn btn-secondary" onClick={logout}>
          Log out
        </button>
      </div>
      <p className="muted">Logged in as {user.email}</p>

      <form className="card form" onSubmit={save}>
        <h2>Profile</h2>
        <ErrorAlert error={error} />
        {saved && (
          <div className="notice" role="status">
            Profile saved.
          </div>
        )}
        <Field label="Name" hint="What your friends see." error={fieldError(error, 'displayName')}>
          {(p) => <input {...p} required maxLength={50} value={displayName} onChange={(e) => setDisplayName(e.target.value)} />}
        </Field>
        <Field label="Time zone" hint="Decides where your days start and end for daily totals." error={fieldError(error, 'timeZone')}>
          {(p) => (
            <select {...p} value={timeZone} onChange={(e) => setTimeZone(e.target.value)}>
              {timeZones(timeZone).map((tz) => (
                <option key={tz} value={tz}>
                  {tz.replace(/_/g, ' ')}
                </option>
              ))}
            </select>
          )}
        </Field>
        <div>
          <VisibilityToggle value={visibility} onChange={setVisibility} />
          <p className="field-hint" style={{ marginTop: 4 }}>
            The default for new workouts, meals, measurements and goals. You can change it on each one.
          </p>
        </div>
        <div>
          <button className="btn" type="submit" disabled={busy}>
            {busy ? 'Saving…' : 'Save profile'}
          </button>
        </div>
      </form>

      <section className="section card">
        <h2>Your data</h2>
        <ErrorAlert error={dataError} />
        <p>Download everything FitTrack stores about you as a JSON file.</p>
        <div className="actions">
          <button className="btn btn-secondary" onClick={exportData}>
            Download my data
          </button>
        </div>
        <h3 className="section">Delete account</h3>
        <p>Deletes your account with all workouts, meals, measurements, goals and friendships. This cannot be undone.</p>
        <button className="btn btn-danger" onClick={deleteAccount}>
          Delete my account
        </button>
      </section>
    </main>
  )
}
