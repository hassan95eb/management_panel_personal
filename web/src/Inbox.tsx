import { useEffect, useState, type FormEvent } from 'react'

type Item = { id: string; text: string; createdAt: string; projectId: string | null; taskId: string | null }
type Project = { id: string; name: string; archivedAt: string | null }

const headers = { 'Content-Type': 'application/json', 'X-Panel-Request': '1' }
const input = 'w-full min-w-0 rounded-lg border border-slate-600 bg-slate-950 px-3 py-2'
const button = 'min-h-11 rounded-lg border border-slate-600 px-3 py-2 disabled:opacity-50'

export function Inbox({ projects, onOpenTask, onChanged }: {
  projects: Project[]
  onOpenTask: (projectId: string, taskId: string) => void
  onChanged: () => void
}) {
  const [items, setItems] = useState<Item[] | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    fetch('/api/inbox', { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error()
        const items: Item[] = await response.json()
        if (!controller.signal.aborted) setItems(items)
      })
      .catch(() => {
        if (!controller.signal.aborted) setError('دریافت صندوق ورودی ممکن نیست.')
      })
    return () => controller.abort()
  }, [])

  async function save(event: FormEvent<HTMLFormElement>, item?: Item) {
    event.preventDefault()
    if (busy) return
    const form = event.currentTarget
    const data = new FormData(form, (event.nativeEvent as SubmitEvent).submitter)
    const convert = data.get('action') === 'convert'
    const body = item
      ? { projectId: data.get('projectId'), taskTitle: convert ? data.get('taskTitle') : null }
      : { text: data.get('text') }

    setBusy(true)
    setError('')
    try {
      const response = await fetch(item ? `/api/inbox/${item.id}/project` : '/api/inbox', {
        method: item ? 'PUT' : 'POST',
        headers,
        body: JSON.stringify(body),
      })
      if (!response.ok) {
        setError(response.status === 409 ? 'این مورد قبلاً به تسک تبدیل شده است.' : 'ذخیره ممکن نیست. متن و پروژه را بررسی کن.')
        return
      }
      const saved: Item = await response.json()
      setItems((current) => item
        ? (current ?? []).map((entry) => entry.id === saved.id ? saved : entry)
        : [saved, ...(current ?? [])])
      if (!item) form.reset()
      if (saved.taskId) onChanged()
    } catch {
      setError('ارتباط با سرور برقرار نشد.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="grid gap-4 rounded-xl border border-slate-700 p-4" aria-labelledby="inbox-heading">
      <h2 id="inbox-heading" className="text-xl font-semibold">صندوق ورودی</h2>
      <form className="grid gap-2" onSubmit={(event) => void save(event)}>
        <label htmlFor="inbox-text">یادداشت یا کار جدید</label>
        <textarea id="inbox-text" name="text" className={input} rows={2} maxLength={4000} required />
        <button className={button} disabled={busy || items === null}>ثبت سریع</button>
      </form>
      {error && <p role="alert" className="text-rose-300">{error}</p>}
      {items?.length === 0 && <p className="text-slate-300">صندوق ورودی خالی است.</p>}
      {items?.map((item) => {
        const project = projects.find((entry) => entry.id === item.projectId)
        return (
          <article key={item.id} className="grid min-w-0 gap-3 rounded-lg border border-slate-700 p-3">
            <p className="whitespace-pre-wrap break-words">{item.text}</p>
            {project && <p className="text-sm text-slate-300">پروژهٔ {project.name}</p>}
            {item.projectId && item.taskId ? (
              <button type="button" className={button} onClick={() => onOpenTask(item.projectId!, item.taskId!)}>
                مشاهدهٔ تسک ایجادشده
              </button>
            ) : (
              <form className="grid gap-2" onSubmit={(event) => void save(event, item)}>
                <label className="grid gap-1">پروژه
                  <select name="projectId" required className={input} defaultValue={item.projectId ?? ''}>
                    <option value="" disabled>انتخاب پروژه</option>
                    {projects.filter((entry) => !entry.archivedAt).map((entry) => (
                      <option key={entry.id} value={entry.id}>{entry.name}</option>
                    ))}
                  </select>
                </label>
                <label className="grid gap-1">عنوان تسک برای تبدیل
                  <input name="taskTitle" className={input} maxLength={200} defaultValue={item.text.slice(0, 200)} />
                </label>
                <div className="grid gap-2 sm:grid-cols-2">
                  <button className={button} disabled={busy} name="action" value="assign">اتصال به پروژه</button>
                  <button className={button} disabled={busy} name="action" value="convert">تبدیل به تسک</button>
                </div>
              </form>
            )}
          </article>
        )
      })}
    </section>
  )
}
