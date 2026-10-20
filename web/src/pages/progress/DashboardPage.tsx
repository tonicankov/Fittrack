import { useState } from 'react'
import { Link } from 'react-router-dom'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Line,
  LineChart,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { api, query } from '../../api/client'
import type { Nutrition, WeekVolume, WeightTrend, WorkoutsPerWeek } from '../../api/types'
import ChartCard from '../../components/ChartCard'
import { ErrorAlert } from '../../components/Form'
import { formatDate, formatNumber, parseDate } from '../../lib/format'
import { useAsync } from '../../lib/useAsync'

// Recharts takes colours as props, so read them once from the CSS tokens
const css = (name: string, fallback: string) =>
  typeof window === 'undefined' ? fallback : getComputedStyle(document.documentElement).getPropertyValue(name).trim() || fallback

const shortDate = new Intl.DateTimeFormat(undefined, { day: 'numeric', month: 'short' })
const tick = (date: string) => shortDate.format(parseDate(date))

const ranges = [
  { value: '30d', label: '30 days' },
  { value: '90d', label: '90 days' },
  { value: '1y', label: '1 year' },
  { value: 'all', label: 'All' },
]

const axisProps = {
  tickLine: false,
  axisLine: false,
  fontSize: 12,
} as const

function Segmented({ value, options, onChange, label }: { value: string; options: { value: string; label: string }[]; onChange: (v: string) => void; label: string }) {
  return (
    <div className="segmented" role="group" aria-label={label}>
      {options.map((o) => (
        <button key={o.value} type="button" aria-pressed={value === o.value} onClick={() => onChange(o.value)}>
          {o.label}
        </button>
      ))}
    </div>
  )
}

/** Evenly spaced whole-number ticks around the values, with a little room above and below. */
function evenTicks(values: number[]): number[] {
  const min = Math.floor(Math.min(...values) - 0.5)
  const max = Math.ceil(Math.max(...values) + 0.5)
  const step = Math.max(1, Math.ceil((max - min) / 4))
  const start = Math.floor(min / step) * step
  const ticks: number[] = []
  for (let t = start; t < max + step; t += step) ticks.push(t)
  return ticks
}

