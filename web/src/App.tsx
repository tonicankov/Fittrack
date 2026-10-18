import { Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import Layout from './components/Layout'
import LogPage from './pages/entries/LogPage'
import MealsPage from './pages/entries/MealsPage'
import WorkoutsPage, { WorkoutEditorPage } from './pages/entries/WorkoutsPage'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'

function Placeholder({ title }: { title: string }) {
  return (
    <main className="page">
      <h1>{title}</h1>
    </main>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route element={<Layout />}>
          <Route index element={<Placeholder title="Feed" />} />
          <Route path="log" element={<LogPage />}>
            <Route index element={<Navigate to="workouts" replace />} />
            <Route path="workouts" element={<WorkoutsPage />} />
            <Route path="workouts/new" element={<WorkoutEditorPage />} />
            <Route path="workouts/:id/edit" element={<WorkoutEditorPage />} />
            <Route path="meals" element={<MealsPage />} />
          </Route>
          <Route path="progress" element={<Placeholder title="Progress" />} />
          <Route path="friends" element={<Placeholder title="Friends" />} />
          <Route path="settings" element={<Placeholder title="Me" />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
    </AuthProvider>
  )
}
