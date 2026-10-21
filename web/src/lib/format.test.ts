import { describe, expect, it } from 'vitest'
import { addDays, formatDuration, initials, parseDate, timeAgo, toDateString } from './format'

describe('calendar dates', () => {
  it('parses a date at local midnight so the day does not shift', () => {
    const d = parseDate('2026-10-12')
    expect(d.getFullYear()).toBe(2026)
    expect(d.getMonth()).toBe(9)
    expect(d.getDate()).toBe(12)
  })

  it('round-trips through toDateString', () => {
    expect(toDateString(parseDate('2026-01-05'))).toBe('2026-01-05')
  })

  it('adds days across month ends', () => {
    expect(addDays('2026-10-31', 1)).toBe('2026-11-01')
    expect(addDays('2026-03-01', -1)).toBe('2026-02-28')
  })
})

describe('timeAgo', () => {
  const now = new Date('2026-10-12T12:00:00Z')

  it('says "just now" within a minute', () => {
    expect(timeAgo('2026-10-12T11:59:30Z', now)).toBe('just now')
  })

  it('counts minutes and hours', () => {
    expect(timeAgo('2026-10-12T11:45:00Z', now)).toBe('15 min ago')
    expect(timeAgo('2026-10-12T09:00:00Z', now)).toBe('3 h ago')
  })
})

describe('small formatters', () => {
  it('formats durations in minutes', () => {
    expect(formatDuration(600)).toBe('10 min')
    expect(formatDuration(630)).toBe('10:30 min')
  })

  it('takes up to two initials', () => {
    expect(initials('Antoniy Tsankov')).toBe('AT')
    expect(initials('nadia')).toBe('N')
    expect(initials('Anna Maria van Dijk')).toBe('AM')
  })
})
