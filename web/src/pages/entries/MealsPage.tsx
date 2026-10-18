import { useState, type FormEvent } from 'react'
import { api, query } from '../../api/client'
import type { DailyTotals, Meal, MealType, Visibility } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'
import { Empty, ErrorAlert, Field, fieldError, VisibilityToggle } from '../../components/Form'
import { addDays, formatDate, formatNumber, formatTime, fromLocalInput, toLocalInput, today } from '../../lib/format'
import { useAsync } from '../../lib/useAsync'

const mealTypes: MealType[] = ['Breakfast', 'Lunch', 'Dinner', 'Snack']

const toNumber = (value: string) => (value.trim() === '' ? null : Number(value.replace(',', '.')))

/** A sensible meal type for the current time of day. */
function guessType(date = new Date()): MealType {
  const hour = date.getHours()
  if (hour < 11) return 'Breakfast'
  if (hour < 15) return 'Lunch'
  if (hour >= 17 && hour < 22) return 'Dinner'
  return 'Snack'
}

function MealForm({ meal, day, onDone }: { meal?: Meal; day: string; onDone: () => void }) {
  const { user } = useAuth()
  const defaultTime = day === today() ? new Date() : new Date(`${day}T12:00`)
  const [form, setForm] = useState({
    name: meal?.name ?? '',
    type: meal?.type ?? guessType(defaultTime),
    eatenAt: toLocalInput(meal?.eatenAt ?? defaultTime),
    calories: meal ? String(meal.calories) : '',
    proteinG: meal?.proteinG == null ? '' : String(meal.proteinG),
    carbsG: meal?.carbsG == null ? '' : String(meal.carbsG),
    fatG: meal?.fatG == null ? '' : String(meal.fatG),
  })
  const [visibility, setVisibility] = useState<Visibility>(meal?.visibility ?? user?.defaultVisibility ?? 'Friends')
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)

  const set = (key: keyof typeof form) => (e: { target: { value: string } }) => setForm({ ...form, [key]: e.target.value })

  async function submit(event: FormEvent) {
    event.preventDefault()
    const body = {
      name: form.name,
      type: form.type,
      eatenAt: fromLocalInput(form.eatenAt),
      calories: Number(form.calories),
      proteinG: toNumber(form.proteinG),
      carbsG: toNumber(form.carbsG),
      fatG: toNumber(form.fatG),
      visibility,
    }
    setBusy(true)
    setError(null)
    try {
      if (meal) await api.put<Meal>(`/api/meals/${meal.id}`, body)
      else await api.post<Meal>('/api/meals', body)
      onDone()
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="card form" onSubmit={submit} data-kind="meal">
      <ErrorAlert error={error} />
      <div className="form-row">
        <Field label="What you ate" error={fieldError(error, 'name')}>
          {(p) => <input {...p} required maxLength={100} placeholder="Oats with banana" value={form.name} onChange={set('name')} />}
        </Field>
        <Field label="Meal">
          {(p) => (
            <select {...p} value={form.type} onChange={set('type')}>
              {mealTypes.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
          )}
        </Field>
      </div>
      <div className="form-row">
        <Field label="Eaten at" error={fieldError(error, 'eatenAt')}>
          {(p) => <input {...p} type="datetime-local" required value={form.eatenAt} onChange={set('eatenAt')} />}
        </Field>
        <Field label="Calories (kcal)" error={fieldError(error, 'calories')}>
          {(p) => <input {...p} type="number" inputMode="numeric" min={0} max={10000} required value={form.calories} onChange={set('calories')} />}
        </Field>
      </div>
      <div className="form-row">
        <Field label="Protein (g)" error={fieldError(error, 'proteinG')}>
          {(p) => <input {...p} type="number" inputMode="decimal" min={0} step="0.1" value={form.proteinG} onChange={set('proteinG')} />}
        </Field>
        <Field label="Carbs (g)" error={fieldError(error, 'carbsG')}>
          {(p) => <input {...p} type="number" inputMode="decimal" min={0} step="0.1" value={form.carbsG} onChange={set('carbsG')} />}
        </Field>
        <Field label="Fat (g)" error={fieldError(error, 'fatG')}>
          {(p) => <input {...p} type="number" inputMode="decimal" min={0} step="0.1" value={form.fatG} onChange={set('fatG')} />}
        </Field>
      </div>
      <VisibilityToggle value={visibility} onChange={setVisibility} />
      <div className="actions">
        <button className="btn" type="submit" disabled={busy}>
          {busy ? 'Saving…' : meal ? 'Save changes' : 'Save meal'}
        </button>
        <button className="btn btn-secondary" type="button" onClick={onDone}>
          Cancel
        </button>
      </div>
    </form>
  )
}

export default function MealsPage() {
  const [day, setDay] = useState(today())
  const [editing, setEditing] = useState<Meal | 'new' | null>(null)
  const [error, setError] = useState<unknown>(null)
  const { data, reload, loading } = useAsync(async () => {
    const [meals, totals] = await Promise.all([
      api.get<Meal[]>(`/api/meals${query({ date: day })}`),
      api.get<DailyTotals[]>(`/api/meals/daily-totals${query({ from: day, to: day })}`),
    ])
    return { meals, totals: totals[0] ?? null }
  }, [day])

  async function remove(meal: Meal) {
    if (!confirm(`Delete "${meal.name}"?`)) return
    try {
      await api.delete(`/api/meals/${meal.id}`)
      await reload()
    } catch (err) {
      setError(err)
    }
  }

  const done = () => {
    setEditing(null)
    void reload()
  }

  const totals = data?.totals

  return (
    <>
      <div className="page-header">
        <h2>Meals</h2>
        <div className="actions">
          <button className="btn btn-secondary btn-small" onClick={() => setDay(addDays(day, -1))} aria-label="Previous day">
            Previous
          </button>
          <input
            type="date"
            aria-label="Day"
            value={day}
            max={today()}
            onChange={(e) => e.target.value && setDay(e.target.value)}
            style={{ width: 'auto' }}
          />
          <button className="btn btn-secondary btn-small" onClick={() => setDay(addDays(day, 1))} disabled={day >= today()} aria-label="Next day">
            Next
          </button>
        </div>
      </div>

      <div className="stats" data-kind="meal" style={{ marginBottom: 16 }}>
        <div className="stat">
          <span className="stat-value">{totals ? totals.calories : '–'}</span>
          <span className="stat-unit">kcal</span>
          <span className="stat-label">{formatDate(day)}</span>
        </div>
        <div className="stat">
          <span className="stat-value">{totals?.proteinG ? formatNumber(totals.proteinG, 0) : '–'}</span>
          <span className="stat-unit">g</span>
          <span className="stat-label">Protein</span>
        </div>
        <div className="stat">
          <span className="stat-value">{totals?.carbsG ? formatNumber(totals.carbsG, 0) : '–'}</span>
          <span className="stat-unit">g</span>
          <span className="stat-label">Carbs</span>
        </div>
        <div className="stat">
          <span className="stat-value">{totals?.fatG ? formatNumber(totals.fatG, 0) : '–'}</span>
          <span className="stat-unit">g</span>
          <span className="stat-label">Fat</span>
        </div>
      </div>

      <ErrorAlert error={error} />
      {editing === 'new' ? (
        <MealForm day={day} onDone={done} />
      ) : (
        <div className="actions" style={{ marginBottom: 16 }}>
          <button className="btn" onClick={() => setEditing('new')}>
            Log meal
          </button>
        </div>
      )}

      {!loading && data?.meals.length === 0 && editing !== 'new' && (
        <Empty>
          <p>Nothing logged for {formatDate(day)}.</p>
        </Empty>
      )}

      <ul className="entries">
        {data?.meals.map((meal) =>
          editing !== 'new' && editing?.id === meal.id ? (
            <li key={meal.id}>
              <MealForm meal={meal} day={day} onDone={done} />
            </li>
          ) : (
            <li key={meal.id} className="entry" data-kind="meal">
              <div className="entry-head">
                <h3 className="entry-title">{meal.name}</h3>
                <span className="entry-meta">{meal.calories} kcal</span>
              </div>
              <div className="entry-meta">
                {meal.type}, {formatTime(meal.eatenAt)}
                {meal.proteinG != null && `, ${formatNumber(meal.proteinG)} g protein`}
                {meal.visibility === 'Private' && ', only you'}
              </div>
              <div className="entry-actions">
                <button className="btn btn-secondary btn-small" onClick={() => setEditing(meal)}>
                  Edit
                </button>
                <button className="btn btn-danger btn-small" onClick={() => remove(meal)}>
                  Delete
                </button>
              </div>
            </li>
          ),
        )}
      </ul>
    </>
  )
}
