import { StrictMode, useEffect, useState } from 'react'
import { createRoot } from 'react-dom/client'
import './styles.css'

const labels = {
  checking: 'در حال بررسی اتصال به API…',
  connected: 'ارتباط با API برقرار است.',
  unavailable: 'ارتباط با API برقرار نشد.',
}

function App() {
  const [status, setStatus] = useState<keyof typeof labels>('checking')

  useEffect(() => {
    const controller = new AbortController()

    async function checkApi() {
      try {
        const response = await fetch('/api/health', { signal: controller.signal })
        if (!response.ok) throw new Error('Health check failed')
        const result = await response.json()
        setStatus(result.status === 'ok' ? 'connected' : 'unavailable')
      } catch {
        if (!controller.signal.aborted) setStatus('unavailable')
      }
    }

    void checkApi()
    return () => controller.abort()
  }, [])

  return (
    <main className="mx-auto grid min-h-svh w-full max-w-3xl place-items-center px-5 py-12">
      <section className="w-full rounded-3xl border border-slate-700 bg-slate-900 p-8 sm:p-12">
        <p className="mb-4 text-sm font-medium text-teal-300">پنل مدیریت پروژه</p>
        <h1 className="text-4xl font-bold sm:text-5xl">اتاق فرمان</h1>
        <p role="status" className="mt-6 text-base text-slate-300">{labels[status]}</p>
      </section>
    </main>
  )
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
