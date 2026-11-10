import { useEffect, useState, type FormEvent } from 'react'
import { ActivityForm } from './ActivityForm'
import { ActivityHistory } from './ActivityHistory'

const statuses = {
  todo: 'انجام‌نشده',
  'in-progress': 'در حال انجام',
  blocked: 'مسدود',
  done: 'انجام‌شده',
}

const priorities = { low: 'کم', medium: 'متوسط', high: 'زیاد' }

type ProjectTask = {
  id: string
  title: string
  status: keyof typeof statuses
  priority: keyof typeof priorities
  dueDate: string | null
  blockedReason: string | null
}

const inputClass = 'w-full rounded-lg border border-slate-600 bg-slate-950 px-3 py-2'
const buttonClass = 'rounded-lg border border-slate-600 px-3 py-2 disabled:opacity-50'

export function ProjectTasks({ projectId, archived, onChanged }: { projectId: string; archived: boolean; onChanged: () => void }) {
  const path = `/api/projects/${projectId}/tasks`
  const [tasks, setTasks] = useState<ProjectTask[] | null>(null)
  const [editor, setEditor] = useState<ProjectTask | 'new' | null>(null)
  const [status, setStatus] = useState<ProjectTask['status']>('todo')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [historyRevision, setHistoryRevision] = useState(0)

  function refreshHistory() {
    setHistoryRevision((revision) => revision + 1)
  }

  useEffect(() => {
    const controller = new AbortController()
    fetch(path, { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error()
        const items: ProjectTask[] = await response.json()
        if (!controller.signal.aborted) setTasks(items)
      })
      .catch(() => {
        if (!controller.signal.aborted) setError('دریافت تسک‌ها ممکن نیست.')
      })
    return () => controller.abort()
  }, [path])

  function openEditor(task: ProjectTask | 'new') {
    setError('')
    setStatus(task === 'new' ? 'todo' : task.status)
    setEditor(task)
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!editor || busy || archived) return
    const form = new FormData(event.currentTarget)

    setBusy(true)
    setError('')
    try {
      const response = await fetch(editor === 'new' ? path : `${path}/${editor.id}`, {
        method: editor === 'new' ? 'POST' : 'PUT',
        headers: { 'Content-Type': 'application/json', 'X-Panel-Request': '1' },
        body: JSON.stringify({
          title: form.get('title'),
          status,
          priority: form.get('priority'),
          dueDate: form.get('dueDate') || null,
          blockedReason: status === 'blocked' ? form.get('blockedReason') : null,
        }),
      })
      if (!response.ok) {
        setError(response.status === 400
          ? 'عنوان، موعد و دلیل مسدود بودن را بررسی کنید.'
          : 'ذخیرهٔ تسک ممکن نیست. وضعیت پروژه را بررسی کنید.')
        return
      }
      const saved: ProjectTask = await response.json()
      setTasks((items) => [...(items ?? []).filter((task) => task.id !== saved.id), saved])
      if (editor !== 'new') refreshHistory()
      onChanged()
      setEditor(null)
    } catch {
      setError('ارتباط با سرور برقرار نشد.')
    } finally {
      setBusy(false)
    }
  }

  const selected = editor === 'new' ? null : editor
  const sorted = [...(tasks ?? [])].sort((a, b) =>
    (a.dueDate ?? '9999-99-99').localeCompare(b.dueDate ?? '9999-99-99') || a.title.localeCompare(b.title, 'fa'))

  return (
    <section className="space-y-3 border-t border-slate-700 pt-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h4 className="font-bold">تسک‌ها</h4>
        {!archived && (
          <button className={buttonClass} disabled={busy || !tasks} onClick={() => openEditor('new')}>تسک جدید</button>
        )}
      </div>
      {error && <p role="alert" className="text-rose-300">{error}</p>}
      {!tasks && !error && <p role="status">در حال دریافت تسک‌ها…</p>}
      {tasks?.length === 0 && <p>هنوز تسکی ثبت نشده است.</p>}
      {editor && !archived && (
        <form key={selected?.id ?? 'new'} onSubmit={(event) => void save(event)}>
          <fieldset disabled={busy} className="grid gap-3 rounded-lg border border-slate-700 p-3">
            <legend className="px-1">{selected ? 'ویرایش تسک' : 'تسک جدید'}</legend>
            <label>عنوان تسک<input className={inputClass} name="title" required maxLength={200} defaultValue={selected?.title} /></label>
            <label>وضعیت تسک
              <select className={inputClass} value={status} onChange={(event) => setStatus(event.target.value as ProjectTask['status'])}>
                {Object.entries(statuses).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
              </select>
            </label>
            <label>اولویت
              <select className={inputClass} name="priority" defaultValue={selected?.priority ?? 'medium'}>
                {Object.entries(priorities).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
              </select>
            </label>
            <label>موعد اختیاری<input className={inputClass} name="dueDate" type="date" defaultValue={selected?.dueDate ?? ''} /></label>
            {status === 'blocked' && (
              <label>دلیل مسدود بودن<textarea className={inputClass} name="blockedReason" required maxLength={1000} defaultValue={selected?.blockedReason ?? ''} /></label>
            )}
            <div className="flex flex-wrap gap-2">
              <button className="rounded-lg bg-teal-300 px-4 py-2 text-slate-950">ذخیرهٔ تسک</button>
              <button className={buttonClass} type="button" onClick={() => setEditor(null)}>انصراف</button>
            </div>
          </fieldset>
        </form>
      )}
      {sorted.length > 0 && (
        <ul className="space-y-2">
          {sorted.map((task) => (
            <li key={task.id} className="space-y-2 rounded-lg border border-slate-700 p-3">
              <p className="font-medium">{task.title}</p>
              <p className="text-sm text-slate-300">
                {statuses[task.status]} · اولویت {priorities[task.priority]}
                {task.dueDate && <> · موعد <time dir="ltr" dateTime={task.dueDate}>{task.dueDate}</time></>}
              </p>
              {task.blockedReason && <p className="whitespace-pre-wrap">{task.blockedReason}</p>}
              {!archived && (
                <button className={buttonClass} disabled={busy} onClick={() => openEditor(task)}>ویرایش تسک</button>
              )}
            </li>
          ))}
        </ul>
      )}
      {!archived && <ActivityForm projectId={projectId} tasks={sorted} onSaved={() => { refreshHistory(); onChanged() }} />}
      <ActivityHistory key={`${archived}-${historyRevision}`} projectId={projectId} tasks={sorted} />
    </section>
  )
}
