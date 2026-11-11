import { useEffect, useState, type FormEvent } from 'react'
import { CodexKeys } from './CodexKeys'
import { Dashboard } from './Dashboard'
import { ProjectTasks } from './ProjectTasks'

const statuses = {
  planned: 'برنامه‌ریزی‌شده',
  active: 'فعال',
  paused: 'متوقف',
  completed: 'تکمیل‌شده',
}

type ProjectFields = {
  name: string
  description: string | null
  status: keyof typeof statuses
  repositoryUrl: string | null
  environmentUrl: string | null
  nextStep: string | null
}

type Project = ProjectFields & { id: string; archivedAt: string | null }

const headers = { 'Content-Type': 'application/json', 'X-Panel-Request': '1' }
const inputClass = 'w-full rounded-lg border border-slate-600 bg-slate-950 px-3 py-2'
const buttonClass = 'rounded-lg border border-slate-600 px-3 py-2 disabled:opacity-50'

export function Projects() {
  const [projects, setProjects] = useState<Project[] | null>(null)
  const [editor, setEditor] = useState<Project | 'new' | null>(null)
  const [taskProjectId, setTaskProjectId] = useState<string | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [dashboardRevision, setDashboardRevision] = useState(0)

  function refreshDashboard() {
    setDashboardRevision((revision) => revision + 1)
  }

  useEffect(() => {
    const controller = new AbortController()
    fetch('/api/projects', { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error()
        const items: Project[] = await response.json()
        if (!controller.signal.aborted) setProjects(items)
      })
      .catch(() => {
        if (!controller.signal.aborted) setError('دریافت پروژه‌ها ممکن نیست. صفحه را دوباره بارگذاری کنید.')
      })
    return () => controller.abort()
  }, [])

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!editor || busy) return
    const data = Object.fromEntries(new FormData(event.currentTarget))
    const path = editor === 'new' ? '/api/projects' : `/api/projects/${editor.id}`

    setBusy(true)
    setError('')
    try {
      const response = await fetch(path, {
        method: editor === 'new' ? 'POST' : 'PUT',
        headers,
        body: JSON.stringify(data),
      })
      if (!response.ok) {
        setError(response.status === 400
          ? 'نام، وضعیت و لینک‌های پروژه را بررسی کنید. لینک‌ها باید با http یا https شروع شوند.'
          : 'ذخیرهٔ پروژه ممکن نیست. اگر در دستگاه دیگری آرشیو شده، صفحه را دوباره بارگذاری کنید.')
        return
      }
      const saved: Project = await response.json()
      setProjects((items) => [...(items ?? []).filter((item) => item.id !== saved.id), saved])
      setEditor(null)
      refreshDashboard()
    } catch {
      setError('ارتباط با سرور برقرار نشد.')
    } finally {
      setBusy(false)
    }
  }

  async function archive(project: Project) {
    if (busy || !window.confirm(`«${project.name}» آرشیو شود؟`)) return
    setBusy(true)
    setError('')
    try {
      const response = await fetch(`/api/projects/${project.id}/archive`, { method: 'PUT', headers })
      if (!response.ok) throw new Error()
      const detail = await fetch(`/api/projects/${project.id}`)
      if (!detail.ok) throw new Error()
      const archived: Project = await detail.json()
      setProjects((items) => items?.map((item) => item.id === archived.id ? archived : item) ?? null)
      setEditor(null)
      refreshDashboard()
    } catch {
      setError('وضعیت آرشیو مشخص نیست. صفحه را دوباره بارگذاری کنید.')
    } finally {
      setBusy(false)
    }
  }

  const selected = editor === 'new' ? null : editor
  const sorted = [...(projects ?? [])].sort((a, b) =>
    Number(Boolean(a.archivedAt)) - Number(Boolean(b.archivedAt)) || a.name.localeCompare(b.name, 'fa'))

  return (
    <section className="mt-8 space-y-4 border-t border-slate-700 pt-6">
      <Dashboard revision={dashboardRevision} onOpenProject={setTaskProjectId} />
      <CodexKeys projects={sorted} />
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-2xl font-bold">پروژه‌ها</h2>
        <button className={buttonClass} disabled={busy || !projects} onClick={() => { setError(''); setEditor('new') }}>
          پروژهٔ جدید
        </button>
      </div>
      {error && <p role="alert" className="text-rose-300">{error}</p>}
      {!projects && !error && <p role="status">در حال دریافت پروژه‌ها…</p>}
      {projects?.length === 0 && <p>هنوز پروژه‌ای ثبت نشده است.</p>}
      {editor && (
        <form key={selected?.id ?? 'new'} onSubmit={(event) => void save(event)} className="grid gap-4 rounded-xl border border-slate-700 p-4">
          <h3 className="font-bold">{selected ? 'ویرایش پروژه' : 'پروژهٔ جدید'}</h3>
          <fieldset disabled={busy} className="grid gap-3">
            <label>نام پروژه<input className={inputClass} name="name" required maxLength={200} defaultValue={selected?.name} /></label>
            <label>توضیح<textarea className={inputClass} name="description" maxLength={4000} defaultValue={selected?.description ?? ''} /></label>
            <label>وضعیت
              <select className={inputClass} name="status" defaultValue={selected?.status ?? 'active'}>
                {Object.entries(statuses).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
              </select>
            </label>
            <label>لینک ریپو<input className={inputClass} name="repositoryUrl" type="url" dir="ltr" maxLength={2048} defaultValue={selected?.repositoryUrl ?? ''} /></label>
            <label>لینک محیط<input className={inputClass} name="environmentUrl" type="url" dir="ltr" maxLength={2048} defaultValue={selected?.environmentUrl ?? ''} /></label>
            <label>قدم بعدی<textarea className={inputClass} name="nextStep" maxLength={1000} defaultValue={selected?.nextStep ?? ''} /></label>
            <div className="flex flex-wrap gap-2">
              <button className="rounded-lg bg-teal-300 px-4 py-2 text-slate-950">ذخیره</button>
              <button className={buttonClass} type="button" onClick={() => setEditor(null)}>انصراف</button>
            </div>
          </fieldset>
        </form>
      )}
      {sorted.map((project) => (
        <article id={`project-${project.id}`} key={project.id} className="space-y-3 rounded-xl border border-slate-700 p-4 [overflow-wrap:anywhere]">
          <h3 className="text-lg font-bold">{project.name}</h3>
          <p className="text-sm text-teal-300">{project.archivedAt ? `آرشیوشده · ${statuses[project.status]}` : statuses[project.status]}</p>
          {project.description && <p className="whitespace-pre-wrap">{project.description}</p>}
          {project.nextStep && <p className="whitespace-pre-wrap"><strong>قدم بعدی</strong> {project.nextStep}</p>}
          <div className="flex flex-wrap gap-3">
            {project.repositoryUrl && <a className="underline" href={project.repositoryUrl} target="_blank" rel="noreferrer">ریپو</a>}
            {project.environmentUrl && <a className="underline" href={project.environmentUrl} target="_blank" rel="noreferrer">محیط</a>}
          </div>
          {!project.archivedAt && (
            <div className="flex flex-wrap gap-2">
              <button className={buttonClass} disabled={busy} onClick={() => { setError(''); setEditor(project) }}>ویرایش</button>
              <button className={buttonClass} disabled={busy} onClick={() => void archive(project)}>آرشیو</button>
            </div>
          )}
          <button className={buttonClass} aria-expanded={taskProjectId === project.id} onClick={() => setTaskProjectId(taskProjectId === project.id ? null : project.id)}>
            {taskProjectId === project.id ? "بستن جزئیات" : "تسک‌ها و تاریخچه"}
          </button>
          {taskProjectId === project.id && <ProjectTasks key={project.id} projectId={project.id} archived={Boolean(project.archivedAt)} onChanged={refreshDashboard} />}
        </article>
      ))}
    </section>
  )
}
