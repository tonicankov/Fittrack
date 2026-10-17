import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { ErrorAlert, Field, fieldError } from '../components/Form'

export default function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const [form, setForm] = useState({
    inviteCode: params.get('code') ?? '',
    displayName: '',
    email: '',
    password: '',
  })
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)

  const set = (key: keyof typeof form) => (e: { target: { value: string } }) => setForm({ ...form, [key]: e.target.value })

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await register(form)
      navigate('/')
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="auth">
      <div className="auth-box">
        <h1 className="display">Join FitTrack</h1>
        <p className="muted">FitTrack is invite-only. Ask a friend who uses it for a code.</p>
        <form className="card form" onSubmit={submit}>
          <ErrorAlert error={error} />
          <Field label="Invite code" error={fieldError(error, 'inviteCode')}>
            {(p) => <input {...p} required autoCapitalize="characters" value={form.inviteCode} onChange={set('inviteCode')} />}
          </Field>
          <Field label="Your name" hint="This is what your friends see." error={fieldError(error, 'displayName')}>
            {(p) => <input {...p} required maxLength={50} autoComplete="name" value={form.displayName} onChange={set('displayName')} />}
          </Field>
          <Field label="E-mail address" error={fieldError(error, 'email')}>
            {(p) => <input {...p} type="email" required autoComplete="email" value={form.email} onChange={set('email')} />}
          </Field>
          <Field label="Password" hint="At least 8 characters." error={fieldError(error, 'password')}>
            {(p) => (
              <input {...p} type="password" required minLength={8} autoComplete="new-password" value={form.password} onChange={set('password')} />
            )}
          </Field>
          <button className="btn" type="submit" disabled={busy}>
            {busy ? 'Creating your account…' : 'Create account'}
          </button>
        </form>
        <p className="small muted" style={{ marginTop: 16 }}>
          Already have an account? <Link to="/login">Log in</Link>
        </p>
      </div>
    </main>
  )
}
