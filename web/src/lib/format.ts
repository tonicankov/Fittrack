// Formatting helpers. Timestamps come from the API in UTC and are shown in the
// browser's time zone; calendar dates ("2026-10-12") are shown as they are.

const dateFormat = new Intl.DateTimeFormat(undefined, { weekday: 'short', day: 'numeric', month: 'short' })
const timeFormat = new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit' })

/** "2026-10-12" → a Date at local midnight (not UTC, which would shift the day). */
export function parseDate(date: string): Date {
  const [y, m, d] = date.split('-').map(Number)
  return new Date(y, m - 1, d)
}

/** A Date → "2026-10-12" in local time. */
export function toDateString(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

export function today(): string {
  return toDateString(new Date())
}

export function addDays(date: string, days: number): string {
  const d = parseDate(date)
  d.setDate(d.getDate() + days)
  return toDateString(d)
}

export function formatDate(date: string): string {
  return dateFormat.format(parseDate(date))
}

export function formatDateTime(iso: string): string {
  const d = new Date(iso)
  return `${dateFormat.format(d)}, ${timeFormat.format(d)}`
}

export function formatTime(iso: string): string {
  return timeFormat.format(new Date(iso))
}

/** "just now", "5 min ago", "3 h ago", then the date. */
export function timeAgo(iso: string, now: Date = new Date()): string {
  const seconds = Math.round((now.getTime() - new Date(iso).getTime()) / 1000)
  if (seconds < 60) return 'just now'
  const minutes = Math.round(seconds / 60)
  if (minutes < 60) return `${minutes} min ago`
  const hours = Math.round(minutes / 60)
  if (hours < 24) return `${hours} h ago`
  return formatDateTime(iso)
}

/** The value for an <input type="datetime-local"> showing this moment in local time. */
export function toLocalInput(iso: string | Date): string {
  const d = typeof iso === 'string' ? new Date(iso) : iso
  return `${toDateString(d)}T${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}

/** An <input type="datetime-local"> value → ISO string in UTC. */
export function fromLocalInput(value: string): string {
  return new Date(value).toISOString()
}

/** Up to one decimal, without trailing ".0". */
export function formatNumber(value: number, decimals = 1): string {
  return Number(value.toFixed(decimals)).toLocaleString(undefined, { maximumFractionDigits: decimals })
}

export function formatDuration(seconds: number): string {
  const m = Math.floor(seconds / 60)
  const s = seconds % 60
  return s === 0 ? `${m} min` : `${m}:${String(s).padStart(2, '0')} min`
}

export function initials(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]!.toUpperCase())
    .join('')
}
