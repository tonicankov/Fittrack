import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, query } from '../../api/client'
import type { FriendRequest, Page, Post, PostType } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'
import { Empty, ErrorAlert } from '../../components/Form'
import { initials, timeAgo } from '../../lib/format'
import { useHubEvent, useHubReconnects } from '../../lib/feedHub'

const kindOf: Record<PostType, string> = {
  WorkoutLogged: 'workout',
  GoalAchieved: 'goal',
  WeeklyTargetMet: 'goal',
}

export default function FeedPage() {
  const { user } = useAuth()
  const reconnects = useHubReconnects()
  const [posts, setPosts] = useState<Post[]>([])
  const [fresh, setFresh] = useState<Set<string>>(new Set())
  const [cursor, setCursor] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [request, setRequest] = useState<FriendRequest | null>(null)

  // Load the newest page on open, and again after a reconnect so nothing is missed
  useEffect(() => {
    let cancelled = false
    setLoading(true)
    api
      .get<Page<Post>>('/api/feed')
      .then((page) => {
        if (cancelled) return
        setPosts(page.items)
        setCursor(page.nextBefore)
      })
      .catch((err) => !cancelled && setError(err))
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
  }, [reconnects])

  useHubEvent<Post>('PostCreated', (post) => {
    setPosts((list) => (list.some((p) => p.id === post.id) ? list : [post, ...list]))
    setFresh((set) => new Set(set).add(post.id))
  })
  useHubEvent<string>('PostRemoved', (id) => setPosts((list) => list.filter((p) => p.id !== id)))
  useHubEvent<FriendRequest>('FriendRequestReceived', setRequest)

  async function loadOlder() {
    if (!cursor) return
    try {
      const page = await api.get<Page<Post>>(`/api/feed${query({ before: cursor })}`)
      setPosts((list) => [...list, ...page.items])
      setCursor(page.nextBefore)
    } catch (err) {
      setError(err)
    }
  }

  return (
    <main className="page">
      <div className="page-header">
        <h1>Feed</h1>
        <Link className="btn" to="/log/workouts/new">
          Log workout
        </Link>
      </div>

      {request && (
        <div className="notice" role="status" style={{ marginBottom: 16 }}>
          {request.displayName} sent you a friend request. <Link to="/friends">Answer it</Link>
        </div>
      )}
      <ErrorAlert error={error} />

      {!loading && posts.length === 0 && (
        <Empty>
          <p>Your feed shows the workouts and goals you and your friends share. It is quiet here for now.</p>
          <Link className="btn" to="/friends">
            Invite a friend
          </Link>
        </Empty>
      )}

      <ul className="entries">
        {posts.map((post) => (
          <li key={post.id} className={`entry${fresh.has(post.id) ? ' post-new' : ''}`} data-kind={kindOf[post.type]}>
            <div className="post">
              <span className="avatar" aria-hidden="true">
                {initials(post.authorName)}
              </span>
              <div>
                <div className="entry-head">
                  {post.authorId === user?.id ? (
                    <span className="entry-title">You</span>
                  ) : (
                    <Link to={`/friends/${post.authorId}`} className="entry-title">
                      {post.authorName}
                    </Link>
                  )}
                  <time className="entry-meta" dateTime={post.createdAt}>
                    {timeAgo(post.createdAt)}
                  </time>
                </div>
                <p style={{ margin: 0 }}>{post.summary}</p>
              </div>
            </div>
          </li>
        ))}
      </ul>

      {cursor && (
        <div className="actions" style={{ marginTop: 16 }}>
          <button className="btn btn-secondary" onClick={loadOlder}>
            Show older posts
          </button>
        </div>
      )}
    </main>
  )
}
