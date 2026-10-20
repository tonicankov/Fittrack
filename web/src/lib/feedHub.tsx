import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'
import { createContext, useContext, useEffect, useRef, useState, type ReactNode } from 'react'

interface HubState {
  connection: HubConnection | null
  /** Increases after every reconnect, so pages can reload what they may have missed. */
  reconnects: number
}

const HubContext = createContext<HubState>({ connection: null, reconnects: 0 })

/** Keeps one SignalR connection open while the member is logged in. */
export function FeedHubProvider({ token, children }: { token: string | null; children: ReactNode }) {
  const [state, setState] = useState<HubState>({ connection: null, reconnects: 0 })

  useEffect(() => {
    if (!token) return
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/feed', { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    let active = true
    connection.onreconnected(() => {
      if (active) setState((s) => ({ ...s, reconnects: s.reconnects + 1 }))
    })
    connection
      .start()
      .then(() => {
        if (active) setState((s) => ({ ...s, connection }))
      })
      .catch(() => {
        // The feed still works without live updates; it loads on every visit
      })

    return () => {
      active = false
      setState((s) => ({ ...s, connection: null }))
      void connection.stop()
    }
  }, [token])

  return <HubContext.Provider value={state}>{children}</HubContext.Provider>
}

/** Calls the handler whenever the server sends this event. */
export function useHubEvent<T>(event: string, handler: (payload: T) => void) {
  const { connection } = useContext(HubContext)
  const latest = useRef(handler)

  useEffect(() => {
    latest.current = handler
  })

  useEffect(() => {
    if (!connection) return
    const listener = (payload: T) => latest.current(payload)
    connection.on(event, listener)
    return () => connection.off(event, listener)
  }, [connection, event])
}

export function useHubReconnects() {
  return useContext(HubContext).reconnects
}
