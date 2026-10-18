import { useEffect, useState, type FormEvent } from 'react'
import { api, query } from '../../api/client'
import type { ExerciseKind, Visibility, Workout, WorkoutRequest } from '../../api/types'
import { ErrorAlert, Field, fieldError, VisibilityToggle } from '../../components/Form'
import { fromLocalInput, toLocalInput } from '../../lib/format'

interface SetDraft {
  reps: string
  weightKg: string
}

interface ExerciseDraft {
  key: number
  name: string
  kind: ExerciseKind
  sets: SetDraft[]
  minutes: string
  distanceKm: string
}

let nextKey = 1
const newExercise = (kind: ExerciseKind = 'Strength'): ExerciseDraft => ({
  key: nextKey++,
  name: '',
  kind,
  sets: kind === 'Strength' ? [{ reps: '', weightKg: '' }] : [],
  minutes: '',
  distanceKm: '',
})

function fromWorkout(workout: Workout): ExerciseDraft[] {
  return workout.exercises.map((x) => ({
    key: nextKey++,
    name: x.name,
    kind: x.kind,
    sets: x.sets.map((s) => ({ reps: String(s.reps), weightKg: String(s.weightKg) })),
    minutes: x.durationSeconds == null ? '' : String(Math.round((x.durationSeconds / 60) * 10) / 10),
    distanceKm: x.distanceKm == null ? '' : String(x.distanceKm),
  }))
}

const toNumber = (value: string) => (value.trim() === '' ? null : Number(value.replace(',', '.')))

function useExerciseSuggestions(text: string) {
  const [names, setNames] = useState<string[]>([])
  useEffect(() => {
    let cancelled = false
    api
      .get<string[]>(`/api/exercises/suggestions${query({ q: text })}`)
      .then((result) => !cancelled && setNames(result))
      .catch(() => !cancelled && setNames([]))
    return () => {
      cancelled = true
    }
  }, [text])
  return names
}

interface Props {
  workout?: Workout
  defaultVisibility: Visibility
  onSaved: (workout: Workout) => void
  onCancel: () => void
}