export default function DashboardPage() {
  const [range, setRange] = useState('90d')
  const [days, setDays] = useState('7')

  const weight = useAsync(() => api.get<WeightTrend>(`/api/stats/weight${query({ range })}`), [range])
  const training = useAsync(
    () =>
      Promise.all([
        api.get<WorkoutsPerWeek>('/api/stats/workouts-per-week?weeks=12'),
        api.get<WeekVolume[]>('/api/stats/volume?weeks=12'),
      ]),
    [],
  )
  const nutrition = useAsync(() => api.get<Nutrition>(`/api/stats/nutrition${query({ days })}`), [days])

  const colors = {
    workout: css('--chart-workout', '#2357c6'),
    meal: css('--chart-meal', '#1e8a4c'),
    body: css('--chart-body', '#a87a00'),
    grid: css('--chart-grid', '#e4e8ed'),
    axis: css('--chart-axis', '#4a5566'),
    target: css('--chart-target', '#1d2430'),
  }

  const weightPoints = weight.data?.points ?? []
  const weightTicks = evenTicks([
    ...weightPoints.map((p) => p.weightKg),
    ...(weight.data?.targetKg != null ? [weight.data.targetKg] : []),
  ])
  const latestWeight = weightPoints.at(-1)
  const perWeek = training.data?.[0]
  const volume = training.data?.[1] ?? []
  const thisWeek = perWeek?.weeks.at(-1)
  const todayNutrition = nutrition.data?.days.at(-1)

  return (
    <>
      <ErrorAlert error={weight.error ?? training.error ?? nutrition.error} />

      <div className="stats">
        <div className="stat" data-kind="body">
          <span className="stat-value">{latestWeight ? formatNumber(latestWeight.weightKg) : '–'}</span>
          <span className="stat-unit">kg</span>
          <span className="stat-label">
            {latestWeight ? `Weight on ${formatDate(latestWeight.date)}` : 'No weigh-ins in this period'}
          </span>
        </div>
        <div className="stat" data-kind="workout">
          <span className="stat-value">{thisWeek?.workouts ?? '–'}</span>
          {perWeek?.target != null && <span className="stat-unit">/ {perWeek.target}</span>}
          <span className="stat-label">Workouts this week</span>
        </div>
        <div className="stat" data-kind="meal">
          <span className="stat-value">{todayNutrition?.calories ?? '–'}</span>
          <span className="stat-unit">kcal</span>
          <span className="stat-label">
            Today{nutrition.data?.caloriesTarget != null ? `, target ${nutrition.data.caloriesTarget}` : ''}
          </span>
        </div>
      </div>

      <ChartCard
        title="Body weight"
        summary={
          weightPoints.length > 0
            ? `Body weight from ${formatNumber(weightPoints[0].weightKg)} kg to ${formatNumber(weightPoints[weightPoints.length - 1].weightKg)} kg`
            : 'No weigh-ins in this period'
        }
        controls={<Segmented label="Period" value={range} options={ranges} onChange={setRange} />}
        table={{ columns: ['Date', 'Weight (kg)'], rows: weightPoints.map((p) => [formatDate(p.date), formatNumber(p.weightKg, 2)]) }}
      >
        {weightPoints.length === 0 ? (
          <p className="muted" style={{ padding: 16 }}>
            No weigh-ins in this period. <Link to="/log/body">Record your weight</Link>
          </p>
        ) : (
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={weightPoints} margin={{ top: 16, right: 56, bottom: 0, left: 0 }}>
              <CartesianGrid vertical={false} stroke={colors.grid} />
              <XAxis dataKey="date" tickFormatter={tick} {...axisProps} stroke={colors.axis} minTickGap={24} />
              <YAxis
                {...axisProps}
                stroke={colors.axis}
                width={40}
                domain={[weightTicks[0], weightTicks[weightTicks.length - 1]]}
                ticks={weightTicks}
                interval={0}
              />
              <Tooltip
                labelFormatter={(d) => formatDate(String(d))}
                formatter={(v) => [`${formatNumber(Number(v), 2)} kg`, 'Weight']}
              />
              {weight.data?.targetKg != null && (
                <ReferenceLine
                  y={weight.data.targetKg}
                  stroke={colors.target}
                  strokeDasharray="4 4"
                  label={{ value: `Target ${formatNumber(weight.data.targetKg)}`, position: 'right', fill: colors.axis, fontSize: 12 }}
                />
              )}
              <Line
                type="monotone"
                dataKey="weightKg"
                stroke={colors.body}
                strokeWidth={2}
                dot={weightPoints.length <= 31 ? { r: 4, fill: colors.body, stroke: '#fff', strokeWidth: 2 } : false}
                activeDot={{ r: 5, stroke: '#fff', strokeWidth: 2 }}
                isAnimationActive={false}
              />
            </LineChart>
          </ResponsiveContainer>
        )}
      </ChartCard>

      <ChartCard
        title="Workouts per week"
        summary={`Workouts per week over the last 12 weeks${perWeek?.target != null ? `, against a target of ${perWeek.target}` : ''}`}
        table={{
          columns: ['Week of', 'Workouts'],
          rows: (perWeek?.weeks ?? []).map((w) => [formatDate(w.weekStart), w.workouts]),
        }}
      >
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={perWeek?.weeks ?? []} margin={{ top: 16, right: 56, bottom: 0, left: 0 }}>
            <CartesianGrid vertical={false} stroke={colors.grid} />
            <XAxis dataKey="weekStart" tickFormatter={tick} {...axisProps} stroke={colors.axis} minTickGap={16} />
            <YAxis {...axisProps} stroke={colors.axis} width={40} allowDecimals={false} />
            <Tooltip labelFormatter={(d) => `Week of ${formatDate(String(d))}`} formatter={(v) => [v, 'Workouts']} cursor={{ fill: colors.grid }} />
            {perWeek?.target != null && (
              <ReferenceLine
                y={perWeek.target}
                stroke={colors.target}
                strokeDasharray="4 4"
                label={{ value: `Target ${perWeek.target}`, position: 'right', fill: colors.axis, fontSize: 12 }}
              />
            )}
            <Bar dataKey="workouts" fill={colors.workout} maxBarSize={24} radius={[4, 4, 0, 0]} isAnimationActive={false} />
          </BarChart>
        </ResponsiveContainer>
      </ChartCard>

      <ChartCard
        title="Strength volume per week"
        summary="Kilograms lifted per week (repetitions times weight), over the last 12 weeks"
        table={{ columns: ['Week of', 'Volume (kg)'], rows: volume.map((w) => [formatDate(w.weekStart), formatNumber(w.volumeKg, 0)]) }}
      >
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={volume} margin={{ top: 16, right: 16, bottom: 0, left: 0 }}>
            <CartesianGrid vertical={false} stroke={colors.grid} />
            <XAxis dataKey="weekStart" tickFormatter={tick} {...axisProps} stroke={colors.axis} minTickGap={16} />
            <YAxis
              {...axisProps}
              stroke={colors.axis}
              width={48}
              tickFormatter={(v: number) => (v >= 10000 ? `${Math.round(v / 1000)}K` : v.toLocaleString())}
            />
            <Tooltip
              labelFormatter={(d) => `Week of ${formatDate(String(d))}`}
              formatter={(v) => [`${formatNumber(Number(v), 0)} kg`, 'Volume']}
              cursor={{ fill: colors.grid }}
            />
            <Bar dataKey="volumeKg" fill={colors.workout} maxBarSize={24} radius={[4, 4, 0, 0]} isAnimationActive={false} />
          </BarChart>
        </ResponsiveContainer>
      </ChartCard>

      {(['calories', 'proteinG'] as const).map((key) => {
        const isCalories = key === 'calories'
        const target = isCalories ? nutrition.data?.caloriesTarget : nutrition.data?.proteinTarget
        const unit = isCalories ? 'kcal' : 'g'
        const series = nutrition.data?.days ?? []
        return (
          <ChartCard
            key={key}
            title={isCalories ? 'Calories per day' : 'Protein per day'}
            summary={`${isCalories ? 'Calories' : 'Protein'} per day over the last ${days} days. Days without logged meals are left empty.`}
            controls={
              isCalories ? (
                <Segmented
                  label="Period"
                  value={days}
                  options={[
                    { value: '7', label: '7 days' },
                    { value: '30', label: '30 days' },
                  ]}
                  onChange={setDays}
                />
              ) : undefined
            }
            table={{
              columns: ['Date', `${isCalories ? 'Calories' : 'Protein'} (${unit})`],
              rows: series.map((d) => [formatDate(d.date), d[key] == null ? 'not logged' : formatNumber(d[key]!, 0)]),
            }}
          >
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={series} margin={{ top: 16, right: 64, bottom: 0, left: 0 }}>
                <CartesianGrid vertical={false} stroke={colors.grid} />
                <XAxis dataKey="date" tickFormatter={tick} {...axisProps} stroke={colors.axis} minTickGap={16} />
                <YAxis {...axisProps} stroke={colors.axis} width={48} allowDecimals={false} />
                <Tooltip
                  labelFormatter={(d) => formatDate(String(d))}
                  formatter={(v) => [`${formatNumber(Number(v), 0)} ${unit}`, isCalories ? 'Calories' : 'Protein']}
                  cursor={{ fill: colors.grid }}
                />
                {target != null && (
                  <ReferenceLine
                    y={target}
                    stroke={colors.target}
                    strokeDasharray="4 4"
                    label={{ value: `Target ${formatNumber(target, 0)}`, position: 'right', fill: colors.axis, fontSize: 12 }}
                  />
                )}
                <Bar dataKey={key} fill={colors.meal} maxBarSize={24} radius={[4, 4, 0, 0]} isAnimationActive={false} />
              </BarChart>
            </ResponsiveContainer>
          </ChartCard>
        )
      })}
    </>
  )
}
