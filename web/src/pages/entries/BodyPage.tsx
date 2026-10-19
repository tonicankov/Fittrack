import { useEffect, useState, type FormEvent } from 'react'
import { api } from '../../api/client'
import type { Measurement, Visibility } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'
import { Empty, ErrorAlert, Field, fieldError, VisibilityToggle } from '../../components/Form'
import { formatDate, formatNumber, today } from '../../lib/format'
import { useAsync } from '../../lib/useAsync'

const toNumber = (value: string) => (value.trim() === '' ? null : Number(value.replace(',', '.')))

export default function BodyPage() {
  const { user } = useAuth()
  const { data: measurements, reload, loading } = useAsync(() => api.get<Measurement[]>('/api/measurements'), [])
  const [date, setDate] = useState(today())
  const [form, setForm] = useState({ weightKg: '', bodyFatPct: '', waistCm: '' })
  const [visibility, setVisibility] = useState<Visibility>(user?.defaultVisibility ?? 'Friends')
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)
  const [saved, setSaved] = useState<string | null>(null)

  const existing = measurements?.find((m) => m.date === date)

  // Open with today's measurement filled in, if there is one
  const [prefilled, setPrefilled] = useState(false)
  useEffect(() => {
    if (measurements && !prefilled) {
      setPrefilled(true)
      pick(today())
    }
  }, [measurements, prefilled])
  const set = (key: keyof typeof form) => (e: { target: { value: string } }) => setForm({ ...form, [key]: e.target.value })

  function pick(day: string) {
    setDate(day)
    setSaved(null)
    const m = measurements?.find((x) => x.date === day)
    setForm({
      weightKg: m ? String(m.weightKg) : '',
      bodyFatPct: m?.bodyFatPct == null ? '' : String(m.bodyFatPct),
      waistCm: m?.waistCm == null ? '' : String(m.waistCm),
    })
    if (m) setVisibility(m.visibility)
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await api.put<Measurement>(`/api/measurements/${date}`, {
        weightKg: toNumber(form.weightKg),
        bodyFatPct: toNumber(form.bodyFatPct),
        waistCm: toNumber(form.waistCm),
        visibility,
      })
      setSaved(`Saved for ${formatDate(date)}.`)
      await reload()
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  async function remove(m: Measurement) {
    if (!confirm(`Delete the measurement of ${formatDate(m.date)}?`)) return
    try {
      await api.delete(`/api/measurements/${m.date}`)
      await reload()
    } catch (err) {
      setError(err)
    }
  }

  return (
    <>
      <h2>Body</h2>
      <form className="card form" onSubmit={submit} data-kind="body" style={{ marginBottom: 24 }}>
        <ErrorAlert error={error} />
        {saved && (
          <div className="notice" role="status">
            {saved}
          </div>
        )}
        {existing && !saved && (
          <p className="field-hint" style={{ margin: 0 }}>
            You already recorded {formatDate(date)}. Saving replaces that measurement.
          </p>
        )}
        <div className="form-row">
          <Field label="Date" error={fieldError(error, 'date')}>
            {(p) => <input {...p} type="date" required max={today()} value={date} onChange={(e) => e.target.value && pick(e.target.value)} />}
          </Field>
          <Field label="Weight (kg)" error={fieldError(error, 'weightKg')}>
            {(p) => <input {...p} type="number" inputMode="decimal" step="0.1" min={20} max={400} required value={form.weightKg} onChange={set('weightKg')} />}
          </Field>
        </div>
        <div className="form-row">
          <Field label="Body fat (%)" hint="Optional" error={fieldError(error, 'bodyFatPct')}>
            {(p) => <input {...p} type="number" inputMode="decimal" step="0.1" min={2} max={75} value={form.bodyFatPct} onChange={set('bodyFatPct')} />}
          </Field>
          <Field label="Waist (cm)" hint="Optional" error={fieldError(error, 'waistCm')}>
            {(p) => <input {...p} type="number" inputMode="decimal" step="0.5" min={30} max={250} value={form.waistCm} onChange={set('waistCm')} />}
          </Field>
        </div>
        <VisibilityToggle value={visibility} onChange={setVisibility} />
        <div>
          <button className="btn" type="submit" disabled={busy}>
            {busy ? 'Saving…' : 'Save measurement'}
          </button>
        </div>
      </form>

      {!loading && measurements?.length === 0 && (
        <Empty>
          <p>No measurements yet. Weigh in at the same time of day for a fair trend.</p>
        </Empty>
      )}
      <ul className="entries">
        {measurements?.map((m) => (
          <li key={m.id} className="entry" data-kind="body">
            <div className="entry-head">
              <h3 className="entry-title">{formatNumber(m.weightKg, 2)} kg</h3>
              <span className="entry-meta">{formatDate(m.date)}</span>
            </div>
            <div className="entry-meta">
              {[
                m.bodyFatPct != null ? `${formatNumber(m.bodyFatPct)}% body fat` : null,
                m.waistCm != null ? `waist ${formatNumber(m.waistCm)} cm` : null,
                m.visibility === 'Private' ? 'only you' : null,
              ]
                .filter(Boolean)
                .join(', ')}
            </div>
            <div className="entry-actions">
              <button className="btn btn-secondary btn-small" onClick={() => pick(m.date)}>
                Edit
              </button>
              <button className="btn btn-danger btn-small" onClick={() => remove(m)}>
                Delete
              </button>
            </div>
          </li>
        ))}
      </ul>
    </>
  )
}
