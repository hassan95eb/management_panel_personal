import { useEffect, useState } from 'react'

type Activity = {
  id: string
  report: string
  source: string
  taskId: string | null
  link: string | null
  createdAt: string
}

type ActivityPage = { items: Activity[]; hasMore: boolean }

const pageSize = 20
const formatter = new Intl.DateTimeFormat('fa-IR', { dateStyle: 'medium', timeStyle: 'short' })
const buttonClass = 'rounded-lg border border-slate-600 px-3 py-2 disabled:opacity-50'

export function ActivityHistory({ projectId, tasks }: {
  projectId: string
  tasks: { id: string; title: string }[]
}) {
  const [offset, setOffset] = useState(0)
  const [page, setPage] = useState<ActivityPage | null>(null)
  const [failed, setFailed] = useState(false)
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    fetch(`/api/projects/${projectId}/activities?offset=${offset}`, { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error()
        const result: ActivityPage = await response.json()
        if (!controller.signal.aborted) setPage(result)
      })
      .catch(() => {
        if (!controller.signal.aborted) setFailed(true)
      })
    return () => controller.abort()
  }, [projectId, offset, attempt])

  function changePage(next: number) {
    setPage(null)
    setFailed(false)
    setOffset(next)
  }

  return (
    <section className="min-w-0 space-y-3 border-t border-slate-700 pt-3">
      <h4 className="font-bold">تاریخچهٔ پروژه</h4>
      {!page && !failed && <p role="status">در حال دریافت تاریخچه…</p>}
      {failed && (
        <div className="flex flex-wrap items-center gap-2">
          <p role="alert" className="text-rose-300">دریافت تاریخچه ممکن نیست.</p>
          <button className={buttonClass} onClick={() => { setFailed(false); setAttempt(attempt + 1) }}>تلاش دوباره</button>
        </div>
      )}
      {page?.items.length === 0 && <p>{offset === 0 ? 'هنوز فعالیتی ثبت نشده است.' : 'در این صفحه فعالیتی وجود ندارد.'}</p>}
      {page && page.items.length > 0 && (
        <ul className="space-y-2">
          {page.items.map((activity) => (
            <li key={activity.id} className="space-y-2 rounded-lg border border-slate-700 p-3 [overflow-wrap:anywhere]">
              <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-slate-300">
                <time dateTime={activity.createdAt}>{formatter.format(new Date(activity.createdAt))}</time>
                <span>{activity.source === 'manual' ? 'دستی' : activity.source === 'automatic' ? 'خودکار' : activity.source}</span>
              </div>
              <p className="whitespace-pre-wrap">{activity.report}</p>
              {activity.taskId && <p className="text-sm text-slate-300">تسک مرتبط · {tasks.find((task) => task.id === activity.taskId)?.title ?? activity.taskId}</p>}
              {activity.link && /^https?:\/\//i.test(activity.link) && (
                <a className="inline-block text-teal-300 underline" href={activity.link} target="_blank" rel="noreferrer">باز کردن لینک گزارش</a>
              )}
            </li>
          ))}
        </ul>
      )}
      {(offset > 0 || page?.hasMore) && (
        <nav aria-label="صفحه‌بندی تاریخچه" className="flex flex-wrap gap-2">
          <button className={buttonClass} disabled={offset === 0 || (!page && !failed)} onClick={() => changePage(offset - pageSize)}>صفحهٔ قبل</button>
          <button className={buttonClass} disabled={!page?.hasMore} onClick={() => changePage(offset + pageSize)}>صفحهٔ بعد</button>
        </nav>
      )}
    </section>
  )
}
