import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../../api/client'
import type { Friend, FriendRequest, Invite } from '../../api/types'
import { Empty, ErrorAlert, Field, fieldError } from '../../components/Form'
import { formatDate, initials } from '../../lib/format'
import { useAsync } from '../../lib/useAsync'

function inviteLink(code: string) {
  return `${window.location.origin}/register?code=${code}`
}

function InviteSection() {
  const { data: invites, reload } = useAsync(() => api.get<Invite[]>('/api/invites'), [])
  const [error, setError] = useState<unknown>(null)
  const [copied, setCopied] = useState<string | null>(null)

  async function create() {
    setError(null)
    try {
      await api.post<Invite>('/api/invites')
      await reload()
    } catch (err) {
      setError(err)
    }
  }

  async function copy(code: string) {
    try {
      await navigator.clipboard.writeText(inviteLink(code))
      setCopied(code)
    } catch {
      setCopied(null)
    }
  }

  const open = invites?.filter((i) => !i.isUsed && !i.isExpired) ?? []
  const used = invites?.filter((i) => i.isUsed) ?? []

  return (
    <section className="section">
      <div className="page-header">
        <h2>Invite someone</h2>
        <button className="btn btn-secondary" onClick={create}>
          Create invite
        </button>
      </div>
      <p className="muted">
        Send the link to someone you train with. It works once and expires after 7 days. When they join, you become friends.
      </p>
      <ErrorAlert error={error} />
      <ul className="entries">
        {open.map((invite) => (
          <li key={invite.id} className="entry">
            <div className="entry-head">
              <h3 className="entry-title" style={{ fontFamily: 'var(--font-display)', fontSize: '1.4rem', letterSpacing: '0.08em' }}>
                {invite.code}
              </h3>
              <span className="entry-meta">until {formatDate(invite.expiresAt.slice(0, 10))}</span>
            </div>
            <div className="entry-actions">
              <button className="btn btn-secondary btn-small" onClick={() => copy(invite.code)}>
                {copied === invite.code ? 'Link copied' : 'Copy link'}
              </button>
            </div>
          </li>
        ))}
        {used.map((invite) => (
          <li key={invite.id} className="entry">
            <div className="entry-head">
              <span>{invite.usedByName ?? 'Someone'} joined with your invite</span>
              <span className="entry-meta">{invite.usedAt ? formatDate(invite.usedAt.slice(0, 10)) : ''}</span>
            </div>
          </li>
        ))}
      </ul>
    </section>
  )
}

export default function FriendsPage() {
  const friends = useAsync(() => api.get<Friend[]>('/api/friends'), [])
  const requests = useAsync(() => api.get<FriendRequest[]>('/api/friends/requests'), [])
  const [email, setEmail] = useState('')
  const [sendError, setSendError] = useState<unknown>(null)
  const [sent, setSent] = useState<string | null>(null)
  const [error, setError] = useState<unknown>(null)

  async function send(event: FormEvent) {
    event.preventDefault()
    setSendError(null)
    setSent(null)
    try {
      const request = await api.post<FriendRequest>('/api/friends/requests', { email })
      setSent(`Request sent to ${request.displayName}.`)
      setEmail('')
      await requests.reload()
    } catch (err) {
      setSendError(err)
    }
  }

  async function act(action: () => Promise<unknown>) {
    setError(null)
    try {
      await action()
      await Promise.all([friends.reload(), requests.reload()])
    } catch (err) {
      setError(err)
    }
  }

  const incoming = requests.data?.filter((r) => r.direction === 'incoming') ?? []
  const outgoing = requests.data?.filter((r) => r.direction === 'outgoing') ?? []

  return (
    <main className="page">
      <h1>Friends</h1>
      <ErrorAlert error={error} />

      {incoming.length > 0 && (
        <section style={{ marginBottom: 24 }}>
          <h2>Friend requests</h2>
          <ul className="entries">
            {incoming.map((r) => (
              <li key={r.id} className="entry">
                <div className="entry-head">
                  <span className="entry-title">{r.displayName}</span>
                  <span className="entry-meta">{formatDate(r.createdAt.slice(0, 10))}</span>
                </div>
                <div className="entry-actions">
                  <button className="btn btn-small" onClick={() => act(() => api.post(`/api/friends/requests/${r.id}/accept`))}>
                    Accept
                  </button>
                  <button className="btn btn-secondary btn-small" onClick={() => act(() => api.delete(`/api/friends/requests/${r.id}`))}>
                    Decline
                  </button>
                </div>
              </li>
            ))}
          </ul>
        </section>
      )}

      <section>
        <h2>Your friends</h2>
        {!friends.loading && friends.data?.length === 0 && (
          <Empty>
            <p>No friends yet. Invite someone below, or add a member by e-mail address.</p>
          </Empty>
        )}
        <ul className="entries">
          {friends.data?.map((f) => (
            <li key={f.userId} className="entry">
              <div className="post">
                <span className="avatar" aria-hidden="true">
                  {initials(f.displayName)}
                </span>
                <div>
                  <Link to={`/friends/${f.userId}`} className="entry-title">
                    {f.displayName}
                  </Link>
                  <div className="entry-meta">Friends since {formatDate(f.since.slice(0, 10))}</div>
                </div>
              </div>
            </li>
          ))}
        </ul>
      </section>

      <section className="section">
        <h2>Add a friend</h2>
        <form className="card form" onSubmit={send}>
          {sent && (
            <div className="notice" role="status">
              {sent}
            </div>
          )}
          {sendError && !fieldError(sendError, 'email') ? <ErrorAlert error={sendError} /> : null}
          <Field label="Their e-mail address" error={fieldError(sendError, 'email')}>
            {(p) => <input {...p} type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />}
          </Field>
          <div>
            <button className="btn" type="submit">
              Send friend request
            </button>
          </div>
        </form>
        {outgoing.length > 0 && (
          <ul className="entries" style={{ marginTop: 12 }}>
            {outgoing.map((r) => (
              <li key={r.id} className="entry">
                <div className="entry-head">
                  <span>Waiting for {r.displayName}</span>
                  <button className="btn btn-secondary btn-small" onClick={() => act(() => api.delete(`/api/friends/requests/${r.id}`))}>
                    Cancel request
                  </button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      <InviteSection />
    </main>
  )
}
