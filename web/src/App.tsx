import { Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider, useAuth } from './auth/AuthContext'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'

function Home() {
  const { user, logout } = useAuth()
  if (!user) return <Navigate to="/login" replace />
  return (
    <main className="page">
      <h1>Hi {user.displayName}</h1>
      <button className="btn btn-secondary" onClick={logout}>
        Log out
      </button>
    </main>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="*" element={<Home />} />
      </Routes>
    </AuthProvider>
  )
}
