import type { Goal, GoalMetric } from '../api/types'
import { formatDate, formatNumber } from './format'

export const goalMetrics: { metric: GoalMetric; label: string; unit: string; min: number; max: number; step: number }[] = [
  { metric: 'BodyWeight', label: 'Target weight', unit: 'kg', min: 20, max: 400, step: 0.1 },
  { metric: 'WorkoutsPerWeek', label: 'Workouts per week', unit: 'workouts', min: 1, max: 14, step: 1 },
  { metric: 'DailyCalories', label: 'Calories per day', unit: 'kcal', min: 500, max: 10000, step: 50 },
  { metric: 'DailyProtein', label: 'Protein per day', unit: 'g', min: 10, max: 1000, step: 5 },
]

export const metricInfo = (metric: GoalMetric) => goalMetrics.find((m) => m.metric === metric)!

/** One line that says where the goal stands, e.g. "2 of 3 workouts this week". */
export function describeProgress(goal: Goal): string {
  const info = metricInfo(goal.metric)
  const target = `${formatNumber(goal.targetValue)} ${info.unit}`

  if (goal.status === 'Achieved') {
    return goal.achievedAt ? `Reached on ${formatDate(goal.achievedAt.slice(0, 10))}` : 'Reached'
  }
  if (goal.status === 'Archived') return `Archived. Target was ${target}.`

  const current = goal.progress?.current
  switch (goal.metric) {
    case 'BodyWeight':
      return current == null
        ? `Target ${target}${goal.targetDate ? ` by ${formatDate(goal.targetDate)}` : ''}`
        : `${formatNumber(current)} kg now, target ${target}${goal.targetDate ? ` by ${formatDate(goal.targetDate)}` : ''}`
    case 'WorkoutsPerWeek':
      return current == null ? `Target ${target}` : `${current} of ${formatNumber(goal.targetValue)} workouts this week`
    case 'DailyCalories':
      return current == null ? `Target ${target}` : `${formatNumber(current, 0)} of ${target} today`
    case 'DailyProtein':
      return current == null ? `Target ${target}` : `${formatNumber(current, 0)} of ${target} today`
  }
}
