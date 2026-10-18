import { Outlet } from 'react-router-dom'
import { SubNav } from '../../components/Layout'

export default function LogPage() {
  return (
    <main className="page">
      <h1>Log</h1>
      <SubNav
        links={[
          { to: '/log/workouts', label: 'Workouts' },
          { to: '/log/meals', label: 'Meals' },
        ]}
      />
      <Outlet />
    </main>
  )
}
