import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, setAccessToken, setUnauthorizedHandler } from '../api/client'
import type { AuthResponse, Me } from '../api/types'

const STORAGE_KEY = 'fittrack.session'

interface Session {
  token: string
  expiresAt: string
  user: Me
}

interface AuthContextValue {
  user: Me | null
  token: string | null
  login: (email: string, password: string) => Promise<void>
  register: (input: { inviteCode: string; email: string; displayName: string; password: string }) => Promise<void>
  logout: () => void
  updateUser: (user: Me) => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

function loadSession(): Session | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const session = JSON.parse(raw) as Session
    return new Date(session.expiresAt) > new Date() ? session : null
  } catch {
    return null
  }
}

function saveSession(session: Session | null) {
  try {
    if (session) localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
    else localStorage.removeItem(STORAGE_KEY)
  } catch {
    // storage unavailable (private mode); the session then lasts until the tab closes
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(() => {
    const stored = loadSession()
    setAccessToken(stored?.token ?? null)
    return stored
  })

  const apply = useCallback((next: Session | null) => {
    setAccessToken(next?.token ?? null)
    saveSession(next)
    setSession(next)
  }, [])

  const logout = useCallback(() => apply(null), [apply])

  useEffect(() => {
    setUnauthorizedHandler(logout)
    return () => setUnauthorizedHandler(null)
  }, [logout])

  const fromResponse = useCallback(
    (response: AuthResponse) => apply({ token: response.accessToken, expiresAt: response.expiresAt, user: response.user }),
    [apply],
  )

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session?.user ?? null,
      token: session?.token ?? null,
      login: async (email, password) => fromResponse(await api.post<AuthResponse>('/api/auth/login', { email, password })),
      register: async (input) =>
        fromResponse(
          await api.post<AuthResponse>('/api/auth/register', {
            ...input,
            timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
          }),
        ),
      logout,
      updateUser: (user) => {
        if (session) apply({ ...session, user })
      },
    }),
    [session, apply, fromResponse, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider')
  return context
}
