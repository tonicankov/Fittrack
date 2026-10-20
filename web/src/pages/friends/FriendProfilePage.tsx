import { useParams } from 'react-router-dom'
import { api, query } from '../../api/client'
import type { Goal, Meal, Measurement, Page, UserProfile, Workout } from '../../api/types'
import GoalCard from '../../components/GoalCard'
import { ErrorAlert } from '../../components/Form'
import { formatDate, formatNumber, formatTime, initials } from '../../lib/format'
import { useAsync } from '../../lib/useAsync'
import { WorkoutCard } from '../entries/WorkoutsPage'

/** What a friend shares: everything they marked as visible to friends. */
export default function FriendProfilePage() {
  const { id = '' } = useParams()
  const { data, error, loading } = useAsync(async () => {
    const userId = query({ userId: id })
    const [profile, goals, workouts, measurements, meals] = await Promise.all([
      api.get<UserProfile>(`/api/users/${id}`),
      api.get<Goal[]>(`/api/goals${userId}`),
      api.get<Page<Workout>>(`/api/workouts${userId}`),
      api.get<Measurement[]>(`/api/measurements${userId}`),
      api.get<Meal[]>(`/api/meals${userId}`),
    ])
    return { profile, goals, workouts: workouts.items, measurements, meals }
  }, [id])

  if (loading) return <main className="page" />
  if (error || !data) {
    return (
      <main className="page">
        <ErrorAlert error={error} />
      </main>
    )
  }

  const { profile, goals, workouts, measurements, meals } = data
  const nothingShared = goals.length + workouts.length + measurements.length + meals.length === 0

  return (
    <main className="page">
      <div className="post" style={{ alignItems: 'center', marginBottom: 24 }}>
        <span className="avatar" aria-hidden="true" style={{ width: 56, height: 56, fontSize: '1.5rem' }}>
          {initials(profile.displayName)}
        </span>
        <div>
          <h1 style={{ margin: 0 }}>{profile.displayName}</h1>
          {profile.friendsSince && <span className="muted">Friends since {formatDate(profile.friendsSince.slice(0, 10))}</span>}
        </div>
      </div>

      {nothingShared && <p className="muted">{profile.displayName} has not shared anything with friends yet.</p>}

      {goals.length > 0 && (
        <section className="section">
          <h2>Goals</h2>
          <ul className="entries">
            {goals.filter((g) => g.status !== 'Archived').map((g) => (
              <GoalCard key={g.id} goal={g} />
            ))}
          </ul>
        </section>
      )}

      {workouts.length > 0 && (
        <section className="section">
          <h2>Recent workouts</h2>
          <ul className="entries">
            {workouts.map((w) => (
              <WorkoutCard key={w.id} workout={w} />
            ))}
          </ul>
        </section>
      )}

      {meals.length > 0 && (
        <section className="section">
          <h2>Meals today</h2>
          <ul className="entries">
            {meals.map((m) => (
              <li key={m.id} className="entry" data-kind="meal">
                <div className="entry-head">
                  <span className="entry-title">{m.name}</span>
                  <span className="entry-meta">{m.calories} kcal</span>
                </div>
                <div className="entry-meta">
                  {m.type}, {formatTime(m.eatenAt)}
                </div>
              </li>
            ))}
          </ul>
        </section>
      )}

      {measurements.length > 0 && (
        <section className="section">
          <h2>Body</h2>
          <ul className="entries">
            {measurements.slice(0, 10).map((m) => (
              <li key={m.id} className="entry" data-kind="body">
                <div className="entry-head">
                  <span className="entry-title">{formatNumber(m.weightKg, 2)} kg</span>
                  <span className="entry-meta">{formatDate(m.date)}</span>
                </div>
              </li>
            ))}
          </ul>
        </section>
      )}
    </main>
  )
}
