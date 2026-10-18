import { useState, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, query } from '../../api/client'
import type { Page, Workout } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'
import { Empty, ErrorAlert } from '../../components/Form'
import { formatDateTime, formatDuration, formatNumber } from '../../lib/format'
import { useAsync } from '../../lib/useAsync'
import WorkoutForm from './WorkoutForm'

export function WorkoutCard({ workout, actions }: { workout: Workout; actions?: ReactNode }) {
  return (
    <li className="entry" data-kind="workout">
      <div className="entry-head">
        <h3 className="entry-title">{workout.name}</h3>
        <span className="entry-meta">{workout.durationMinutes} min</span>
      </div>
      <div className="entry-meta">
        {formatDateTime(workout.performedAt)}
        {workout.visibility === 'Private' && ', only you'}
      </div>
      <ul className="sets">
        {workout.exercises.map((x) => (
          <li key={x.id}>
            <span>{x.name}</span>
            <span className="muted">
              {x.kind === 'Strength'
                ? x.sets.map((s) => (s.weightKg > 0 ? `${s.reps}×${formatNumber(s.weightKg, 2)}` : `${s.reps}`)).join(', ')
                : [x.durationSeconds ? formatDuration(x.durationSeconds) : null, x.distanceKm ? `${formatNumber(x.distanceKm, 2)} km` : null]
                    .filter(Boolean)
                    .join(', ')}
            </span>
          </li>
        ))}
      </ul>
      {workout.notes && <p className="small muted" style={{ margin: '8px 0 0' }}>{workout.notes}</p>}
      {actions && <div className="entry-actions">{actions}</div>}
    </li>
  )
}

export default function WorkoutsPage() {
  const navigate = useNavigate()
  const [older, setOlder] = useState<Workout[]>([])
  const [cursor, setCursor] = useState<string | null>(null)
  const [error, setError] = useState<unknown>(null)
  const { data, reload, loading } = useAsync(async () => {
    const page = await api.get<Page<Workout>>('/api/workouts')
    setOlder([])
    setCursor(page.nextBefore)
    return page.items
  }, [])

  async function loadOlder() {
    if (!cursor) return
    try {
      const page = await api.get<Page<Workout>>(`/api/workouts${query({ before: cursor })}`)
      setOlder((list) => [...list, ...page.items])
      setCursor(page.nextBefore)
    } catch (err) {
      setError(err)
    }
  }

  async function remove(workout: Workout) {
    if (!confirm(`Delete "${workout.name}"? This cannot be undone.`)) return
    try {
      await api.delete(`/api/workouts/${workout.id}`)
      await reload()
    } catch (err) {
      setError(err)
    }
  }

  const workouts = [...(data ?? []), ...older]

  return (
    <>
      <div className="page-header">
        <h2>Workouts</h2>
        <Link className="btn" to="/log/workouts/new">
          Log workout
        </Link>
      </div>
      <ErrorAlert error={error} />
      {!loading && workouts.length === 0 && (
        <Empty>
          <p>No workouts yet. Log your first one after your next session.</p>
          <Link className="btn" to="/log/workouts/new">
            Log workout
          </Link>
        </Empty>
      )}
      <ul className="entries">
        {workouts.map((w) => (
          <WorkoutCard
            key={w.id}
            workout={w}
            actions={
              <>
                <button className="btn btn-secondary btn-small" onClick={() => navigate(`/log/workouts/${w.id}/edit`)}>
                  Edit
                </button>
                <button className="btn btn-danger btn-small" onClick={() => remove(w)}>
                  Delete
                </button>
              </>
            }
          />
        ))}
      </ul>
      {cursor && (
        <div className="actions" style={{ marginTop: 16 }}>
          <button className="btn btn-secondary" onClick={loadOlder}>
            Show older workouts
          </button>
        </div>
      )}
    </>
  )
}

export function WorkoutEditorPage() {
  const { id } = useParams()
  const { user } = useAuth()
  const navigate = useNavigate()
  const { data: workout, error, loading } = useAsync(
    () => (id ? api.get<Workout>(`/api/workouts/${id}`) : Promise.resolve(null)),
    [id],
  )

  if (loading) return null
  if (error) return <ErrorAlert error={error} />

  return (
    <>
      <h2>{workout ? 'Edit workout' : 'Log workout'}</h2>
      <WorkoutForm
        workout={workout ?? undefined}
        defaultVisibility={user?.defaultVisibility ?? 'Friends'}
        onSaved={() => navigate('/log/workouts')}
        onCancel={() => navigate('/log/workouts')}
      />
    </>
  )
}