export default function WorkoutForm({ workout, defaultVisibility, onSaved, onCancel }: Props) {
  const [name, setName] = useState(workout?.name ?? '')
  const [performedAt, setPerformedAt] = useState(toLocalInput(workout?.performedAt ?? new Date()))
  const [duration, setDuration] = useState(workout ? String(workout.durationMinutes) : '')
  const [notes, setNotes] = useState(workout?.notes ?? '')
  const [visibility, setVisibility] = useState<Visibility>(workout?.visibility ?? defaultVisibility)
  const [exercises, setExercises] = useState<ExerciseDraft[]>(() => (workout ? fromWorkout(workout) : [newExercise()]))
  const [activeName, setActiveName] = useState('')
  const suggestions = useExerciseSuggestions(activeName)
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)

  const update = (key: number, change: Partial<ExerciseDraft>) =>
    setExercises((list) => list.map((x) => (x.key === key ? { ...x, ...change } : x)))

  const updateSet = (key: number, index: number, change: Partial<SetDraft>) =>
    setExercises((list) =>
      list.map((x) => (x.key === key ? { ...x, sets: x.sets.map((s, i) => (i === index ? { ...s, ...change } : s)) } : x)),
    )

  // A new set starts as a copy of the previous one: most sets repeat the same reps and weight
  const addSet = (key: number) =>
    setExercises((list) =>
      list.map((x) => (x.key === key ? { ...x, sets: [...x.sets, x.sets.at(-1) ?? { reps: '', weightKg: '' }] } : x)),
    )

  const removeSet = (key: number, index: number) =>
    setExercises((list) => list.map((x) => (x.key === key ? { ...x, sets: x.sets.filter((_, i) => i !== index) } : x)))

  async function submit(event: FormEvent) {
    event.preventDefault()
    const body: WorkoutRequest = {
      name,
      performedAt: fromLocalInput(performedAt),
      durationMinutes: Number(duration),
      notes: notes.trim() || null,
      visibility,
      exercises: exercises.map((x) => ({
        name: x.name,
        kind: x.kind,
        durationSeconds: x.kind === 'Cardio' && toNumber(x.minutes) != null ? Math.round(toNumber(x.minutes)! * 60) : null,
        distanceKm: x.kind === 'Cardio' ? toNumber(x.distanceKm) : null,
        sets: x.kind === 'Strength' ? x.sets.map((s) => ({ reps: Number(s.reps), weightKg: toNumber(s.weightKg) ?? 0 })) : [],
      })),
    }

    setBusy(true)
    setError(null)
    try {
      const saved = workout
        ? await api.put<Workout>(`/api/workouts/${workout.id}`, body)
        : await api.post<Workout>('/api/workouts', body)
      onSaved(saved)
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="form" onSubmit={submit} data-kind="workout">
      <ErrorAlert error={error} />
      <Field label="Workout" error={fieldError(error, 'name')}>
        {(p) => <input {...p} required maxLength={100} placeholder="Leg day" value={name} onChange={(e) => setName(e.target.value)} />}
      </Field>
      <div className="form-row">
        <Field label="Started" error={fieldError(error, 'performedAt')}>
          {(p) => <input {...p} type="datetime-local" required value={performedAt} onChange={(e) => setPerformedAt(e.target.value)} />}
        </Field>
        <Field label="Duration (min)" error={fieldError(error, 'durationMinutes')}>
          {(p) => (
            <input {...p} type="number" inputMode="numeric" min={1} max={600} required value={duration} onChange={(e) => setDuration(e.target.value)} />
          )}
        </Field>
      </div>

      <datalist id="exercise-names">
        {suggestions.map((s) => (
          <option key={s} value={s} />
        ))}
      </datalist>

      {exercises.map((x, i) => (
        <fieldset key={x.key}>
          <legend>Exercise {i + 1}</legend>
          <div className="form">
            <div className="form-row">
              <Field label="Name" error={fieldError(error, `exercises[${i}].name`)}>
                {(p) => (
                  <input
                    {...p}
                    list="exercise-names"
                    required
                    maxLength={100}
                    value={x.name}
                    onFocus={() => setActiveName(x.name)}
                    onChange={(e) => {
                      update(x.key, { name: e.target.value })
                      setActiveName(e.target.value)
                    }}
                  />
                )}
              </Field>
              <Field label="Type">
                {(p) => (
                  <select
                    {...p}
                    value={x.kind}
                    onChange={(e) => {
                      const kind = e.target.value as ExerciseKind
                      update(x.key, { kind, sets: kind === 'Strength' && x.sets.length === 0 ? [{ reps: '', weightKg: '' }] : x.sets })
                    }}
                  >
                    <option value="Strength">Strength</option>
                    <option value="Cardio">Cardio</option>
                  </select>
                )}
              </Field>
            </div>

            {x.kind === 'Strength' ? (
              <div className="form" role="group" aria-label={`Sets of exercise ${i + 1}`}>
                {fieldError(error, `exercises[${i}].sets`) && <span className="field-error">{fieldError(error, `exercises[${i}].sets`)}</span>}
                {x.sets.map((s, j) => (
                  <div className="set-row" key={j}>
                    <span className="set-number" aria-label={`Set ${j + 1}`}>
                      {j + 1}
                    </span>
                    <input
                      aria-label={`Set ${j + 1} reps`}
                      aria-invalid={Boolean(fieldError(error, `exercises[${i}].sets[${j}].reps`))}
                      type="number"
                      inputMode="numeric"
                      min={1}
                      placeholder="Reps"
                      required
                      value={s.reps}
                      onChange={(e) => updateSet(x.key, j, { reps: e.target.value })}
                    />
                    <input
                      aria-label={`Set ${j + 1} weight in kg`}
                      type="number"
                      inputMode="decimal"
                      min={0}
                      step="0.25"
                      placeholder="kg"
                      value={s.weightKg}
                      onChange={(e) => updateSet(x.key, j, { weightKg: e.target.value })}
                    />
                    <button type="button" className="btn btn-secondary btn-small" onClick={() => removeSet(x.key, j)} aria-label={`Remove set ${j + 1}`}>
                      Remove
                    </button>
                  </div>
                ))}
                <div>
                  <button type="button" className="btn btn-secondary btn-small" onClick={() => addSet(x.key)}>
                    Add set
                  </button>
                </div>
              </div>
            ) : (
              <div className="form-row">
                <Field label="Time (min)" error={fieldError(error, `exercises[${i}].durationSeconds`)}>
                  {(p) => (
                    <input {...p} type="number" inputMode="decimal" min={0} step="0.5" value={x.minutes} onChange={(e) => update(x.key, { minutes: e.target.value })} />
                  )}
                </Field>
                <Field label="Distance (km)" error={fieldError(error, `exercises[${i}].distanceKm`)}>
                  {(p) => (
                    <input {...p} type="number" inputMode="decimal" min={0} step="0.01" value={x.distanceKm} onChange={(e) => update(x.key, { distanceKm: e.target.value })} />
                  )}
                </Field>
              </div>
            )}

            {exercises.length > 1 && (
              <div>
                <button type="button" className="btn-link" onClick={() => setExercises((list) => list.filter((e) => e.key !== x.key))}>
                  Remove this exercise
                </button>
              </div>
            )}
          </div>
        </fieldset>
      ))}

      {fieldError(error, 'exercises') && <span className="field-error">{fieldError(error, 'exercises')}</span>}
      <div className="actions">
        <button type="button" className="btn btn-secondary" onClick={() => setExercises((list) => [...list, newExercise()])}>
          Add strength exercise
        </button>
        <button type="button" className="btn btn-secondary" onClick={() => setExercises((list) => [...list, newExercise('Cardio')])}>
          Add cardio
        </button>
      </div>

      <Field label="Notes" error={fieldError(error, 'notes')}>
        {(p) => <textarea {...p} maxLength={1000} value={notes} onChange={(e) => setNotes(e.target.value)} />}
      </Field>
      <VisibilityToggle value={visibility} onChange={setVisibility} />

      <div className="actions">
        <button className="btn" type="submit" disabled={busy}>
          {busy ? 'Saving…' : workout ? 'Save changes' : 'Save workout'}
        </button>
        <button className="btn btn-secondary" type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  )
}
