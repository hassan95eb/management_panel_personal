import { useEffect, useState, type FormEvent } from 'react'

type Grant = { projectId: string; canRead: boolean; canWriteActivity: boolean }
type Key = { id: string; name: string; revokedAt: string | null; grants: Grant[] }
type Project = { id: string; name: string; archivedAt: string | null }

const path = '/api/codex-keys'
const headers = { 'Content-Type': 'application/json', 'X-Panel-Request': '1' }
const button = 'rounded-lg border border-slate-600 px-3 py-2 disabled:opacity-50'

export function CodexKeys({ projects }: { projects: Project[] }) {
  return (
    <details className="rounded-xl border border-slate-700 p-4">
      <summary className="cursor-pointer font-bold">کلیدهای Codex</summary>
      <KeyManager projects={projects} />
    </details>
  )
}

function KeyManager({ projects }: { projects: Project[] }) {
  const [keys, setKeys] = useState<Key[] | null>(null)
  const [token, setToken] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [attempt, setAttempt] = useState(0)
  const active = projects.filter((project) => !project.archivedAt)

  useEffect(() => {
    const controller = new AbortController()
    fetch(path, { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error()
        const items: Key[] = await response.json()
        if (!controller.signal.aborted) setKeys(items)
      })
      .catch(() => {
        if (!controller.signal.aborted) setError('دریافت کلیدها ممکن نیست.')
      })
    return () => controller.abort()
  }, [attempt])

  async function issue(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy || !keys) return
    const form = event.currentTarget
    const values = new FormData(form)
    const grants = active.map(({ id }) => ({
      projectId: id,
      canRead: values.has(`read-${id}`),
      canWriteActivity: values.has(`write-${id}`),
    })).filter((grant) => grant.canRead || grant.canWriteActivity)
    if (grants.length === 0 || grants.length > 100) {
      setError('برای یک تا صد پروژه دست‌کم یک مجوز انتخاب کنید.')
      return
    }

    setBusy(true)
    setError('')
    setToken('')
    try {
      const response = await fetch(path, {
        method: 'POST',
        headers,
        body: JSON.stringify({ name: values.get('name'), grants }),
      })
      if (!response.ok) throw new Error()
      const { token: issued, ...key }: Key & { token: string } = await response.json()
      setToken(issued)
      setKeys((items) => [key, ...(items ?? [])])
      form.reset()
    } catch {
      setError('صدور کلید تأیید نشد. فهرست کلیدها را بررسی کنید.')
    } finally {
      setBusy(false)
    }
  }

  async function revoke(key: Key) {
    if (busy || !window.confirm(`کلید «${key.name}» لغو شود؟`)) return
    setBusy(true)
    setError('')
    try {
      const response = await fetch(`${path}/${key.id}`, { method: 'DELETE', headers })
      if (!response.ok) throw new Error()
      setKeys((items) => items?.map((item) =>
        item.id === key.id ? { ...item, revokedAt: new Date().toISOString() } : item) ?? null)
      setToken('')
    } catch {
      setError('لغو کلید تأیید نشد.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="mt-4 space-y-4 [overflow-wrap:anywhere]">
      {error && <p role="alert" className="text-rose-300">{error}</p>}
      {!keys && (error
        ? <button className={button} onClick={() => { setError(''); setAttempt((value) => value + 1) }}>تلاش دوباره</button>
        : <p role="status">در حال دریافت کلیدها…</p>)}
      {keys && active.length > 0 && (
        <form onSubmit={(event) => void issue(event)}>
          <fieldset disabled={busy || Boolean(token)} className="grid gap-3">
            <legend className="font-medium">صدور کلید جدید</legend>
            <label>نام کلید
              <input name="name" required maxLength={100} className="block w-full rounded-lg border border-slate-600 bg-slate-950 px-3 py-2" />
            </label>
            {active.map((project) => (
              <fieldset key={project.id} className="flex flex-wrap gap-3 rounded-lg border border-slate-700 p-2">
                <legend className="px-1">{project.name}</legend>
                <label><input type="checkbox" name={`read-${project.id}`} /> خواندن</label>
                <label><input type="checkbox" name={`write-${project.id}`} /> ثبت فعالیت</label>
              </fieldset>
            ))}
            <button className={button + ' justify-self-start'}>صدور کلید</button>
          </fieldset>
        </form>
      )}
      {keys && active.length === 0 && <p>برای صدور کلید، ابتدا یک پروژهٔ آرشیونشده ایجاد کنید.</p>}
      {token && (
        <div className="space-y-2 rounded-lg border border-teal-400 p-3">
          <p>این کلید فقط همین‌جا نمایش داده می‌شود. آن را در جای امن ذخیره کنید.</p>
          <code dir="ltr" className="block break-all select-all">{token}</code>
          <button className={button} onClick={() => setToken('')}>بستن نمایش کلید</button>
        </div>
      )}
      {keys?.length === 0 && <p>هنوز کلیدی صادر نشده است.</p>}
      <ul className="space-y-2">
        {keys?.map((key) => (
          <li key={key.id} className="space-y-2 rounded-lg border border-slate-700 p-3">
            <p className="font-medium">{key.name} · {key.revokedAt ? 'لغوشده' : 'فعال'}</p>
            <ul className="space-y-1 text-sm text-slate-300">
              {key.grants.map((grant) => (
                <li key={grant.projectId}>
                  {projects.find((project) => project.id === grant.projectId)?.name ?? grant.projectId}
                  {' · '}{[grant.canRead && 'خواندن', grant.canWriteActivity && 'ثبت فعالیت'].filter(Boolean).join('، ')}
                </li>
              ))}
            </ul>
            {!key.revokedAt && <button className={button} disabled={busy} onClick={() => void revoke(key)}>لغو کلید</button>}
          </li>
        ))}
      </ul>
    </div>
  )
}
