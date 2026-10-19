import type { ReactNode } from 'react'
import type { Goal } from '../api/types'
import { describeProgress, metricInfo } from '../lib/goals'

export default function GoalCard({ goal, actions }: { goal: Goal; actions?: ReactNode }) {
  const percent = goal.status === 'Achieved' ? 100 : (goal.progress?.percent ?? 0)
  return (
    <li className="entry" data-kind="goal">
      <div className="entry-head">
        <h3 className="entry-title">{metricInfo(goal.metric).label}</h3>
        <span className="entry-meta">
          {goal.status === 'Active' ? (goal.progress?.isMet ? 'On target' : `${Math.round(percent)}%`) : goal.status}
        </span>
      </div>
      <div className="entry-meta">
        {describeProgress(goal)}
        {goal.visibility === 'Private' && ', only you'}
      </div>
      {goal.status !== 'Archived' && (
        <div
          className="progress"
          role="progressbar"
          aria-valuemin={0}
          aria-valuemax={100}
          aria-valuenow={Math.round(percent)}
          aria-label={`${metricInfo(goal.metric).label} progress`}
        >
          <div className="progress-fill" style={{ width: `${percent}%` }} />
        </div>
      )}
      {actions && <div className="entry-actions">{actions}</div>}
    </li>
  )
}
