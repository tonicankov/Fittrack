import { describe, expect, it } from 'vitest'
import type { Goal } from '../api/types'
import { describeProgress } from './goals'

const goal = (overrides: Partial<Goal>): Goal => ({
  id: 'g1',
  userId: 'u1',
  metric: 'WorkoutsPerWeek',
  targetValue: 3,
  startValue: null,
  startDate: '2026-10-01',
  targetDate: null,
  status: 'Active',
  achievedAt: null,
  visibility: 'Friends',
  progress: { current: 2, percent: 66.7, isMet: false, period: 'thisWeek' },
  ...overrides,
})

describe('describeProgress', () => {
  it('describes weekly workouts', () => {
    expect(describeProgress(goal({}))).toBe('2 of 3 workouts this week')
  })

  it('falls back to the target when a friend cannot see the numbers', () => {
    expect(describeProgress(goal({ progress: { current: null, percent: 50, isMet: false, period: 'thisWeek' } }))).toBe(
      'Target 3 workouts',
    )
  })

  it('mentions archived goals', () => {
    expect(describeProgress(goal({ status: 'Archived', progress: null }))).toBe('Archived. Target was 3 workouts.')
  })
})
