import { useState, type FormEvent } from 'react'

type TaskOption = { id: string; title: string }

export function ActivityForm({ projectId, tasks }: { projectId: string; tasks: TaskOption[] | null }) {
  const [open, setOpen] = useState(false)
  const [busy, setBusy] = useState(false)
  const [feedback, setFeedback] = useState<'saved' | 'invalid' | 'failed' | null>(null)
  const inputClass = 'w-full rounded-lg border border-slate-600 bg-slate-950 px-3 py-2'

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy) return
    const data = new FormData(event.currentTarget)
    setBusy(true)
    setFeedback(null)

    try {
      const response = await fetch(`/api/projects/${projectId}/activities`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-Panel-Request': '1' },
        body: JSON.stringify({
          report: data.get('report'),
          link: data.get('link') || null,
          taskId: data.get('taskId') || null,
        }),
      })
      if (response.status === 201) {
        setOpen(false)
        setFeedback('saved')
      } else {
        setFeedback(response.status === 400 ? 'invalid' : 'failed')
      }
    } catch {
      setFeedback('failed')
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="space-y-3 border-t border-slate-700 pt-3">
      <button
        type="button"
        className="rounded-lg border border-slate-600 px-3 py-2"
        aria-expanded={open}
        disabled={busy}
        onClick={() => { setFeedback(null); setOpen(!open) }}
      >
        {open ? 'بستن فرم گزارش' : 'ثبت گزارش کار'}
      </button>
      {open && (
        <form onSubmit={(event) => void save(event)}>
          <fieldset disabled={busy} className="grid gap-3 rounded-lg border border-slate-700 p-3">
            <legend className="px-1">نتیجهٔ کار</legend>
            <label>گزارش<textarea className={inputClass} name="report" required maxLength={4000} /></label>
            <label>لینک اختیاری<input className={inputClass} name="link" type="url" dir="ltr" maxLength={2048} /></label>
            <label>تسک مرتبط
              <select className={inputClass} name="taskId">
                <option value="">فقط پروژه</option>
                {tasks?.map((task) => <option key={task.id} value={task.id}>{task.title}</option>)}
              </select>
            </label>
            <button className="justify-self-start rounded-lg bg-teal-300 px-4 py-2 text-slate-950">ذخیرهٔ گزارش</button>
          </fieldset>
        </form>
      )}
      {feedback === 'saved' && <p role="status" className="text-teal-300">گزارش ثبت شد.</p>}
      {feedback === 'invalid' && <p role="alert" className="text-rose-300">متن گزارش، لینک و تسک مرتبط را بررسی کنید.</p>}
      {feedback === 'failed' && <p role="alert" className="text-rose-300">ثبت گزارش تأیید نشد. وضعیت پروژه و اتصال را بررسی کنید.</p>}
    </section>
  )
}
