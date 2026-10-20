import { Outlet } from 'react-router-dom'
import { SubNav } from '../../components/Layout'

export default function ProgressPage() {
  return (
    <main className="page">
      <h1>Progress</h1>
      <SubNav
        links={[
          { to: '/progress', label: 'Dashboard', end: true },
          { to: '/progress/goals', label: 'Goals' },
        ]}
      />
      <Outlet />
    </main>
  )
}
