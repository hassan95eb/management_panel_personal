import { useEffect, useState, type ReactNode } from 'react'

type DashboardTask = {
  id: string
  projectId: string
  projectName: string
  title: string
  dueDate: string | null
  blockedReason: string | null
}

type DashboardData = {
  dueToday: DashboardTask[]
  overdue: DashboardTask[]
  upcoming: DashboardTask[]
  blocked: DashboardTask[]
  activeProjects: { id: string; name: string; nextStep: string | null }[]
  recent: { id: string; projectId: string; projectName: string; report: string; createdAt: string }[]
}

const taskGroups = [
  ['dueToday', 'موعد امروز'],
  ['overdue', 'گذشته از موعد'],
  ['upcoming', 'سررسیدهای پیش رو'],
  ['blocked', 'تسک‌های مسدود'],
] as const

const dateTime = new Intl.DateTimeFormat('fa-IR', { dateStyle: 'medium', timeStyle: 'short' })

function localDate() {
  const now = new Date()
  return [now.getFullYear(), now.getMonth() + 1, now.getDate()]
    .map((part) => String(part).padStart(2, '0')).join('-')
}

function SummaryCard({ title, empty, children }: { title: string; empty: boolean; children: ReactNode }) {
  return (
    <section className="min-w-0 space-y-3 rounded-xl border border-slate-700 p-4 [overflow-wrap:anywhere]">
      <h3 className="font-bold">{title}</h3>
      {empty ? <p className="text-sm text-slate-300">موردی برای نمایش نیست.</p> : (
        <ul className="space-y-2 divide-y divide-slate-700 [&>li]:pb-2">{children}</ul>
      )}
    </section>
  )
}

export function Dashboard({ revision, onOpenProject }: {
  revision: number
  onOpenProject: (id: string) => void
}) {
  const [data, setData] = useState<DashboardData | null>(null)
  const [failed, setFailed] = useState(false)
  const [retry, setRetry] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    fetch(`/api/dashboard?today=${localDate()}`, { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error()
        const result: DashboardData = await response.json()
        if (!controller.signal.aborted) {
          setData(result)
          setFailed(false)
        }
      })
      .catch(() => {
        if (!controller.signal.aborted) setFailed(true)
      })
    return () => controller.abort()
  }, [revision, retry])

  function projectLink(id: string, label: string) {
    return <a className="text-teal-300 underline" href={`#project-${id}`} onClick={() => onOpenProject(id)}>{label}</a>
  }

  return (
    <section className="space-y-4 pb-6">
      <header>
        <h2 className="text-2xl font-bold">داشبورد امروز</h2>
        <p className="mt-1 text-sm text-slate-300">حداکثر ۱۰ مورد در هر بخش</p>
      </header>
      {!data && !failed && <p role="status">در حال دریافت داشبورد…</p>}
      {failed && (
        <div className="flex flex-wrap items-center gap-2">
          <p role="alert" className="text-rose-300">به‌روزرسانی داشبورد ممکن نیست.</p>
          <button className="rounded-lg border border-slate-600 px-3 py-2" onClick={() => { setFailed(false); setRetry(retry + 1) }}>
            تلاش دوباره
          </button>
        </div>
      )}
      {data && (
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          {taskGroups.map(([key, title]) => (
            <SummaryCard key={key} title={title} empty={data[key].length === 0}>
              {data[key].map((task) => (
                <li key={task.id}>
                  {projectLink(task.projectId, task.title)}
                  <p className="text-sm text-slate-300">
                    {task.projectName}
                    {task.dueDate && <> · <time dir="ltr" dateTime={task.dueDate}>{task.dueDate}</time></>}
                  </p>
                  {key === 'blocked' && task.blockedReason && <p className="whitespace-pre-wrap text-sm">{task.blockedReason}</p>}
                </li>
              ))}
            </SummaryCard>
          ))}
          <SummaryCard title="پروژه‌های فعال و قدم بعدی" empty={data.activeProjects.length === 0}>
            {data.activeProjects.map((project) => (
              <li key={project.id}>
                {projectLink(project.id, project.name)}
                <p className="whitespace-pre-wrap text-sm text-slate-300">{project.nextStep ?? 'قدم بعدی مشخص نشده است.'}</p>
              </li>
            ))}
          </SummaryCard>
          <SummaryCard title="فعالیت‌های اخیر" empty={data.recent.length === 0}>
            {data.recent.map((activity) => (
              <li key={activity.id}>
                {projectLink(activity.projectId, activity.projectName)}
                <p className="line-clamp-3 whitespace-pre-wrap text-sm">{activity.report}</p>
                <time className="text-sm text-slate-300" dateTime={activity.createdAt}>{dateTime.format(new Date(activity.createdAt))}</time>
              </li>
            ))}
          </SummaryCard>
        </div>
      )}
    </section>
  )
}
