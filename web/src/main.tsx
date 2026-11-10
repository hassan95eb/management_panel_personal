import { StrictMode, useEffect, useState, type FormEvent } from 'react'
import { createRoot } from 'react-dom/client'
import './styles.css'

function App() {
  const [session, setSession] = useState<'checking' | 'guest' | 'owner' | 'unavailable'>('checking')
  const [password, setPassword] = useState('')
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    fetch('/api/auth/session', { signal: controller.signal })
      .then((response) => {
        if (controller.signal.aborted) return
        setSession(response.ok ? 'owner' : response.status === 401 ? 'guest' : 'unavailable')
      })
      .catch(() => {
        if (!controller.signal.aborted) setSession('unavailable')
      })
    return () => controller.abort()
  }, [])

  async function changeSession(event: FormEvent<HTMLFormElement>, action: 'login' | 'logout') {
    event.preventDefault()
    if (busy) return
    setBusy(true)
    setMessage('')

    try {
      const response = await fetch(`/api/auth/${action}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-Panel-Request': '1' },
        body: action === 'login' ? JSON.stringify({ password }) : undefined,
      })

      if (response.status === 204 || (action === 'logout' && response.status === 401)) {
        setPassword('')
        setSession(action === 'login' ? 'owner' : 'guest')
      } else if (response.status === 429) {
        setMessage('تعداد تلاش‌ها زیاد است. یک دقیقه دیگر امتحان کنید.')
      } else if (response.status === 401) {
        setMessage('رمز عبور درست نیست.')
      } else {
        setMessage('انجام درخواست ممکن نیست. دوباره تلاش کنید.')
      }
    } catch {
      setMessage('ارتباط با سرور برقرار نشد.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="mx-auto grid min-h-svh w-full max-w-3xl place-items-center px-5 py-12">
      <section className="w-full rounded-3xl border border-slate-700 bg-slate-900 p-8 sm:p-12">
        <p className="mb-4 text-sm font-medium text-teal-300">پنل مدیریت پروژه</p>
        <h1 className="text-4xl font-bold sm:text-5xl">اتاق فرمان</h1>
        {session === 'checking' && <p role="status" className="mt-6">در حال بررسی نشست…</p>}
        {session === 'unavailable' && (
          <p role="alert" className="mt-6">بررسی نشست ممکن نیست. صفحه را دوباره بارگذاری کنید.</p>
        )}
        {session === 'guest' && (
          <form className="mt-6 grid gap-4" onSubmit={(event) => void changeSession(event, 'login')}>
            <label htmlFor="owner-password">رمز عبور مالک</label>
            <input
              id="owner-password"
              type="password"
              autoComplete="current-password"
              required
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              className="rounded-lg border border-slate-600 px-3 py-2"
            />
            <button disabled={busy} className="rounded-lg bg-teal-300 px-4 py-2 font-medium text-slate-950 disabled:opacity-50">
              ورود
            </button>
          </form>
        )}
        {session === 'owner' && (
          <form className="mt-6 grid gap-4" onSubmit={(event) => void changeSession(event, 'logout')}>
            <p>با حساب مالک وارد شده‌اید.</p>
            <button disabled={busy} className="justify-self-start rounded-lg border border-slate-600 px-4 py-2 disabled:opacity-50">
              خروج
            </button>
          </form>
        )}
        {message && <p role="alert" className="mt-4 text-rose-300">{message}</p>}
      </section>
    </main>
  )
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
