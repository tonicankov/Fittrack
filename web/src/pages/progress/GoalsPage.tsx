import { useState, type FormEvent } from 'react'
import { api } from '../../api/client'
import type { Goal, GoalMetric, Visibility } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'
import GoalCard from '../../components/GoalCard'
import { Empty, ErrorAlert, Field, fieldError, VisibilityToggle } from '../../components/Form'
import { addDays, today } from '../../lib/format'
import { goalMetrics, metricInfo } from '../../lib/goals'
import { useAsync } from '../../lib/useAsync'

function NewGoalForm({ taken, onDone }: { taken: GoalMetric[]; onDone: () => void }) {
  const { user } = useAuth()
  const available = goalMetrics.filter((m) => !taken.includes(m.metric))
  const [metric, setMetric] = useState<GoalMetric>(available[0]?.metric ?? 'BodyWeight')
  const [target, setTarget] = useState('')
  const [targetDate, setTargetDate] = useState('')
  const [visibility, setVisibility] = useState<Visibility>(user?.defaultVisibility ?? 'Friends')
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)
  const info = metricInfo(metric)

  if (available.length === 0) {
    return <p className="muted">You have an active goal of every kind. Archive one to set a new target.</p>
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await api.post<Goal>('/api/goals', {
        metric,
        targetValue: Number(target.replace(',', '.')),
        targetDate: metric === 'BodyWeight' && targetDate ? targetDate : null,
        visibility,
      })
      onDone()
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="card form" onSubmit={submit} data-kind="goal">
      <ErrorAlert error={error} />
      <div className="form-row">
        <Field label="Goal" error={fieldError(error, 'metric')}>
          {(p) => (
            <select {...p} value={metric} onChange={(e) => setMetric(e.target.value as GoalMetric)}>
              {available.map((m) => (
                <option key={m.metric} value={m.metric}>
                  {m.label}
                </option>
              ))}
            </select>
          )}
        </Field>
        <Field label={`Target (${info.unit})`} error={fieldError(error, 'targetValue')}>
          {(p) => (
            <input
              {...p}
              type="number"
              inputMode="decimal"
              required
              min={info.min}
              max={info.max}
              step={info.step}
              value={target}
              onChange={(e) => setTarget(e.target.value)}
            />
          )}
        </Field>
        {metric === 'BodyWeight' && (
          <Field label="By (optional)" error={fieldError(error, 'targetDate')}>
            {(p) => <input {...p} type="date" min={addDays(today(), 1)} value={targetDate} onChange={(e) => setTargetDate(e.target.value)} />}
          </Field>
        )}
      </div>
      <VisibilityToggle value={visibility} onChange={setVisibility} />
      <div className="actions">
        <button className="btn" type="submit" disabled={busy}>
          {busy ? 'Saving…' : 'Set goal'}
        </button>
        <button className="btn btn-secondary" type="button" onClick={onDone}>
          Cancel
        </button>
      </div>
    </form>
  )
}

export default function GoalsPage() {
  const { data: goals, reload, loading } = useAsync(() => api.get<Goal[]>('/api/goals'), [])
  const [adding, setAdding] = useState(false)
  const [error, setError] = useState<unknown>(null)

  async function change(goal: Goal, change: Partial<Pick<Goal, 'status' | 'visibility'>>) {
    try {
      await api.put<Goal>(`/api/goals/${goal.id}`, {
        targetValue: goal.targetValue,
        targetDate: goal.targetDate,
        visibility: change.visibility ?? goal.visibility,
        status: change.status ?? null,
      })
      await reload()
    } catch (err) {
      setError(err)
    }
  }

  async function remove(goal: Goal) {
    if (!confirm('Delete this goal?')) return
    try {
      await api.delete(`/api/goals/${goal.id}`)
      await reload()
    } catch (err) {
      setError(err)
    }
  }

  const active = goals?.filter((g) => g.status === 'Active') ?? []
  const past = goals?.filter((g) => g.status !== 'Active') ?? []

  return (
    <>
      <div className="page-header">
        <h2>Goals</h2>
        {!adding && (
          <button className="btn" onClick={() => setAdding(true)}>
            Set a goal
          </button>
        )}
      </div>
      <ErrorAlert error={error} />
      {adding && (
        <div style={{ marginBottom: 24 }}>
          <NewGoalForm
            taken={active.map((g) => g.metric)}
            onDone={() => {
              setAdding(false)
              void reload()
            }}
          />
        </div>
      )}

      {!loading && goals?.length === 0 && !adding && (
        <Empty>
          <p>No goals yet. A target makes the dashboard much more useful.</p>
          <button className="btn" onClick={() => setAdding(true)}>
            Set a goal
          </button>
        </Empty>
      )}

      <ul className="entries">
        {active.map((goal) => (
          <GoalCard
            key={goal.id}
            goal={goal}
            actions={
              <>
                <button
                  className="btn btn-secondary btn-small"
                  onClick={() => change(goal, { visibility: goal.visibility === 'Friends' ? 'Private' : 'Friends' })}
                >
                  {goal.visibility === 'Friends' ? 'Make private' : 'Share with friends'}
                </button>
                <button className="btn btn-secondary btn-small" onClick={() => change(goal, { status: 'Archived' })}>
                  Archive
                </button>
              </>
            }
          />
        ))}
      </ul>

      {past.length > 0 && (
        <section className="section">
          <h3>Earlier goals</h3>
          <ul className="entries">
            {past.map((goal) => (
              <GoalCard
                key={goal.id}
                goal={goal}
                actions={
                  <button className="btn btn-danger btn-small" onClick={() => remove(goal)}>
                    Delete
                  </button>
                }
              />
            ))}
          </ul>
        </section>
      )}
    </>
  )
}
