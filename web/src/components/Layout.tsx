import { NavLink, Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

const links = [
  { to: '/', label: 'Feed', end: true },
  { to: '/log', label: 'Log', end: false },
  { to: '/progress', label: 'Progress', end: false },
  { to: '/friends', label: 'Friends', end: false },
  { to: '/settings', label: 'Me', end: false },
]

/** The frame around every page once you are logged in: top bar on wide screens, tab bar on phones. */
export default function Layout() {
  const { user } = useAuth()
  if (!user) return <Navigate to="/login" replace />

  return (
    <div className="shell">
      <header className="topbar">
        <NavLink to="/" className="brand">
          <span className="brand-mark" aria-hidden="true" />
          FitTrack
        </NavLink>
        <nav className="nav" aria-label="Main">
          {links.map((link) => (
            <NavLink key={link.to} to={link.to} end={link.end}>
              {link.label}
            </NavLink>
          ))}
        </nav>
      </header>
      <Outlet />
      <nav className="tabbar" aria-label="Main">
        {links.map((link) => (
          <NavLink key={link.to} to={link.to} end={link.end}>
            {link.label}
          </NavLink>
        ))}
      </nav>
    </div>
  )
}

/** Tabs within a section, e.g. Workouts / Meals / Body under Log. */
export function SubNav({ links }: { links: { to: string; label: string; end?: boolean }[] }) {
  return (
    <nav className="subnav" aria-label="Section">
      {links.map((link) => (
        <NavLink key={link.to} to={link.to} end={link.end ?? false}>
          {link.label}
        </NavLink>
      ))}
    </nav>
  )
}
